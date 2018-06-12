using Microsoft.ComplexEventProcessing;
using Microsoft.ComplexEventProcessing.Adapters;

namespace OutputAdapter
{
    public sealed class TracerFactory : IOutputAdapterFactory<TracerConfig>
    {

        public OutputAdapterBase Create(TracerConfig configInfo, EventShape eventShape, CepEventType cepEventType)
        {
            OutputAdapterBase adapter = default(OutputAdapterBase);
            switch (eventShape)
            {
                case EventShape.Point:
                    adapter = new TracerPointOutputAdapter(configInfo, cepEventType);
                    break;

            }

            return adapter;
        }

        public void Dispose()
        {
        }
    }
}
