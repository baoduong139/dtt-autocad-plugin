using System;
using System.Diagnostics;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using DTools.Core;
using DTT_AutoCAD_Plugin.Common;
using DTT_AutoCAD_Plugin.Modules;
using DTT_AutoCAD_Plugin.UI;

[assembly: ExtensionApplication(typeof(DTT_AutoCAD_Plugin.AppEntry))]
[assembly: CommandClass(typeof(DTT_AutoCAD_Plugin.AppEntry))]
[assembly: CommandClass(typeof(DTT_AutoCAD_Plugin.Modules.ArchitectureTools))]
[assembly: CommandClass(typeof(DTT_AutoCAD_Plugin.Modules.StructureTools))]
[assembly: CommandClass(typeof(DTT_AutoCAD_Plugin.Modules.MepTools))]
[assembly: CommandClass(typeof(DTT_AutoCAD_Plugin.Modules.SuperUtilityTools))]
[assembly: CommandClass(typeof(DTT_AutoCAD_Plugin.Modules.ElectricalTools))]
[assembly: CommandClass(typeof(DTT_AutoCAD_Plugin.Modules.ProExpansionCommands))]
[assembly: CommandClass(typeof(DTT_AutoCAD_Plugin.Modules.DTT_MaintenanceTools))]
[assembly: CommandClass(typeof(DTT_AutoCAD_Plugin.Modules.DTT_DynamicBlockCommands))]

namespace DTT_AutoCAD_Plugin
{
    /// <summary>
    /// ĐIỂM VÀO CHÍNH CỦA PLUGIN DTOOLS PRO (AUTODESK EXTENSION APPLICATION)
    /// Đã tái cấu trúc theo kiến trúc Safe Startup & Lazy Loading 100% (P0 Crash Prevention)
    /// </summary>
    public class AppEntry : IExtensionApplication
    {
        private static bool _bannerPrinted = false;
        private static bool _unhandledHookInstalled = false;

        static AppEntry()
        {
            // BẬT CƠ CHẾ RESOLVE TỰ ĐỘNG CHO CÁC PHIÊN BẢN AUTOCAD KHÁC NHAU (R24.0 - R26.0)
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolveAutoCADAssemblies;
            }
            catch { }
        }

        /// <summary>
        /// Khởi tạo an toàn tuyệt đối khi AutoCAD nạp plugin:
        /// 1. KHÔNG khởi tạo WPF Palette (tránh lỗi 0xc000041d / e0434352h khi UI AutoCAD chưa sẵn sàng)
        /// 2. KHÔNG đọc file symbols_database.json hay quét đĩa
        /// 3. KHÔNG chạy lệnh LISP hoặc sửa thanh thực đơn AutoCAD
        /// 4. Ghi log khởi động chi tiết vào %LocalAppData%\DTools\Logs\dtools_startup.log
        /// </summary>
        public void Initialize()
        {
            var sw = Stopwatch.StartNew();

            try
            {
                // 1. Cài đặt bẫy UnhandledException cấp AppDomain
                if (!_unhandledHookInstalled)
                {
                    AppDomain.CurrentDomain.UnhandledException += OnUnhandledAppDomainException;
                    _unhandledHookInstalled = true;
                }

                // 2. Thu thập thông tin phiên bản AutoCAD
                string acadVer = "Unknown";
                try
                {
                    acadVer = Application.Version != null ? Application.Version.ToString() : "N/A";
                }
                catch { }

                // 3. Ghi chẩn đoán khởi động
                DToolsLogger.LogStartup(acadVer, DToolsConfiguration.Version, safeStartup: DToolsConfiguration.SafeStartupEnabled);
                DToolsLogger.LogInfo("Startup", "Kích hoạt chế độ Safe Startup: Tác vụ nặng (Palette, I/O, LISP) đã được trì hoãn (Lazy Loading).");

                // 4. In thông báo chào mừng an toàn (nếu có tài liệu mở sẵn hoặc chờ DocumentCreated)
                PrintStartupBannerSafe();

                sw.Stop();
                DToolsLogger.LogInfo("Startup", $"AppEntry.Initialize hoàn tất an toàn trong {sw.ElapsedMilliseconds} ms.");
            }
            catch (System.Exception ex)
            {
                sw.Stop();
                DToolsLogger.LogFatal("Startup", "Ngoại lệ phát sinh trong AppEntry.Initialize()", ex);
                // SAFE STARTUP: Tuyệt đối không throw ngoại lệ ra ngoài để bảo vệ tiến trình AutoCAD
            }
        }

