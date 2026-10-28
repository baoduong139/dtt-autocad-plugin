using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DTools.Core
{
    /// <summary>
    /// Bộ định vị tài nguyên độc lập, loại bỏ 100% đường dẫn tuyệt đối hardcode của máy lập trình
    /// Tự động xác định đường dẫn Bundle, ApplicationData, LocalAppData và Assembly Location
    /// </summary>
    public static class ResourceLocator
    {
        private static string _customBaseDirectory = null;

        /// <summary>
        /// Cho phép thiết lập thư mục gốc tùy biến phục vụ Unit Testing
        /// </summary>
        public static void SetCustomBaseDirectory(string path)
        {
            _customBaseDirectory = path;
        }

        public static string GetAssemblyDirectory()
        {
            if (!string.IsNullOrEmpty(_customBaseDirectory) && Directory.Exists(_customBaseDirectory))
                return _customBaseDirectory;

            try
            {
                string loc = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(loc))
                    return Path.GetDirectoryName(loc);
            }
            catch { }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string GetSymbolsDatabasePath()
        {
            string asmDir = GetAssemblyDirectory();

            string[] candidates = new string[]
            {
                Path.Combine(asmDir, "symbols_database.json"),
                Path.Combine(asmDir, "Contents", "symbols_database.json"),
                Path.Combine(Directory.GetParent(asmDir)?.FullName ?? asmDir, "symbols_database.json"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Autodesk\ApplicationPlugins\DTT_Tools.bundle\Contents\symbols_database.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "symbols_database.json")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }

            return candidates[0]; // Trả về đường dẫn mặc định
        }

        public static string GetBlocksDirectory()
        {
            string asmDir = GetAssemblyDirectory();

            string[] candidates = new string[]
            {
                Path.Combine(asmDir, "Blocks"),
                Path.Combine(asmDir, "Contents", "Blocks"),
                Path.Combine(Directory.GetParent(asmDir)?.FullName ?? asmDir, "Blocks"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Autodesk\ApplicationPlugins\DTT_Tools.bundle\Contents\Blocks"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Blocks")
            };

            foreach (var dir in candidates)
            {
                if (Directory.Exists(dir))
                    return dir;
            }

            return candidates[0];
        }

        public static string GetUserDataDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dtoolsDir = Path.Combine(appData, "DToolsPro");
            if (!Directory.Exists(dtoolsDir))
            {
                try { Directory.CreateDirectory(dtoolsDir); } catch { }
            }
            return dtoolsDir;
        }

        public static string GetLogsDirectory()
        {
            string logDir = Path.Combine(GetUserDataDirectory(), "Logs");
            if (!Directory.Exists(logDir))
            {
                try { Directory.CreateDirectory(logDir); } catch { }
            }
            return logDir;
        }

        public static string GetSettingsFilePath()
        {
            return Path.Combine(GetUserDataDirectory(), "settings.json");
        }
    }
}
