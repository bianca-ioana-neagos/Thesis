using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntroHost.SimulatedInputAdapter
{
    /// <summary>
    /// This is the configuration type for the SimulatedDataInputFactory.  Use instances
    /// of this class to configure how often the simulated data input adapter raises events.
    /// </summary>
    public struct SimulatedInputAdapterConfig
    {
        /// <summary>
        /// How often to send a CTI event (1 = send a CTI event with every data event)
        /// </summary>
        public uint CtiFrequency { get; set; }

        /// <summary>
        /// The baseline period at which the adapter will generate simulated 
        /// events in milliseconds (i.e. a value of 500 means that the adapter 
        /// will raise an event twice a second, every 500 milliseconds).
        /// </summary>
        public int EventPeriod { get; set; }

        /// <summary>
        /// The maximum random offset on top of the event period.
        /// </summary>
        public int EventPeriodRandomOffset { get; set; }

        /// <summary>
        /// TODO
        /// </summary>
        public string TypeInitializer { get; set; } 
    }
}
