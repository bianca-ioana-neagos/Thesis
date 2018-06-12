using System;
using System.ServiceModel;
using Microsoft.ComplexEventProcessing;
using Microsoft.ComplexEventProcessing.Linq;
using Microsoft.ComplexEventProcessing.ManagementService;
using Microsoft.ComplexEventProcessing.Diagnostics.Views.PerformanceCounters;
using System.Reactive;
using System.Runtime.InteropServices;
using OutputAdapter;
using Utils;
using Application = System.Windows.Forms.Application;

namespace Crimes
{
    static class Program
    {
       
        static void Main()
        {

           // Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CrimesForm());

        }
    }
}
