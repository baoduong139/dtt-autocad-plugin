using System;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;

namespace DTT_AutoCAD_Plugin.Common
{
    /// <summary>
    /// BỘ QUẢN LÝ TÀI NGUYÊN NỘI BỘ DTOOLS (RELATIVE PATH & RESOURCE MANAGER)
    /// Đảm bảo plugin hoạt động 100% độc lập khi chia sẻ sang máy tính khác (Plug & Play)
    /// </summary>
    public static class DtoolsResourceManager
    {
        #region 1. Xác Định Đường Dẫn Động (Dynamic Root Directory)

        /// <summary>
        /// Lấy thư mục gốc chứa file DLL của Dtools đang thực thi
        /// </summary>
        public static string GetDtoolsRootDir()
        {
            string dllLocation = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(dllLocation);
        }

        public static string GetDtoolsRootDirectory() => GetDtoolsRootDir();

        /// <summary>
        /// Đường dẫn tới thư mục YQArch nội bộ
        /// </summary>
        public static string GetYQArchDir()
        {
            string root = GetDtoolsRootDir();
            string path = Path.Combine(root, "YQArch");
            if (!Directory.Exists(path))
            {
                path = Path.Combine(root, "yqarch");
            }
            return path;
        }

        /// <summary>
        /// Đường dẫn tới thư mục YQArch\sys nội bộ
        /// </summary>
        public static string GetYQArchSysDir()
        {
            string yqDir = GetYQArchDir();
            string sysPath = Path.Combine(yqDir, "sys");
            if (!Directory.Exists(sysPath))
            {
                sysPath = Path.Combine(yqDir, "yqarch", "sys");
            }
            return sysPath;
        }

        #endregion

        #region 2. Đăng Ký Support Search Path Tự Động

        /// <summary>
        /// Tự động thêm đường dẫn nội bộ vào danh sách Support File Search Path của AutoCAD
        /// </summary>
        public static void EnsureSupportPaths()
        {
            try
            {
                string yqDir = GetYQArchDir();
                string yqSysDir = GetYQArchSysDir();

                if (Directory.Exists(yqDir)) AddToSupportPath(yqDir);
                if (Directory.Exists(yqSysDir)) AddToSupportPath(yqSysDir);
            }
            catch (System.Exception ex)
            {
                PrintMessage($"\n⚠️ [Dtools] Lỗi cập nhật Support Path: {ex.Message}");
            }
        }

        public static void AddToSupportPath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

            try
            {
                dynamic acadApp = Application.AcadApplication;
                dynamic preferences = acadApp.Preferences;
                dynamic files = preferences.Files;

                string currentSupportPath = (string)files.SupportPath;
                if (!currentSupportPath.ToLower().Contains(folderPath.ToLower()))
                {
                    files.SupportPath = currentSupportPath + ";" + folderPath;
                }
            }
            catch { }
        }

        public static void AddDirectoryToSupportPath(string folderPath) => AddToSupportPath(folderPath);

        #endregion

        #region 3. Khử Trùng & Nạp Menu Nội Bộ

        /// <summary>
        /// Khử trùng menu cũ/rác, nạp file CUIX nội bộ và kích hoạt LISP khởi tạo
        /// </summary>
        public static void InitializeYQArchInternal(bool forceReload = false)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                EnsureSupportPaths();

                string yqDir = GetYQArchDir();
                string yqSysDir = GetYQArchSysDir();
                string cuixPath = Path.Combine(yqSysDir, "yqarch.cuix");
                string lspPath = Path.Combine(yqDir, "yqarch.lsp");

                if (!File.Exists(lspPath))
                {
                    lspPath = Path.Combine(yqSysDir, "yqstart.lsp");
                }

                if (!File.Exists(cuixPath))
                {
                    PrintMessage($"\n❌ [Dtools] Không tìm thấy file menu CUIX tại: {cuixPath}");
                    return;
                }

                string cuixEscaped = cuixPath.Replace("\\", "/");
                string lspEscaped = lspPath.Replace("\\", "/");

