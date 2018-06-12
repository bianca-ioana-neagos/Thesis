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

namespace StreamInsight.Samples.HelloToll
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reactive;
    using System.Reflection;
    using Microsoft.ComplexEventProcessing;
    using Microsoft.ComplexEventProcessing.Linq;

    static class HelloToll
    {
        [DisplayName("Pass-through")]
        [Description("Pass-through query to just show input stream in the same form as we show output.")]
        static void PassThrough(Application app)
        {
            var inputStream = app.GetTollReadings();
            app.DisplayIntervalResults(inputStream);
        }

        [DisplayName("Tumbling Count")]
        [Description("Every 3 minutes, report the number of vehicles processed " +
                     "that were being processed at some point during that period at " +
                     "the toll station since the last result. Report the result at a " +
                     "point in time, at the end of the 3 minute window.")]
        static void TumblingCount(Application app)
        {
            var inputStream = app.GetTollReadings();
            var query = from win in inputStream.TumblingWindow(TimeSpan.FromMinutes(3))
                        select win.Count();
            app.DisplayPointResults(query);
        }
        
        [DisplayName("Hopping Count")]
        [Description("Report the count of vehicles being processed at some time over " +
                     "a 3 minute window, with the window moving in one minute hops. " +
                     "Provide the counts as of the last reported result as of a point " +
                     "in time, reflecting the vehicles processed over the last 3 minutes.")]
        static void HoppingCount(Application app)
        {
            var inputStream = app.GetTollReadings();
            var countStream = from win in inputStream.HoppingWindow(TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(1))
                              select win.Count();
            var query = countStream
                .ToPointEventStream();
            app.DisplayPointResults(query);
        }

        [DisplayName("Partitioned Hopping window")]
        [Description("Find the toll generated from vehicles being processed at each " +
                     "toll station at some time over a 3 minute window, with the time advancing " +
                     "in one minute hops. Provide the value as of the last reported result.")]
        static void PartitionedHoppingWindow(Application app)
        {
            var inputStream = app.GetTollReadings();
            var query = from e in inputStream
                        group e by e.TollId into perTollBooth
                        from win in perTollBooth.HoppingWindow(
                                      TimeSpan.FromMinutes(3), // Window Size
                                      TimeSpan.FromMinutes(1)) // Hop Size
                        select new Toll
                        {
                            TollId = perTollBooth.Key,
                            TollAmount = win.Sum(e => e.Toll),
                            VehicleCount = win.Count() // computed as a bonus, not asked in the query
                        };
            app.DisplayPointResults(query);
        }

        [DisplayName("Partitioned Sliding window")]
        [Description("Find the most recent toll generated from vehicles being processed " +
                     "at each station over a 1 minute window reporting the result every time a " +
                     "change occurs in the input.")]
        static void PartitionedSlidingWindow(Application app)
        {
            var inputStream = app.GetTollReadings();
            var query = from e in inputStream.AlterEventDuration(e => e.EndTime - e.StartTime + TimeSpan.FromMinutes(1))
                        group e by e.TollId into perTollBooth
                        from win in perTollBooth.SnapshotWindow()
                        select new Toll
                        {
                            TollId = perTollBooth.Key,
                            TollAmount = win.Sum(e => e.Toll),
                            VehicleCount = win.Count() // computed as a bonus, not asked in the query
                        };
            app.DisplayIntervalResults(query);
        }

        [DisplayName("Partitioned Moving Average")]
        [Description("Moving average over the results of [Partitioned Sliding window].")]
        static void PartitionedMovingAverage(Application app)
        {
            var inputStream = app.GetTollReadings();
            var partitionedSlidingWindow = from e in inputStream.AlterEventDuration(e => e.EndTime - e.StartTime + TimeSpan.FromMinutes(1))
                                           group e by e.TollId into perTollBooth
                                           from win in perTollBooth.SnapshotWindow()
                                           select new Toll
                                           {
                                               TollId = perTollBooth.Key,
                                               TollAmount = win.Sum(e => e.Toll),
                                               VehicleCount = win.Count() // computed as a bonus, not asked in the query
                                           };
            var query = from e in partitionedSlidingWindow.ToPointEventStream()
                        select new TollAverage
                        {
                            TollId = e.TollId,
                            AverageToll = e.TollAmount / e.VehicleCount
                        };
           app.DisplayIntervalResults(query);
        }

        [DisplayName("Inner Join")]
        [Description("Report the output whenever Toll Booth 2 has processed the same number " +
                     "of vehicles as Toll Booth 1, computed over the last 1 minute, every time " +
                     "a change occurs in either stream.")]
        static void InnerJoin(Application app)
        {
            var inputStream = app.GetTollReadings();
            var partitionedSlidingWindow = from e in inputStream.AlterEventDuration(e => e.EndTime - e.StartTime + TimeSpan.FromMinutes(1))
                                           group e by e.TollId into perTollBooth
                                           from win in perTollBooth.SnapshotWindow()
                                           select new Toll
                                           {
                                               TollId = perTollBooth.Key,
                                               TollAmount = win.Sum(e => e.Toll),
                                               VehicleCount = win.Count() // computed as a bonus, not asked in the query
                                           };
            var stream1 = from e in partitionedSlidingWindow where e.TollId == "1" select e;
            var stream2 = from e in partitionedSlidingWindow where e.TollId == "2" select e;
            var query = from e1 in stream1
                        join e2 in stream2
                        on e1.VehicleCount equals e2.VehicleCount
                        select new TollCompare
                        {
                            TollId1 = e1.TollId,
                            TollId2 = e2.TollId,
                            VehicleCount = e1.VehicleCount
                        };
            app.DisplayIntervalResults(query);
        }

        [DisplayName("Cross Join")]
        [Description("Variation of [Inner Join] with cross join instead.")]
        static void CrossJoin(Application app)
        {
            var inputStream = app.GetTollReadings();
            var partitionedSlidingWindow = from e in inputStream.AlterEventDuration(e => e.EndTime - e.StartTime + TimeSpan.FromMinutes(1))
                                           group e by e.TollId into perTollBooth
                                           from win in perTollBooth.SnapshotWindow()
                                           select new Toll
                                           {
                                               TollId = perTollBooth.Key,
                                               TollAmount = win.Sum(e => e.Toll),
                                               VehicleCount = win.Count() // computed as a bonus, not asked in the query
                                           };
            var stream1 = from e in partitionedSlidingWindow where e.TollId == "1" select e;
            var stream2 = from e in partitionedSlidingWindow where e.TollId == "2" select e;
            var query = from e1 in stream1
                        from e2 in stream2
                        select new TollCompare
                        {
                            TollId1 = e1.TollId,
                            TollId2 = e2.TollId,
                            VehicleCount = e1.VehicleCount
                        };
            app.DisplayIntervalResults(query);
        }

        [DisplayName("Theta Join")]
        [Description("Variation of [Inner Join] with theta join instead. " +
                     "Report the output whenever Toll Booth 2 has processed lesser number " +
                     "of vehicles as Toll Booth 1, computed over the last 1 minute, every time " +
                     "a change occurs in either stream.")]
        static void ThetaJoin(Application app)
        {
            var inputStream = app.GetTollReadings();
            var partitionedSlidingWindow = from e in inputStream.AlterEventDuration(e => e.EndTime - e.StartTime + TimeSpan.FromMinutes(1))
                                           group e by e.TollId into perTollBooth
                                           from win in perTollBooth.SnapshotWindow()
                                           select new Toll
                                           {
                                               TollId = perTollBooth.Key,
                                               TollAmount = win.Sum(e => e.Toll),
                                               VehicleCount = win.Count() // computed as a bonus, not asked in the query
                                           };
            var stream1 = from e in partitionedSlidingWindow where e.TollId == "1" select e;
            var stream2 = from e in partitionedSlidingWindow where e.TollId == "2" select e;
            var query = from e1 in stream1
                        from e2 in stream2
                        where e1.VehicleCount > e2.VehicleCount
                        select new TollCompare
                        {
                            TollId1 = e1.TollId,
                            TollId2 = e2.TollId,
                            VehicleCount = e1.VehicleCount
                        };
            app.DisplayIntervalResults(query);
        }

        [DisplayName("Left Anti Join")]
        [Description("Report toll violators – owners of vehicles that pass through an automated " +
                     "toll booth without a valid EZ-Pass tag read.")]
        static void LeftAntiJoin(Application app)
        {
            var inputStream = app.GetTollReadings();

            // Simulate the reference stream from inputStream itself - convert it to a point event stream
            var referenceStream = from e in inputStream.ToPointEventStream() select e;

            // Simulate the tag violations in the observed stream by filtering out specific
            // vehicles. Let us filter out all the events in the dataset with a Tag length of 0.
            // In a real scenario, these events will not exist at all – this simulation is only
            // because we are reusing the same input stream for this example.
            // The events that were filtered out should be the ones that show up in the output of Q7.
            var observedStream = from e in inputStream.ToPointEventStream()
                                 where 0 != e.Tag.Length
                                 select e;

            // Report tag violations
            var query = referenceStream.LeftAntiJoin(observedStream, (left, right) => true);
            app.DisplayPointResults(query);
        }

        [DisplayName("TopK")]
        [Description("Report the top 2 toll amounts from the results of [Partitioned Sliding window] " +
                     "over a 3 minute tumbling window.")]
        static void TopK(Application app)
        {
            var inputStream = app.GetTollReadings();
            var partitionedSlidingWindow = from e in inputStream.AlterEventDuration(e => e.EndTime - e.StartTime + TimeSpan.FromMinutes(1))
                                           group e by e.TollId into perTollBooth
                                           from win in perTollBooth.SnapshotWindow()
                                           select new Toll
                                           {
                                               TollId = perTollBooth.Key,
                                               TollAmount = win.Sum(e => e.Toll),
                                               VehicleCount = win.Count() // computed as a bonus, not asked in the query
                                           };
            var query = from window in partitionedSlidingWindow.TumblingWindow(TimeSpan.FromMinutes(3))
                        from top in
                            (from e in window
                             orderby e.TollAmount descending
                             select e
                             ).Take(2,
                                e => new TopEvents
                                {
                                    TollRank = e.Rank,
                                    TollAmount = e.Payload.TollAmount,
                                    TollId = e.Payload.TollId,
                                    VehicleCount = e.Payload.VehicleCount
                                })
                        select top;
            app.DisplayPointResults(query);
        }

        [DisplayName("Filter and Project")]
        [Description("Report the top 2 toll amounts from the results of [Partitioned Sliding Window] " +
                     "over a 3 minute tumbling window, but only show results for TollStation ID 1.")]
        static void FilterProject(Application app)
        {
            var inputStream = app.GetTollReadings();
            var partitionedSlidingWindow = from e in inputStream.AlterEventDuration(e => e.EndTime - e.StartTime + TimeSpan.FromMinutes(1))
                                           group e by e.TollId into perTollBooth
                                           from win in perTollBooth.SnapshotWindow()
                                           select new Toll
                                           {
                                               TollId = perTollBooth.Key,
                                               TollAmount = win.Sum(e => e.Toll),
                                               VehicleCount = win.Count() // computed as a bonus, not asked in the query
                                           };
            var topK = from window in partitionedSlidingWindow.TumblingWindow(TimeSpan.FromMinutes(3))
                       from top in
                           (from e in window
                            orderby e.TollAmount descending
                            select e
                            ).Take(2,
                               e => new TopEvents
                               {
                                   TollRank = e.Rank,
                                   TollAmount = e.Payload.TollAmount,
                                   TollId = e.Payload.TollId,
                                   VehicleCount = e.Payload.VehicleCount
                               })
                       select top;
            var query = from e in topK
                   where e.TollId == "1"
                   select e;
            app.DisplayPointResults(query);
        }

        [DisplayName("Outer Join")]
        [Description("Outer Join using query primitives.")]
        static void OuterJoin(Application app)
        {
            var inputStream = app.GetTollReadings();

            // Simulate the left stream input from inputStream
            var outerJoin_L = from e in inputStream
                              select new
                              {
                                  LicensePlate = e.LicensePlate,
                                  Make = e.Make,
                                  Model = e.Model,
                              };

            // Simulate the right stream input from inputStream – eliminate all events with Toyota as the vehicle
            // These should be the rows in the outer joined result with NULL values for Toll and LicensePlate
            var outerJoin_R = from e in inputStream
                              where e.Make != "Toyota"
                              select new
                              {
                                  LicensePlate = e.LicensePlate,
                                  Toll = e.Toll,
                                  TollId = e.TollId
                              };

            // Inner join the two simulated input streams
            var innerJoin = from left in outerJoin_L
                            from right in outerJoin_R
                            where left.LicensePlate == right.LicensePlate
                            select new TollOuterJoin
                            {
                                LicensePlate = left.LicensePlate,
                                Make = left.Make,
                                Model = left.Model,
                                Toll = right.Toll,
                                TollId = right.TollId
                            };

            // Left anti join the two input simulated streams, and add the Project
            var leftAntiJoin = outerJoin_L
                .LeftAntiJoin(outerJoin_R, left => left.LicensePlate, right => right.LicensePlate)
                .Select(left => new TollOuterJoin
                    {
                        LicensePlate = left.LicensePlate,
                        Make = left.Make,
                        Model = left.Model,
                        Toll = null,
                        TollId = null
                    });

            // Union the two streams to complete a Left Outer Join operation
            var query = innerJoin.Union(leftAntiJoin);
            app.DisplayIntervalResults(query);
        }

        [DisplayName("UDF")]
        [Description("For each vehicle that is being processed at an EZ-Pass booth, report " +
                     "the TollReading if the tag does not exist, has expired, or is reported stolen.")]
        static void UDF(Application app)
        {
            var inputStream = app.GetTollReadings();
            var query = from e in inputStream
                        where 0 == e.Tag.Length || TagInfo.IsLostOrStolen(e.Tag) || TagInfo.IsExpired(e.Tag)
                        select new TollViolation
                        {
                            LicensePlate = e.LicensePlate,
                            Make = e.Make,
                            Model = e.Model,
                            State = e.State,
                            Tag = e.Tag,
                            TollId = e.TollId
                        };
            app.DisplayIntervalResults(query);
        }

        [DisplayName("UDA")]
        [Description("Over a 3 minute tumbling window, find the ratio of out-of-state " +
                     "vehicles to total vehicles being processed at a toll station.")]
        static void _11_UDA(Application app)
        {
            var inputStream = app.GetTollReadings();
            var query = from win in inputStream.TumblingWindow(TimeSpan.FromMinutes(3))
                        select win.UserDefinedAggregate<TollReading, OutOfStateVehicleRatio, float>(null);
            app.DisplayPointResults(query);
        }

        [DisplayName("UDA2")]
        [Description("Variation of [UDA] that uses OutOfStateVehicleRatio2 UDA.")]
        static void UDA2(Application app)
        {
            var inputStream = app.GetTollReadings();
            var query = from win in inputStream.TumblingWindow(TimeSpan.FromMinutes(3))
                        select win.UserDefinedAggregateWithMapping<TollReading, OutOfStateVehicleRatio2, string, float>(e => e.State, null);
            app.DisplayPointResults(query);
        }

        [DisplayName("UDO")]
        [Description("Over a one hour tumbling window, report all commercial vehicles " +
                     "with tonnage greater than one ton (2K lbs), along with their arrival " +
                     "times at the toll, and any charges due to weight violation. Overweight " +
                     "charges during the rush hour (7am to 7pm) are double that of non-rush hours.")]
        static void UDO(Application app)
        {
            var inputStream = app.GetTollReadings();
            var query = from win in inputStream.TumblingWindow(TimeSpan.FromHours(1))
                        from e in win.UserDefinedOperator(() => new VehicleWeights())
                        select e;
            app.DisplayPointResults(query);
        }

        #region Program plumbing
        /// <summary>
        /// Defines a stream of TollReadings used throughout examples. A simulated data array
        /// is wrapped into the Enumerable source which is then converted to a stream. 
        /// </summary>
        /// <param name="app">StreamInsight application object.</param>
        /// <returns>Returns stream of simulated TollReadings used in examples.</returns>
        static IQStreamable<TollReading> GetTollReadings(this Application app)
        {
            return app.DefineEnumerable(() =>
                // Simulated readings data defined as an array.
                // IntervalEvent objects are constructed directly to avoid copying.
                new[] {
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 01, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 03, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "1", LicensePlate = "JNB 7001", State = "NY", Make = "Honda",     Model = "CRV",     VehicleType = 1, VehicleWeight = 0,      Toll =  7.0f, Tag = ""         }),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 02, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 03, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "1", LicensePlate = "YXZ 1001", State = "NY", Make = "Toyota",    Model = "Camry",   VehicleType = 1, VehicleWeight = 0,      Toll =  4.0f, Tag = "123456789"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 02, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 04, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "3", LicensePlate = "ABC 1004", State = "CT", Make = "Ford",      Model = "Taurus",  VehicleType = 1, VehicleWeight = 0,      Toll =  5.0f, Tag = "456789123"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 03, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 07, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "2", LicensePlate = "XYZ 1003", State = "CT", Make = "Toyota",    Model = "Corolla", VehicleType = 1, VehicleWeight = 0,      Toll =  4.0f, Tag = ""         }),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 03, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 08, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "1", LicensePlate = "BNJ 1007", State = "NY", Make = "Honda",     Model = "CRV",     VehicleType = 1, VehicleWeight = 0,      Toll =  5.0f, Tag = "789123456"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 05, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 07, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "2", LicensePlate = "CDE 1007", State = "NJ", Make = "Toyota",    Model = "4x4",     VehicleType = 1, VehicleWeight = 0,      Toll =  6.0f, Tag = "321987654"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 06, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 09, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "2", LicensePlate = "BAC 1005", State = "NY", Make = "Toyota",    Model = "Camry",   VehicleType = 1, VehicleWeight = 0,      Toll =  5.5f, Tag = "567891234"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 07, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 10, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "1", LicensePlate = "ZYX 1002", State = "NY", Make = "Honda",     Model = "Accord",  VehicleType = 1, VehicleWeight = 0,      Toll =  6.0f, Tag = "234567891"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 07, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 10, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "2", LicensePlate = "ZXY 1001", State = "PA", Make = "Toyota",    Model = "Camry",   VehicleType = 1, VehicleWeight = 0,      Toll =  4.0f, Tag = "987654321"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 08, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 10, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "3", LicensePlate = "CBA 1008", State = "PA", Make = "Ford",      Model = "Mustang", VehicleType = 1, VehicleWeight = 0,      Toll =  4.5f, Tag = "891234567"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 09, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 11, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "2", LicensePlate = "DCB 1004", State = "NY", Make = "Volvo",     Model = "S80",     VehicleType = 1, VehicleWeight = 0,      Toll =  5.5f, Tag = "654321987"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 09, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 16, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "2", LicensePlate = "CDB 1003", State = "PA", Make = "Volvo",     Model = "C30",     VehicleType = 1, VehicleWeight = 0,      Toll =  5.0f, Tag = "765432198"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 09, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 10, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "3", LicensePlate = "YZX 1009", State = "NY", Make = "Volvo",     Model = "V70",     VehicleType = 1, VehicleWeight = 0,      Toll =  4.5f, Tag = "912345678"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 10, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 12, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "3", LicensePlate = "BCD 1002", State = "NY", Make = "Toyota",    Model = "Rav4",    VehicleType = 1, VehicleWeight = 0,      Toll =  5.5f, Tag = "876543219"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 10, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 14, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "1", LicensePlate = "CBD 1005", State = "NY", Make = "Toyota",    Model = "Camry",   VehicleType = 1, VehicleWeight = 0,      Toll =  4.0f, Tag = "543219876"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 11, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 13, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "1", LicensePlate = "NJB 1006", State = "CT", Make = "Ford",      Model = "Focus",   VehicleType = 1, VehicleWeight = 0,      Toll =  4.5f, Tag = "678912345"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 12, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 15, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "3", LicensePlate = "PAC 1209", State = "NJ", Make = "Chevy",     Model = "Malibu",  VehicleType = 1, VehicleWeight = 0,      Toll =  6.0f, Tag = "219876543"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 15, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 22, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "2", LicensePlate = "BAC 1005", State = "PA", Make = "Peterbilt", Model = "389",     VehicleType = 2, VehicleWeight = 2.675f, Toll = 15.5f, Tag = "567891234"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 15, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 18, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "3", LicensePlate = "EDC 3109", State = "NJ", Make = "Ford",      Model = "Focus",   VehicleType = 1, VehicleWeight = 0,      Toll =  4.0f, Tag = "198765432"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 18, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 20, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "2", LicensePlate = "DEC 1008", State = "NY", Make = "Toyota",    Model = "Corolla", VehicleType = 1, VehicleWeight = 0,      Toll =  4.0f, Tag = ""         }),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 20, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 22, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "1", LicensePlate = "DBC 1006", State = "NY", Make = "Honda",     Model = "Civic",   VehicleType = 1, VehicleWeight = 0,      Toll =  5.0f, Tag = "432198765"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 20, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 23, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "2", LicensePlate = "APC 2019", State = "NJ", Make = "Honda",     Model = "Civic",   VehicleType = 1, VehicleWeight = 0,      Toll =  4.0f, Tag = "345678912"}),
                    IntervalEvent.CreateInsert(new DateTime(2009, 06, 25, 12, 22, 0, DateTimeKind.Utc), new DateTime(2009, 06, 25, 12, 25, 0, DateTimeKind.Utc),
                        new TollReading { TollId = "1", LicensePlate = "EDC 1019", State = "NJ", Make = "Honda",     Model = "Accord",  VehicleType = 1, VehicleWeight = 0,      Toll =  4.0f, Tag = ""         }),
                })
                // Predefined AdvanceTimeSettings.IncreasingStartTime is used to insert CTIs after each event, which occurs later than the previous one.
                .ToIntervalStreamable(e => e, AdvanceTimeSettings.IncreasingStartTime);
        }

        static void DisplayPointResults<TPayload>(this Application app, IQStreamable<TPayload> resultStream)
        {
            // Define observer that formats arriving events as points to the console window.
            var consoleObserver = app.DefineObserver(() => Observer.Create<PointEvent<TPayload>>(ConsoleWritePoint));

            // Bind resultStream stream to consoleObserver.
            var binding = resultStream.Bind(consoleObserver);

            // Run example by creating a process from the binding we built above.
            using (binding.Run("ExampleProcess"))
            {
                Console.WriteLine("***Hit Return to exit after viewing query output***");
                Console.WriteLine();
                Console.ReadLine();
            }
        }

        static void ConsoleWritePoint<TPayload>(PointEvent<TPayload> e)
        {
            if (e.EventKind == EventKind.Insert)
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "INSERT <{0}> {1}", e.StartTime.DateTime, e.Payload.ToString()));
            else
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "CTI    <{0}>", e.StartTime.DateTime));
        }

        static void DisplayIntervalResults<TPayload>(this Application app, IQStreamable<TPayload> resultStream)
        {
            // Define observer that formats arriving events as intervals to the console window.
            var consoleObserver = app.DefineObserver(() => Observer.Create<IntervalEvent<TPayload>>(ConsoleWriteInterval));

            // Bind resultStream stream to consoleObserver.
            var binding = resultStream.Bind(consoleObserver);

            // Run example query by creating a process from the binding we've built above.
            using (binding.Run("ExampleProcess"))
            {
                Console.WriteLine("***Hit Return to exit after viewing query output***");
                Console.WriteLine();
                Console.ReadLine();
            }
        }

        static void ConsoleWriteInterval<TPayload>(IntervalEvent<TPayload> e)
        {
            if (e.EventKind == EventKind.Insert)
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "INSERT <{0} - {1}> {2}", e.StartTime.DateTime, e.EndTime.DateTime, e.Payload.ToString()));
            else
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "CTI    <{0}>", e.StartTime.DateTime));
        }

        static void Main()
        {
            using (var server = Server.Create("Default"))
            {
                Application application = server.CreateApplication("TollStationApp");

                var demos = (from mi in typeof(HelloToll).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                            let nameAttr = mi.GetCustomAttributes(typeof(DisplayNameAttribute), false)
                                .OfType<DisplayNameAttribute>()
                                .SingleOrDefault()
                            let descriptionAttr = mi.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                .OfType<DescriptionAttribute>()
                                .SingleOrDefault()
                            where null != nameAttr
                            select new { Action = mi, Name = nameAttr.DisplayName, Description = descriptionAttr.Description }).ToArray();

                while (true)
                {
                    Console.WriteLine();
                    Console.WriteLine("Pick an action:");
                    for (int demo = 0; demo < demos.Length; demo++)
                    {
                        Console.WriteLine("{0,4} - {1}", demo, demos[demo].Name);
                    }

                    Console.WriteLine("Exit - Exit from Demo.");
                    var response = Console.ReadLine().Trim();
                    if (string.Equals(response, "exit", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(response, "e", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    int demoToRun;
                    demoToRun = Int32.TryParse(response, NumberStyles.Integer, CultureInfo.InvariantCulture, out demoToRun)
                        ? demoToRun
                        : -1;

                    if (0 <= demoToRun && demoToRun < demos.Length)
                    {
                        Console.WriteLine();
                        Console.WriteLine(demos[demoToRun].Name);
                        Console.WriteLine(demos[demoToRun].Description);
                        demos[demoToRun].Action.Invoke(null, new[] { application });
                    }
                    else
                    {
                        Console.WriteLine("Unknown Query Demo");
                    }
                }
            }
        }
        #endregion
    }
}