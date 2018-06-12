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

namespace HelloWorld
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using Microsoft.ComplexEventProcessing;
    using Microsoft.ComplexEventProcessing.Linq;

    // Input events to imitate sensor readings
    public class SensorReading
    {
        public int Time { get; set; }
        public int Value { get; set; }

        public override string ToString()
        {
            return new { Time, Value }.ToString();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // The StreamInsight engine is a server that can be embedded (in-memory) or remote (e.g. the Azure Service).
            // We first use Server.Create to create a server instance and return a handle to that instance.
            using (Server server = Server.Create("Default"))
            {
                Application application = server.CreateApplication("app");

                // We will be building a query that takes a stream of SensorReading events. 
                // It will work the same way on real-time data or past recorded events.
                IQStreamable<SensorReading> inputStream = null;

                Console.WriteLine("Press L for Live or H for Historic Data");
                ConsoleKeyInfo key = Console.ReadKey();
                Console.WriteLine();

                if (key.Key == ConsoleKey.L)
                {
                    inputStream = CreateStream(application, true);
                }
                else if (key.Key == ConsoleKey.H)
                {
                    inputStream = CreateStream(application, false);
                }
                else
                {
                    Console.WriteLine("invalid key");
                    return;
                }

                // The query is detecting when a threshold is crossed upwards.
                // See the Visio drawing TresholdCrossedUpward.vsd for the intuition.
                int threshold = 42;

                // Alter all events 1 sec in the future.
                var alteredForward = inputStream.AlterEventStartTime(s => s.StartTime.AddSeconds(1));

                // Compare each event that occurs at input with the previous event.
                // Note that, this one works for strictly ordered, strictly (e.g 1 sec) regular streams.
                var crossedThreshold = from evt in inputStream
                                       from prev in alteredForward
                                       where prev.Value < threshold && evt.Value > threshold
                                       select new
                                       {
                                           Time = evt.Time,
                                           Low = prev.Value,
                                           High = evt.Value
                                       };

                foreach (var outputSample in crossedThreshold.ToEnumerable())
                {
                    Console.WriteLine(outputSample);
                }

                Console.WriteLine("Done. Press ENTER to terminate");
                Console.ReadLine();
            }
        }

        private static readonly SensorReading[] HistoricData = new SensorReading[]
        {
            new SensorReading { Time = 1, Value = 0 },
            new SensorReading { Time = 2, Value = 20 },
            new SensorReading { Time = 3, Value = 15 },
            new SensorReading { Time = 4, Value = 30 },
            new SensorReading { Time = 5, Value = 45 }, // here we crossed the threshold upward
            new SensorReading { Time = 6, Value = 50 },
            new SensorReading { Time = 7, Value = 30 }, // here we crossed downward // **** note that the current query logic only detects upward swings. ****/
            new SensorReading { Time = 8, Value = 35 },
            new SensorReading { Time = 9, Value = 60 }, // here we crossed upward again
            new SensorReading { Time = 10, Value = 20 }
        };

        private static IObservable<SensorReading> SimulateLiveData()
        {
            return ToObservableInterval(HistoricData, TimeSpan.FromMilliseconds(1000), Scheduler.ThreadPool);
        }

        private static IObservable<T> ToObservableInterval<T>(IEnumerable<T> source, TimeSpan period, IScheduler scheduler)
        {
            return Observable.Using(
                () => source.GetEnumerator(),
                it => Observable.Generate(
                    default(object),
                    _ => it.MoveNext(),
                    _ => _,
                    _ =>
                    {
                        Console.WriteLine("Input {0}", it.Current);
                        return it.Current;
                    },
                    _ => period, scheduler));
        }

        static IQStreamable<SensorReading> CreateStream(Application application, bool isRealTime)
        {
            DateTime startTime = new DateTime(2011, 1, 1);

            if (isRealTime)
            {
                // Live data uses IQbservable<>
                return
                    application.DefineObservable(() => SimulateLiveData()).ToPointStreamable(
                        r => PointEvent<SensorReading>.CreateInsert(startTime.AddSeconds(r.Time), r),
                        AdvanceTimeSettings.StrictlyIncreasingStartTime);
            }
            // Historic data uses IQueryable<>
            return
                application.DefineEnumerable(() => HistoricData).ToPointStreamable(
                    r => PointEvent<SensorReading>.CreateInsert(startTime.AddSeconds(r.Time), r),
                    AdvanceTimeSettings.StrictlyIncreasingStartTime);
        }
    }
}
