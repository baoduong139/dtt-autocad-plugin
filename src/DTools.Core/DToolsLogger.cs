using System;
using System.IO;
using System.Text;

namespace DTools.Core
{
    /// <summary>
    /// HỆ THỐNG GHI NHẬT KÝ CHẨN ĐOÁN & SỰ CỐ DTOOLS (DIAGNOSTIC & AUDIT LOGGER)
    /// Ghi nhật ký chi tiết vào %LocalAppData%\DTools\Logs phục vụ truy vết P0 Crash & Safe Startup
    /// </summary>
    public static class DToolsLogger
    {
        private static readonly object _syncLock = new object();
        private static string _logDirectory;
        private static string _startupLogFile;
        private static string _dailyLogFile;
        private static bool _initialized = false;

        public static string LogDirectory
        {
            get
            {
                EnsureInitialized();
                return _logDirectory;
            }
        }

        public static string StartupLogPath
        {
            get
            {
                EnsureInitialized();
                return _startupLogFile;
            }
        }

        public static string DailyLogPath
        {
            get
            {
                EnsureInitialized();
                return _dailyLogFile;
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;

            lock (_syncLock)
            {
                if (_initialized) return;

                try
                {
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    _logDirectory = Path.Combine(localAppData, "DTools", "Logs");
                    if (!Directory.Exists(_logDirectory))
                    {
                        Directory.CreateDirectory(_logDirectory);
                    }
                }
                catch
                {
                    // Fallback to Temp if LocalAppData fails
                    string temp = Path.GetTempPath();
                    _logDirectory = Path.Combine(temp, "DTools", "Logs");
                    if (!Directory.Exists(_logDirectory))
                    {
                        Directory.CreateDirectory(_logDirectory);
                    }
                }

                _startupLogFile = Path.Combine(_logDirectory, "dtools_startup.log");
                string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
                _dailyLogFile = Path.Combine(_logDirectory, $"dtools_{dateStr}.log");
                _initialized = true;
            }
        }

        public static void LogStartup(string acadVersion, string pluginVersion, bool safeStartup = true)
        {
            EnsureInitialized();
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"[DTOOLS STARTUP AUDIT] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"AutoCAD Version : {acadVersion}");
            sb.AppendLine($"Plugin Version  : {pluginVersion}");
            sb.AppendLine($"OS Version      : {Environment.OSVersion}");
            sb.AppendLine($"CLR Runtime     : {Environment.Version}");
            sb.AppendLine($"64-Bit Process  : {Environment.Is64BitProcess}");
            sb.AppendLine($"Safe Startup    : {(safeStartup ? "ENABLED (Lazy Loading Active)" : "DISABLED")}");
            sb.AppendLine($"Machine / User  : {Environment.MachineName} / {Environment.UserName}");
            sb.AppendLine("================================================================================");

            WriteRaw(sb.ToString(), writeToStartupLog: true);
        }

        public static void LogInfo(string tag, string message)
        {
            WriteEntry("INFO", tag, message, null);
        }

        public static void LogWarning(string tag, string message)
        {
            WriteEntry("WARN", tag, message, null);
        }

        public static void LogError(string tag, string message, Exception ex = null)
        {
            WriteEntry("ERROR", tag, message, ex);
        }

        public static void LogFatal(string tag, string message, Exception ex = null)
        {
            WriteEntry("FATAL", tag, message, ex);
        }

        public static void LogException(string taskName, Exception ex)
        {
            LogError(taskName, $"Ngoại lệ phát sinh trong tác vụ '{taskName}': {ex?.Message}", ex);
        }

        private static void WriteEntry(string level, string tag, string message, Exception ex)
        {
            EnsureInitialized();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var sb = new StringBuilder();
            sb.Append($"[{timestamp}] [{level,-5}] [{tag}] {message}");

            if (ex != null)
            {
                sb.AppendLine();
                sb.AppendLine($"   Exception Type : {ex.GetType().FullName}");
                sb.AppendLine($"   Message        : {ex.Message}");
                if (ex.InnerException != null)
                {
                    sb.AppendLine($"   InnerException : {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                }
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    sb.AppendLine($"   Stack Trace    :");
                    sb.AppendLine(ex.StackTrace);
                }
            }

            string entry = sb.ToString();
            WriteRaw(entry, writeToStartupLog: level == "FATAL" || tag == "Startup" || tag == "AppDomain");
        }

        private static void WriteRaw(string text, bool writeToStartupLog)
        {
            lock (_syncLock)
            {
                try
                {
                    // Luôn ghi vào daily log
                    File.AppendAllText(_dailyLogFile, text + Environment.NewLine, Encoding.UTF8);

                    // Nếu là startup log hoặc fatal, ghi đồng thời vào dtools_startup.log
                    if (writeToStartupLog)
                    {
                        File.AppendAllText(_startupLogFile, text + Environment.NewLine, Encoding.UTF8);
                    }
                }
                catch
                {
                    // Không bao giờ để lỗi ghi file làm sập plugin
                }
            }
        }
    }
}
