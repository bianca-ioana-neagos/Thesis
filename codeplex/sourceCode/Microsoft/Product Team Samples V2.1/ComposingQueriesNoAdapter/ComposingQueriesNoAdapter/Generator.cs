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
    using System.Collections.Generic;
    using System.Timers;
    using Microsoft.ComplexEventProcessing;
    using Microsoft.ComplexEventProcessing.Linq;

    public abstract class GeneratorBase<T, TEvent> : IObservable<TEvent> where TEvent : TypedEvent<T>
    {
        private readonly List<IObserver<TEvent>> eventObservers;
        protected readonly int EventInterval;
        protected readonly int EventIntervalDiff;
        protected readonly Random Rnd;

        protected GeneratorBase(int eventInterval, int eventIntervalDiff)
        {
            this.EventInterval = eventInterval;
            this.EventIntervalDiff = eventIntervalDiff;

            this.eventObservers = new List<IObserver<TEvent>>();
            this.Rnd = new Random();

            OnTimedEvent(null, null);
        }

        private void OnTimedEvent(object source, ElapsedEventArgs args)
        {
            foreach (var eventObserver in eventObservers)
            {
                TEvent e;
                PopulateEvent(out e);
                if (e == null)
                {
                    eventObserver.OnError(new Exception("Event is null"));
                }
                else
                {
                    eventObserver.OnNext(e);
                }
            }
            var timerInterval = Math.Max(EventInterval - (this.Rnd.Next(EventIntervalDiff * 2) - EventIntervalDiff), 1);
            var timer = new Timer(timerInterval);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = false;
            timer.Enabled = true;
        }

        protected abstract void PopulateEvent(out TEvent typedEvent);

        public IDisposable Subscribe(IObserver<TEvent> eventObserver)
        {
            if (!eventObservers.Contains(eventObserver))
            {
                eventObservers.Add(eventObserver);
            }
            return new Unsubscriber(eventObservers, eventObserver);
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<TEvent>> observers;
            private IObserver<TEvent> observer;

            public Unsubscriber(List<IObserver<TEvent>> observers, IObserver<TEvent> observer)
            {
                this.observers = observers;
                this.observer = observer;
            }

            public void Dispose()
            {
                if (this.observer != null && observers.Contains(observer))
                {
                    observers.Remove(observer);
                }
            }
        }
    } 

    /// <summary>
    /// Point event generator
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    public class PointGenerator<T> : GeneratorBase<T, PointEvent<T>>
        where T : new()
    {
        public PointGenerator(int eventInterval, int eventIntervalVariance)
            : base(eventInterval, eventIntervalVariance)
        { }

        public static IQStreamable<T> GetStreamable(Application app, int cti, int interval, int variance)
        {
            var ats = new AdvanceTimeSettings(new AdvanceTimeGenerationSettings((uint)cti, TimeSpan.FromTicks(-1)), null, AdvanceTimePolicy.Drop);
            return app.DefineObservable(() => new PointGenerator<T>(interval, variance)).ToPointStreamable(e => e, ats);
        }

        protected override void PopulateEvent(out PointEvent<T> typedEvent)
        {
            typedEvent = PointEvent.CreateInsert(DateTime.Now, new T());
        }
    }


    /// <summary>
    /// Interval event generator
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    public class IntervalGenerator<T> : GeneratorBase<T, IntervalEvent<T>>
        where T : new()
    {
        public IntervalGenerator(int eventInterval, int eventIntervalVariance)
            : base(eventInterval, eventIntervalVariance)
        { }

        public static IQStreamable<T> GetStreamable(Application app, int cti, int interval, int variance)
        {
            var ats = new AdvanceTimeSettings(new AdvanceTimeGenerationSettings((uint)cti, TimeSpan.FromTicks(-1)), null, AdvanceTimePolicy.Drop);
            return app.DefineObservable(() => new IntervalGenerator<T>(interval, variance)).ToIntervalStreamable(e => e, ats);
        }

        protected override void PopulateEvent(out IntervalEvent<T> typedEvent)
        {
            typedEvent = IntervalEvent.CreateInsert(DateTime.Now,
                                                    DateTime.Now +
                                                    TimeSpan.FromMilliseconds(
                                                        Math.Max(
                                                            EventInterval -
                                                            (this.Rnd.Next(EventIntervalDiff*2) - EventIntervalDiff), 1)),
                                                    new T());
        }
    }

    /// <summary>
    /// Edge event generator
    /// </summary>
    /// <typeparam name="T">type</typeparam>
    public class EdgeGenerator<T> : GeneratorBase<T, EdgeEvent<T>>
        where T : new()
    {
        private Dictionary<string, DeviceState<T>> dataSourceState = new Dictionary<string, DeviceState<T>>();

        public EdgeGenerator(int eventInterval, int eventIntervalVariance)
            : base(eventInterval, eventIntervalVariance)
        { }

        public static IQStreamable<T> GetStreamable(Application app, int cti, int interval, int variance)
        {
            var ats = new AdvanceTimeSettings(new AdvanceTimeGenerationSettings((uint)cti, TimeSpan.FromTicks(-1)), null, AdvanceTimePolicy.Drop);
            return app.DefineObservable(() => new EdgeGenerator<T>(interval, variance)).ToEdgeStreamable(e => e, ats);
        }

        protected override void PopulateEvent(out EdgeEvent<T> typedEvent)
        {
            var t = new T();
            if (this.dataSourceState.ContainsKey(t.ToString()))
            {
                typedEvent = EdgeEvent<T>.CreateEnd(this.dataSourceState[t.ToString()].Timestamp, DateTime.Now, t);
                if (typedEvent == null)
                {
                    throw new Exception("Cannot create EdgeEvent End");
                }
                this.dataSourceState.Remove(t.ToString());
            }
            else
            {
                typedEvent = EdgeEvent<T>.CreateStart(DateTime.Now, t);
                if (typedEvent == null)
                {
                    throw new Exception("Cannot create EdgeEvent Start");
                }
                this.dataSourceState.Add(t.ToString(), new DeviceState<T>(t, DateTime.Now));
            }
        }
    }
}
