using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class DiffUtils
    {

        public Dictionary<string, double> ComputeDifference(string file1, string file2)
        {
            var diff = new Dictionary<string, double[]>();
            var difff = new Dictionary<string, double>();

            StreamReader data1 = new StreamReader(file1);
            StreamReader data2 = new StreamReader(file2);

            string line1, line2;
            while ((line1 = data1.ReadLine()) != null && (line2 = data2.ReadLine()) != null)
            {
                var values = new double[2];
                var date = line1.Split('-')[0];
                values[0] = double.Parse(line1.Split('-')[1], System.Globalization.CultureInfo.InvariantCulture);

                values[1] = double.Parse(line2.Split('-')[1], System.Globalization.CultureInfo.InvariantCulture);
                diff.Add(date, values);

            }

            foreach (var pair in diff)
            {
                var str = pair.Value;
                var dd = Math.Abs(str[0] - str[1]);
                difff.Add(pair.Key, dd);
            }
            data1.Close();
            data2.Close();
            return difff;
        }

        public double ComputeErrorAverage(List<Dictionary<string, double>> errors, string filename)
        {
            StreamWriter strm = File.CreateText(filename);
            strm.Flush();
         

            var avg = errors.SelectMany(d => d)
                .GroupBy(
                    kvp => kvp.Key,
                    (key, kvps) => new { Key = key, Value = kvps.Average(kvp => kvp.Value) }
                )
                .ToDictionary(x => x.Key, x => x.Value);

            foreach (var a in avg)
            {
                strm.WriteLine(a.Key + " - " + a.Value);
            }

            strm.Close();

            var average = avg.Values.Average();
            return average;
        }
    }
}
