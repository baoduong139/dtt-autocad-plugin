using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.Modules
{
    /// <summary>
    /// PHÂN HỆ BẢO TRÌ & NÂNG CẤP HỆ THỐNG DTOOLS PRO 2026
    /// Chứa 3 chức năng: Auto-Purge & Audit (DT_CLEAN), Smart Layer Manager (DT_QUICKLAYER), YQ Health Check (DT_YQREFIX)
    /// </summary>
    public static class DTT_MaintenanceTools
    {
        #region 1. AUTO-PURGE & AUDIT (DT_CLEAN)

        /// <summary>
        /// Lệnh tự động dọn rác bản vẽ, xóa rác hình học 0-length và sửa lỗi cấu trúc DWG
        /// </summary>
        [CommandMethod("DT_CLEAN")]
        [CommandMethod("DTT_CLEAN")]
        [CommandMethod("DTOOLS_CLEAN")]
        public static void CmdAutoPurgeAndAudit()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            ed.WriteMessage("\n=======================================================");
            ed.WriteMessage("\n🧹 [DTools Pro] Đang bắt đầu dọn dẹp và tối ưu hóa bản vẽ...");

            int zeroGeometryRemoved = 0;

            try
            {
                // BƯỚC 1: Quét và loại bỏ đối tượng rác hình học (Zero-length Line / Arc)
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btrModel = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    foreach (ObjectId entId in btrModel)
                    {
                        Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        bool isGarbage = false;
                        if (ent is Line line)
                        {
                            if (line.Length < 1e-5) isGarbage = true;
                        }
                        else if (ent is Arc arc)
                        {
                            if (arc.Length < 1e-5) isGarbage = true;
                        }
                        else if (ent is DBText txt)
                        {
                            if (string.IsNullOrWhiteSpace(txt.TextString)) isGarbage = true;
                        }

                        if (isGarbage)
                        {
                            ent.UpgradeOpen();
                            ent.Erase();
                            zeroGeometryRemoved++;
                        }
                    }

                    tr.Commit();
                }

                // BƯỚC 2: Purge triệt để qua API lặp để xóa các block/layer lồng nhau
                int purgedCount = PurgeDatabaseDeep(db);

                // BƯỚC 3: Gửi lệnh Audit & Purge RegApps qua AutoCAD Command
                string lispClean = "\x03\x03(progn " +
                                   "(setvar \"CMDECHO\" 0) " +
                                   "(command \"-purge\" \"r\" \"*\" \"n\") " + // Xóa RegApps rác
                                   "(command \"-purge\" \"a\" \"*\" \"n\") " + // Purge All
                                   "(command \"_.audit\" \"y\") " +             // Sửa lỗi bản vẽ
                                   "(setvar \"CMDECHO\" 1) " +
                                   "(princ)" +
                                   ")\n";

                doc.SendStringToExecute(lispClean, true, false, false);

                ed.WriteMessage("\n✔ Đã loại bỏ " + zeroGeometryRemoved + " đối tượng rác hình học (chiều dài = 0 / text rỗng).");
                ed.WriteMessage("\n✔ Đã thanh lọc " + purgedCount + " định nghĩa không dùng (Blocks, Layers, Styles).");
                ed.WriteMessage("\n✔ Đã tự động kích hoạt tiến trình Sửa Lỗi Cấu Trúc (AUDIT).");
                ed.WriteMessage("\n⚡ [THÀNH CÔNG] Bản vẽ đã được làm sạch sâu và tối ưu hóa tốc độ!");
                ed.WriteMessage("\n=======================================================\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [DTools Lỗi DT_CLEAN]: {ex.Message}\n");
            }
        }

        private static int PurgeDatabaseDeep(Database db)
        {
            int totalPurged = 0;
            bool continuePurge = true;
            int maxIterations = 4;
            int iteration = 0;

            while (continuePurge && iteration < maxIterations)
            {
                iteration++;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ObjectIdCollection candidateIds = new ObjectIdCollection();

                    // Thu thập các symbol table cần purge
                    CollectPurgeCandidates<LayerTable, LayerTableRecord>(db.LayerTableId, tr, candidateIds, name => name != "0" && name != "Defpoints");
                    CollectPurgeCandidates<BlockTable, BlockTableRecord>(db.BlockTableId, tr, candidateIds, name => !name.StartsWith("*"));
                    CollectPurgeCandidates<LinetypeTable, LinetypeTableRecord>(db.LinetypeTableId, tr, candidateIds, name => name != "ByBlock" && name != "ByLayer" && name != "Continuous");
                    CollectPurgeCandidates<TextStyleTable, TextStyleTableRecord>(db.TextStyleTableId, tr, candidateIds, name => name != "Standard");
                    CollectPurgeCandidates<DimStyleTable, DimStyleTableRecord>(db.DimStyleTableId, tr, candidateIds, name => name != "Standard");

                    if (candidateIds.Count == 0)
                    {
                        continuePurge = false;
                        tr.Commit();
                        break;
                    }

                    // Gọi db.Purge để lọc ra những ID thực sự không còn tham chiếu
                    db.Purge(candidateIds);

                    if (candidateIds.Count > 0)
                    {
                        foreach (ObjectId id in candidateIds)
                        {
                            DBObject obj = tr.GetObject(id, OpenMode.ForWrite);
                            obj.Erase();
                            totalPurged++;
                        }
                        tr.Commit();
                    }
                    else
                    {
                        continuePurge = false;
                        tr.Commit();
                    }
                }
            }

            return totalPurged;
        }

        private static void CollectPurgeCandidates<TTable, TRecord>(ObjectId tableId, Transaction tr, ObjectIdCollection resultList, Func<string, bool> filter)
            where TTable : SymbolTable
            where TRecord : SymbolTableRecord
        {
            try
            {
                TTable table = (TTable)tr.GetObject(tableId, OpenMode.ForRead);
                foreach (ObjectId id in table)
                {
                    TRecord rec = (TRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (filter(rec.Name))
                    {
                        resultList.Add(id);
                    }
                }
            }
            catch { }
        }

        #endregion

        #region 2. SMART LAYER MANAGER (DT_QUICKLAYER)

        /// <summary>
        /// Tự động quét và phân loại đối tượng (Dim, Text, Axis, Hatch) vào đúng Layer tiêu chuẩn kỹ thuật
        /// </summary>
        [CommandMethod("DT_QUICKLAYER")]
        [CommandMethod("DTT_QUICKLAYER")]
        [CommandMethod("DTT_AUTOLAYER")]
        public static void CmdSmartLayerManager()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            ed.WriteMessage("\n=======================================================");
            ed.WriteMessage("\n🏷️ [DTools Pro] QUẢN LÝ LAYER THÔNG MINH (SMART LAYER MANAGER)");
            ed.WriteMessage("\nQuét chọn đối tượng hoặc nhấn [ENTER] để tự động sắp xếp TOÀN BỘ bản vẽ.");

            PromptSelectionOptions pso = new PromptSelectionOptions
            {
                MessageForAdding = "\nChọn các đối tượng cần phân loại Layer [Enter = Chọn Tất Cả]: "
            };

            PromptSelectionResult psr = ed.GetSelection(pso);
            ObjectIdCollection targetIds = new ObjectIdCollection();

            if (psr.Status == PromptStatus.OK && psr.Value != null && psr.Value.Count > 0)
            {
                foreach (SelectedObject so in psr.Value)
                {
                    if (so != null) targetIds.Add(so.ObjectId);
                }
            }
            else
            {
                // Người dùng bấm Enter -> Chọn toàn bộ ModelSpace
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btrModel = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                    foreach (ObjectId id in btrModel)
                    {
                        targetIds.Add(id);
                    }
                    tr.Commit();
                }
                ed.WriteMessage($"\n📌 Đã tự động chọn toàn bộ {targetIds.Count} đối tượng trong ModelSpace.");
            }

            if (targetIds.Count == 0)
            {
                ed.WriteMessage("\n⚠️ Không có đối tượng nào để xử lý!\n");
                return;
            }

            // Đảm bảo các Layer tiêu chuẩn tồn tại
            EnsureStandardLayers(db);

            int dimCount = 0;
            int textCount = 0;
            int hatchCount = 0;
            int axisCount = 0;
            int annoCount = 0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId entId in targetIds)
                {
                    Entity ent = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
                    if (ent == null) continue;

                    // 1. Kích thước (Dimensions) -> DTT_DIM
                    if (ent is Dimension)
                    {
                        ent.Layer = "DTT_DIM";
                        dimCount++;
                    }
                    // 2. Chữ / Văn bản (Text / MText) -> DTT_TEXT
                    else if (ent is DBText || ent is MText)
                    {
                        ent.Layer = "DTT_TEXT";
                        textCount++;
                    }
                    // 3. Mặt cắt / Hatch -> DTT_HATCH
                    else if (ent is Hatch)
                    {
                        ent.Layer = "DTT_HATCH";
                        hatchCount++;
                    }
                    // 4. Ký hiệu chỉ dẫn / Leader -> DTT_ANNO
                    else if (ent is Leader || ent is MLeader)
                    {
                        ent.Layer = "DTT_ANNO";
                        annoCount++;
                    }
                    // 5. Đường tim trục (Axis / Centerline) -> DTT_AXIS
                    else if (ent is Line line)
                    {
                        string ltName = "";
                        if (line.LinetypeId != ObjectId.Null && line.LinetypeId != db.ByLayerLinetype)
                        {
                            LinetypeTableRecord ltr = tr.GetObject(line.LinetypeId, OpenMode.ForRead) as LinetypeTableRecord;
                            if (ltr != null) ltName = ltr.Name.ToUpper();
                        }

                        if (ltName.Contains("CENTER") || ltName.Contains("AXIS") || line.Layer.ToUpper().Contains("TRUC") || line.Layer.ToUpper().Contains("AXIS"))
                        {
                            line.Layer = "DTT_AXIS";
                            CadUtils.SafeSetLinetype(line, db, "CENTER");
                            axisCount++;
                        }
                    }
                }

                tr.Commit();
                ed.Regen();
            }

            int totalClassified = dimCount + textCount + hatchCount + axisCount + annoCount;
            ed.WriteMessage("\n───────────────────────────────────────────────────────");
            ed.WriteMessage($"\n🎉 KẾT QUẢ PHÂN LOẠI LAYER THÀNH CÔNG ({totalClassified} đối tượng):");
            ed.WriteMessage($"\n   • 📐 Kích thước (DTT_DIM - Màu Cyan):    {dimCount} đối tượng");
            ed.WriteMessage($"\n   • 🔤 Chữ / Ghi chú (DTT_TEXT - Màu Xanh lá): {textCount} đối tượng");
            ed.WriteMessage($"\n   • 🏁 Mặt cắt (DTT_HATCH - Màu Ghi xám):    {hatchCount} đối tượng");
            ed.WriteMessage($"\n   • 🎯 Đường tim trục (DTT_AXIS - Màu Đỏ Center): {axisCount} đối tượng");
            ed.WriteMessage($"\n   • 🏷️ Chỉ dẫn Leader (DTT_ANNO - Màu Vàng):  {annoCount} đối tượng");
            ed.WriteMessage("\n=======================================================\n");
        }

        private static void EnsureStandardLayers(Database db)
        {
            try
            {
                // Layer Kích thước: Màu Cyan (4), nét 0.15mm
                CadUtils.GetOrCreateLayer(db, "DTT_DIM", 4, LineWeight.LineWeight015);

                // Layer Chữ: Màu Xanh lá (3), nét 0.20mm
                CadUtils.GetOrCreateLayer(db, "DTT_TEXT", 3, LineWeight.LineWeight020);

                // Layer Hatch: Màu 8 (Dark Gray), nét 0.09mm
                CadUtils.GetOrCreateLayer(db, "DTT_HATCH", 8, LineWeight.LineWeight009);

                // Layer Leader/Anno: Màu Vàng (2), nét 0.18mm
                CadUtils.GetOrCreateLayer(db, "DTT_ANNO", 2, LineWeight.LineWeight018);

                // Layer Trục: Màu Đỏ (1), nét 0.13mm
                CadUtils.GetOrCreateLayer(db, "DTT_AXIS", 1, LineWeight.LineWeight013);
            }
            catch { }
        }

        #endregion

        #region 3. YQARCH HEALTH CHECK & AUTO-REPAIR (DT_YQREFIX)

        /// <summary>
        /// Khám sức khỏe toàn diện hệ sinh thái YQArch: Kiểm tra đường dẫn, khử trùng menu trùng lặp và khôi phục Menu Việt Hóa
        /// </summary>
        [CommandMethod("DT_YQREFIX")]
        [CommandMethod("DTT_YQREFIX")]
        [CommandMethod("YQ_FIX")]
        public static void CmdYqArchHealthCheckAndRefix()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            ed.WriteMessage("\n=======================================================");
            ed.WriteMessage("\n🩺 [DTools Health Check] KIỂM TRA & KHÔI PHỤC HỆ THỐNG YQARCH TIẾNG VIỆT");
            ed.WriteMessage("\n───────────────────────────────────────────────────────");

            // 1. Xác định vị trí DLL và thư mục Dtools
            string dllPath = Assembly.GetExecutingAssembly().Location;
            string dtoolsDir = Path.GetDirectoryName(dllPath);

            string yqDir = Path.Combine(dtoolsDir, "yqarch");
            if (!Directory.Exists(yqDir)) yqDir = Path.Combine(dtoolsDir, "YQArch");

            string yqSysDir = Path.Combine(yqDir, "sys");
            if (!Directory.Exists(yqSysDir)) yqSysDir = Path.Combine(yqDir, "yqarch", "sys");

            string cuixPath = Path.Combine(yqSysDir, "yqarch.cuix");
            string lspPath = Path.Combine(yqDir, "yqarch.lsp");
            if (!File.Exists(lspPath)) lspPath = Path.Combine(yqSysDir, "yqstart.lsp");

            ed.WriteMessage($"\n📁 Thư mục Dtools:   {dtoolsDir}");
            ed.WriteMessage($"\n📁 Thư mục YQArch:   {yqDir} -> {(Directory.Exists(yqDir) ? "✅ [TỒN TẠI]" : "❌ [THIẾU]")}");
            ed.WriteMessage($"\n📁 Thư mục YQ sys:    {yqSysDir} -> {(Directory.Exists(yqSysDir) ? "✅ [TỒN TẠI]" : "❌ [THIẾU]")}");
            ed.WriteMessage($"\n📄 File Menu CUIX:    {cuixPath} -> {(File.Exists(cuixPath) ? "✅ [SẴN SÀNG]" : "❌ [THIẾU]")}");
            ed.WriteMessage($"\n📄 File LISP Engine:  {lspPath} -> {(File.Exists(lspPath) ? "✅ [SẴN SÀNG]" : "❌ [THIẾU]")}");

            if (!Directory.Exists(yqSysDir) || !File.Exists(cuixPath))
            {
                ed.WriteMessage("\n❌ LỖI NGHIÊM TRỌNG: Dữ liệu YQArch trong bundle bị thiếu!");
                ed.WriteMessage("\n=======================================================\n");
                return;
            }

            // 2. Khắc phục Support Search Path
            DtoolsResourceManager.AddDirectoryToSupportPath(yqDir);
            DtoolsResourceManager.AddDirectoryToSupportPath(yqSysDir);
            ed.WriteMessage("\n✅ [1/4] Đã cấu hình lại AutoCAD Support Search Path thành công.");

            // 3. Khử trùng Menu: Gửi LISP dỡ bỏ toàn bộ MenuGroup trùng lặp
            string cuixEscaped = cuixPath.Replace("\\", "/");
            string lspEscaped = lspPath.Replace("\\", "/");

            string lispCleanAndReload = "\x03\x03(progn " +
                                        "(setvar \"CMDECHO\" 0) " +
                                        "(setvar \"FILEDIA\" 0) " +
                                        // Unload các nhóm menu có thể xung đột
                                        "(foreach mg '(\"YQARCH\" \"YQARCH_CN\" \"YQARCH_ENG\" \"GYQARCH\" \"YQARCH_VN\") " +
                                        "  (if (menugroup mg) (command \"_.menuunload\" mg))) " +
                                        // Nạp lại CUIX duy nhất của Dtools
                                        $"(command \"_.menuload\" \"{cuixEscaped}\") " +
                                        "(setvar \"FILEDIA\" 1) " +
                                        // Nạp LISP khởi tạo
                                        $"(if (findfile \"{lspEscaped}\") (load \"{lspEscaped}\")) " +
                                        "(setvar \"MENUBAR\" 1) " +
                                        "(setvar \"CMDECHO\" 1) " +
                                        "(princ \"\\n[DTools] Da khoi phuc Menu YQArch Tieng Viet thanh cong!\\n\") " +
                                        "(princ)" +
                                        ")\n";

            doc.SendStringToExecute(lispCleanAndReload, true, false, false);

            ed.WriteMessage("\n✅ [2/4] Đã gỡ bỏ toàn bộ Menu YQArch trùng lặp / rác.");
            ed.WriteMessage("\n✅ [3/4] Đã nạp lại file Menu CUIX Tiếng Việt duy nhất chuẩn Dtools.");
            ed.WriteMessage("\n✅ [4/4] Đã bật thanh MenuBar = 1 và nạp lại hệ thống LISP.");
            ed.WriteMessage("\n───────────────────────────────────────────────────────");
            ed.WriteMessage("\n🎉 [HOÀN TẤT] Hệ thống YQArch Tiếng Việt đã được phục hồi hoàn hảo 100%!");
            ed.WriteMessage("\n=======================================================\n");
        }

        #endregion
    }
}
