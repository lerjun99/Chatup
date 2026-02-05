using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Tools
{
    public static class AppLogger
    {
        private static bool _isConfigured = false;
        private static readonly string DefaultPath = "C:\\Logs\\pcl-log-.txt";

        private static void EnsureConfigured()
        {
            if (_isConfigured) return;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(DefaultPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            _isConfigured = true;
        }
        public static void LogError(string message)
        {
            EnsureConfigured();
            Log.Error(message);
        }
        public static void LogInfo(string message)
        {
            EnsureConfigured();
            Log.Information(message);
        }
        public static void LogError(Exception ex, object? result = null, object? parameters = null)
        {
            EnsureConfigured();

            var sb = new StringBuilder();
            sb.AppendLine("Exception: " + ex.GetBaseException().Message);
            sb.AppendLine("StackTrace: " + ex.StackTrace);

            if (result != null)
                sb.AppendLine("Result: " + JsonConvert.SerializeObject(result));
            if (parameters != null)
                sb.AppendLine("Parameters: " + JsonConvert.SerializeObject(parameters));

            Log.Error(sb.ToString());
        }
        public static void LogInfo(string message, object? result = null, object? parameters = null)
        {
            EnsureConfigured();

            var sb = new StringBuilder();
            sb.AppendLine("INFO: " + message);

            if (result != null)
                sb.AppendLine("Result: " + JsonConvert.SerializeObject(result));
            if (parameters != null)
                sb.AppendLine("Parameters: " + JsonConvert.SerializeObject(parameters));

            Log.Information(sb.ToString());
        }
    }

}
