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

namespace StreamInsight.Samples.Utility
{
    using System.IO;
    using Microsoft.ComplexEventProcessing;

    /// <summary>
    /// Interval version of the simple text file writer output.
    /// <para/>
    /// TextFileIntervalOutput generates interval events into a CSV file with the format:
    /// 
    ///   {EventKind}{delimiter}{Starttime}{delimiter}{EndTime}{delimiter}{field2}{delimiter}...{fieldN}
    /// 
    /// By generic events, we mean that, for a given output device (in this case a CSV file), you
    /// can specify an arbitrary payload class or struct at the time of query binding. This enables
    /// you to reuse this implementation for multiple instantiations of applications - where
    /// each instantiation corresponds to a particular payload type, and is associated/bound 1:1
    /// with a query.
    /// <para/>
    /// </summary>
    public class TextFileIntervalEventObserver<TPayload> : TextFileObserverBase<TPayload, IntervalEvent<TPayload>>
   {
       public TextFileIntervalEventObserver(string fileName, string delimiter) : base(fileName, delimiter) { 
       }

       protected override void OnNext(IntervalEvent<TPayload> currentEvent, StreamWriter writer)
       {
            if (EventKind.Cti == currentEvent.EventKind)
            {
                writer.Write("CTI");
                writer.Write(Delimiter);
                writer.Write(GetTimeByCulture(currentEvent.StartTime.ToString()));
                
            }
            else
            {
                writer.Write("INSERT");
                writer.Write(Delimiter);
                writer.Write(GetTimeByCulture(currentEvent.StartTime.ToString()));
                writer.Write(Delimiter);
                writer.Write(GetTimeByCulture(currentEvent.EndTime.ToString()));
                writer.Write(Delimiter);

                SerializePayload(writer, currentEvent);
            }
            writer.Flush();
       }
   }
}
