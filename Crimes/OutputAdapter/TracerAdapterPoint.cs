using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.ComplexEventProcessing;
using Microsoft.ComplexEventProcessing.Adapters;

namespace OutputAdapter
{
    internal sealed class TracerPointOutputAdapter : PointOutputAdapter
    {

        private readonly Tracer tracer;

        public TracerPointOutputAdapter(TracerConfig config, CepEventType type)
        {
            this.tracer = new Tracer(config, type);
        }

        public override void Start()
        {
            new Thread(this.ConsumeEvents).Start();
        }

        public override void Resume()
        {
            new Thread(this.ConsumeEvents).Start();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Catching all exceptions to avoid stopping process")]
        private void ConsumeEvents()
        {
            PointEvent currentEvent = default(PointEvent);

            try
            {
                while (true)
                {
                    if (AdapterState.Stopping == AdapterState)
                    {
                        Stopped();
                        return;
                    }

                    if (DequeueOperationResult.Empty == Dequeue(out currentEvent))
                    {
                        Ready();
                        return;
                    }

                    if (currentEvent.EventKind == EventKind.Insert)
                    {
                        string prefix = String.Format(CultureInfo.InvariantCulture, "{0} -", currentEvent.StartTime.Date);
                        this.tracer.TraceInsert(currentEvent, prefix);
                    }
                    else if (currentEvent.EventKind == EventKind.Cti)
                    {
                        this.tracer.TraceCti(currentEvent.StartTime);
                    }

                    ReleaseEvent(ref currentEvent);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }
    }
}
