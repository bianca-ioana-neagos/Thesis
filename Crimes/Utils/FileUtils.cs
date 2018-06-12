using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.ComplexEventProcessing;
using Microsoft.ComplexEventProcessing.Linq;
using Model;

namespace Utils
{
    public class FileUtils
    {
        public static List<PointEvent<Crime>> _fullData = new List<PointEvent<Crime>>();
        private static List<PointEvent<Crime>> _partialData = new List<PointEvent<Crime>>();
        private static Random _random = new Random();


        public void Cleanup(string fileName)
        {
            var cleanUpCmd = "/C pushd D:\\workspace\\licenta\\Crimes\\Crimes\\bin\\Debug\\" + fileName + " && rm *";
            System.Diagnostics.Process.Start("CMD.exe", cleanUpCmd);
        }

        public IQStreamable<Crime> GetFullStream(Application app)
        {
            return app.DefineEnumerable(() => _fullData).ToStreamable(AdvanceTimeSettings.IncreasingStartTime);
        }

        public IQStreamable<Crime> GetPartStream(Application app)
        {
            return app.DefineEnumerable(() => _partialData).ToStreamable(AdvanceTimeSettings.IncreasingStartTime);
        }

        public void ReadCrimes(Application app)
        {

            StreamReader r = new StreamReader("crimes.csv");
            r.ReadLine();

            string line;
            while ((line = r.ReadLine()) != null)
            {
                var date = line.Split(new char[] { ';' }, 2)[0];
                var address = line.Split(';')[1];
                var district = line.Split(';')[2];
                var descr = line.Split(';')[3];
                var code = line.Split(';')[4];
                var severity = int.Parse(line.Split(';')[5]);
                var crime = new Crime() { Address = address, District = district, Descr = descr, Code = code, Severity = severity };
                PointEvent<Crime> pe = PointEvent.CreateInsert(DateTime.ParseExact(date, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture), crime);
                _fullData.Add(pe);
            }
        }

        public void TruncateCrimes(Application app, int howMany, int seqLength)
        {
            _partialData = new List<PointEvent<Crime>>(_fullData);
        
            var count = _partialData.Count;
            var istart = 0;
            var iend = istart + seqLength - 1;

            while (count - howMany > 0 && count > istart + seqLength)
            {
                var howManyToDelete = howMany;
                var toDelete = iend;
                while (howManyToDelete > 0)
                {
                    int pos = _random.Next(istart, toDelete);
                    _partialData.RemoveAt(pos);
                    count -= 1;
                    howManyToDelete -= 1;
                    toDelete -= 1;
                }

                istart = iend - howMany + 1;
                iend = istart + seqLength - 1;
            }

        }
    }
}
