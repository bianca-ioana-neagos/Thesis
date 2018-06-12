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

namespace Checkpointing
{
    using System;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading;
    using Microsoft.ComplexEventProcessing;
    using Microsoft.ComplexEventProcessing.Linq;

    public class Checkpointing
    {
        public static void Go()
        {
            var delay = TimeSpan.FromSeconds(.002);

            var metaConfig = new SqlCeMetadataProviderConfiguration
            {
                DataSource = "metadata.db",
                CreateDataSourceIfMissing = true
            };

            // Set up checkpointing. This needs a location to place the log files.
            var chkConfig = new CheckpointConfiguration
            {
                LogPath = "log",
                CreateLogPathIfMissing = true
            };

            using (var server = Server.Create("Default", metaConfig, chkConfig))
            {
                string appName = "CheckpointingDemo";
                string procName = "myProc";

                Application app;

                if (!server.Applications.TryGetValue(appName, out app))
                {
                    app = server.CreateApplication(appName);
                }

                CepProcess proc;

                if (app.Processes.TryGetValue(procName, out proc))
                {
                    Console.WriteLine("Resuming process...");
                    proc.Resume();
                }
                else
                {
                    Console.WriteLine("Creating process...");

                    // Without replay:
                    //var csvIn = app.DefineEnumerable(() => XYCsvReader.Read("sample.csv").RateLimit(delay));
                    // With replay:
                    var csvIn = app.DefineObservable((DateTimeOffset? hwm) => XYCsvReader.Read("sample.csv", hwm).RateLimit(delay, Scheduler.ThreadPool));

                    var csvStream = csvIn.ToPointStreamable(x => x, AdvanceTimeSettings.UnorderedStartTime(TimeSpan.FromSeconds(15)));

                    var q = from x in csvStream.TumblingWindow(TimeSpan.FromMinutes(1))
                            select new XYPayload { X = x.Avg(p => p.X), Y = x.Avg(p => p.Y) };

                    // Without de-duplication:
                    //var csvOut = app.DefineObserver(() => XYCsvWriter.Write("results.csv", true));
                    // With de-duplication:
                    var csvOut = app.DefineObserver((DateTimeOffset? hwm, int offset) => XYCsvWriter.Write("output.csv", true, hwm, offset));

                    proc = q.Bind(csvOut).RunCheckpointable(procName);
                }

                using (CheckpointLoop(server, app.CheckpointableProcesses[procName], TimeSpan.FromSeconds(1)))
                {
                    Console.WriteLine("Started checkpointing... Press enter to shut down normally...");
                    Console.ReadLine();
                }

                Console.WriteLine("Deleting process and exiting.");
                app.Processes[procName].Delete();
            }
        }

        public static IDisposable CheckpointLoop(Server server, CepCheckpointableProcess proc, TimeSpan delay)
        {
            Uri queryUri = server.Enumerate(new Uri(proc.Name.ToString() + "/Query")).First();
            
            // Don't start checkpointing until the query is actually running.
            string queryState;
            do
            {
                // sleep for a second
                Thread.Sleep(TimeSpan.FromSeconds(1));
                DiagnosticView dv = server.GetDiagnosticView(queryUri);
                queryState = (string)dv[DiagnosticViewProperty.QueryState];
            }
            while ("Running" != queryState);

            return Observable.Interval(delay, Scheduler.ThreadPool).Subscribe(
                i => {
                    var task = proc.CheckpointAsync();
                    task.Wait();
                    if (task.IsFaulted) return; 
                    Console.WriteLine("** Checkpointed! ({0})", i);
                }, (Exception e) => 
                    Console.WriteLine("Error encountered during checkpointing: {0}", e.Message));
        }
    }
}
