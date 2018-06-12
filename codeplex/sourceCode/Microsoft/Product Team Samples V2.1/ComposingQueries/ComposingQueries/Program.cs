//*********************************************************
//
//  Copyright (c) Microsoft. All rights reserved.
//  This code is licensed under the Apache 2.0 License.
//  THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OR
//  CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
//  INCLUDING, WITHOUT LIMITATION, ANY IMPLIED WARRANTIES
//  OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
//  PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

namespace StreamInsight.Samples.ComposingQueries
{
    using System;
    using Microsoft.ComplexEventProcessing;
    using Microsoft.ComplexEventProcessing.Linq;
    using StreamInsight.Samples.Adapters.DataGenerator;
    using StreamInsight.Samples.Adapters.OutputTracer;

    /// <summary>
    /// Console Application.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main routine.
        /// </summary>
        internal static void Main()
        {
            // Creating an embedded server.
            //
            // NOTE: replace "Default" with the instance name you provided
            // during StreamInsight setup.
            using (Server server = Server.Create("Default"))
            {
                // Comment out if you want to create a service host and expose
                // the server's endpoint:

                //ServiceHost host = new ServiceHost(server.CreateManagementService());

                //host.AddServiceEndpoint(
                //    typeof(IManagementService),
                //    new WSHttpBinding(SecurityMode.Message),
                //    "http://localhost/MyStreamInsightServer");

                // To enable remote connection / debugging, you also need to uncomment the
                // lines "host.Open()" and "host.Close()".
                // In addition, the URI needs to be provisioned for the
                // account that runs this process (unless Administrator). To do this, open
                // an admin shell and execute the following command, using the respective
                // user credentials:
                // netsh http add urlacl url=http://+:80/MyStreamInsightServer user=<domain\username>

                //host.Open();

                try
                {
                    Application myApp = server.CreateApplication("DeviceReadings");

                    // Configuration of the data generator input adapter.
                    var generatorConfig = new GeneratorConfig()
                    {
                        CtiFrequency = 1,
                        EventInterval = 500,
                        EventIntervalVariance = 450,
                        DeviceCount = 5,
                        MaxValue = 100
                    };

                    // Define a source stream based on an adapter implementation, which requests point events from the simulator
                    var inputStream = myApp.DefineStreamable<GeneratedEvent>(typeof(GeneratorFactory), generatorConfig, EventShape.Point, null);

                    // Configuration of the tracer output adapter 1.
                    var tracerConfig1 = new TracerConfig()
                    {
                        DisplayCtiEvents = false,
                        SingleLine = true,
                        TraceName = "Deltas",
                        TracerKind = TracerKind.Console
                    };

                    var outputStream1 = myApp.DefineStreamableSink<AnnotatedValue>(typeof(TracerFactory),
                                      tracerConfig1,
                                      EventShape.Point,
                                      StreamEventOrder.FullyOrdered);

                    // Configuration of the tracer output adapter 2.
                    var tracerConfig2 = new TracerConfig()
                    {
                        DisplayCtiEvents = false,
                        SingleLine = true,
                        TraceName = "MaxDeltas",
                        TracerKind = TracerKind.Console
                    };

                    var outputStream2= myApp.DefineStreamableSink<float>(typeof(TracerFactory),
                                           tracerConfig2,
                                           EventShape.Interval,
                                           StreamEventOrder.FullyOrdered);

                    // Annotate the original values with the delta events by joining them.
                    // The aggregate over the count window produces a point event at
                    // the end of the window, which coincides with the second event in
                    // the window, so that they can be joined.
                    var annotatedValues = inputStream.Multicast(s =>
                        from left in s
                        join right in
                            (from e in s
                             group e by e.DeviceId into eachGroup
                             from win in eachGroup.CountWindow(2)
                             select new { ValueDelta = win.Delta(e => e.Value), SourceID = eachGroup.Key }
                             )
                        on left.DeviceId equals right.SourceID
                        select new AnnotatedValue() { DeviceID = left.DeviceId, Value = left.Value, ValueDelta = right.ValueDelta });

                    var filtered = from e in annotatedValues
                                   where e.DeviceID == "0"
                                   select e;

                    var maxDelta = from win in filtered.TumblingWindow(TimeSpan.FromSeconds(5))
                                         select win.Max(e => e.ValueDelta) ;

                    // Hydra is used to bind multiple queries
                    using (annotatedValues.Bind(outputStream1).With(maxDelta.Bind(outputStream2)).Run())
                    {
                        // Wait for keystroke to end.
                        Console.WriteLine("Press enter to stop.");
                        Console.ReadLine();
                    }                 

                    Console.WriteLine("Process stopped. Press enter to exit application.");
                    Console.ReadLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.ReadLine();
                }
            }
        }

        /// <summary>
        /// Output type of the primary query.
        /// Dynamic query composition requires explicit types at the query
        /// boundaries.
        /// </summary>
        public class AnnotatedValue
        {
            /// <summary>
            /// Gets or sets the device ID.
            /// </summary>
            public string DeviceID { get; set; }

            /// <summary>
            /// Gets or sets the current reading value of the device.
            /// </summary>
            public float Value { get; set; }

            /// <summary>
            /// Gets or sets the delta between the previous and the current value.
            /// </summary>
            public float ValueDelta { get; set; }
        }
    }
}
