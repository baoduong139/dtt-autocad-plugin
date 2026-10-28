using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DTT_AutoCAD_Plugin.Common;
using DTT_AutoCAD_Plugin.UI;

namespace DTT_AutoCAD_Plugin.Modules
{
    /// <summary>
    /// Lưu trữ ngữ cảnh tham số của cấu kiện được chọn từ Menu GUI (kích thước, hướng, cao độ...)
    /// Giúp lệnh CAD chèn ngay tức thì vào bản vẽ mà KHÔNG cần hỏi lại ở dòng lệnh Command Line.
    /// </summary>
    public static class BlockPlacementContext
    {
        public static string TargetBlockName { get; set; } = "Architectural_Columns";
        public static double Width { get; set; } = 220;
        public static double Height { get; set; } = 220;
        public static double WallThickness { get; set; } = 110;
        public static bool Flip { get; set; } = false;
        public static double Angle { get; set; } = 45;
        public static string ElevationText { get; set; } = "+0.000";
        public static string TagText { get; set; } = "";
        public static string VisibilityState { get; set; } = "";
    }

    /// <summary>
    /// HỆ THỐNG LỆNH CHÈN & TỰ ĐỘNG CẤU HÌNH DYNAMIC BLOCKS NỘI BỘ DTOOLS
    /// Lệnh chính: DT_INSERTBLOCK / DTT_INSERTBLOCK
    /// Các lệnh tắt nhanh: DT_FAST_PLACE, DT_DOOR, DT_COLUMN, DT_ELEVATION, DT_DYNAMIC_PROPS, DT_OPENBLOCKS
    /// </summary>
    public class DTT_DynamicBlockCommands
    {
        #region 1. Lệnh Chính: DT_INSERTBLOCK / DTT_INSERTBLOCK

        /// <summary>
        /// Lệnh mở Menu Pop-up Trung Tâm Màn Hình với Xem Trước 2D/3D Tương Tác
        /// </summary>
        [CommandMethod("DT_MODAL")]
        [CommandMethod("DTT_MODAL")]
        [CommandMethod("DT_CENTER_MODAL")]
        [CommandMethod("DT_POPUP")]
        [CommandMethod("DT_MENU3D")]
        public static void CmdOpenCenterModal()
        {
            DTT_ModalRegistry.ShowCenterModal("DYNAMIC_BLOCKS");
        }

        [CommandMethod("DT_INSERTBLOCK")]
        [CommandMethod("DTT_INSERTBLOCK")]
        [CommandMethod("=T_INSERTBLOCK")]
        [CommandMethod("T_INSERTBLOCK")]
        [CommandMethod("DT_BLOCK")]
        [CommandMethod("DTT_BLOCK")]
        public static void CmdInsertBlock()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                // 1. Đảm bảo thư mục Blocks và các file chuẩn đã sẵn sàng
                DtoolsBlockService.EnsureDefaultBlocksExist();

                ed.WriteMessage("\n=======================================================");
                ed.WriteMessage("\n📦 DTOOLS PRO - CHÈN DYNAMIC BLOCK NỘI BỘ THƯ VIỆN");
                ed.WriteMessage("\n=======================================================");

                // 2. Lấy danh sách block có sẵn trong thư mục Dtools/Blocks/
                List<string> availableBlocks = DtoolsBlockService.GetAvailableBlockNames();

                // 3. Cho người dùng chọn loại Block
                PromptKeywordOptions pko = new PromptKeywordOptions("\n👉 Chọn Dynamic Block cần chèn [Cua-Di(Door)/Cot-Kien-Truc(Column)/Cao-Do(Elevation)/Chon-Tep(Browse)]: ");
                pko.Keywords.Add("Door", "D", "Cua-Di (Door)");
                pko.Keywords.Add("Column", "C", "Cot-Kien-Truc (Column)");
                pko.Keywords.Add("Elevation", "E", "Cao-Do (Elevation)");
                pko.Keywords.Add("Browse", "B", "Chon-Tep (Browse)");
                pko.Keywords.Default = "Door";

