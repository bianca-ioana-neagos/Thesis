using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ComplexEventProcessing.Adapters;
using Microsoft.ComplexEventProcessing;
using System.Diagnostics;

namespace IntroHost.SimulatedInputAdapter
{
    public class SimulatedInputIntervalAdapter<TPayload> :
     TypedIntervalInputAdapter<TPayload>
    {
        /// <summary>
        /// Store the simulated input configuration
        /// </summary>
        private SimulatedInputAdapterConfig config;

        /// <summary>
        /// Create a log object using the category name from the factory
        /// </summary>
        private StreamInsightLog trace = new StreamInsightLog(
            SimulatedInputFactory.ADAPTER_NAME);

        /// <summary>
        /// The timer object responsible for periodically raising events
        /// </summary>
        private System.Threading.Timer myTimer;

        /// <summary>
        /// Reference to the type initializer if one is specified
        /// </summary>
        private ITypeInitializer<TPayload> init;

        /// <summary>
        /// Lock object used to synchronize access to raising events.  This is
        /// primarily for use during debugging when stepping through the 
        /// adapter code (as the timer will continue to fire, resulting in 
        /// multiple threads trying to raise an event concurrently).
        /// </summary>
        private object lockObj = new object();

        /// <summary>
        /// Random object used to create the offset to when the next event
        /// should be raised
        /// </summary>
        private Random rand;

        public SimulatedInputIntervalAdapter(SimulatedInputAdapterConfig config)
        {
            // Hold onto the configuration, and generate a type initializer if
            // one has been configured.
            this.config = config;
            if (this.config.TypeInitializer != null)
            {
                init = (ITypeInitializer<TPayload>)Activator.CreateInstance(
                    Type.GetType(config.TypeInitializer));
            }
        }

        /// <summary>
        /// All events are raised asynchronously.  If the timer fires while
        /// the adapter is paused the RaiseEvent function fires it will 
        /// immediately exit so we don't need to account for that here
        /// </summary>
        public override void Resume()
        {
        }

        /// <summary>
        /// Create the random time interval generator and the thread timer
        /// used to schedule events being raised.  Start it 500 ms to give
        /// everything time to settle out
        /// </summary>
        public override void Start()
        {
            rand = new Random();

            myTimer = new System.Threading.Timer(
                new System.Threading.TimerCallback(RaiseEvent),
                null, 500, config.EventPeriod);
        }

        /// <summary>
        /// When the timer fires, check for the state of the adapter; if running
        /// raise a new simulated event
        /// </summary>
        /// <param name="state"></param>
        private void RaiseEvent(object state)
        {
            // Ensure that the adapter is in the running state.  If we're 
            // shutting down, kill the timer and signal Stopped()
            if (AdapterState.Stopping == AdapterState)
            {
                myTimer.Dispose();
                Stopped();
            }
            if (AdapterState.Running != AdapterState)
                return;

            // Allocate a point event to hold the data for the incoming message.  
            // If the event could not be allocated, exit the function
            lock (lockObj)
            {
                // The next event will be raised at now + event period ms, plus a 
                // random offset
                int nextEventInterval = config.EventPeriod +
                    rand.Next(config.EventPeriodRandomOffset);
                myTimer.Change(nextEventInterval, nextEventInterval);

                IntervalEvent<TPayload> currEvent = CreateInsertEvent();
                if (currEvent == null)
                    return;
                currEvent.StartTime = DateTime.Now;
                currEvent.EndTime = currEvent.StartTime.AddMilliseconds(nextEventInterval);

                // Create a payload object, and fill with values if we have a
                // an initializer defined
                currEvent.Payload = (TPayload)Activator.CreateInstance(typeof(TPayload));
                if (init != null)
                    init.FillValues(currEvent.Payload);

                if (trace.ShouldLog(TraceEventType.Verbose))
                {
                    trace.LogMsg(TraceEventType.Verbose, "INSERT - {0}",
                        currEvent.FormatEventForDisplay(false));
                }

                // If the event cannot be enqueued, release the memory and signal that
                // the adapter is ready to process more events (via. Ready())
                if (EnqueueOperationResult.Full == Enqueue(ref currEvent))
                {
                    ReleaseEvent(ref currEvent);
                    Ready();
                }
            }

           
        }
    }
}
