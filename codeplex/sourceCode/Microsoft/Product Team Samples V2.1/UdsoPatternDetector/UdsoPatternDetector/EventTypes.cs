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
    using Utility;

    public class StartTimeInfo 
    {
        public const int BeginTimeIndex = 0;

        [ImportField(BeginTimeIndex)]
        public DateTime StartTime { get; set; }
    }

    /// <summary>
    /// Stock ticker information type.
    /// </summary>
    public class StockTick
    {
        public const int StockSymbolIndex = 0;
        public const int PriceChangeIndex = 1;

        /// <summary>
        /// Gets or sets the stock symbol
        /// </summary>
        [ImportField(StockSymbolIndex)]
        public string StockSymbol { get; set; }

        /// <summary>
        /// Gets or sets the change in stock price
        /// </summary>
        [ImportField(PriceChangeIndex)]
        public int PriceChange { get; set; }
    }

    /// <summary>
    /// Event type representing the register of the transition network. A register
    /// is used to maintain additional state (context) during pattern matching using
    /// an augmented finite automaton. For example, if we want to detect a sequence
    /// of downticks followed by a sequence of the same number of upticks, we can
    /// use the register as a counter to keep track of the number of downticks that
    /// are yet to be matched by an uptick.
    /// </summary>
    public class Register
    {
        public const int CounterIndex = 0;
        public const int StockSymbolIndex = 1;

        /// <summary>
        /// Gets or sets the register counter, which tracks #downticks - #upticks
        /// </summary>
        [ImportField(CounterIndex)]
        public int Counter { get; set; }

        [ImportField(StockSymbolIndex)]
        public string StockSymbol { get; set; }
    }
}
