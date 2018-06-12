using OutputAdapter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.ComplexEventProcessing;
using Microsoft.ComplexEventProcessing.Linq;
using Microsoft.ComplexEventProcessing.ManagementService;
using Utils;
using System.Windows.Forms.DataVisualization.Charting;

namespace Crimes
{
    public partial class CrimesForm : Form
    {
     
        public int SeqLengthVal;
        public int NoOfTriesVal=1;

        public CrimesForm()
        {
            InitializeComponent();

            linkLabel1.Text = "See the 'correct' results";
            linkLabel2.Text = "See the severity absolute error results";

            linkLabel1.Visible = false;
            linkLabel2.Visible = false;

            chart1.Visible = false;
            chart2.Visible = false;


        }

        private void seqLength_TextChanged(object sender, EventArgs e)
        {
            TextBox objTextBox = (TextBox)sender;
            string theText = objTextBox.Text;
            SeqLengthVal = int.Parse(theText);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown objTextBox = (NumericUpDown)sender;
            string theText = objTextBox.Value.ToString(CultureInfo.InvariantCulture);
            NoOfTriesVal = int.Parse(theText);
        }

        private void run_Click(object sender, EventArgs e)
        {
            RunApp();  
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"D:\workspace\licenta\Crimes\Crimes\bin\Debug\results\result.txt");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"D:\workspace\licenta\Crimes\Crimes\bin\Debug\results\severityErrors.txt");
        }


        private void RunApp()
        {
            using (var server = Server.Create("Crimes"))
            {
                var host = new ServiceHost(server.CreateManagementService());
                host.AddServiceEndpoint(typeof(IManagementService), new WSHttpBinding(SecurityMode.Message), "http://localhost/MyStreamInsightServer");
                host.Open();
             
                var crimeApp = server.CreateApplication("serverApp");
                progressBar1.Value += 10;
                foreach (var crimeAppQuery in crimeApp.Queries)
                {
                    var keyValuePair = crimeAppQuery;
                    Console.WriteLine(keyValuePair.Key + "  -  "+ keyValuePair.Value);
                }

                FileUtils util = new FileUtils();
                PerformanceUtils utilPerf = new PerformanceUtils();
                QueryUtils utilQuery = new QueryUtils(util, crimeApp);
                SeverityUtils utilApp = new SeverityUtils();
                DiffUtils utilDiff = new DiffUtils();
                var noOfTries = NoOfTriesVal;
            
                util.Cleanup("results2");
                util.Cleanup("performance");
                util.Cleanup("performance2");
                progressBar1.Value += 10;

                utilApp.FullDataRun(crimeApp,util,utilPerf, utilQuery, server);
                linkLabel1.Visible = true;
                utilPerf.GetCpuLatency("performance/performanceFull.txt");
                progressBar1.Value += 10;

                var howMany = 1;
                var avg = 0.0;
                
                Dictionary<double, double> avgErrors = new Dictionary<double, double>();
                Dictionary<double, double> limit = new Dictionary<double, double>();
                Dictionary<double, double> avgCpu = new Dictionary<double, double>();
                Dictionary<double, double> avgP = new Dictionary<double, double>();

                progressBar1.Value += 10;

                while (avg >= 0 && avg < 0.5)
                {
                    if (howMany >= SeqLengthVal)
                    {
                        Console.WriteLine("connot delete more tuples than there are in the sequence");
                        break;
                    }
                    
                    utilApp.PartDataRun(crimeApp, util, utilPerf, utilQuery, server, noOfTries, howMany, SeqLengthVal);
                    
                    DirectoryInfo d = new DirectoryInfo(@"D:\workspace\licenta\Crimes\Crimes\bin\Debug\results2");
                    FileInfo[] files = d.GetFiles("*.txt");

                    DirectoryInfo dP = new DirectoryInfo(@"D:\workspace\licenta\Crimes\Crimes\bin\Debug\performance2");
                    FileInfo[] filesP = dP.GetFiles("*.txt");

                    var errors = new List<Dictionary<string, double>>();
                    var errorsP = new List<Dictionary<string, double>>();
                    var errorsCpu = new List<Dictionary<string, double>>();

                    foreach (var file in files)
                    {
                        errors.Add(utilDiff.ComputeDifference("results/result.txt", file.FullName));
                    }

                    avg = utilDiff.ComputeErrorAverage(errors, "results/severityErrors.txt");
                    avgErrors.Add(howMany, avg);
                    Console.WriteLine(avg + "  " + howMany);
                    howMany += 1;
                    progressBar1.Maximum+=10;

                    foreach (var f in files)
                    {
                        f.Delete();
                    }

                   
                    foreach (var file in filesP)
                    {
                        utilPerf.GetCpuLatency(file.FullName);
                    }

                    foreach (var file in filesP)
                    {
                        errorsP.Add(utilDiff.ComputeDifference("performance/performanceFull.txt", file.FullName));
                    }

                    foreach (var err in errorsP)
                    {
                        var kvp = err.First();          
                        errorsCpu.Add(new Dictionary<string, double> { { kvp.Key, kvp.Value } });
                        err.Remove(kvp.Key);
                    }

                    var avgPerf = utilDiff.ComputeErrorAverage(errorsP, "latencyErrors.txt");
                    var ap = (avgPerf / 1000.0) % 60;
                    avgP.Add(howMany, ap);

                    var avgC = utilDiff.ComputeErrorAverage(errorsCpu, "cpuErrors.txt");
                    avgCpu.Add(howMany, avgC);

                    foreach (var f in filesP)
                    {
                        f.Delete();
                    }
                    progressBar1.Value += 10;
                }
               // progressBar1.Value += 20;

                foreach (var a in avgErrors)
                {
                    limit.Add(a.Key, 0.5);
                }

                CreateChart(chart1, avgErrors, limit, "Severity error", "Error limit");
                CreateChart(chart2,avgP, avgCpu, "Latency", "Cpu");

                linkLabel2.Visible = true;

                System.Threading.Thread.Sleep(1000);

                //progressBar1.Value += 20;

                host.Close();
            }
        }

        private void CreateChart(Chart chart, Dictionary<double, double> dict1, Dictionary<double, double> dict2, string name1, string name2)
        {
            var series = new Series(name1);
            var series2 = new Series(name2);
            series.ChartType = SeriesChartType.Line;
            series2.ChartType = SeriesChartType.Line;

            series.Points.DataBindXY(dict1.Keys, dict1.Values);
            series2.Points.DataBindXY(dict2.Keys, dict2.Values);

            chart.Series.Add(series);
            chart.Series.Add(series2);

            chart.ChartAreas[0].AxisX.Title = "How many tuples were removed";
            chart.ChartAreas[0].AxisY.Title = "The value of the error";

            chart.Series[1].Color = Color.Blue;
            chart.Series[1].BorderWidth = 2;
            chart.Series[2].Color = Color.Red;
            chart.Series[2].BorderWidth = 2;
            progressBar1.Value += 10;
            chart.Visible = true;
        }

      
    }
}
