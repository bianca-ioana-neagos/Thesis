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

namespace StreamInsight.Samples.ComposingQueries
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using Microsoft.ComplexEventProcessing;

    /// <summary>
    /// Base class for Tracer, which is an IObserver of TypedEvent.
    /// Derived classes must implement function
    ///     protected abstract void OnNextEvent(TEvent currentEvent) 
    /// to specify how to handle different events.
    /// </summary>
    abstract class TracerBase<T, TEvent> : IObserver<TEvent> where TEvent : TypedEvent<T>
    {
        private PropertyInfo[] _properties;
        private readonly bool _displayCti;
        private readonly bool _singleLine;
        private readonly string _traceName;

        public bool DisplayCti { get { return _displayCti; } }

        protected TracerBase(string traceName, bool displayCti, bool singleLine)
        {
            _traceName = traceName;
            _displayCti = displayCti;
            _singleLine = singleLine;
        }

        protected void AppendPayload(ref StringBuilder builder, object payload)
        {
            if (_properties == null)
                _properties = payload.GetType().GetProperties();

            foreach (PropertyInfo p in _properties)
            {
                object value = p.GetValue(payload, null);

                if (_singleLine)
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, " {0}", value);
                }
                else
                {
                    builder.AppendFormat("\t{0} = {1}", p.Name, value);
                }
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(TEvent value)
        {
            OnNextEvent(value);
        }

        protected abstract void OnNextEvent(TEvent currentEvent);
    }

    /// <summary>
    /// Point Event Tracer
    /// </summary>
    class TracerPoint<T> : TracerBase<T, PointEvent<T>>
    {
        public TracerPoint(string traceName, bool displayCti, bool singleLine)
            : base(traceName, displayCti, singleLine)
        { }

        protected override void OnNextEvent(PointEvent<T> currentEvent)
        {
            if (currentEvent.EventKind == EventKind.Insert)
            {
                StringBuilder builder = new StringBuilder()
                .Append("Point at ")
                .Append(currentEvent.StartTime.DateTime.ToShortTimeString());
                AppendPayload(ref builder, currentEvent.Payload);

                Console.WriteLine(builder.ToString());
            }
            else if (currentEvent.EventKind == EventKind.Cti)
            {
                if (DisplayCti)
                {
                    Console.WriteLine(
                        String.Format(CultureInfo.InvariantCulture, "CTI at {1}", currentEvent.StartTime));
                }
            }
        }
    }

    /// <summary>
    /// Interval Event Tracer
    /// </summary>
    class TracerInterval<T> : TracerBase<T, IntervalEvent<T>>
    {
        public TracerInterval(string traceName, bool displayCti, bool singleLine)
            : base(traceName, displayCti, singleLine)
        { }

        protected override void OnNextEvent(IntervalEvent<T> currentEvent)
        {
            if (currentEvent.EventKind == EventKind.Insert)
            {
                StringBuilder builder = new StringBuilder()
                .Append("Interval from ")
                .Append(currentEvent.StartTime.DateTime.ToShortTimeString())
                .Append(" to ")
                .Append(currentEvent.EndTime);
                AppendPayload(ref builder, currentEvent.Payload);;

                Console.WriteLine(builder.ToString());
            }
            else if (currentEvent.EventKind == EventKind.Cti)
            {
                if (DisplayCti)
                {
                    Console.WriteLine(
                        String.Format(CultureInfo.InvariantCulture, "CTI at {1}", currentEvent.StartTime));
                }
            }
        }
    }

    /// <summary>
    /// Edge Event Tracer
    /// </summary>
    class TracerEdge<T> : TracerBase<T, EdgeEvent<T>>
    {
        public TracerEdge(string traceName, bool displayCti, bool singleLine)
            : base(traceName, displayCti, singleLine)
        { }

        protected override void OnNextEvent(EdgeEvent<T> currentEvent)
        {
            if (currentEvent.EventKind == EventKind.Insert)
            {
                StringBuilder builder = new StringBuilder();

                if (currentEvent.EdgeType == EdgeType.Start)
                {
                    builder
                        .Append("Edge start at :").Append(currentEvent.StartTime);
                }
                else
                {
                    builder
                        .Append(" Edge end at ")
                        .Append(currentEvent.EndTime)
                        .Append(" Edge start at :")
                        .Append(currentEvent.StartTime);
                }

                AppendPayload(ref builder, currentEvent.Payload); ;

                Console.WriteLine(builder.ToString());
            }
            else if (currentEvent.EventKind == EventKind.Cti)
            {
                if (DisplayCti)
                {
                    Console.WriteLine(
                        String.Format(CultureInfo.InvariantCulture, "CTI at {1}", currentEvent.StartTime));
                }
            }
        }
    }
}