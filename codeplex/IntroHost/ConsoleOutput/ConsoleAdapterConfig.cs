using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntroHost.ConsoleOutput
{
    /// <summary>
    /// Possible trace types.
    /// </summary>
    public enum TraceTarget
    {
        /// <summary>
        /// Write messages to the debug output.
        /// </summary>
        Debug,

        /// <summary>
        /// Write messages to the console.
        /// </summary>
        Console,
    }

    /// <summary>
    /// Configuration structure for tracer output adapters.
    /// </summary>
    public class ConsoleAdapterConfig
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not to display CTI events in the output stream.
        /// </summary>
        public bool DisplayCtiEvents { get; set; }

        /// <summary>
        /// Gets or sets a value indicating which output stream to use.
        /// </summary>
        public TraceTarget Target { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to output each event in
        /// a single line or use a more verbose output format.
        /// </summary>
        public bool SingleLine { get; set; }
    }
}
