using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ComplexEventProcessing;
using System.Reflection;

namespace IntroHost
{
    public static class StreamInsightUtils
    {
        /// <summary>
        /// Given an untyped PointEvent generate a textual representation suitable for console,
        /// trace, etc, output.
        /// </summary>
        /// <param name="currEvent">An untyped PointEvent</param>
        /// <returns>A formatted string of the contents of the PointEvent</returns>
        public static string FormatEventForDisplay<TPayload>(this TypedEvent<TPayload> evt,
            bool verbose)
        {
            StringBuilder sb = new StringBuilder();             
            DateTimeOffset startTime = DateTimeOffset.MinValue;
            DateTimeOffset endTime = DateTimeOffset.MinValue;

            if (evt is PointEvent<TPayload>)
            {                
                startTime = ((PointEvent<TPayload>)evt).StartTime;
                sb.AppendFormat("POINT({0}) ", startTime.ToString("hh:mm:ss.fff"));
            }

            if (evt is IntervalEvent<TPayload>)
            {
                startTime = ((IntervalEvent<TPayload>)evt).StartTime;
                endTime = ((IntervalEvent<TPayload>)evt).EndTime;
                sb.AppendFormat("INTERVAL({0} - {1}) ",
                    startTime.ToString("hh:mm:ss.fff"),
                    endTime.ToString("hh:mm:ss.fff"));
            }

            if (evt is EdgeEvent<TPayload>)
            {
                startTime = ((EdgeEvent<TPayload>)evt).StartTime;
                endTime = DateTimeOffset.MaxValue;
                sb.AppendFormat("EDGE({0} - ?) ",
                    startTime.ToString("hh:mm:ss.fff"));
            }

            if (EventKind.Cti == evt.EventKind)
            {
                sb.AppendFormat(" CTI - start time: {0}", startTime);
            }
            else if (verbose)
                AddPayloadDetailsList<TPayload>(sb, evt);
            else
                AddPayloadDetailsRow<TPayload>(sb, evt);

            return sb.ToString();
        }

        public static void AddPayloadDetailsList<TPayload>(StringBuilder sb,
            TypedEvent<TPayload> evt)
        {
            sb.AppendLine();

            foreach (PropertyInfo p in typeof(TPayload).GetProperties())
            {
                object value = p.GetValue(evt.Payload, null);
                sb.AppendFormat("\t{0}:\t\t{1}\r\n", p.Name, ((value != null) ? value.ToString() : "NULL"));
            }
        }

        public static void AddPayloadDetailsRow<TPayload>(StringBuilder sb,
            TypedEvent<TPayload> evt)
        {
            foreach (PropertyInfo p in typeof(TPayload).GetProperties())
            {
                object value = p.GetValue(evt.Payload, null);
                sb.AppendFormat("{0} = {1}, ", p.Name, ((value != null) ? value.ToString() : "NULL"));
            }
        }

        public static void AddHeaderRow<TPayload>(StringBuilder sb)
        {
            foreach (PropertyInfo p in typeof(TPayload).GetProperties())
            {
                sb.AppendFormat("{0}, ", p.Name);
            }
        }

        /// <summary>
        /// Given an untyped PointEvent generate a textual representation suitable for console,
        /// trace, etc, output.
        /// </summary>
        /// <param name="currEvent">An untyped PointEvent</param>
        /// <returns>A formatted string of the contents of the PointEvent</returns>
        public static string FormatEventForDisplay(this UntypedEvent evt,
            CepEventType eventType, bool verbose)
        {
            StringBuilder sb = new StringBuilder();
            DateTimeOffset startTime = DateTimeOffset.MinValue;
            DateTimeOffset endTime = DateTimeOffset.MinValue;

            if (evt is PointEvent)
            {
                startTime = ((PointEvent)evt).StartTime;
                sb.AppendFormat("POINT({0}) ", startTime.ToString("hh:mm:ss.fff"));
            }

            if (evt is IntervalEvent)
            {
                startTime = ((IntervalEvent)evt).StartTime;
                endTime = ((IntervalEvent)evt).EndTime;
                sb.AppendFormat("INTERVAL({0} - {1}) ",
                    startTime.ToString("hh:mm:ss.fff"),
                    endTime.ToString("hh:mm:ss.fff"));
            }

            if (evt is EdgeEvent)
            {
                startTime = ((EdgeEvent)evt).StartTime;
                endTime = DateTimeOffset.MaxValue;
                sb.AppendFormat("EDGE({0} - ?) ",
                    startTime.ToString("hh:mm:ss.fff"));
            }

            if (EventKind.Cti == evt.EventKind)
            {
                sb.AppendFormat(" CTI - start time: {0}", startTime);
            }
            else if (verbose)
                AddPayloadDetailsList(sb, evt, eventType);
            else
                AddPayloadDetailsRow(sb, evt, eventType);

            return sb.ToString();
        }

        public static void AddPayloadDetailsList(StringBuilder sb,
            UntypedEvent evt, CepEventType eventType)
        {
            sb.AppendLine();
            foreach (var p in eventType.FieldsByOrdinal)
            {
                object value = evt.GetField(p.Key);
                sb.AppendFormat("\t{0}:\t\t{1}\r\n", p.Value.Name,
                    ((value != null) ? value.ToString() : "NULL"));
            }
        }

        public static void AddPayloadDetailsRow(StringBuilder sb,
            UntypedEvent evt, CepEventType eventType)
        {
            foreach (var p in eventType.FieldsByOrdinal)
            {
                object value = evt.GetField(p.Key);
                sb.AppendFormat("{0} = {1}, ", p.Value.Name,                     
                    ((value != null) ? value.ToString() : "NULL"));
            }
        }

        public static void AddHeaderRow(StringBuilder sb,
          CepEventType eventType)
        {
            foreach (var p in eventType.FieldsByOrdinal)
            {
                sb.AppendFormat("{0}, ", p.Value.Name);
            }
        }
    }
}