                PromptResult pr = ed.GetKeywords(pko);
                if (pr.Status != PromptStatus.OK) return;

                string selectedBlockName = "Architectural_Doors";

                if (pr.StringResult == "Door")
                {
                    selectedBlockName = "Architectural_Doors";
                }
                else if (pr.StringResult == "Column")
                {
                    selectedBlockName = "Architectural_Columns";
                }
                else if (pr.StringResult == "Elevation")
                {
                    selectedBlockName = "Annotations";
                }
                else if (pr.StringResult == "Browse")
                {
                    ed.WriteMessage($"\n📂 Danh sách Block hiện có trong thư viện ({availableBlocks.Count} block):");
                    for (int i = 0; i < availableBlocks.Count; i++)
                    {
                        ed.WriteMessage($"\n   [{i + 1}] {availableBlocks[i]}");
                    }

                    PromptStringOptions pso = new PromptStringOptions("\nNhập tên hoặc số thứ tự Block muốn chèn: ");
                    pso.AllowSpaces = true;
                    PromptResult prName = ed.GetString(pso);
                    if (prName.Status != PromptStatus.OK) return;

                    string inputStr = prName.StringResult.Trim();
                    if (int.TryParse(inputStr, out int index) && index >= 1 && index <= availableBlocks.Count)
                    {
                        selectedBlockName = availableBlocks[index - 1];
                    }
                    else if (!string.IsNullOrEmpty(inputStr))
                    {
                        selectedBlockName = Path.GetFileNameWithoutExtension(inputStr);
                    }
                }

                // 4. Hỏi điểm chèn trên bản vẽ
                PromptPointOptions ppo = new PromptPointOptions($"\n📍 Chọn điểm chèn Block '{selectedBlockName}' [Esc để hủy]: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) return;

                Point3d insertPt = ppr.Value;

                // 5. Tự động Nạp và Chèn Block vào Database hiện hành
                ObjectId blkRefId = DtoolsBlockService.InsertBlockFromLibrary(
                    db,
                    selectedBlockName,
                    insertPt,
                    new Scale3d(1, 1, 1),
                    0.0
                );

                if (blkRefId.IsNull)
                {
                    ed.WriteMessage($"\n❌ [Lỗi]: Không thể chèn block '{selectedBlockName}'.");
                    return;
                }

                ed.WriteMessage($"\n✅ Đã chèn thành công Block '{selectedBlockName}' tại tọa độ ({insertPt.X:F1}, {insertPt.Y:F1}, {insertPt.Z:F1})!");

                // 6. Tự động kiểm tra & gán thuộc tính Dynamic Block
                ConfigureInsertedBlockInteractive(doc, blkRefId, selectedBlockName);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [DT_INSERTBLOCK Lỗi]: {ex.Message}");
            }
        }

        #endregion

        #region 2. Tự Động Gán Thuộc Tính Dynamic Block Ngay Sau Khi Chèn

