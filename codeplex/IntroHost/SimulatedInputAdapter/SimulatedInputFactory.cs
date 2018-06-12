using System;
using System.Diagnostics;
using Microsoft.ComplexEventProcessing;
using Microsoft.ComplexEventProcessing.Adapters;

namespace IntroHost.SimulatedInputAdapter
{
    public class SimulatedInputFactory : ITypedInputAdapterFactory<SimulatedInputAdapterConfig>,
        ITypedDeclareAdvanceTimeProperties<SimulatedInputAdapterConfig>  
    {
        internal static readonly string ADAPTER_NAME = "SimulatedInput";
        private static readonly StreamInsightLog trace = new StreamInsightLog(ADAPTER_NAME);
            
        /// <summary>
        /// Based on a configuration and an event type generate an adapter reference
        /// </summary>
        public InputAdapterBase Create<TPayload>(SimulatedInputAdapterConfig configInfo, 
            EventShape eventShape) 
        {
            InputAdapterBase ret = default(InputAdapterBase);
            switch (eventShape)
            {
                case EventShape.Point:
                    ret = new SimulatedInputPointAdapter<TPayload>(configInfo);
                    break;
            
                case EventShape.Interval:
                    ret = new SimulatedInputIntervalAdapter<TPayload>(configInfo);
                    break;

                case EventShape.Edge:
                    ret = new SimulatedInputEdgeAdapter<TPayload>(configInfo);
                    break;
            }

            return ret;
        }

        /// <summary>
        /// No shared resources in the adapter - empty Dispose
        /// </summary>
        public void Dispose()
        { }

        /// <summary>
        /// Declaratively advance application time (i.e. inject CTI's)
        /// </summary>
        /// <returns></returns>
        public AdapterAdvanceTimeSettings DeclareAdvanceTimeProperties<TPayload>(
            SimulatedInputAdapterConfig configInfo, EventShape eventShape)
        {
            trace.LogMsg(TraceEventType.Information, 
                "Advance time policy: CTI f {0}, CTI offset {1}, time policy: {2}",
                configInfo.CtiFrequency, TimeSpan.FromTicks(-1), AdvanceTimePolicy.Adjust);

            var timeGenSettings = new AdvanceTimeGenerationSettings(configInfo.CtiFrequency,
                TimeSpan.FromTicks(-1), true);

            return new AdapterAdvanceTimeSettings(timeGenSettings, AdvanceTimePolicy.Adjust);
        }
    }
}