                // Lisp nguyên tử:
                // 1. Unload các MenuGroup xung đột hoặc MenuGroup cũ nếu cần
                // 2. Nạp CUIX nội bộ (set FILEDIA = 0 để không hiện popup)
                // 3. Nạp yqarch.lsp nội bộ
                // 4. Set MENUBAR = 1
                string lispCommand = "\x03\x03(progn " +
                                     "(setvar \"CMDECHO\" 0) " +
                                     "(setvar \"FILEDIA\" 0) " +
                                     (forceReload ? "(if (menugroup \"yqarch\") (command \"_.menuunload\" \"yqarch\")) " : "") +
                                     "(foreach mg '(\"YQARCH_CN\" \"YQARCH_ENG\" \"GYQARCH\" \"YQARCH_VN\") " +
                                     "  (if (menugroup mg) (command \"_.menuunload\" mg))) " +
                                     $"(if (null (menugroup \"yqarch\")) (command \"_.menuload\" \"{cuixEscaped}\")) " +
                                     "(setvar \"FILEDIA\" 1) " +
                                     "(setvar \"MENUBAR\" 1) " +
                                     (File.Exists(lspPath) ? $"(if (findfile \"{lspEscaped}\") (load \"{lspEscaped}\")) " : "") +
                                     "(setvar \"CMDECHO\" 1) " +
                                     "(princ)" +
                                     ")\n";