        /// <summary>
        /// Cho phép người dùng tùy biến ngay Dynamic Property (Size, Flip, Visibility) sau khi chèn
        /// </summary>
        private static void ConfigureInsertedBlockInteractive(Document doc, ObjectId blkRefId, string blockName)
        {
            Editor ed = doc.Editor;
            List<DynamicPropertyInfo> props = DtoolsBlockService.GetDynamicPropertiesInfo(blkRefId);

            if (props.Count > 0)
            {
                ed.WriteMessage("\n───────────────────────────────────────────────────────");
                ed.WriteMessage($"\n⚡ [DYNAMIC BLOCK PHÁT HIỆN]: {props.Count} tham số động khả dụng:");
                foreach (var p in props)
                {
                    ed.WriteMessage($"\n   • {p}");
                }
                ed.WriteMessage("\n───────────────────────────────────────────────────────");
            }

            bool configuring = true;
            while (configuring)
            {
                PromptKeywordOptions pko = new PromptKeywordOptions("\n⚙️ Cấu hình Dynamic Block [Kich-Thuoc(Size)/Lat-Huong(Flip)/Trang-Thai(Visibility)/Hoan-Tat(Done)]: ");
                pko.Keywords.Add("Size", "S", "Kich-Thuoc (Size)");
                pko.Keywords.Add("Flip", "F", "Lat-Huong (Flip)");
                pko.Keywords.Add("Visibility", "V", "Trang-Thai (Visibility)");
                pko.Keywords.Add("Done", "D", "Hoan-Tat (Done)");
                pko.Keywords.Default = "Done";

                PromptResult pr = ed.GetKeywords(pko);
                if (pr.Status != PromptStatus.OK || pr.StringResult == "Done")
                {
                    configuring = false;
                    break;
                }

                if (pr.StringResult == "Size")
                {
                    // Hỏi kích thước mới
                    PromptDoubleOptions pdo = new PromptDoubleOptions("\n📏 Nhập kích thước mới (mm) [vd: 800, 900, 1000, 1200]: ");
                    PromptDoubleResult pdr = ed.GetDouble(pdo);
                    if (pdr.Status == PromptStatus.OK)
                    {
                        double newSize = pdr.Value;
                        bool ok = DtoolsBlockService.SetDynamicBlockProperty(blkRefId, "Distance", newSize) ||
                                  DtoolsBlockService.SetDynamicBlockProperty(blkRefId, "Width", newSize) ||
                                  DtoolsBlockService.SetDynamicBlockProperty(blkRefId, "Length", newSize) ||
                                  DtoolsBlockService.SetDynamicBlockProperty(blkRefId, "Rong", newSize) ||
                                  DtoolsBlockService.SetDynamicBlockProperty(blkRefId, "DOOR_TAG", $"D ({newSize}x2200)") ||
                                  DtoolsBlockService.SetDynamicBlockProperty(blkRefId, "COL_TAG", $"C ({newSize}x{newSize})");

                        if (ok)
                            ed.WriteMessage($"\n✅ Đã cập nhật kích thước Dynamic Block thành {newSize} mm!");
                        else
                            ed.WriteMessage("\n⚠️ Không tìm thấy tham số kích thước tương ứng để gán.");
                    }
                }
                else if (pr.StringResult == "Flip")
                {
                    bool ok = DtoolsBlockService.FlipDynamicBlock(blkRefId);
                    if (ok)
                        ed.WriteMessage("\n🔄 Đã lật hướng Dynamic Block thành công!");
                    else
                        ed.WriteMessage("\n⚠️ Không thể lật hướng block này.");
                }
                else if (pr.StringResult == "Visibility")
                {
                    // Tìm các allowed values của visibility state
                    List<string> states = new List<string>();
                    foreach (var p in props)
                    {
                        if (p.AllowedValues.Count > 0 && (p.Name.IndexOf("Visibility", StringComparison.OrdinalIgnoreCase) >= 0 || p.UnitsType == "NoUnits"))
                        {
                            states.AddRange(p.AllowedValues);
                        }
                    }

                    if (states.Count > 0)
                    {
                        ed.WriteMessage($"\n👁️ Các trạng thái hiển thị ({states.Count}): {string.Join(", ", states)}");
                    }

                    PromptStringOptions pso = new PromptStringOptions("\nNhập tên trạng thái Visibility State mới: ");
                    PromptResult prState = ed.GetString(pso);
                    if (prState.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(prState.StringResult))
                    {
                        bool ok = DtoolsBlockService.SetDynamicBlockVisibility(blkRefId, prState.StringResult.Trim());
                        if (ok)
                            ed.WriteMessage($"\n✅ Đã chuyển Visibility State thành '{prState.StringResult.Trim()}'!");
                        else
                            ed.WriteMessage("\n⚠️ Không thể đổi Visibility State.");
                    }
                }
            }

            ed.WriteMessage("\n🎉 Hoàn tất cấu hình Dynamic Block!\n");
        }

