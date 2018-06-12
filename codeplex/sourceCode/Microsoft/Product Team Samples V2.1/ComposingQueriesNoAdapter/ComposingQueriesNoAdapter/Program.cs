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

                    // Generate input point event observable.
                    var inputPointObservable = PointGenerator<GeneratedEvent>.GetStreamable(myApp, 1, 500, 450);

                    // Annotate the original values with the delta events by joining them.
                    // The aggregate over the count window produces a point event at
                    // the end of the window, which coincides with the second event in
                    // the window, so that they can be joined.
                    var annotatedValues = inputPointObservable.Multicast(inp =>
                        from left in inp
                        join right in
                            (from e in inp
                             group e by e.DeviceId into eachGroup
                             from win in eachGroup.CountWindow(2)
                             select new { ValueDelta = win.Delta(e => e.Value), SourceID = eachGroup.Key }
                             )
                        on left.DeviceId equals right.SourceID
                        select new AnnotatedValue() { DeviceID = left.DeviceId, Value = left.Value, ValueDelta = right.ValueDelta });

                    // Specify the secondary query logic: select only a specific sensor.
                    var filtered = from e in annotatedValues
                                   where e.DeviceID == "0"
                                   select e;

                    // Find the maximum of all sensor values within 5 second windows -
                    // provided the window contains one or more events.
                    var maxDelta = from win in filtered.TumblingWindow(TimeSpan.FromSeconds(5))
                                   select new MaxValue() { Value = win.Max(e => e.ValueDelta) };

                    // Point Observable for annotatedValues.
                    var annotatedValuesObservable = annotatedValues.ToPointObservable();

                    // Interval Observable for maxDelta.
                    var maxDeltaObservable = maxDelta.ToIntervalObservable();

                    // Turn annotatedValues into an observable of point events.
                    var annotatedValuesObserver = myApp.DefineObserver(() => new TracerPoint<AnnotatedValue>("", false, false));

                    // Observer for maxDelta.
                    var maxDeltaObserver = myApp.DefineObserver(() => new TracerInterval<MaxValue>("\nMaxDeltas\n", false, false));
               
                    using (annotatedValuesObservable.Bind(annotatedValuesObserver).With(maxDeltaObservable.Bind(maxDeltaObserver)).Run())
                    {                      
                        // Wait for key stroke to end.
                        Console.WriteLine("Press enter to stop at any time.");
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
    }
}
