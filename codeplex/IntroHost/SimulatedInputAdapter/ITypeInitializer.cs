using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntroHost.SimulatedInputAdapter
{
    public interface ITypeInitializer<TPayload>
    {
        void FillValues(TPayload obj);
    }
}
