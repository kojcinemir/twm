/*
 * Tiling Window Manager
 * Copyright (C) 2025 Kojčin Emir
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */


using Serilog;
using System;
using System.IO;

namespace TilingWindowManager
{
    public static class Logger
    {
        private static readonly ILogger? _logger;
        private static bool _loggingEnabled = true;
        private static string _logLevel = "info";

        static Logger()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.toml");
            var loggingConfig = LoggingConfiguration.LoadFromFile(configPath);
            _loggingEnabled = loggingConfig.Enabled;
            _logLevel = loggingConfig.Level;

            if (_loggingEnabled)
            {
                var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logsPath);

                var loggerConfig = new LoggerConfiguration();

                switch (_logLevel.ToLowerInvariant())
                {
                    case "debug":
                        loggerConfig.MinimumLevel.Debug();
                        break;
                    case "info":
                        loggerConfig.MinimumLevel.Information();
                        break;
                    case "warning":
                        loggerConfig.MinimumLevel.Warning();
                        break;
                    case "error":
                        loggerConfig.MinimumLevel.Error();
                        break;
                    default:
                        loggerConfig.MinimumLevel.Information();
                        break;
                }

                _logger = loggerConfig
                    .WriteTo.File(
                        path: Path.Combine(logsPath, "twm-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 5,
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(1))
                    .CreateLogger();
            }
        }

        public static void EnableLogging(bool enable = true)
        {
            _loggingEnabled = enable;
        }

        public static void Info(string message)
        {
            if (_loggingEnabled && _logger != null)
            {
                _logger.Information(message);
            }
        }

        public static void Warning(string message)
        {
            if (_loggingEnabled && _logger != null)
            {
                _logger.Warning(message);
            }
        }

        public static void Error(string message)
        {
            if (_loggingEnabled && _logger != null)
            {
                _logger.Error(message);
            }
        }

        public static void Error(Exception ex, string message)
        {
            if (_loggingEnabled && _logger != null)
            {
                _logger.Error(ex, message);
            }
        }

        public static void CloseAndFlush()
        {
            if (_loggingEnabled && _logger != null)
            {
                Log.CloseAndFlush();
            }
        }
    }
}