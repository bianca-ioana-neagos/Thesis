using System;
using Microsoft.ComplexEventProcessing;
using Microsoft.ComplexEventProcessing.Linq;
using Model;

namespace Utils
{
    public class QueryUtils
    {
        private FileUtils _util;
        private Application _app;
    
        public QueryUtils(FileUtils util, Application app)
        {
            this._util = util;
            this._app = app;
        }

        public void DataQuery(IQStreamable<Crime> source, out IQStreamable<double> queryResult)
        {
            var alignment = new DateTime(TimeSpan.FromHours(0).Ticks, DateTimeKind.Utc);
            queryResult = from e in source.TumblingWindow(TimeSpan.FromHours(24), alignment)
                          select e.Avg(w => w.Severity);

           
            
        }
    }
}
