using System;
using NLog;
using Microsoft.ApplicationInsights;

namespace SampleAADv2Bot.Services
{
    /// <summary>
    /// Logging Service 
    /// </summary>
    [Serializable]
    public class LoggingService : ILoggingService
    {
        [NonSerialized]
        private readonly ILogger logger;

        /// <summary>
        /// Logging Service constructor
        /// </summary>
        public LoggingService()
        {
            logger =  LogManager.GetLogger("ScheduleBotForSfB");
        }
        /// <summary>
        /// Trace method
        /// </summary>
        /// <param name="message">Trace message</param>
        public void Trace(string message)
        {
            logger.Trace(message);
        }

        /// <summary>
        /// Trace method with additional parameters
        /// </summary>
        /// <param name="message">Trace message</param>
        /// <param name="args">Additional parameters</param>
        public void Trace(string message, params object[] args)
        {
            logger.Trace(message, args);
        }

        /// <summary>
        /// Debug method
        /// </summary>
        /// <param name="message">Debug message</param>
        public void Debug(string message)
        {
            logger.Debug(message);
        }

        /// <summary>
        /// Debug method with additional parameters
        /// </summary>
        /// <param name="message">Debug message</param>
        /// <param name="args">Additional parameters</param>
        public void Debug(string message, params object[] args)
        {
            logger.Debug(message, args);
        }


        /// <summary>
        /// Info method
        /// </summary>
        /// <param name="message">Info message</param>
        public void Info(string message)
        {
            logger.Info(message);
        }

        /// <summary>
        /// Info method with additional parameters
        /// </summary>
        /// <param name="message">Info message</param>
        /// <param name="args">Additional parameters</param>
        public void Info(string message, params object[] args)
        {
            logger.Info(message, args);
        }

        /// <summary>
        /// Warning  method
        /// </summary>
        /// <param name="message">Warning message</param>
        public void Warning(string message)
        {
            logger.Warn(message);
        }

        /// <summary>
        /// Warning method with additional parameters
        /// </summary>
        /// <param name="message">Warning message</param>
        /// <param name="args">Additional parameters</param>
        public void Warning(string message, params object[] args)
        {
            logger.Warn(message, args);
        }

        /// <summary>
        /// Error method
        /// </summary>
        /// <param name="message">Error message</param>
        public void Error(string message)
        {
            logger.Error(message);
        }

        /// <summary>
        /// Error method with additional parameters
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="args">Additional parameters</param>
        public void Error(string message, params object[] args)
        {
            logger.Error(message, args);
        }

        /// <summary>
        /// Error method with exception
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="message">Error message</param>
        /// <param name="isStackTraceIncluded">Boolean marker for including stack trace</param>
        public void Error(Exception exception, string message = null, bool isStackTraceIncluded = true) => logger.Error(exception, message, isStackTraceIncluded);


        /// <summary>
        /// Fatal method
        /// </summary>
        /// <param name="message">Fatal message</param>
        public void Fatal(string message)
        {
            logger.Fatal(message);
        }

        /// <summary>
        /// Fatal method with additional parameters
        /// </summary>
        /// <param name="message">Fatal message</param>
        /// <param name="args">Additional parameters</param>
        public void Fatal(string message, params object[] args)
        {
            logger.Fatal(message, args);
        }

        /// <summary>
        /// Fatal method with exception
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="message">Fatal message</param>
        /// <param name="isStackTraceIncluded">Boolean marker for including stack trace</param>
        public void Fatal(Exception exception, string message = null, bool isStackTraceIncluded = true)
        {
            logger.Fatal(exception, message, isStackTraceIncluded);
        }

    }
}