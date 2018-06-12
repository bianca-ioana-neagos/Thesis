using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ComplexEventProcessing;
using Microsoft.ComplexEventProcessing.Linq;
using OutputAdapter;

namespace Utils
{
    public class SeverityUtils
    {

        public void FullDataRun(Application app, FileUtils util, PerformanceUtils utilPerf, QueryUtils utilQuery, Server server)
        {
            util.Cleanup("results");

            util.ReadCrimes(app);

            var crimeSourceFull = util.GetFullStream(app);

            utilQuery.DataQuery(crimeSourceFull, out var severityAverageFull);

            var tracerConfig = new TracerConfig()
            {
                DisplayCtiEvents = false,
                SingleLine = true,
                TraceName = "",
                TracerKind = TracerKind.FileFull
            };

            var outputStream = app.DefineStreamableSink<double>(typeof(TracerFactory),
                tracerConfig,
                EventShape.Point,
                StreamEventOrder.FullyOrdered);

            using (severityAverageFull.Bind(outputStream).Run("process"))
            {
                utilPerf.MonitorPerformance(server, "performance/performanceFull.txt");
                System.Threading.Thread.Sleep(1000);
            }
        }

        public void PartDataRun(Application app, FileUtils util, PerformanceUtils utilPerf, QueryUtils utilQuery,
            Server server, int noOfTries, int howMany, int seqLength)
        {

            while (noOfTries != 0)
            {
                util.TruncateCrimes(app, howMany, seqLength);

                var crimeSourcePart = util.GetPartStream(app);

                utilQuery.DataQuery(crimeSourcePart, out var severityAveragePart);

                var tracerConfig2 = new TracerConfig()
                {
                    DisplayCtiEvents = false,
                    SingleLine = true,
                    TraceName = "",
                    TracerKind = TracerKind.FilePartial
                };

                var outputStream2 = app.DefineStreamableSink<double>(typeof(TracerFactory),
                    tracerConfig2,
                    EventShape.Point,
                    StreamEventOrder.FullyOrdered);

                using (severityAveragePart.Bind(outputStream2).Run("process"))
                {
                    utilPerf.MonitorPerformance(server, "performance2/performancePart" + new Random().Next(100) + ".txt");
                    System.Threading.Thread.Sleep(1000);
                }
                noOfTries--;
            }
        }
    }
}
