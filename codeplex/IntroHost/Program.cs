using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ComplexEventProcessing.Linq;
using IntroHost.SimulatedInputAdapter;
using Microsoft.ComplexEventProcessing;
using IntroHost.ConsoleOutput;

namespace IntroHost
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize the StreamInsight log using the default settings from
            // the app.config file
            StreamInsightLog.Init();

            // Use the StreamInsight "Default" instance
            string instanceName = "Default";

            // Embed the StreamInsight engine
            using (Server cepServer = Server.Create(instanceName))
            {
                // Create an application in which to host our queries and adapters
                Application cepApplication = cepServer.CreateApplication("simple");

                // Create an input data stream using the SimulatedInput adapter,
                // raising events of the SimpleEventType type, and using the 
                // SimpleEventTypeFiller class to fill in the payload fields.  We
                // will raise a Point event every 100 ms (10 every second).
                var input = CepStream<SimpleEventType>.Create("inputStream",
                    typeof(SimulatedInputFactory), new SimulatedInputAdapterConfig()
                    {
                         CtiFrequency = 1,
                         EventPeriod = 100,
                         EventPeriodRandomOffset = 0,
                         TypeInitializer = typeof(SimpleEventTypeFiller).AssemblyQualifiedName
                    }, 
                    EventShape.Point);

                // Simple pass through query.  Grab the input values, pass to the 
                // output adapter
                //var query = from e in input select e;

                // Aggregating query.  Average the meter values by meter over a 
                // 3 second window, along with the min, max and number of events
                // for that meter
                var query = from e in input
                               group e by e.MeterId into meterGroups
                               from win in meterGroups.HoppingWindow(
                                   TimeSpan.FromSeconds(3),
                                   TimeSpan.FromSeconds(2),
                                   HoppingWindowOutputPolicy.ClipToWindowEnd)
                               select new
                               {
                                   meterId = meterGroups.Key,
                                   avg = win.Avg(e => e.Value),
                                   max = win.Max(e => e.Value),
                                   min = win.Min(e => e.Value),
                                   count = win.Count()
                               };
                               

                // Bind the query to an output adapter, writing events to the 
                // console
                var output = query.ToQuery(cepApplication, "simpleQuery", "A Simple Query",
                    typeof(ConsoleAdapterFactory), new ConsoleAdapterConfig()
                    {
                        DisplayCtiEvents = true,
                        SingleLine = false,
                        Target = TraceTarget.Console
                    }, EventShape.Point, StreamEventOrder.FullyOrdered);

                output.Start();

                Console.WriteLine("Query active - press <enter> to shut down");
                Console.ReadLine();

                output.Stop();
            }
        }
    }
}
