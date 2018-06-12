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
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using Microsoft.ComplexEventProcessing;

    public class XYCsvWriter
    {
        public static IObserver<PointEvent<XYPayload>> Write(string path, bool echoToConsole, DateTimeOffset? hwm, int offset)
        {
            var duplicates = GetDuplicates(path, hwm, offset);

            // return an observer that uses the dictionary to filter out duplicates
            var writer = GetStringLogObserver(path, false);
            return Observer.Create<PointEvent<XYPayload>>(
                x =>
                {
                    var str = FormatOutputLine(x);
                    if (x.EventKind == EventKind.Insert && duplicates.Contains(str))
                    {
                        duplicates.Remove(str);
                        if (echoToConsole) Console.WriteLine("** Dropping: " + str);
                    }
                    else
                    {
                        if (echoToConsole && x.EventKind == EventKind.Insert) Console.WriteLine(str);
                        writer.OnNext(str);
                    }
                },
                err => writer.OnError(err),
                () => writer.OnCompleted());
        }

        // Write without de-duplication.
        public static IObserver<PointEvent<XYPayload>> Write(string path, bool echoToConsole)
        {
            return Write(path, echoToConsole, null, 0);
        }

        private static HashSet<string> GetDuplicates(string path, DateTimeOffset? hwm, int offset)
        {
            if (hwm == null)
            {
                return new HashSet<string>();
            }
            else
            {
                Console.WriteLine("** HWM = " + hwm.Value);
                Console.WriteLine("** OFF = " + offset);

                var input = File.ReadLines(path)
                    .Select(x => x.Split(new char[] { ',' }, 2))
                    .SkipWhile(x => DateTimeOffset.Parse(x[0], DateTimeFormatInfo.InvariantInfo) < hwm.Value)
                    .Skip(offset)
                    .Where(x => x.Length > 1)
                    .Select(x => string.Format("{0},{1}", x[0], x[1]));

                var duplicates = new HashSet<string>(input);

                foreach (var x in duplicates) Console.WriteLine("** ToDedup: " + x);

                return duplicates;
            }
        }

        // An observer that will write output to the given path.
        private static IObserver<string> GetStringLogObserver(string path, bool overwrite)
        {
            var writer = new StreamWriter(path, overwrite);
            return Observer.Create<string>(
                x =>
                {
                    writer.WriteLine(x);
                    writer.Flush();
                },
                err => writer.Dispose(),
                () => writer.Dispose());
        }

        // Format a line to write to the output: we'll write both CTIs and Inserts.
        private static string FormatOutputLine(PointEvent<XYPayload> evt)
        {
            if (evt.EventKind == EventKind.Insert)
            {
                return string.Format("{0},{1}", evt.StartTime, evt.Payload);
            }
            else
            {
                return evt.StartTime.ToString();
            }
        }
    }
}