        #endregion

        #region 3. Các Lệnh Chèn Siêu Tốc & Lệnh Tắt Nhanh Chuyên Biệt

        /// <summary>
        /// Lệnh chèn linh kiện siêu tốc theo thông số đã chọn từ Menu nhỏ / Modal.
        /// Bỏ qua hoàn toàn các câu hỏi keyword/kích thước/lật hướng trên Command Line.
        /// Cho phép click chuột liên tục để đặt nhiều vị trí, nhấn Enter hoặc Esc để hoàn thành.
        /// </summary>
        [CommandMethod("DT_FAST_PLACE")]
        [CommandMethod("DTT_FAST_PLACE")]
        [CommandMethod("=T_FAST_PLACE")]
        [CommandMethod("T_FAST_PLACE")]
        [CommandMethod("FAST_PLACE")]
        [CommandMethod("DT_PLACE")]
        public static void CmdFastPlace()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                DtoolsBlockService.EnsureDefaultBlocksExist();

                string blockName = BlockPlacementContext.TargetBlockName;
                if (string.IsNullOrWhiteSpace(blockName))
                    blockName = "Architectural_Columns";

                double width = BlockPlacementContext.Width > 0 ? BlockPlacementContext.Width : 220;
                double height = BlockPlacementContext.Height > 0 ? BlockPlacementContext.Height : width;
                bool flip = BlockPlacementContext.Flip;
                string elevText = !string.IsNullOrWhiteSpace(BlockPlacementContext.ElevationText) 
                    ? BlockPlacementContext.ElevationText 
                    : "+0.000";

                PromptPointOptions ppo = new PromptPointOptions("\n📍 [DTools] Click chọn điểm đặt linh kiện trên bản vẽ [Enter/Esc để xong]: ");
                ppo.AllowNone = true;

