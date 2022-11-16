using System;
using Microsoft.Extensions.Logging;

namespace Blockcore.Utilities
{
    /// <summary>
    /// Extension methods for classes and interfaces related to logging.
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Converts <see cref="Microsoft.Extensions.Logging.LogLevel"/> to <see cref="NLog.LogLevel"/>.
        /// </summary>
        /// <param name="logLevel">Log level value to convert.</param>
        /// <returns>NLog value of the log level.</returns>
        public static NLog.LogLevel ToNLogLevel(this LogLevel logLevel)
        {
            NLog.LogLevel res = NLog.LogLevel.Trace;

            switch (logLevel)
            {
                case LogLevel.Trace: res = NLog.LogLevel.Trace; break;
                case LogLevel.Debug: res = NLog.LogLevel.Debug; break;
                case LogLevel.Information: res = NLog.LogLevel.Info; break;
                case LogLevel.Warning: res = NLog.LogLevel.Warn; break;
                case LogLevel.Error: res = NLog.LogLevel.Error; break;
                case LogLevel.Critical: res = NLog.LogLevel.Fatal; break;
            }

            return res;
        }

        /// <summary>
        /// Converts a string to a <see cref="NLog.LogLevel"/>.
        /// </summary>
        /// <param name="logLevel">Log level value to convert.</param>
        /// <returns>NLog value of the log level.</returns>
        public static NLog.LogLevel ToNLogLevel(this string logLevel)
        {
            logLevel = logLevel.ToLowerInvariant();

            return logLevel switch
            {
                "trace" => NLog.LogLevel.Trace,
                "debug" => NLog.LogLevel.Debug,
                "info" or "information" => NLog.LogLevel.Info,
                "warn" or "warning" => NLog.LogLevel.Warn,
                "error" => NLog.LogLevel.Error,
                "fatal" or "critical" or "crit" => NLog.LogLevel.Fatal,
                "off" => NLog.LogLevel.Off,
                _ => throw new Exception($"Failed converting {logLevel} to a member of NLog.LogLevel."),
            };
        }
    }
}