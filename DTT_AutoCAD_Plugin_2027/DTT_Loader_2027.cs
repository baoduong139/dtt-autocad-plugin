using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(DTT_AutoCAD_Plugin_2027.DTT_Loader_2027))]
[assembly: CommandClass(typeof(DTT_AutoCAD_Plugin_2027.DTT_Loader_2027))]

namespace DTT_AutoCAD_Plugin_2027
{
    /// <summary>
    /// BỘ KHỞI TẠO NẠP ĐỘNG RAM & QUẢN LÝ CẬP NHẬT CHO AUTOCAD ELECTRICAL 2027 (.NET 8)
    /// - Không khóa file trên đĩa nhờ kỹ thuật Assembly.Load(File.ReadAllBytes)
    /// - Tự động định tuyến phiên bản mới nhất từ %AppData%/DTT_Tools_2027/Versions/
    /// - Tích hợp Auto-Updater nền và các lệnh kiểm tra hệ thống
    /// </summary>
    public class DTT_Loader_2027 : IExtensionApplication
    {
        private static bool _initialized = false;
        private static readonly Dictionary<string, Assembly> _inMemoryAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        private static string _activeModuleDirectory = "";

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                // 1. Xác định thư mục chứa DLL nghiệp vụ (Ưu tiên bản cập nhật trong %AppData%)
                ResolveActiveModuleDirectory();

                // 2. Đăng ký Hook nạp động từ RAM (Zero File Lock)
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolveInMemory;

                // 3. Nạp sẵn các thư viện lõi vào RAM
                PreloadBusinessAssemblies();

                // 4. In thông tin khởi động an toàn
                PrintWelcomeBanner();

