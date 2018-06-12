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
    using System;
    using System.IO;
    using Microsoft.ComplexEventProcessing;

    /// <summary>
    /// Point version of the simple text file writer output.
    /// <para/>
    /// TextFilePointOutput generates point events into a CSV file with the format:
    /// 
    ///   {EventKind}{delimiter}{StartTime}{delimiter}{field1}{delimiter}...{fieldN}
    /// 
    /// By generic events, we mean that, for a given output device (in this case a CSV file), you
    /// can specify an arbitrary payload class or struct at the time of query binding. This enables
    /// you to reuse this implementation for multiple instantiations 
    /// - where each instantiation corresponds to a particular payload type, and is associated/bound 1:1  with a query.
    /// <para/>
    /// </summary>
   public class TextFilePointEventObserver<TPayload> : TextFileObserverBase<TPayload, PointEvent<TPayload>>
   {
       public TextFilePointEventObserver(string fileName, string delimiter) : base(fileName, delimiter) { 
       }

       protected override void OnNext(PointEvent<TPayload> currentEvent, StreamWriter writer)
       {
            if (EventKind.Cti == currentEvent.EventKind)
            {
                writer.Write("CTI");
                writer.Write(Delimiter);
                writer.Write(GetTimeByCulture(currentEvent.StartTime.ToString()));
            }
            else
            {
                writer.Write("StartTime = " + currentEvent.StartTime);
                writer.Write(Delimiter);
                SerializePayload(writer, currentEvent);
            }
            writer.Write(Environment.NewLine);
            writer.Flush();
       }
   }
}