                doc.SendStringToExecute(lispCommand, true, false, false);
            }
            catch (System.Exception ex)
            {
                PrintMessage($"\n❌ [Dtools Lỗi]: Khởi tạo YQArch thất bại: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo và gắn MenuBar kéo thả trực tiếp 'DTools Pro' lên thanh thực đơn AutoCAD
        /// </summary>
        public static void EnsureDToolsTopMenuBar()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                string lispMenu = "(progn " +
                                  "(vl-load-com) " +
                                  "(setq *acadApp* (vlax-get-acad-object)) " +
                                  "(setq *mGroups* (vla-get-menugroups *acadApp*)) " +
                                  "(setq *curGroup* (vla-item *mGroups* 0)) " +
                                  "(setq *pMenus* (vla-get-menus *curGroup*)) " +
                                  "(setq *dttMenu* nil) " +
                                  "(vlax-for m *pMenus* (if (= (vla-get-name m) \"DTools Pro\") (setq *dttMenu* m))) " +
                                  "(if *dttMenu* " +
                                  "  (progn " +
                                  "    (vl-catch-all-apply 'vla-removefrommenubar (list *dttMenu*)) " +
                                  "    (vl-catch-all-apply 'vla-delete (list *dttMenu*)) " +
                                  "    (setq *dttMenu* nil) " +
                                  "  ) " +
                                  ") " +
                                  "(setq *dttMenu* (vla-add *pMenus* \"DTools Pro\")) " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"⚡ Bật/Tắt Palette DTools (DTT)\" \"^C^CDTT \") " +
                                  "(vla-addseparator *dttMenu* (1+ (vla-get-count *dttMenu*))) " +
                                  // --- BẢO DƯỠNG & SỬA LỖI DWG ---
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"🧹 Dọn Rác & Sửa Lỗi DWG (DT_CLEAN)\" \"^C^CDT_CLEAN \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"🏷️ Phân Loại Layer Chuẩn (DT_QUICKLAYER)\" \"^C^CDT_QUICKLAYER \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"🔤 Chuẩn Hóa Font Tiếng Việt (DTT_FIXFONT)\" \"^C^CDTT_FIXFONT \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"📐 Đưa Toàn Bộ Về Z=0 (DTT_FLATTEN)\" \"^C^CDTT_FLATTEN \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"🗑️ Xóa Đối Tượng Trùng Lặp (_OVERKILL)\" \"^C^C_OVERKILL \") " +
                                  "(vla-addseparator *dttMenu* (1+ (vla-get-count *dttMenu*))) " +
                                  // --- THƯ VIỆN DYNAMIC BLOCKS NỘI BỘ ---
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"📦 Chèn Dynamic Block (DT_INSERTBLOCK)\" \"^C^CDT_INSERTBLOCK \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"🚪 Chèn Cửa Đi Động (DT_DOOR)\" \"^C^CDT_DOOR \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"🏛️ Chèn Cột Kiến Trúc Động (DT_COLUMN)\" \"^C^CDT_COLUMN \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"📐 Chèn Ký Hiệu Cao Độ (DT_ELEVATION)\" \"^C^CDT_ELEVATION \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"⚙️ Xem & Sửa Thuộc Tính Động (DT_PROPS)\" \"^C^CDT_PROPS \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"📂 Mở Thư Mục Blocks Thư Viện (DT_OPENBLOCKS)\" \"^C^CDT_OPENBLOCKS \") " +
                                  "(vla-addseparator *dttMenu* (1+ (vla-get-count *dttMenu*))) " +
                                  // --- YQARCH KIẾN TRÚC TIẾNG VIỆT ---
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"🔄 Khôi Phục YQArch Tiếng Việt (DT_YQREFIX)\" \"^C^CDT_YQREFIX \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"⚙️ Cấu Hình Hệ Thống YQArch (yqsys)\" \"^C^Cyqsys \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"🧱 Vẽ Tường Cắt Giao Tự Động (WW)\" \"^C^Cww \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"✂️ Cắt Tỉa Tường & Cột (TW)\" \"^C^CTW \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"🚪 Mở Cửa Đi & Cửa Sổ (AD)\" \"^C^CAD \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"📏 Dim Tự Động Tổng & Chi Tiết (DD)\" \"^C^CDD \") " +
                                  "(vla-addseparator *dttMenu* (1+ (vla-get-count *dttMenu*))) " +
                                  // --- CƠ ĐIỆN & TỰ ĐỘNG HÓA ---
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"📦 Thiết Kế Tủ Điện 2D/3D (DTT_CABINET)\" \"^C^CDTT_CABINET \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"🧊 Dựng Mô Hình 3D Tủ Điện (DTT_CABINET_3D)\" \"^C^CDTT_CABINET_3D \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"🏠 Mặt Bằng Điện 2D/3D (DTT_ELEC_FLOORPLAN)\" \"^C^CDTT_ELEC_FLOORPLAN \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"🎛️ Thư Viện PLC I/O Parametric (DTT_PLC)\" \"^C^CDTT_PLC \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"📊 Xuất Bảng Kê BOM Sang Excel (DTT_EXPORT_BOM)\" \"^C^CDTT_EXPORT_BOM \") " +
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"💧 Menu Cấp Thoát Nước MEP (DTT_MEP_MENU)\" \"^C^CDTT_MEP_MENU \") " +
                                  "(vla-addseparator *dttMenu* (1+ (vla-get-count *dttMenu*))) " +
                                  // --- CẬP NHẬT MENU ---
                                  "(vla-addmenuitem *dttMenu* (1+ (vla-get-count *dttMenu*)) \"📋 Cập Nhật / Khôi Phục Menu Này (DT_MENU)\" \"^C^CDT_MENU \") " +
                                  "(vla-insertinmenubar *dttMenu* (1- (vla-get-count (vla-get-menubar *acadApp*)))) " +
                                  "(setvar \"MENUBAR\" 1) " +
                                  "(princ) " +
                                  ")\n";

                doc.SendStringToExecute(lispMenu, true, false, false);
            }
            catch { }
        }

        public static void EnableMenuBar()
        {
            try
            {
                Application.SetSystemVariable("MENUBAR", 1);
            }
            catch
            {
                try
                {
                    Document doc = Application.DocumentManager.MdiActiveDocument;
                    doc?.SendStringToExecute("(setvar \"MENUBAR\" 1)\n", true, false, false);
                }
                catch { }
            }
        }

        public static void PrintMessage(string message)
        {
            try
            {
                Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage(message);
            }
            catch { }
        }

        #endregion
    }
}
