using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ComplexEventProcessing;

namespace Utils
{
    public class PerformanceUtils
    {
        public void RetrieveDiagnostics(DiagnosticView diagview, string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName, true))
            {
               // sw.WriteLine("Diagnostic View for '" + diagview.ObjectName + "':");
                foreach (KeyValuePair<string, object> diagprop in diagview)
                {
                    sw.WriteLine(" " + diagprop.Key + " - " + diagprop.Value); 
                }
            }

        }

        public void MonitorPerformance(Server server, string fileName)
        {

            RetrieveDiagnostics(server.GetDiagnosticView(new Uri("cep:/Server/Application/serverApp/Entity/process/Query/StreamableBinding_1")), "notImp.txt");

            DiagnosticSettings settings = new DiagnosticSettings(DiagnosticAspect.DiagnosticViews, DiagnosticLevel.Always);
            
            server.SetDiagnosticSettings(new Uri("cep:/Server"), settings);
            //RetrieveDiagnostics(server.GetDiagnosticView(new Uri("cep:/Server/Query")), "notImp.txt");

            RetrieveDiagnostics(server.GetDiagnosticView(new Uri("cep:/Server/Application/serverApp/Entity/process/Query/StreamableBinding_1")), fileName);

//            DiagnosticSettings set = server.GetDiagnosticSettings(new Uri("cep:/Server/Application/serverApp/Entity/process/Query/StreamableBinding_1"));
//            set.Aspects |= DiagnosticAspect.PerformanceCounters;
//            server.SetDiagnosticSettings(new Uri("cep:/Server/Application/serverApp/Query/StreamableBinding_1"), settings);
//
//
//
//
//            RetrieveDiagnostics(server.GetDiagnosticView(new Uri("cep:/Server/Application/serverApp/Entity/process/Query/StreamableBinding_1")), "counters.txt");

        }

        public void GetCpuLatency(string filename)
        {
            StreamReader data1 = new StreamReader(filename);
            StreamWriter temp = new StreamWriter("temp.txt");

            for (int i = 0; i < 11; i++)
            {
                data1.ReadLine();

            }

            var cpu = data1.ReadLine();
            temp.WriteLine(cpu);

            while (!data1.EndOfStream)
            {
                var line = data1.ReadLine();
                if (line.Contains("Latency"))
                    temp.WriteLine(line);
            }
            data1.Close();
            temp.Close();

            File.Delete(filename);

            File.Move("temp.txt", filename);
        }
    }
}
