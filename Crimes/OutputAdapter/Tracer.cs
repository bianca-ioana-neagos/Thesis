using System;
using System.Globalization;
using System.Text;
using Microsoft.ComplexEventProcessing;

namespace OutputAdapter
{
    internal sealed class Tracer
    {
        private readonly Action<string> _trace;
        private readonly TracerConfig _config;
        private readonly CepEventType _type;

        public Tracer(TracerConfig config, CepEventType type)
        {
            this._trace = TracerRouter.GetHandler(config.TracerKind, config.TraceName);
            this._config = config;
            this._type = type;
        }

        public void TraceInsert(UntypedEvent evt, string prefix)
        {
            StringBuilder builder = new StringBuilder()
                .Append(this._config.TraceName)
                .Append(prefix);

            if (evt.EventKind != EventKind.Insert)
            {
                throw new InvalidOperationException();
            }

            for (int ordinal = 0; ordinal < this._type.Fields.Count; ordinal++)
            {
                if (this._config.SingleLine)
                {
                    builder.Append(" ");
                }
                else
                {
                    builder
                        .AppendLine()
                        .Append('\t')
                        .Append(this._type.FieldsByOrdinal[ordinal].Name)
                        .Append(" = ");
                }

                object value = evt.GetField(ordinal) ?? "NULL";
                builder.Append(String.Format(CultureInfo.InvariantCulture, "{0}", value));
            }

            this._trace(builder.ToString());
        }

        public void TraceCti(DateTimeOffset time)
        {
            if (this._config.DisplayCtiEvents)
            {
                this._trace(String.Format(CultureInfo.InvariantCulture, "{0}: CTI at {1}", this._config.TraceName, time));
            }
        }
    }
}
