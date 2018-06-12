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
    using Microsoft.ComplexEventProcessing;

    public class XYCsvReader
    {
        // Read CSV
        public static IEnumerable<PointEvent<XYPayload>> Read(string path)
        {
            foreach (string line in File.ReadLines(path))
            {
                var parts = line.Split(new char[] { ',' });
                var rval = PointEvent.CreateInsert(
                    DateTimeOffset.Parse(parts[0], DateTimeFormatInfo.InvariantInfo),
                    new XYPayload
                    {
                        X = double.Parse(parts[1], CultureInfo.InvariantCulture ),
                        Y = double.Parse(parts[2], CultureInfo.InvariantCulture)
                    });
                yield return rval;
            }
        }

        // Read CSV starting after a given high water mark (HWM).
        public static IEnumerable<PointEvent<XYPayload>> Read(string path, DateTimeOffset? hwm)
        {
            if (hwm == null) return Read(path);
            else
            {
                return Read(path).SkipWhile(x => hwm.Value > x.StartTime);
            }
        }
    }
}