        public void Terminate()
        {
            try
            {
                DToolsLogger.LogInfo("Shutdown", "DTools Pro đang dọn dẹp và kết thúc.");
                if (DttPaletteManager.IsCreated)
                {
                    DttPaletteManager.CloseAndDispose();
                }
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogError("Shutdown", "Lỗi khi kết thúc DTools Pro", ex);
            }
        }

        #region Safe Startup Banner & Document Handling

        private static void PrintStartupBannerSafe()
        {
            try
            {
                Document doc = Application.DocumentManager?.MdiActiveDocument;
                if (doc != null && doc.Editor != null)
                {
                    PrintBanner(doc.Editor);
                    _bannerPrinted = true;
                }
                else if (Application.DocumentManager != null)
                {
                    // AutoCAD khởi động mà chưa mở tài liệu (Start Screen): Đăng ký DocumentCreated
                    Application.DocumentManager.DocumentCreated += OnFirstDocumentCreated;
                }
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogWarning("Startup", "Không thể in banner khởi động: " + ex.Message);
            }
        }

        private static void OnFirstDocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            try
            {
                if (!_bannerPrinted && e.Document != null && e.Document.Editor != null)
                {
                    PrintBanner(e.Document.Editor);
                    _bannerPrinted = true;
                }

                if (Application.DocumentManager != null)
                {
                    Application.DocumentManager.DocumentCreated -= OnFirstDocumentCreated;
                }
            }
            catch { }
        }

        private static void PrintBanner(Editor ed)
        {
            if (ed == null) return;
            try
            {
                ed.WriteMessage("\n=======================================================");
                ed.WriteMessage($"\n⚡ {DToolsConfiguration.ProductName} (v{DToolsConfiguration.Version}) - HỆ THỐNG TIỆN ÍCH AUTOCAD CHUYÊN NGHIỆP");
                ed.WriteMessage("\n🛡️ Trạng thái: SAFE STARTUP (Lazy Loading 100% • 8GB RAM Safe • Chống Crash)");
                ed.WriteMessage("\n📌 Gõ lệnh 'DTT' hoặc 'DTOOLS' để BẬT / TẮT Palette công cụ.");
                ed.WriteMessage("\n🌟 MENU POPUP TRUNG TÂM 2D/3D: 'DT_MODAL' / 'DT_CENTER_MODAL' / 'DT_POPUP'");
                ed.WriteMessage("\n📦 THƯ VIỆN DYNAMIC BLOCKS: 'DT_INSERTBLOCK' | CỬA: 'DT_DOOR' | CỘT: 'DT_COLUMN'");
                ed.WriteMessage("\n🔧 BẢO DƯỠNG BẢN VẼ: 'DT_CLEAN' | FONT: 'DTT_FIXFONT' | PHẲNG HÓA: 'DTT_FLATTEN'");
                ed.WriteMessage("\n⚡ NẠP MENU YQARCH: 'DTT_LOAD_YQ' / 'YQ_RELOAD'");
                ed.WriteMessage("\n=======================================================\n");
            }
            catch { }
        }

        #endregion

        #region Global Exception Trap & Assembly Resolve

        private static void OnUnhandledAppDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as System.Exception;
                DToolsLogger.LogFatal("AppDomain",
                    $"Bẫy ngoại lệ chưa xử lý (IsTerminating = {e.IsTerminating}): {ex?.Message}",
                    ex ?? new System.Exception(e.ExceptionObject?.ToString() ?? "Unknown exception"));
            }
            catch { }
        }

        private static Assembly ResolveAutoCADAssemblies(object sender, ResolveEventArgs args)
        {
            try
            {
                string reqName = new AssemblyName(args.Name).Name;
                if (string.Equals(reqName, "accoremgd", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(reqName, "acdbmgd", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(reqName, "acmgd", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(reqName, "AcCui", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(reqName, "AdWindows", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (string.Equals(loaded.GetName().Name, reqName, StringComparison.OrdinalIgnoreCase))
                        {
                            return loaded;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        #endregion

        #region User Commands

        [CommandMethod("DTT")]
        [CommandMethod("DTOOLS")]
        [CommandMethod("DTT_PALETTE")]
        public static void CmdTogglePalette()
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc?.Editor;

                // 1. Luôn kích hoạt thanh thực đơn MenuBar "DTools Pro" trên đỉnh màn hình (MENUBAR=1)
                try
                {
                    DtoolsResourceManager.EnsureDToolsTopMenuBar();
                }
                catch { }

                // 2. Luôn mở thanh công cụ Palette DTT bên trái màn hình (Bảng điều khiển trung tâm duy nhất)
                try
                {
                    DttPaletteManager.ShowPalette();
                }
                catch { }

                if (ed != null)
                {
                    ed.WriteMessage("\n=======================================================");
                    ed.WriteMessage("\n⚡ [DTT CAD MASTER] BẢNG ĐIỀU KHIỂN TRUNG TÂM ĐÃ SẴN SÀNG:");
                    ed.WriteMessage("\n  1. Palette: Đang hiển thị bảng điều khiển bên trái màn hình.");
                    ed.WriteMessage("\n  2. MenuBar: 'DTools Pro' trên đỉnh màn hình AutoCAD.");
                    ed.WriteMessage("\n  3. Popup Menu: Gõ 'DTT_POPUP' nếu cần mở hộp thoại tổng hợp.");
                    ed.WriteMessage("\n=======================================================\n");
                }

                // Đã tắt tự động Show cửa sổ Quick Menu Dialog (tránh popup che khuất màn hình vẽ)
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogError("PaletteCommand", "Lỗi khi gọi lệnh DTT/DTOOLS", ex);
                Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage(
                    $"\n[DTools ERROR] Lỗi hiển thị Palette: {ex.Message}\n");
            }
        }

        [CommandMethod("DTT_POPUP")]
        [CommandMethod("DTT_POPUP_MENU")]
        [CommandMethod("DTT_QUICK_MENU")]
        public static void CmdShowQuickMenu()
        {
            try
            {
                DTT_QuickMenuDialog.ShowMainMenu();
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogError("QuickMenu", "Lỗi mở Quick Menu", ex);
            }
        }

        [CommandMethod("DTT_YQ")]
        [CommandMethod("DTT_QUICK")]
        [CommandMethod("YQ_QUICK")]
        public static void CmdOpenYqQuick()
        {
            try
            {
                DttPaletteManager.SelectTab("PanelYqQuick");
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogError("YqQuickCommand", "Lỗi khi chọn tab YQ Quick", ex);
                Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage(
                    $"\n[DTools ERROR] Lỗi hiển thị Thao Tác Nhanh YQ: {ex.Message}\n");
            }
        }

        [CommandMethod("DTT_LOAD_YQ")]
        [CommandMethod("DTT_LOAD_YQ_MENU")]
        [CommandMethod("YQ_RELOAD")]
        public static void CmdLoadYqMenu()
        {
            try
            {
                DtoolsResourceManager.InitializeYQArchInternal(forceReload: true);
                DtoolsResourceManager.EnsureDToolsTopMenuBar();
                DToolsConfiguration.IsYqMenuLoaded = true;
                DToolsLogger.LogInfo("YqMenu", "Đã nạp thành công Menu & Động cơ YQArch theo yêu cầu người dùng.");
                DtoolsResourceManager.PrintMessage("\n⚡ [Dtools]: Đã nạp lại Menu & Động cơ YQArch vào AutoCAD!\n");
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogError("YqMenu", "Lỗi nạp Menu YQArch", ex);
                DtoolsResourceManager.PrintMessage($"\n❌ [Dtools Lỗi]: Không thể nạp Menu YQArch: {ex.Message}\n");
            }
        }

        [CommandMethod("DT_MENU")]
        [CommandMethod("DTT_MENU")]
        public static void CmdRebuildMenu()
        {
            try
            {
                DtoolsResourceManager.EnsureDToolsTopMenuBar();
                DttPaletteManager.ShowPalette();
                DToolsLogger.LogInfo("Menu", "Đã cập nhật Menu DTools Pro trên thanh MenuBar và mở bảng Palette.");
                DtoolsResourceManager.PrintMessage("\n⚡ [DTools Pro]: Đã kích hoạt Menu 'DTools Pro' trên thanh MenuBar và bảng Palette bên trái!\n");
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogError("Menu", "Lỗi cập nhật Menu DTools Pro", ex);
                DtoolsResourceManager.PrintMessage($"\n❌ [DTools Lỗi]: Không thể tạo Menu: {ex.Message}\n");
            }
        }

        [CommandMethod("DTT_FIXFONT")]
        public static void CmdFixFont()
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    DrawingDoctor.FixFontVietnamese(doc);
                }
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogError("FixFont", "Lỗi thực thi DTT_FIXFONT", ex);
            }
        }

        [CommandMethod("DTT_FLATTEN")]
        public static void CmdFlatten()
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    DrawingDoctor.FlattenZ(doc);
                }
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogError("Flatten", "Lỗi thực thi DTT_FLATTEN", ex);
            }
        }

        #endregion
    }
}
