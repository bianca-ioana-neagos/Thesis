using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IntroHost.SimulatedInputAdapter;

namespace IntroHost
{
    public class SimpleEventType
    {
        public string MeterId { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; } 
    }

    public class SimpleEventTypeFiller : ITypeInitializer<SimpleEventType>
    {
        private Random rand = new Random();
             
        private string[] Meters = new string[]
        {
            "ValveOne",
            "ValveTwo",
            "ValveThree"
        };

        public void FillValues(SimpleEventType obj)
        {
            obj.MeterId = Meters[rand.Next(Meters.Length)];
            obj.Value = rand.NextDouble() * 100;
            obj.Timestamp = DateTime.Now;
        }
    }
}
