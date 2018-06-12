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

namespace StreamInsight.Samples.PatternDetector
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Threading;
    using Microsoft.ComplexEventProcessing;
    using Microsoft.ComplexEventProcessing.Linq;
    using UserExtensions.Afa;
    using Utility;

    /// <summary>
    /// This sample uses the StreamInsight UDSO framework to implement pattern detection. See the
    /// file README.txt for more details.
    /// </summary>
    class PatternDetectorSample
    {
        /// <summary>
        /// Our main routine. Creates a random input stream of stock quotes, and performs
        /// pattern detection for every stock symbol in this stream.
        /// </summary>
        /// <param name="args">The command line arguments (unused).</param>
        static void Main(string[] args)
        {
            using (var server = Server.Create("Default"))
            {
                var outputFile = "SampleOutput.txt";

                Console.WriteLine("The program is running and writing results to {0}...", outputFile);

                var app = server.CreateApplication("PatternDetector");
                
                // Read original data from text file 
                var stocks = app.DefineEnumerable(() => ReadEvents<StockTick>("StockTicks.csv"));
                
                // Convert to IQStremable
                var stockStream = stocks.ToStreamable(AdvanceTimeSettings.StrictlyIncreasingStartTime);

                // Execute pattern detection over the stream
                var patternQuery = from s in stockStream
                            group s by s.StockSymbol into g
                            from r in g.Scan(() => new AfaOperator<StockTick, Register, AfaEqualDownticksUpticks>(TimeSpan.FromSeconds(10)))
                            select new { r.Counter, StockSymbol = g.Key };

                // Output the query results using Observable/Observer pattern
                var sinkObservable = from p in patternQuery.ToPointObservable()
                                     where p.EventKind == EventKind.Insert                                  
                                     select PointEvent.CreateInsert(p.StartTime,  new Register
                                     {
                                          Counter = p.Payload.Counter, 
                                          StockSymbol = p.Payload.StockSymbol
                                     });
                 
                var sinkObserver = app.DefineObserver(() => new TextFilePointEventObserver<Register>(outputFile, "\t"));

                using (sinkObservable.Subscribe(sinkObserver))
                {
                    Thread.Sleep(1000);               
                }
                
                Console.WriteLine("Done. Press enter to exit.");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Use LINQ Expression to create payload data
        /// </summary>
        /// <typeparam name="T">type</typeparam>
        /// <param name="offset">offset</param>
        /// <returns></returns>
        private static Func<string[], T> CreatePayloadFunction<T>(int offset)
              where T : new()
        {
            var prm = Expression.Parameter(typeof(string[]));

            var convertMethod = ((Func<object, Type, IFormatProvider, object>)Convert.ChangeType).Method;

            var bindings = from p in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                           let setMethod = p.GetSetMethod()
                           where setMethod != null // settable, public, instance properties
                           from ImportFieldAttribute a in p.GetCustomAttributes(typeof(ImportFieldAttribute), false)
                           let partExpression = Expression.ArrayIndex(prm, Expression.Constant(a.Position + offset))
                           let convertCall = Expression.Call(convertMethod, partExpression, Expression.Constant(p.PropertyType), Expression.Constant(CultureInfo.InvariantCulture, typeof(IFormatProvider)))
                           let cast = Expression.Convert(convertCall, p.PropertyType)
                           select Expression.Bind(setMethod, cast);

            var body = Expression.MemberInit(
                  Expression.New(typeof(T)),
                  bindings);

            return Expression.Lambda<Func<string[], T>>(body, prm).Compile();
        }

        /// <summary>
        /// Read events from the file, combine StartTime and Payload into events
        /// </summary>
        /// <typeparam name="T">type</typeparam>
        /// <param name="fileName">file name</param>
        /// <returns></returns>
        private static IEnumerable<PointEvent<T>> ReadEvents<T>(string fileName) where T : new()
        {
            var lines = File.ReadLines(fileName);

            var createStockTick = CreatePayloadFunction<T>(1);
            var conversionStockTick = from l in lines
                                      select createStockTick(l.Split(','));

            var createStartTime = CreatePayloadFunction<StartTimeInfo>(0);
            var conversionStartTime = from l in lines
                                      select createStartTime(l.Split(','));

            return conversionStartTime.Zip(conversionStockTick,(first, second) => PointEvent.CreateInsert(first.StartTime, second));
        }
    }
}
