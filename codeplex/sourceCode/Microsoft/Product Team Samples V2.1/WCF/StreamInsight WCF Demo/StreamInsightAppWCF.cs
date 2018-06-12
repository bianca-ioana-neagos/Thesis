// *********************************************************
//
//  Copyright (c) Microsoft. All rights reserved.
//  This code is licensed under the Apache 2.0 License.
//  THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OR
//  CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
//  INCLUDING, WITHOUT LIMITATION, ANY IMPLIED WARRANTIES
//  OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
//  PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
// *********************************************************


// NOTICE: Please refer to Readme file (README.txt) in this project for instructions about configuring the projects

namespace StreamInsightWCF
{
    using System;
    using EventTypes;
    using Microsoft.ComplexEventProcessing;
    using Microsoft.ComplexEventProcessing.Linq;
    using StreamInsightWCFArtifacts;
    
    class StreamInsightAppWCF
    {
        static void Main(string[] args)
        {
            string port = "8088";
            string wcfSourceURL = String.Format(@"http://localhost:{0}/StreamInsight/wcf/Source/", port);
            string wcfSinkURL = String.Format(@"http://localhost:{0}/StreamInsight/wcf/Sink/", port);

            using (var server = Server.Create("Default"))
            {
                string AppName = "TestApp";

                if (server.Applications.ContainsKey(AppName))
                {
                    server.Applications[AppName].Delete();
                }
                var app = server.CreateApplication(AppName);

                //WCF Artifacts
                var observableWcfSource = app.DefineObservable(() => new WcfObservable(wcfSourceURL));
                var observableWcfSink = app.DefineObserver(() => new WcfObserver(wcfSinkURL));
                var r = new Random(0);
                var query = from x in observableWcfSource.ToPointStreamable(i => PointEvent.CreateInsert<int>(i.T, i.I), AdvanceTimeSettings.IncreasingStartTime).TumblingWindow(TimeSpan.FromMilliseconds(1000))
                            let avgCpu = x.Avg(e => e)
                            select new OutputEvent { O = (byte)avgCpu, Color = (byte)(avgCpu > 70 ? 2 : avgCpu > 45 ? 1 : 0) };

                Console.WriteLine("StreamInsight application using wcf artifacts (WcfObservable)");
                using (query.Bind(observableWcfSink).Run())
                {
                    Console.WriteLine("Sending events...");
                    Console.WriteLine("Press <Enter> to exit.");
                    Console.ReadLine();
                }
            }
        }
    }
}
