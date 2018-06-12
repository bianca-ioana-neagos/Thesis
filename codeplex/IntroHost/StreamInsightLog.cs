using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using log4net.Config;
using System.IO;

namespace IntroHost
{
    /// <summary>
    /// StreamInsight log implementation leveraging log4net.
    /// </summary>
    public class StreamInsightLog
    {
        /// <summary>
        /// Reference to the log4net logging object
        /// </summary>
        private readonly log4net.ILog log;

        /// <summary>
        /// The category under which this object will log messages
        /// </summary>
        private string category;

        /// <summary>
        /// Default initialization of the log4net library based on
        /// entries in the app.config file.
        /// </summary>
        public static void Init()
        {
            XmlConfigurator.Configure();
        }

        /// <summary>
        /// Initialization of the log4net library based on a separate
        /// configuration file.
        /// </summary>
        /// <param name="configFile"></param>
        public static void Init(string configFile)
        {
            XmlConfigurator.ConfigureAndWatch(
                new FileInfo(configFile));
        }

        /// <summary>
        /// Initialize a StreamInsightLog object with the stated category
        /// </summary>
        /// <param name="category"></param>
        public StreamInsightLog(string category)
        {
            this.log = log4net.LogManager.GetLogger(category);
            this.category = category;
        }

        /// <summary>
        /// Log an exception to the ERROR log with the specified message
        /// </summary>
        public void LogException(Exception ex0, string fmt, params object[] vars)
        {
            log.Error(string.Format(fmt, vars), ex0);
        }

        /// <summary>
        /// Log a message of the specific log level
        /// </summary>
        public void LogMsg(TraceEventType type, string fmt, params object[] vars)
        {
            string message;

            if (vars.Any())
                message = String.Format(fmt, vars);
            else
                message = fmt;

            switch (type)
            {
                case TraceEventType.Verbose:
                    log.Debug(message);
                    break;

                case TraceEventType.Information:
                    log.Info(message);
                    break;

                case TraceEventType.Warning:
                    log.Warn(message);
                    break;

                case TraceEventType.Error:
                    log.Error(message);
                    break;

                case TraceEventType.Critical:
                    log.Fatal(message);
                    break;
            }
        }

        public void LogInfo(string fmt, params object[] vars)
        {
            log.InfoFormat(fmt, vars);
        }

        public bool ShouldLog(TraceEventType type)
        {
            switch (type)
            {
                case TraceEventType.Verbose:
                    return log.IsDebugEnabled;

                case TraceEventType.Information:
                    return log.IsInfoEnabled;

                case TraceEventType.Warning:
                    return log.IsWarnEnabled;

                case TraceEventType.Error:
                    return log.IsErrorEnabled;

                case TraceEventType.Critical:
                    return log.IsFatalEnabled;

                default:
                    return false;
            }
        }
    }
}
