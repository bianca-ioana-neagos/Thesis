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
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    public static class MyEnumerables
    {
        // Rate limit an enumerable
        public static IObservable<T> RateLimit<T>(this IEnumerable<T> source, TimeSpan period, IScheduler scheduler)
        {
            return Observable.Using(
              () => source.GetEnumerator(),
              it => Observable.Generate(
               false,
               _ => it.MoveNext(),
               _ => _,
               _ => it.Current,
               _ => period,
               scheduler));
        }
    }
}
