using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ComplexEventProcessing;
using Microsoft.ComplexEventProcessing.Adapters;
using System.Threading;
using System.Diagnostics;

namespace IntroHost.ConsoleOutput
{
    internal sealed class ConsoleEdgeOutputAdapter : EdgeOutputAdapter
    {
        private StreamInsightLog trace;
        private ConsoleAdapterConfig config;
        private CepEventType eventType;

        public ConsoleEdgeOutputAdapter(ConsoleAdapterConfig config,
            CepEventType type)
        {
            trace = new StreamInsightLog(ConsoleAdapterFactory.APP_NAME);
            this.config = config;
            this.eventType = type;
        }

        /// <summary>
        /// Start() is called when the engine wants to let the adapter start producing events.
        /// This method is called on a threadpool thread, which should be released as soon as possible.
        /// </summary>
        public override void Start()
        {
            new Thread(this.ConsumeEvents).Start();
        }

        /// <summary>
        /// Resume() is called when the engine is able to produce further events after having been emptied
        /// by Dequeue() calls. Resume() will only be called after the adapter called Ready().
        /// This method is called on a threadpool thread, which should be released as soon as possible.
        /// </summary>
        public override void Resume()
        {
            new Thread(this.ConsumeEvents).Start();
        }

        /// <summary>
        /// Main worker thread function responsible for dequeueing events and 
        /// posting them to the output stream.
        /// </summary>
        private void ConsumeEvents()
        {
            EdgeEvent currentEvent = default(EdgeEvent);

            try
            {
                while (true)
                {
                    if (AdapterState.Stopping == AdapterState)
                    {
                        Stopped();
                        return;
                    }

                    // Dequeue the event. If the dequeue fails, then the adapter state is suspended
                    // or stopping. Assume the former and call Ready() to indicate
                    // readiness to be resumed, and exit the thread.
                    if (DequeueOperationResult.Empty == Dequeue(out currentEvent))
                    {
                        Ready();
                        return;
                    }

                    string writeMsg = String.Empty;

                    if (currentEvent.EventKind == EventKind.Insert)
                    {
                        writeMsg = currentEvent.FormatEventForDisplay(eventType,
                            !config.SingleLine);
                    }
                    else if (currentEvent.EventKind == EventKind.Cti)
                    {
                        writeMsg = String.Format("CTI - {0}",
                            currentEvent.StartTime.ToString("hh:mm:ss.fff"));
                    }

                    if (config.Target == TraceTarget.Console)
                        Console.WriteLine(writeMsg);
                    else if (config.Target == TraceTarget.Debug)
                        Debug.Write(writeMsg);

                    // Every received event needs to be released.
                    ReleaseEvent(ref currentEvent);
                }
            }
            catch (Exception e)
            {
                trace.LogException(e, "Error in console adapter dequeue");
            }
        }
    }
}
