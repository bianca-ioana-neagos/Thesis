using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ComplexEventProcessing;
using Microsoft.ComplexEventProcessing.Adapters;

namespace IntroHost.ConsoleOutput
{
    public class ConsoleAdapterFactory : IOutputAdapterFactory<ConsoleAdapterConfig>
    {
        internal static readonly string APP_NAME = "ConsoleOutput";

        /// <summary>
        /// Create an instance of a console output adapter that dumps received
        /// events to the .NET Debug or Console window.
        /// </summary>
        public OutputAdapterBase Create(ConsoleAdapterConfig configInfo, 
            EventShape eventShape, CepEventType cepEventType)
        {
            OutputAdapterBase ret = default(OutputAdapterBase);
            switch(eventShape)
            {
                case EventShape.Point:
                    ret = new ConsolePointOutputAdapter(configInfo, cepEventType);
                    break;

                case EventShape.Interval:
                    ret = new ConsoleIntervalOutputAdapter(configInfo, cepEventType);
                    break;

                case EventShape.Edge:
                    ret = new ConsoleEdgeOutputAdapter(configInfo, cepEventType);
                    break;
            }
            return ret;
        }

        public void Dispose()
        {
            
        }
    }
}