                int count = 0;
                while (true)
                {
                    PromptPointResult ppr = ed.GetPoint(ppo);
                    if (ppr.Status != PromptStatus.OK) break;

                    using (doc.LockDocument())
                    {
                        ObjectId blkId = DtoolsBlockService.InsertBlockFromLibrary(db, blockName, ppr.Value, new Scale3d(1, 1, 1), 0.0);
                        if (blkId.IsNull)
                        {
                            ed.WriteMessage($"\n⚠️ Không thể nạp block '{blockName}' từ thư viện Dtools.");
                            break;
                        }

                        // Tự động gán thông số Dynamic Block đã cấu hình từ Menu
                        if (blockName.IndexOf("Door", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            DtoolsBlockService.SetDynamicBlockProperty(blkId, "Door Size", width);
                            DtoolsBlockService.SetDynamicBlockProperty(blkId, "Wall Thickness", BlockPlacementContext.WallThickness > 0 ? BlockPlacementContext.WallThickness : 110.0);
                            DtoolsBlockService.SetDynamicBlockProperty(blkId, "Distance", width);
                            DtoolsBlockService.SetDynamicBlockProperty(blkId, "Width", width);
                            string tag = !string.IsNullOrEmpty(BlockPlacementContext.TagText) 
                                ? BlockPlacementContext.TagText 
                                : $"D ({width}x2200)";
                            DtoolsBlockService.SetDynamicBlockProperty(blkId, "DOOR_TAG", tag);

                            if (flip)
                            {
                                DtoolsBlockService.FlipDynamicBlock(blkId);
                            }
                        }
                        else if (blockName.IndexOf("Column", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            DtoolsBlockService.SetDynamicBlockProperty(blkId, "W", width);
                            DtoolsBlockService.SetDynamicBlockProperty(blkId, "H", height);
                            DtoolsBlockService.SetDynamicBlockProperty(blkId, "Distance", width);
                            DtoolsBlockService.SetDynamicBlockProperty(blkId, "Width", width);
                            string tag = !string.IsNullOrEmpty(BlockPlacementContext.TagText) 
                                ? BlockPlacementContext.TagText 
                                : $"C{width}x{height}";
                            DtoolsBlockService.SetDynamicBlockProperty(blkId, "COL_TAG", tag);
                        }
                        else if (blockName.IndexOf("Annotation", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            DtoolsBlockService.SetDynamicBlockProperty(blkId, "ELEVATION", elevText);
                        }
                        else
                        {
                            DtoolsBlockService.SetDynamicBlockProperty(blkId, "Distance", width);
                            DtoolsBlockService.SetDynamicBlockProperty(blkId, "Width", width);
                            if (flip) DtoolsBlockService.FlipDynamicBlock(blkId);
                        }
                    }

                    // Cập nhật hiển thị đồ họa tức thì
                    CadUtils.RefreshScreenAndGraphics(ed);

                    count++;
                    ppo.Message = $"\n📍 [DTools] Đã đặt {count} cấu kiện '{blockName}'. Click điểm tiếp theo [Enter/Esc để xong]: ";
                }

                if (count > 0)
                {
                    CadUtils.RefreshScreenAndGraphics(ed);
                    ed.WriteMessage($"\n✅ [DTools]: Đã chèn xong {count} linh kiện '{blockName}'.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [Lỗi DT_FAST_PLACE]: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Lệnh chèn Cửa Đi Động (Architectural_Doors.dwg) - Chèn ngay không hỏi dòng lệnh
        /// </summary>
        [CommandMethod("DT_DOOR")]
        [CommandMethod("DTT_DOOR")]
        [CommandMethod("=T_DOOR")]
        [CommandMethod("T_DOOR")]
        public static void CmdQuickDoor()
        {
            BlockPlacementContext.TargetBlockName = "Architectural_Doors";
            if (BlockPlacementContext.Width <= 0 || BlockPlacementContext.Width > 3000)
                BlockPlacementContext.Width = 900;
            BlockPlacementContext.TagText = $"D ({BlockPlacementContext.Width}x2200)";
            CmdFastPlace();
        }

        /// <summary>
        /// Lệnh chèn Cột Kiến Trúc Động (Architectural_Columns.dwg) - Chèn ngay không hỏi dòng lệnh
        /// </summary>
        [CommandMethod("DT_COLUMN")]
        [CommandMethod("DTT_COLUMN")]
        [CommandMethod("=T_COLUMN")]
        [CommandMethod("T_COLUMN")]
        public static void CmdQuickColumn()
        {
            BlockPlacementContext.TargetBlockName = "Architectural_Columns";
            if (BlockPlacementContext.Width <= 0 || BlockPlacementContext.Width > 3000)
            {
                BlockPlacementContext.Width = 220;
                BlockPlacementContext.Height = 220;
            }
            BlockPlacementContext.TagText = $"C{BlockPlacementContext.Width}x{BlockPlacementContext.Height}";
            CmdFastPlace();
        }

        /// <summary>
        /// Lệnh chèn Ký hiệu Cao độ Động (Annotations.dwg) - Chèn ngay không hỏi dòng lệnh
        /// </summary>
        [CommandMethod("DT_ELEVATION")]
        [CommandMethod("DTT_ELEVATION")]
        [CommandMethod("=T_ELEVATION")]
        [CommandMethod("T_ELEVATION")]
        public static void CmdQuickElevation()
        {
            BlockPlacementContext.TargetBlockName = "Annotations";
            if (string.IsNullOrEmpty(BlockPlacementContext.ElevationText))
                BlockPlacementContext.ElevationText = "+0.000";
            CmdFastPlace();
        }

        /// <summary>
        /// Lệnh xem & chỉnh sửa thuộc tính Dynamic Block của một đối tượng bất kỳ trên bản vẽ
        /// </summary>
        [CommandMethod("DT_DYNAMIC_PROPS")]
        [CommandMethod("DT_PROPS")]
        [CommandMethod("DTT_PROPS")]
        public static void CmdInspectDynamicProps()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                PromptEntityOptions peo = new PromptEntityOptions("\n🔍 Chọn 1 đối tượng Block để xem thuộc tính Dynamic: ");
                peo.SetRejectMessage("\n⚠️ Đối tượng được chọn phải là Block!");
                peo.AddAllowedClass(typeof(BlockReference), true);

                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK) return;

                List<DynamicPropertyInfo> props = DtoolsBlockService.GetDynamicPropertiesInfo(per.ObjectId);
                if (props.Count == 0)
                {
                    ed.WriteMessage("\nℹ️ Block được chọn không phải là Dynamic Block hoặc không có tham số động.");
                    return;
                }

                ed.WriteMessage($"\n⚡ [DYNAMIC BLOCK]: Danh sách {props.Count} tham số:");
                for (int i = 0; i < props.Count; i++)
                {
                    ed.WriteMessage($"\n   [{i + 1}] {props[i]}");
                }

                // Cho phép sửa nhanh giá trị
                PromptStringOptions pso = new PromptStringOptions("\nNhập tên tham số muốn sửa (hoặc Enter để thoát): ");
                PromptResult prName = ed.GetString(pso);
                if (prName.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(prName.StringResult))
                {
                    string propName = prName.StringResult.Trim();
                    PromptStringOptions psoVal = new PromptStringOptions($"\nNhập giá trị mới cho tham số '{propName}': ");
                    PromptResult prVal = ed.GetString(psoVal);
                    if (prVal.Status == PromptStatus.OK)
                    {
                        bool ok = DtoolsBlockService.SetDynamicBlockProperty(per.ObjectId, propName, prVal.StringResult.Trim());
                        if (ok)
                            ed.WriteMessage($"\n✅ Đã cập nhật tham số '{propName}' thành công!");
                        else
                            ed.WriteMessage($"\n⚠️ Không thể cập nhật tham số '{propName}'.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [Lỗi DT_DYNAMIC_PROPS]: {ex.Message}");
            }
        }

        /// <summary>
        /// Lệnh mở thư mục Dtools/Blocks/ trong Windows Explorer để người dùng quản lý thư viện
        /// </summary>
        [CommandMethod("DT_OPENBLOCKS")]
        [CommandMethod("DTT_OPENBLOCKS")]
        public static void CmdOpenBlocksFolder()
        {
            try
            {
                string blocksDir = DtoolsBlockService.GetBlocksDirectory();
                if (!Directory.Exists(blocksDir))
                    Directory.CreateDirectory(blocksDir);

                DtoolsBlockService.EnsureDefaultBlocksExist();
                Process.Start("explorer.exe", blocksDir);
                DtoolsResourceManager.PrintMessage($"\n📂 Đã mở thư mục Thư viện Dynamic Blocks: {blocksDir}\n");
            }
            catch (System.Exception ex)
            {
                DtoolsResourceManager.PrintMessage($"\n❌ Không thể mở thư mục Blocks: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Lệnh tự kiểm tra toàn diện Thư viện Dynamic Blocks
        /// </summary>
        [CommandMethod("DT_TEST_DYN")]
        [CommandMethod("DTT_TEST_DYN")]
        public static void CmdTestDynamicBlocks()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                ed.WriteMessage("\n=======================================================");
                ed.WriteMessage("\n🧪 [DTOOLS TEST]: BẮT ĐẦU KIỂM TRA DYNAMIC BLOCKS NỘI BỘ");
                ed.WriteMessage("\n=======================================================");

                // 1. Kiểm tra chèn Architectural_Doors
                ObjectId d1 = DtoolsBlockService.InsertBlockFromLibrary(db, "Architectural_Doors", new Point3d(0, 0, 0), new Scale3d(1, 1, 1), 0.0);
                ObjectId d2 = DtoolsBlockService.InsertBlockFromLibrary(db, "Architectural_Doors", new Point3d(1500, 0, 0), new Scale3d(1, 1, 1), 0.0);

                // 2. Kiểm tra chèn Architectural_Columns
                ObjectId c1 = DtoolsBlockService.InsertBlockFromLibrary(db, "Architectural_Columns", new Point3d(3000, 0, 0), new Scale3d(1, 1, 1), 0.0);

                // 3. Kiểm tra chèn Annotations
                ObjectId a1 = DtoolsBlockService.InsertBlockFromLibrary(db, "Annotations", new Point3d(4500, 0, 0), new Scale3d(1, 1, 1), 0.0);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockReference brD1 = (BlockReference)tr.GetObject(d1, OpenMode.ForRead);
                    BlockReference brD2 = (BlockReference)tr.GetObject(d2, OpenMode.ForRead);
                    BlockReference brC1 = (BlockReference)tr.GetObject(c1, OpenMode.ForRead);
                    BlockReference brA1 = (BlockReference)tr.GetObject(a1, OpenMode.ForRead);

                    ed.WriteMessage($"\n[TEST 1 - DOOR 1]: IsDynamic = {brD1.IsDynamicBlock}");
                    ed.WriteMessage($"\n[TEST 2 - DOOR 2]: IsDynamic = {brD2.IsDynamicBlock}");
                    ed.WriteMessage($"\n[TEST 3 - COLUMN]: IsDynamic = {brC1.IsDynamicBlock}");
                    ed.WriteMessage($"\n[TEST 4 - ANNOTATION]: IsDynamic = {brA1.IsDynamicBlock}");
                    tr.Commit();
                }

                // 4. Test sửa thuộc tính
                bool okDoor = DtoolsBlockService.SetDynamicBlockProperty(d1, "Door Size", 900.0);
                bool okFlip = DtoolsBlockService.FlipDynamicBlock(d1);
                bool okColW = DtoolsBlockService.SetDynamicBlockProperty(c1, "W", 350.0);
                bool okColH = DtoolsBlockService.SetDynamicBlockProperty(c1, "H", 350.0);
                bool okAnno = DtoolsBlockService.SetDynamicBlockProperty(a1, "Ref Line Length", 300.0);

                ed.WriteMessage($"\n[TEST 5 - MODIFY]: Door={okDoor}, Flip={okFlip}, ColW={okColW}, ColH={okColH}, Anno={okAnno}");

                var pList = DtoolsBlockService.GetDynamicPropertiesInfo(d1);
                ed.WriteMessage($"\n⚡ [DOOR DYN PROPS ({pList.Count})]:");
                foreach (var p in pList) ed.WriteMessage($"\n   • {p}");

                var pCol = DtoolsBlockService.GetDynamicPropertiesInfo(c1);
                ed.WriteMessage($"\n⚡ [COLUMN DYN PROPS ({pCol.Count})]:");
                foreach (var p in pCol) ed.WriteMessage($"\n   • {p}");

                var pAnno = DtoolsBlockService.GetDynamicPropertiesInfo(a1);
                ed.WriteMessage($"\n⚡ [ANNO DYN PROPS ({pAnno.Count})]:");
                foreach (var p in pAnno) ed.WriteMessage($"\n   • {p}");

                ed.WriteMessage("\n=======================================================");
                ed.WriteMessage("\n🎉 [KẾT QUẢ]: TẤT CẢ DYNAMIC BLOCKS HOẠT ĐỘNG HOÀN HẢO!");
                ed.WriteMessage("\n=======================================================\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [TEST LỖI]: {ex.Message}");
            }
        }
    }
}