                // 5. Kích hoạt kiểm tra cập nhật ngầm qua GitHub Releases
                GitHubUpdateService2027.StartBackgroundCheck();
            }
            catch (System.Exception ex)
            {
                WriteEditorMessage("\n[DTT 2027 LOADER LỖI]: " + ex.Message);
            }
        }

        public void Terminate()
        {
            // Dọn dẹp tài nguyên khi AutoCAD đóng
        }

        private static void ResolveActiveModuleDirectory()
        {
            string appDataVersions = GitHubUpdateService2027.GetVersionsDirectory();
            string activeVer = GitHubUpdateService2027.GetCurrentActiveVersion();
            string stagedDir = Path.Combine(appDataVersions, activeVer);

            if (Directory.Exists(stagedDir) && File.Exists(Path.Combine(stagedDir, "DTools.Core.dll")))
            {
                _activeModuleDirectory = stagedDir;
            }
            else
            {
                // Mặc định nạp từ thư mục Contents cài đặt gốc
                string asmLoc = Assembly.GetExecutingAssembly().Location;
                _activeModuleDirectory = string.IsNullOrEmpty(asmLoc) ? AppDomain.CurrentDomain.BaseDirectory : Path.GetDirectoryName(asmLoc);
            }
        }

        private static Assembly OnAssemblyResolveInMemory(object sender, ResolveEventArgs args)
        {
            try
            {
                string shortName = new AssemblyName(args.Name).Name;
                if (string.IsNullOrEmpty(shortName)) return null;

                lock (_inMemoryAssemblies)
                {
                    if (_inMemoryAssemblies.TryGetValue(shortName, out Assembly loaded))
                    {
                        return loaded;
                    }

                    if (!string.IsNullOrEmpty(_activeModuleDirectory) && Directory.Exists(_activeModuleDirectory))
                    {
                        string candidate = Path.Combine(_activeModuleDirectory, shortName + ".dll");
                        if (File.Exists(candidate))
                        {
                            byte[] rawBytes = File.ReadAllBytes(candidate);
                            Assembly asm = Assembly.Load(rawBytes);
                            _inMemoryAssemblies[shortName] = asm;
                            return asm;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static void PreloadBusinessAssemblies()
        {
            string[] coreDlls = new string[] { 
                "DTools.Core.dll", 
                "DTools.Application.dll", 
                "DTools.dll",
                "DTT_AutoCAD_Plugin_2027.dll",
                "DTT_AutoCAD_Plugin.dll"
            };

            foreach (string dllName in coreDlls)
            {
                try
                {
                    string p = Path.Combine(_activeModuleDirectory, dllName);
                    if (File.Exists(p))
                    {
                        string key = Path.GetFileNameWithoutExtension(dllName);
                        if (!_inMemoryAssemblies.ContainsKey(key))
                        {
                            byte[] bytes = File.ReadAllBytes(p);
                            Assembly asm = Assembly.Load(bytes);
                            _inMemoryAssemblies[key] = asm;

                            // Tự động khởi tạo AppEntry nếu là assembly plugin chính
                            if (dllName.StartsWith("DTT_AutoCAD_Plugin", StringComparison.OrdinalIgnoreCase))
                            {
                                Type appType = asm.GetType("DTT_AutoCAD_Plugin.AppEntry");
                                if (appType != null)
                                {
                                    object appInstance = Activator.CreateInstance(appType);
                                    MethodInfo initMethod = appType.GetMethod("Initialize");
                                    initMethod?.Invoke(appInstance, null);
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    WriteEditorMessage($"\n[DTT 2027 LOADER CẢNH BÁO]: Không thể nạp trước {dllName}: {ex.Message}");
                }
            }
        }

        private static void PrintWelcomeBanner()
        {
            WriteEditorMessage("\n==================================================================");
            WriteEditorMessage("\n⚡ [DTT TOOLS 2027] BỘ CÔNG CỤ ĐIỆN THÔNG MINH CHO AUTOCAD ELECTRICAL");
            WriteEditorMessage($"\n🚀 Phiên bản .NET: {Environment.Version} (.NET 8 Runtime x64)");
            WriteEditorMessage($"\n📦 Phiên bản Plugin: v{GitHubUpdateService2027.GetCurrentActiveVersion()} (In-Memory RAM Loader - File Unlocked)");
            WriteEditorMessage("\n💡 Các lệnh tiện ích:");
            WriteEditorMessage("\n   - DTT2027_UPDATE : Kiểm tra cập nhật qua GitHub Releases");
            WriteEditorMessage("\n   - DTT2027_INFO   : Xem trạng thái hệ thống và bộ nhớ RAM");
            WriteEditorMessage("\n   - DTT2027_RELOAD : Nạp nóng lại phiên bản mới không cần tắt CAD");
            WriteEditorMessage("\n==================================================================\n");
        }

        [CommandMethod("DTT2027_UPDATE")]
        public void ExecuteManualUpdate()
        {
            _ = GitHubUpdateService2027.CheckForUpdatesAsync(isManual: true);
        }

        [CommandMethod("DTT2027_INFO")]
        public void ExecuteShowInfo()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument?.Editor;
            if (ed == null) return;

            ed.WriteMessage("\n========================================================");
            ed.WriteMessage("\n[DTT 2027] THÔNG TIN HỆ THỐNG VÀ BỘ NHỚ LOADER");
            ed.WriteMessage("\n========================================================");
            ed.WriteMessage($"\n- Host Version        : {Application.Version}");
            ed.WriteMessage($"\n- .NET Environment    : {Environment.Version}");
            ed.WriteMessage($"\n- Active Version      : v{GitHubUpdateService2027.GetCurrentActiveVersion()}");
            ed.WriteMessage($"\n- Active Module Dir   : {_activeModuleDirectory}");
            ed.WriteMessage($"\n- AppData Storage     : {GitHubUpdateService2027.GetAppDataDirectory()}");
            ed.WriteMessage($"\n- Module In-Memory    : {_inMemoryAssemblies.Count} assemblies loaded");
            foreach (var kvp in _inMemoryAssemblies)
            {
                ed.WriteMessage($"\n    + {kvp.Key} -> {kvp.Value.FullName}");
            }
            ed.WriteMessage("\n========================================================\n");
        }

        [CommandMethod("DTT2027_RELOAD")]
        public void ExecuteReload()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument?.Editor;
            try
            {
                ResolveActiveModuleDirectory();
                lock (_inMemoryAssemblies)
                {
                    _inMemoryAssemblies.Clear();
                }
                PreloadBusinessAssemblies();

                if (ed != null)
                {
                    ed.WriteMessage($"\n[DTT 2027 THÀNH CÔNG] Đã nạp nóng lại toàn bộ module từ: {_activeModuleDirectory}");
                    ed.WriteMessage($"\nPhiên bản đang hoạt động: v{GitHubUpdateService2027.GetCurrentActiveVersion()}");
                }
            }
            catch (System.Exception ex)
            {
                if (ed != null)
                {
                    ed.WriteMessage($"\n[DTT 2027 LỖI RELOAD]: {ex.Message}");
                }
            }
        }

        private static void WriteEditorMessage(string msg)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage(msg);
                }
            }
            catch { }
        }
    }
}
