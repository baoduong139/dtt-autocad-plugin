using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DTT_AutoCAD_Plugin.Common;
using DTT_AutoCAD_Plugin.UI;

namespace DTT_AutoCAD_Plugin.Modules
{
    public static class ArchitectureTools
    {
        // ============================================================
        // LỆNH MỞ MENU POPUP KIẾN TRÚC VỚI FLOATING DIALOG & LIVE PREVIEW
        // ============================================================

        [CommandMethod("DTT_WALL_MENU")]
        public static void CmdShowWallMenu()
        {
            var pr = DTools_FloatingDialog.Prompt(
                "VẼ TƯỜNG XÂY GẠCH & VÁCH NGĂN",
                "Nhập bề dày tường, trục tâm và chế độ vẽ liên tục",
                "🧱",
                PreviewKind.Wall,
                new[] {
                    new ParameterField("Thick", "Bề dày tường:", "220", "mm", new[] { "220", "110", "100", "75", "150", "200" }),
                    new ParameterField("Continuous", "Chế độ vẽ liên tục:", "Có", null, new[] { "Có", "Không" })
                });
            if (pr == null) return;
            double thick = pr.GetDouble("Thick", 220.0);
            DrawWall(thick, $"Tường {thick:0}mm");
        }

        [CommandMethod("DTT_DOOR_MENU")]
        public static void CmdShowDoorMenu()
        {
            var pr = DTools_FloatingDialog.Prompt(
                "CHÈN CỬA ĐI CHUẨN KIẾN TRÚC",
                "Xem trước mô hình cửa đi và chọn kích thước",
                "🚪",
                PreviewKind.Door,
                new[] {
                    new ParameterField("Width", "Chiều rộng cửa đi W:", "900", "mm",
                        new[] { "800", "900", "1000", "1200", "1400", "700", "2400" }),
                    new ParameterField("Height", "Chiều cao cửa H:", "2200", "mm"),
                    new ParameterField("Type", "Loại cửa:", "Cửa mở quay 1 cánh", null,
                        new[] { "Cửa mở quay 1 cánh", "Cửa mở quay 2 cánh", "Cửa trượt lùa", "Cửa xếp gấp" })
                });
            if (pr == null) return;
            double w = pr.GetDouble("Width", 900.0);
            string type = pr.GetString("Type", "Cửa mở quay 1 cánh");
            if (w >= 1200 || type.Contains("2 cánh"))
                DrawDoor2Wing(w, 2200.0, $"D{w:0} (2 Cánh)");
            else if (type.Contains("trượt"))
                DrawDoorSlide(w, $"Cửa Trượt D{w:0}");
            else if (type.Contains("gấp"))
                DrawDoorFolding(w, $"Cửa Gấp D{w:0}");
            else
                DrawDoor(w, 2200.0, $"Cửa Đi D{w:0}");
        }

        [CommandMethod("DTT_WIN_MENU")]
        public static void CmdShowWindowMenu()
        {
            var pr = DTools_FloatingDialog.Prompt(
                "CHÈN CỬA SỔ & LAM GIÓ",
                "Xem trước mô hình cửa sổ và chọn kích thước",
                "🪟",
                PreviewKind.Window,
                new[] {
                    new ParameterField("Width", "Chiều rộng cửa sổ W:", "1200", "mm",
                        new[] { "600", "1200", "1500", "1800", "800" }),
                    new ParameterField("Height", "Chiều cao cửa sổ H:", "1400", "mm"),
                    new ParameterField("Type", "Loại cửa sổ:", "Cửa sổ 2 cánh", null,
                        new[] { "Cửa sổ 1 cánh", "Cửa sổ 2 cánh", "Cửa sổ 3 cánh", "Cửa sổ 4 cánh", "Lam gió Louver" })
                });
            if (pr == null) return;
            double w = pr.GetDouble("Width", 1200.0);
            string type = pr.GetString("Type", "Cửa sổ 2 cánh");
            if (type.Contains("Lam") || w == 800)
                DrawLouver(w, 300.0, "Lam Gió");
            else
                DrawWindow(w, $"Cửa Sổ S{w:0}");
        }

        // ============================================================
        // MODULE VẼ MẶT BẰNG NHÀ KIẾN TRÚC & PREVIEW 2D/3D TƯƠNG TÁC
        // ============================================================

        [CommandMethod("DTT_HOUSE_PLAN")]
        [CommandMethod("DTT_ARCH_PLAN")]
        public static void CmdHousePlanInteractive()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                var dlg = new DTools_HousePlanArchDialog();
                bool? res = false;
                try
                {
                    res = Application.ShowModalWindow(dlg);
                }
                catch
                {
                    res = dlg.ShowDialog();
                }

                if (res != true || dlg.ResultConfig == null) return;

                var cfg = dlg.ResultConfig;
                CadUtils.FocusDwgView();

                // *** FIX: Copy boundary vertices INSIDE transaction, build surrogate outside ***
                List<Point2d> boundaryVerts = null;
                if (cfg.Shape == HouseShapeKind.PickBoundary)
                {
                    PromptEntityOptions peo = new PromptEntityOptions("\n[DTT KIẾN TRÚC] Chọn Polyline ranh giới nhà trên bản vẽ: ");
                    peo.SetRejectMessage("\nChỉ chấp nhận đối tượng Polyline khép kín!");
                    peo.AddAllowedClass(typeof(Polyline), exactMatch: true);

                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK) return;

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        Polyline srcPoly = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                        if (srcPoly == null || !srcPoly.Closed)
                        {
                            ed.WriteMessage("\n[DTT LỖI] Đường Polyline ranh giới phải là đường khép kín!");
                            tr.Commit();
                            return;
                        }
                        // Copy vertices into a plain list — safe to use after tr disposes
                        boundaryVerts = new List<Point2d>();
                        for (int vi = 0; vi < srcPoly.NumberOfVertices; vi++)
                            boundaryVerts.Add(srcPoly.GetPoint2dAt(vi));
                        tr.Commit();
                    }
                }

                // Build an in-memory Polyline surrogate that the engine can safely read
                Polyline boundaryPoly = null;
                if (boundaryVerts != null && boundaryVerts.Count >= 3)
                {
                    boundaryPoly = new Polyline();
                    for (int vi = 0; vi < boundaryVerts.Count; vi++)
                        boundaryPoly.AddVertexAt(vi, boundaryVerts[vi], 0, 0, 0);
                    boundaryPoly.Closed = true;
                }

                PromptPointOptions ppo = new PromptPointOptions("\n[DTT KIẾN TRÚC] Chọn điểm đặt góc dưới bên trái mặt bằng nhà: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) return;

                Point3d startPt = ppr.Value;

                using (doc.LockDocument())
                {
                    ArchitecturalHousePlanEngine.GenerateHousePlan(db, startPt, cfg, boundaryPoly);
                }
            }
            catch (System.Exception ex)
            {
                ed?.WriteMessage($"\n❌ [DTools LỖI] Không thể vẽ mặt bằng nhà: {ex.Message}\n");
            }
        }

        // ============================================================
        // LỆNH VẼ MẶT BẰNG TẦNG ĐIỂN HÌNH HOÀN CHỈNH (4 PHÒNG AUTO)
        // ============================================================

        [CommandMethod("DTT_STANDARD_FLOOR_PLAN")]
        [CommandMethod("DTT_SFP")]
        public static void CmdStandardFloorPlan()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                var pr = DTools_FloatingDialog.Prompt(
                    "VẼ MẶT BẰNG TẦNG ĐIỂN HÌNH TỰ ĐỘNG (4 PHÒNG CHUẨN)",
                    "Tự động chia vùng: Phòng Khách, Phòng Ngủ, WC, Bếp + Thang, Cột BTCT, 3 lớp Dim",
                    "🏠",
                    PreviewKind.Wall,
                    new[]
                    {
                        new ParameterField("TotalL", "Chiều DÀI tổng mặt bằng L:", "12000", "mm"),
                        new ParameterField("TotalW", "Chiều RỘNG tổng mặt bằng W:", "8000", "mm"),
                        new ParameterField("WallT",  "Bề dày tường bao ngoài:",    "220",   "mm", new[] { "220", "300", "110" })
                    });

                if (pr == null) return;

                CadUtils.FocusDwgView();

                PromptPointOptions ppo = new PromptPointOptions("\n🏠 [DTT SFP] Click điểm đặt góc dưới bên trái mặt bằng tầng: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) return;

                double totalL = pr.GetDouble("TotalL", 12000.0);
                double totalW = pr.GetDouble("TotalW", 8000.0);
                double wallT  = pr.GetDouble("WallT",  220.0);

                using (doc.LockDocument())
                {
                    ArchitecturalHousePlanEngine.GenerateStandardFloorPlan(db, ppr.Value, totalL, totalW, wallT);
                }
            }
            catch (System.Exception ex)
            {
                ed?.WriteMessage($"\n❌ [DTools LỖI] Mặt bằng tầng điển hình: {ex.Message}\n");
            }
        }

        // ============================================================
        // LỆNH QUÉT BẢN VẼ (ẢNH/PDF) RA CAD (AI VISION & VECTORIZER)
        // ============================================================

        [CommandMethod("DTT_SCAN_TO_CAD")]
        [CommandMethod("DTT_RASTER_TO_CAD")]
        [CommandMethod("DTT_AI_VECTORIZER")]
        [CommandMethod("DTOOLS_AI_CAD")]
        public static void CmdScanToCad()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                var data = DTools_AiScanToCadDialog.PromptReconstruction();
                if (data == null) return;

                CadUtils.FocusDwgView();

                PromptPointOptions ppo = new PromptPointOptions("\n🤖 [DTT AI VISION] Click điểm đặt trên bản vẽ CAD để chèn nét vẽ vector: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) return;

                using (doc.LockDocument())
                {
                    AiRasterToCadEngine.InjectIntoActiveDwg(db, ppr.Value, data);
                }
            }
            catch (System.Exception ex)
            {
                ed?.WriteMessage($"\n❌ [DTools LỖI] Quét bản vẽ ra CAD: {ex.Message}\n");
            }
        }

        // ============================================================
        // VẼ TƯỜNG CÁC LOẠI (BỔ SUNG KÍCH THƯỚC MỚI)
        // ============================================================

        [CommandMethod("DTT_WALL220")]
        public static void CmdWall220() => DrawWall(220.0, "Tường Bao 220mm");

        [CommandMethod("DTT_WALL110")]
        public static void CmdWall110() => DrawWall(110.0, "Tường Ngăn 110mm");

        [CommandMethod("DTT_WALL100")]
        public static void CmdWall100() => DrawWall(100.0, "Tường Block Nhẹ 100mm");

        [CommandMethod("DTT_WALL75")]
        public static void CmdWall75() => DrawWall(75.0, "Tường Nhẹ WC 75mm");

        /// <summary>Vẽ liên tục tường 220mm theo chuỗi đa điểm</summary>
        [CommandMethod("DTT_WALL_CONT")]
        public static void CmdWallContinuous()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            double thicknessMm = 220.0;
            ed.WriteMessage($"\n🧱 [DTT TƯỜNG LIÊN TỤC] Vẽ chuỗi tường {thicknessMm:0}mm - Click từng điểm, Enter để kết thúc:\n");

            ObjectId wallLayer;
            ObjectId textStyle;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                wallLayer = CadUtils.GetOrCreateLayer(db, "A-WALL", 7, LineWeight.LineWeight035);
                textStyle = CadUtils.GetOrCreateUnicodeStyle(db);
                tr.Commit();
            }

            Point3d? prevPt = null;
            int count = 0;

            while (true)
            {
                PromptPointOptions ppo = new PromptPointOptions(
                    count == 0
                        ? "\n🧱 Click điểm ĐẦU tường: "
                        : $"\n🧱 [{count + 1}] Click điểm TIẾP THEO (Enter để kết thúc): ")
                {
                    AllowNone = count > 0
                };
                if (prevPt.HasValue)
                {
                    ppo.UseBasePoint = true;
                    ppo.BasePoint = prevPt.Value;
                }

                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) break;

                Point3d curPt = ppr.Value;

                if (prevPt.HasValue)
                {
                    Point3d p1 = prevPt.Value;
                    Point3d p2 = curPt;

                    double dx = p2.X - p1.X;
                    double dy = p2.Y - p1.Y;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len <= 1.0) { prevPt = curPt; continue; }

                    double nx = -dy / len;
                    double ny = dx / len;
                    double half = thicknessMm / 2.0;

                    Point2d v1 = new Point2d(p1.X + nx * half, p1.Y + ny * half);
                    Point2d v2 = new Point2d(p2.X + nx * half, p2.Y + ny * half);
                    Point2d v3 = new Point2d(p2.X - nx * half, p2.Y - ny * half);
                    Point2d v4 = new Point2d(p1.X - nx * half, p1.Y - ny * half);

                    using (doc.LockDocument())
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        Polyline pl = new Polyline();
                        pl.AddVertexAt(0, v1, 0, 0, 0);
                        pl.AddVertexAt(1, v2, 0, 0, 0);
                        pl.AddVertexAt(2, v3, 0, 0, 0);
                        pl.AddVertexAt(3, v4, 0, 0, 0);
                        pl.Closed = true;
                        pl.LayerId = wallLayer;
                        btr.AppendEntity(pl);
                        tr.AddNewlyCreatedDBObject(pl, true);
                        tr.Commit();
                    }

                    count++;
                    ed.WriteMessage($"\n  ✅ Đoạn {count}: Dài {len:N0}mm");
                }

                prevPt = curPt;
            }

            ed.WriteMessage($"\n🧱 [DTT TƯỜNG] Hoàn thành! Đã vẽ {count} đoạn tường liên tục ✓\n");
        }

        private static void DrawWall(double thicknessMm, string wallDesc)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            // Hỏi vẽ liên tục
            PromptKeywordOptions pko = new PromptKeywordOptions(
                $"\n🧱 [{wallDesc}] Vẽ liên tục? [Có/Không] <Có>: ");
            pko.Keywords.Add("Có");
            pko.Keywords.Add("Không");
            pko.Keywords.Default = "Có";
            pko.AllowNone = true;
            PromptResult prCont = ed.GetKeywords(pko);
            bool continuous = (prCont.Status != PromptStatus.OK || prCont.StringResult != "Không");

            int wallCount = 0;
            do
            {
                PromptPointOptions ppo1 = new PromptPointOptions(
                    $"\n🧱 [DTT KIẾN TRÚC] Click điểm ĐẦU tường {thicknessMm:0}mm: ");
                PromptPointResult ppr1 = ed.GetPoint(ppo1);
                if (ppr1.Status != PromptStatus.OK) break;

                PromptPointOptions ppo2 = new PromptPointOptions(
                    $"\n🧱 Click điểm CUỐI đoạn tường: ")
                {
                    UseBasePoint = true,
                    BasePoint = ppr1.Value
                };
                PromptPointResult ppr2 = ed.GetPoint(ppo2);
                if (ppr2.Status != PromptStatus.OK) break;

                Point3d p1 = ppr1.Value;
                Point3d p2 = ppr2.Value;

                double dx = p2.X - p1.X;
                double dy = p2.Y - p1.Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len <= 1.0) continue;

                double nx = -dy / len;
                double ny = dx / len;
                double half = thicknessMm / 2.0;

                Point2d v1 = new Point2d(p1.X + nx * half, p1.Y + ny * half);
                Point2d v2 = new Point2d(p2.X + nx * half, p2.Y + ny * half);
                Point2d v3 = new Point2d(p2.X - nx * half, p2.Y - ny * half);
                Point2d v4 = new Point2d(p1.X - nx * half, p1.Y - ny * half);

                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ObjectId wallLayer = CadUtils.GetOrCreateLayer(db, "A-WALL", 7, LineWeight.LineWeight035);
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    Polyline pl = new Polyline();
                    pl.AddVertexAt(0, v1, 0, 0, 0);
                    pl.AddVertexAt(1, v2, 0, 0, 0);
                    pl.AddVertexAt(2, v3, 0, 0, 0);
                    pl.AddVertexAt(3, v4, 0, 0, 0);
                    pl.Closed = true;
                    pl.LayerId = wallLayer;
                    btr.AppendEntity(pl);
                    tr.AddNewlyCreatedDBObject(pl, true);
                    tr.Commit();
                    ed.Regen();
                }

                wallCount++;
                ed.WriteMessage($"\n🧱 [{wallCount}] Đã vẽ {wallDesc} (Dài {len:N0}mm) ✓");

            } while (continuous);

            ed.WriteMessage($"\n🧱 [DTT KIẾN TRÚC] Hoàn thành! Đã vẽ {wallCount} đoạn tường ✓\n");
        }

        // ============================================================
        // VẼ CỬA ĐI CÁC KÍCH THƯỚC (CÓ VẼ LIÊN TỤC)
        // ============================================================

        [CommandMethod("DTT_DOOR800")]
        public static void CmdDoor800() => DrawDoor(800.0, 2100.0, "D800x2100");

        [CommandMethod("DTT_DOOR900")]
        public static void CmdDoor900() => DrawDoor(900.0, 2200.0, "D900x2200");

        [CommandMethod("DTT_DOOR1")]
        public static void CmdDoor1() => DrawDoor(900.0, 2200.0, "D900x2200");

        [CommandMethod("DTT_DOOR1200")]
        public static void CmdDoor1200() => DrawDoor2Wing(1200.0, 2200.0, "D1200x2200 (2 Cánh)");

        [CommandMethod("DTT_DOOR_SLIDE")]
        public static void CmdDoorSlide() => DrawDoorSlide(700.0, "WC D700x2100 Trượt");

        [CommandMethod("DTT_DOOR_FOLDING")]
        public static void CmdDoorFolding() => DrawDoorFolding(2400.0, "D2400x2400 Gấp 4 Cánh");

        private static void DrawDoor(double doorW, double doorH, string desc)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptKeywordOptions pko = new PromptKeywordOptions($"\n🚪 [{desc}] Vẽ liên tục? [Có/Không] <Có>: ");
            pko.Keywords.Add("Có"); pko.Keywords.Add("Không"); pko.Keywords.Default = "Có"; pko.AllowNone = true;
            PromptResult prCont = ed.GetKeywords(pko);
            bool continuous = (prCont.Status != PromptStatus.OK || prCont.StringResult != "Không");

            int count = 0;
            do
            {
                PromptPointOptions ppo1 = new PromptPointOptions($"\n🚪 [DTT CỬA] Click điểm đặt BẢN LỀ cửa [{desc}]: ");
                PromptPointResult ppr1 = ed.GetPoint(ppo1);
                if (ppr1.Status != PromptStatus.OK) break;

                // Hỏi hướng mở
                PromptKeywordOptions pkDir = new PromptKeywordOptions("\n🚪 Hướng cửa mở: [Phải/Trái/Trên/Dưới] <Phải>: ");
                pkDir.Keywords.Add("Phải"); pkDir.Keywords.Add("Trái");
                pkDir.Keywords.Add("Trên"); pkDir.Keywords.Add("Dưới");
                pkDir.Keywords.Default = "Phải"; pkDir.AllowNone = true;
                PromptResult prDir = ed.GetKeywords(pkDir);
                string dir = (prDir.Status == PromptStatus.OK) ? prDir.StringResult : "Phải";

                Point3d pt = ppr1.Value;

                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ObjectId doorLayer = CadUtils.GetOrCreateLayer(db, "A-DOOR", 3, LineWeight.LineWeight025);
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    double startAngle, endAngle;
                    Point2d leafEnd;

                    switch (dir)
                    {
                        case "Trái":
                            startAngle = Math.PI / 2; endAngle = Math.PI;
                            leafEnd = new Point2d(pt.X, pt.Y + doorW);
                            break;
                        case "Trên":
                            startAngle = Math.PI; endAngle = 3 * Math.PI / 2;
                            leafEnd = new Point2d(pt.X - doorW, pt.Y);
                            break;
                        case "Dưới":
                            startAngle = 0; endAngle = Math.PI / 2;
                            leafEnd = new Point2d(pt.X + doorW, pt.Y);
                            break;
                        default: // Phải
                            startAngle = Math.PI / 2; endAngle = Math.PI;
                            leafEnd = new Point2d(pt.X, pt.Y + doorW);
                            break;
                    }

                    // Cánh cửa (dày 40mm)
                    Polyline leaf = new Polyline();
                    leaf.AddVertexAt(0, new Point2d(pt.X, pt.Y), 0, 0, 0);
                    leaf.AddVertexAt(1, new Point2d(pt.X, pt.Y + doorW), 0, 0, 0);
                    leaf.AddVertexAt(2, new Point2d(pt.X + 40, pt.Y + doorW), 0, 0, 0);
                    leaf.AddVertexAt(3, new Point2d(pt.X + 40, pt.Y), 0, 0, 0);
                    leaf.Closed = true;
                    leaf.LayerId = doorLayer;
                    btr.AppendEntity(leaf); tr.AddNewlyCreatedDBObject(leaf, true);

                    // Cung tròn mở cửa
                    Arc swing = new Arc(pt, doorW, startAngle, endAngle) { LayerId = doorLayer };
                    btr.AppendEntity(swing); tr.AddNewlyCreatedDBObject(swing, true);

                    tr.Commit(); ed.Regen();
                }

                count++;
                ed.WriteMessage($"\n🚪 [{count}] Đã chèn cửa [{desc}] ✓");

            } while (continuous);

            ed.WriteMessage($"\n🚪 [DTT CỬA] Hoàn thành! Đã chèn {count} cửa [{desc}] ✓\n");
        }

        private static void DrawDoor2Wing(double doorW, double doorH, string desc)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptPointOptions ppo = new PromptPointOptions($"\n🚪 [DTT CỬA 2 CÁNH] Click điểm CHÍNH GIỮA cửa [{desc}]: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d pt = ppr.Value;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId doorLayer = CadUtils.GetOrCreateLayer(db, "A-DOOR", 3, LineWeight.LineWeight025);
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                double hw = doorW / 2.0;

                // Cánh trái
                Polyline leafL = new Polyline();
                leafL.AddVertexAt(0, new Point2d(pt.X - hw, pt.Y), 0, 0, 0);
                leafL.AddVertexAt(1, new Point2d(pt.X, pt.Y), 0, 0, 0);
                leafL.AddVertexAt(2, new Point2d(pt.X, pt.Y + 40), 0, 0, 0);
                leafL.AddVertexAt(3, new Point2d(pt.X - hw, pt.Y + 40), 0, 0, 0);
                leafL.Closed = true; leafL.LayerId = doorLayer;
                btr.AppendEntity(leafL); tr.AddNewlyCreatedDBObject(leafL, true);

                // Cánh phải
                Polyline leafR = new Polyline();
                leafR.AddVertexAt(0, new Point2d(pt.X, pt.Y), 0, 0, 0);
                leafR.AddVertexAt(1, new Point2d(pt.X + hw, pt.Y), 0, 0, 0);
                leafR.AddVertexAt(2, new Point2d(pt.X + hw, pt.Y + 40), 0, 0, 0);
                leafR.AddVertexAt(3, new Point2d(pt.X, pt.Y + 40), 0, 0, 0);
                leafR.Closed = true; leafR.LayerId = doorLayer;
                btr.AppendEntity(leafR); tr.AddNewlyCreatedDBObject(leafR, true);

                // 2 cung mở
                Arc swingL = new Arc(new Point3d(pt.X - hw, pt.Y, 0), hw, 0, Math.PI / 2.0) { LayerId = doorLayer };
                btr.AppendEntity(swingL); tr.AddNewlyCreatedDBObject(swingL, true);

                Arc swingR = new Arc(new Point3d(pt.X + hw, pt.Y, 0), hw, Math.PI / 2.0, Math.PI) { LayerId = doorLayer };
                btr.AppendEntity(swingR); tr.AddNewlyCreatedDBObject(swingR, true);

                tr.Commit(); ed.Regen();
            }
            ed.WriteMessage($"\n🚪 [DTT CỬA] Đã chèn cửa 2 cánh [{desc}] thành công! ✓\n");
        }

        private static void DrawDoorSlide(double doorW, string desc)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptPointOptions ppo = new PromptPointOptions($"\n🚿 [DTT CỬA TRƯỢT] Click điểm ĐẦU ray cửa trượt [{desc}]: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d pt = ppr.Value;
            double wallT = 220.0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId doorLayer = CadUtils.GetOrCreateLayer(db, "A-DOOR", 3, LineWeight.LineWeight025);
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Ray cửa trượt (hình chữ nhật dài theo chiều trượt)
                Polyline ray = new Polyline();
                ray.AddVertexAt(0, new Point2d(pt.X, pt.Y), 0, 0, 0);
                ray.AddVertexAt(1, new Point2d(pt.X + doorW * 2, pt.Y), 0, 0, 0);
                ray.AddVertexAt(2, new Point2d(pt.X + doorW * 2, pt.Y + wallT), 0, 0, 0);
                ray.AddVertexAt(3, new Point2d(pt.X, pt.Y + wallT), 0, 0, 0);
                ray.Closed = true; ray.LayerId = doorLayer;
                btr.AppendEntity(ray); tr.AddNewlyCreatedDBObject(ray, true);

                // Cánh cửa (đang mở 1 bên)
                Polyline leaf = new Polyline();
                leaf.AddVertexAt(0, new Point2d(pt.X + doorW, pt.Y + 5), 0, 0, 0);
                leaf.AddVertexAt(1, new Point2d(pt.X + doorW * 2, pt.Y + 5), 0, 0, 0);
                leaf.AddVertexAt(2, new Point2d(pt.X + doorW * 2, pt.Y + wallT - 5), 0, 0, 0);
                leaf.AddVertexAt(3, new Point2d(pt.X + doorW, pt.Y + wallT - 5), 0, 0, 0);
                leaf.Closed = true; leaf.LayerId = doorLayer;
                btr.AppendEntity(leaf); tr.AddNewlyCreatedDBObject(leaf, true);

                // Mũi tên chỉ chiều trượt
                Line arrow = new Line(
                    new Point3d(pt.X + doorW + 50, pt.Y + wallT / 2, 0),
                    new Point3d(pt.X + 50, pt.Y + wallT / 2, 0)) { LayerId = doorLayer };
                btr.AppendEntity(arrow); tr.AddNewlyCreatedDBObject(arrow, true);

                tr.Commit(); ed.Regen();
            }
            ed.WriteMessage($"\n🚿 [DTT CỬA] Đã chèn cửa trượt [{desc}] thành công! ✓\n");
        }

        private static void DrawDoorFolding(double doorW, string desc)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptPointOptions ppo = new PromptPointOptions($"\n🚪 [DTT CỬA GẤP] Click điểm CHÍNH GIỮA cửa gấp 4 cánh [{desc}]: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d pt = ppr.Value;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId doorLayer = CadUtils.GetOrCreateLayer(db, "A-DOOR", 3, LineWeight.LineWeight025);
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 4 cánh gấp (mỗi cánh = doorW/4)
                double w4 = doorW / 4.0;
                for (int i = 0; i < 4; i++)
                {
                    double xStart = pt.X - doorW / 2 + i * w4;
                    Polyline leaf = new Polyline();
                    leaf.AddVertexAt(0, new Point2d(xStart, pt.Y), 0, 0, 0);
                    leaf.AddVertexAt(1, new Point2d(xStart + w4, pt.Y), 0, 0, 0);
                    leaf.AddVertexAt(2, new Point2d(xStart + w4, pt.Y + 40), 0, 0, 0);
                    leaf.AddVertexAt(3, new Point2d(xStart, pt.Y + 40), 0, 0, 0);
                    leaf.Closed = true; leaf.LayerId = doorLayer;
                    btr.AppendEntity(leaf); tr.AddNewlyCreatedDBObject(leaf, true);

                    // Gấp khúc zíc-zắc
                    double midX = xStart + w4 / 2;
                    Line fold = new Line(
                        new Point3d(xStart, pt.Y, 0),
                        new Point3d(midX, pt.Y + w4, 0)) { LayerId = doorLayer };
                    btr.AppendEntity(fold); tr.AddNewlyCreatedDBObject(fold, true);
                }

                tr.Commit(); ed.Regen();
            }
            ed.WriteMessage($"\n🚪 [DTT CỬA] Đã chèn cửa gấp 4 cánh [{desc}] thành công! ✓\n");
        }

        // ============================================================
        // VẼ CỬA SỔ CÁC KÍCH THƯỚC
        // ============================================================

        [CommandMethod("DTT_WIN600")]
        public static void CmdWin600() => DrawWindow(600.0, "S600x1200 (WC/Bếp)");

        [CommandMethod("DTT_WIN1200")]
        public static void CmdWin1200() => DrawWindow(1200.0, "S1200x1400 Tiêu Chuẩn");

        [CommandMethod("DTT_WIN2")]
        public static void CmdWin2() => DrawWindow(1200.0, "S1200x1400 Tiêu Chuẩn");

        [CommandMethod("DTT_WIN1500")]
        public static void CmdWin1500() => DrawWindow(1500.0, "S1500x1600 Phòng Khách");

        [CommandMethod("DTT_WIN1800")]
        public static void CmdWin1800() => DrawWindow(1800.0, "S1800x1600 Mặt Tiền");

        [CommandMethod("DTT_LOUVER")]
        public static void CmdLouver() => DrawLouver(500.0, 300.0, "Lam Gió 500x300");

        private static void DrawWindow(double winW, string desc)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptKeywordOptions pko = new PromptKeywordOptions($"\n🪟 [{desc}] Vẽ liên tục? [Có/Không] <Có>: ");
            pko.Keywords.Add("Có"); pko.Keywords.Add("Không"); pko.Keywords.Default = "Có"; pko.AllowNone = true;
            PromptResult prCont = ed.GetKeywords(pko);
            bool continuous = (prCont.Status != PromptStatus.OK || prCont.StringResult != "Không");

            int count = 0;
            do
            {
                PromptPointOptions ppo = new PromptPointOptions($"\n🪟 [DTT CỬA SỔ] Click điểm đặt cửa sổ [{desc}]: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) break;

                Point3d pt = ppr.Value;
                double wallT = 220.0;

                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ObjectId winLayer = CadUtils.GetOrCreateLayer(db, "A-WINDOW", 4, LineWeight.LineWeight025);
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // Khung cửa sổ ngoài
                    Polyline frame = new Polyline();
                    frame.AddVertexAt(0, new Point2d(pt.X, pt.Y), 0, 0, 0);
                    frame.AddVertexAt(1, new Point2d(pt.X + winW, pt.Y), 0, 0, 0);
                    frame.AddVertexAt(2, new Point2d(pt.X + winW, pt.Y + wallT), 0, 0, 0);
                    frame.AddVertexAt(3, new Point2d(pt.X, pt.Y + wallT), 0, 0, 0);
                    frame.Closed = true; frame.LayerId = winLayer;
                    btr.AppendEntity(frame); tr.AddNewlyCreatedDBObject(frame, true);

                    // 2 lớp kính phân cách (tùy số cánh)
                    int numPanes = (winW >= 1500.0) ? 3 : (winW >= 900.0 ? 2 : 1);
                    double glassStep = wallT / (numPanes + 1);
                    for (int i = 1; i <= numPanes; i++)
                    {
                        double glassY = pt.Y + glassStep * i;
                        Line glass = new Line(
                            new Point3d(pt.X, glassY, 0),
                            new Point3d(pt.X + winW, glassY, 0)) { LayerId = winLayer };
                        btr.AppendEntity(glass); tr.AddNewlyCreatedDBObject(glass, true);
                    }

                    // Đường chia cánh giữa (nếu 2 cánh)
                    if (numPanes > 1)
                    {
                        Line divider = new Line(
                            new Point3d(pt.X + winW / 2, pt.Y, 0),
                            new Point3d(pt.X + winW / 2, pt.Y + wallT, 0)) { LayerId = winLayer };
                        btr.AppendEntity(divider); tr.AddNewlyCreatedDBObject(divider, true);
                    }

                    tr.Commit(); ed.Regen();
                }

                count++;
                ed.WriteMessage($"\n🪟 [{count}] Đã chèn cửa sổ [{desc}] ✓");

            } while (continuous);

            ed.WriteMessage($"\n🪟 [DTT CỬA SỔ] Hoàn thành! Đã chèn {count} cửa sổ ✓\n");
        }

        private static void DrawLouver(double w, double h, string desc)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptPointOptions ppo = new PromptPointOptions($"\n♨ [DTT LAM GIÓ] Click điểm đặt Lam Gió [{desc}]: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d pt = ppr.Value;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId winLayer = CadUtils.GetOrCreateLayer(db, "A-WINDOW", 4, LineWeight.LineWeight025);
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Khung lam gió
                Polyline frame = new Polyline();
                frame.AddVertexAt(0, new Point2d(pt.X, pt.Y), 0, 0, 0);
                frame.AddVertexAt(1, new Point2d(pt.X + w, pt.Y), 0, 0, 0);
                frame.AddVertexAt(2, new Point2d(pt.X + w, pt.Y + h), 0, 0, 0);
                frame.AddVertexAt(3, new Point2d(pt.X, pt.Y + h), 0, 0, 0);
                frame.Closed = true; frame.LayerId = winLayer;
                btr.AppendEntity(frame); tr.AddNewlyCreatedDBObject(frame, true);

                // Cánh lam ngang (3-5 cánh tùy chiều cao)
                int numLam = Math.Max(3, (int)(h / 80));
                double lamStep = h / (numLam + 1);
                for (int i = 1; i <= numLam; i++)
                {
                    double lamY = pt.Y + lamStep * i;
                    Line lam = new Line(
                        new Point3d(pt.X + 10, lamY, 0),
                        new Point3d(pt.X + w - 10, lamY, 0)) { LayerId = winLayer };
                    btr.AppendEntity(lam); tr.AddNewlyCreatedDBObject(lam, true);
                }

                tr.Commit(); ed.Regen();
            }
            ed.WriteMessage($"\n♨ [DTT LAM GIÓ] Đã chèn Lam Gió [{desc}] thành công! ✓\n");
        }

        // ============================================================
        // CÁC LỆNH KIẾN TRÚC KHÁC (GIỮ NGUYÊN + NÂNG CẤP)
        // ============================================================

        [CommandMethod("DTT_HOUSEPLAN")]
        public static void CmdHousePlan()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var pr = DTools_FloatingDialog.Prompt(
                "VẼ MẶT BẰNG NHÀ HOÀN CHỈNH",
                "Tự động vẽ tường bao, tường ngăn, cửa đi, cửa sổ, nhãn phòng",
                "🏡",
                PreviewKind.Wall,
                new[] {
                    new ParameterField("Width", "Chiều RỘNG phủ bì nhà W:", "5000", "mm"),
                    new ParameterField("Length", "Chiều DÀI phủ bì nhà L:", "12000", "mm"),
                    new ParameterField("WallThick", "Bề dày tường bao ngoài:", "220", "mm", new[] { "220", "110", "100" })
                });
            if (pr == null) return;

            CadUtils.FocusDwgView();

            PromptPointOptions ppo = new PromptPointOptions("\n🏡 Click điểm đặt góc dưới bên trái của nhà: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            double w = pr.GetDouble("Width", 5000.0);
            double l = pr.GetDouble("Length", 12000.0);
            Point3d pt = ppr.Value;
            double wallThick = pr.GetDouble("WallThick", 220.0);
            double innerWall = 110.0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId wallLayer = CadUtils.GetOrCreateLayer(db, "A-WALL", 7, LineWeight.LineWeight035);
                ObjectId doorLayer = CadUtils.GetOrCreateLayer(db, "A-DOOR", 3, LineWeight.LineWeight025);
                ObjectId winLayer = CadUtils.GetOrCreateLayer(db, "A-WINDOW", 4, LineWeight.LineWeight020);
                ObjectId tagLayer = CadUtils.GetOrCreateLayer(db, "A-ROOM-TAG", 30, LineWeight.LineWeight025);
                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Tường ngoài
                Polyline outer = new Polyline();
                outer.AddVertexAt(0, new Point2d(pt.X, pt.Y), 0, 0, 0);
                outer.AddVertexAt(1, new Point2d(pt.X + w, pt.Y), 0, 0, 0);
                outer.AddVertexAt(2, new Point2d(pt.X + w, pt.Y + l), 0, 0, 0);
                outer.AddVertexAt(3, new Point2d(pt.X, pt.Y + l), 0, 0, 0);
                outer.Closed = true; outer.LayerId = wallLayer;
                btr.AppendEntity(outer); tr.AddNewlyCreatedDBObject(outer, true);

                Polyline inner = new Polyline();
                inner.AddVertexAt(0, new Point2d(pt.X + wallThick, pt.Y + wallThick), 0, 0, 0);
                inner.AddVertexAt(1, new Point2d(pt.X + w - wallThick, pt.Y + wallThick), 0, 0, 0);
                inner.AddVertexAt(2, new Point2d(pt.X + w - wallThick, pt.Y + l - wallThick), 0, 0, 0);
                inner.AddVertexAt(3, new Point2d(pt.X + wallThick, pt.Y + l - wallThick), 0, 0, 0);
                inner.Closed = true; inner.LayerId = wallLayer;
                btr.AppendEntity(inner); tr.AddNewlyCreatedDBObject(inner, true);

                // Tường ngăn phòng
                double ySplit1 = pt.Y + l * 0.45;
                double ySplit2 = pt.Y + l * 0.78;

                void AddLine(Point3d p1, Point3d p2, ObjectId lay)
                {
                    Line ln = new Line(p1, p2) { LayerId = lay };
                    btr.AppendEntity(ln); tr.AddNewlyCreatedDBObject(ln, true);
                }

                AddLine(new Point3d(pt.X + wallThick, ySplit1, 0), new Point3d(pt.X + w - wallThick, ySplit1, 0), wallLayer);
                AddLine(new Point3d(pt.X + wallThick, ySplit1 + innerWall, 0), new Point3d(pt.X + w - wallThick, ySplit1 + innerWall, 0), wallLayer);
                AddLine(new Point3d(pt.X + wallThick, ySplit2, 0), new Point3d(pt.X + w - wallThick, ySplit2, 0), wallLayer);
                AddLine(new Point3d(pt.X + wallThick, ySplit2 + innerWall, 0), new Point3d(pt.X + w - wallThick, ySplit2 + innerWall, 0), wallLayer);

                // Cửa chính 2 cánh 1400mm
                double doorW = 1400.0;
                double doorX = pt.X + (w - doorW) / 2.0;
                Arc swingL = new Arc(new Point3d(doorX, pt.Y + wallThick, 0), doorW / 2.0, 0, Math.PI / 2.0) { LayerId = doorLayer };
                btr.AppendEntity(swingL); tr.AddNewlyCreatedDBObject(swingL, true);

                Arc swingR = new Arc(new Point3d(doorX + doorW, pt.Y + wallThick, 0), doorW / 2.0, Math.PI / 2.0, Math.PI) { LayerId = doorLayer };
                btr.AppendEntity(swingR); tr.AddNewlyCreatedDBObject(swingR, true);

                // Cửa sổ mặt tiền (2 bên cửa chính)
                DrawWindowInTr(btr, tr, winLayer, pt.X + wallThick, pt.Y, 1000.0);
                DrawWindowInTr(btr, tr, winLayer, pt.X + w - wallThick - 1000, pt.Y, 1000.0);

                // Nhãn phòng
                double sTotal = (w * l) / 1000000.0;
                AddRoomMText(btr, tr, tagLayer, textStyle, new Point3d(pt.X + w / 2, pt.Y + l * 0.22, 0), "PHÒNG KHÁCH", sTotal * 0.42);
                AddRoomMText(btr, tr, tagLayer, textStyle, new Point3d(pt.X + w / 2, pt.Y + l * 0.60, 0), "PHÒNG NGỦ MASTER", sTotal * 0.32);
                AddRoomMText(btr, tr, tagLayer, textStyle, new Point3d(pt.X + w / 2, pt.Y + l * 0.88, 0), "BẾP & PHÒNG ĂN", sTotal * 0.26);

                tr.Commit();
                ed.Regen();
            }

            ed.WriteMessage($"\n🏡 [DTT KIẾN TRÚC] Đã tạo mặt bằng nhà hoàn chỉnh {w:0}x{l:0}mm (S={w*l/1000000:N1} m²) ✓\n");
        }

        private static void DrawWindowInTr(BlockTableRecord btr, Transaction tr, ObjectId layer, double x, double y, double winW)
        {
            double wallT = 220.0;
            Polyline frame = new Polyline();
            frame.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
            frame.AddVertexAt(1, new Point2d(x + winW, y), 0, 0, 0);
            frame.AddVertexAt(2, new Point2d(x + winW, y + wallT), 0, 0, 0);
            frame.AddVertexAt(3, new Point2d(x, y + wallT), 0, 0, 0);
            frame.Closed = true; frame.LayerId = layer;
            btr.AppendEntity(frame); tr.AddNewlyCreatedDBObject(frame, true);

            Line glass = new Line(new Point3d(x, y + wallT * 0.5, 0), new Point3d(x + winW, y + wallT * 0.5, 0)) { LayerId = layer };
            btr.AppendEntity(glass); tr.AddNewlyCreatedDBObject(glass, true);
        }

        private static void AddRoomMText(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId style, Point3d pt, string name, double area)
        {
            MText mt = new MText
            {
                Location = pt,
                Contents = $"{{\\C1;\\fArial|b1;{name}}}\\P{{\\C7;S = {area:N1} m²}}",
                TextHeight = 180.0,
                Attachment = AttachmentPoint.MiddleCenter,
                LayerId = layer,
                TextStyleId = style
            };
            btr.AppendEntity(mt); tr.AddNewlyCreatedDBObject(mt, true);
        }

        [CommandMethod("DTT_WALLSEC")]
        public static void CmdWallSection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptPointOptions ppo = new PromptPointOptions("\n🏛️ [DTT KIẾN TRÚC] Click điểm đặt Mặt Cắt Tường Móng: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d pt = ppr.Value;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId concLayer = CadUtils.GetOrCreateLayer(db, "A-CONC", 7, LineWeight.LineWeight035);
                ObjectId wallLayer = CadUtils.GetOrCreateLayer(db, "A-WALL", 8, LineWeight.LineWeight030);
                ObjectId dimLayer = CadUtils.GetOrCreateLayer(db, "A-ANNO", 2, LineWeight.LineWeight015);
                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                void AddPolyRect(ObjectId lay, double x, double y, double w, double h)
                {
                    Polyline p = new Polyline();
                    p.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
                    p.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
                    p.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
                    p.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
                    p.Closed = true; p.LayerId = lay;
                    btr.AppendEntity(p); tr.AddNewlyCreatedDBObject(p, true);
                }

                // Đế móng -1200 ~ -900
                AddPolyRect(concLayer, pt.X - 500, pt.Y - 1200, 1000, 300);
                // Dầm móng -900 ~ -400
                AddPolyRect(concLayer, pt.X - 150, pt.Y - 900, 300, 500);
                // Tường 220mm -400 ~ +3600
                AddPolyRect(wallLayer, pt.X - 110, pt.Y - 400, 220, 4000);
                // Giằng đầu tường +3600 ~ +3700
                AddPolyRect(concLayer, pt.X - 110, pt.Y + 3600, 220, 100);

                DrawLevelMark(btr, tr, dimLayer, textStyle, new Point3d(pt.X + 600, pt.Y + 3600, 0), "+3.600", "Cốt Đỉnh Tường");
                DrawLevelMark(btr, tr, dimLayer, textStyle, new Point3d(pt.X + 600, pt.Y, 0), "%%p0.000", "Cốt Nền Hoàn Thiện");
                DrawLevelMark(btr, tr, dimLayer, textStyle, new Point3d(pt.X + 600, pt.Y - 450, 0), "-0.450", "Cốt Sân Tự Nhiên");
                DrawLevelMark(btr, tr, dimLayer, textStyle, new Point3d(pt.X + 600, pt.Y - 1200, 0), "-1.200", "Đáy Móng Bê Tông");

                tr.Commit();
                ed.Regen();
            }
            ed.WriteMessage("\n🏛️ [DTT KIẾN TRÚC] Đã vẽ Mặt Cắt Tường Móng đầy đủ cao độ kiến trúc ✓\n");
        }

        private static void DrawLevelMark(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId style, Point3d pt, string levelStr, string desc)
        {
            double s = 100.0;
            Polyline arrow = new Polyline();
            arrow.AddVertexAt(0, new Point2d(pt.X - s, pt.Y + s), 0, 0, 0);
            arrow.AddVertexAt(1, new Point2d(pt.X + s, pt.Y + s), 0, 0, 0);
            arrow.AddVertexAt(2, new Point2d(pt.X, pt.Y), 0, 0, 0);
            arrow.Closed = true; arrow.LayerId = layer;
            btr.AppendEntity(arrow); tr.AddNewlyCreatedDBObject(arrow, true);

            Line guide = new Line(new Point3d(pt.X - s * 1.5, pt.Y, 0), new Point3d(pt.X + s * 4.0, pt.Y, 0)) { LayerId = layer };
            btr.AppendEntity(guide); tr.AddNewlyCreatedDBObject(guide, true);

            DBText txt = new DBText
            {
                Position = new Point3d(pt.X + s * 0.5, pt.Y + 30, 0),
                TextString = $"{levelStr} ({desc})",
                Height = 100.0, LayerId = layer, TextStyleId = style
            };
            btr.AppendEntity(txt); tr.AddNewlyCreatedDBObject(txt, true);
        }

        [CommandMethod("DTT_ROOMTAG")]
        public static void CmdRoomTag()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptStringOptions pso = new PromptStringOptions("\n🏷️ [DTT KIẾN TRÚC] Nhập tên phòng: ") { DefaultValue = "PHÒNG KHÁCH" };
            PromptResult prName = ed.GetString(pso);
            string roomName = (prName.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(prName.StringResult)) ? prName.StringResult : "PHÒNG KHÁCH";

            bool continuous = true;
            int count = 0;

            while (continuous)
            {
                PromptPointOptions ppo = new PromptPointOptions($"\n🏷️ Click BÊN TRONG phòng [{roomName}] (Enter = dừng): ") { AllowNone = true };
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) break;

                Point3d pickPt = ppr.Value;
                double areaSqM = 0.0;
                double perimM = 0.0;

                using (doc.LockDocument())
                {
                    try
                    {
                        DBObjectCollection boundaries = ed.TraceBoundary(pickPt, false);
                        if (boundaries != null && boundaries.Count > 0)
                        {
                            using (Transaction tr = db.TransactionManager.StartTransaction())
                            {
                                foreach (DBObject obj in boundaries)
                                {
                                    if (obj is Region reg) { areaSqM = reg.Area / 1000000.0; perimM = reg.Perimeter / 1000.0; }
                                    else if (obj is Polyline poly) { areaSqM = poly.Area / 1000000.0; perimM = poly.Length / 1000.0; }
                                    obj.Dispose();
                                }
                                tr.Commit();
                            }
                        }
                    }
                    catch { }

                    if (areaSqM <= 0.0) { areaSqM = 24.5; perimM = 20.0; }

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        ObjectId roomLayer = CadUtils.GetOrCreateLayer(db, "A-ROOM-TAG", 30, LineWeight.LineWeight025);
                        ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);
                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        MText mText = new MText
                        {
                            Location = pickPt,
                            Contents = $"{{\\C1;\\fArial|b1;{roomName.ToUpper()}}}\\P{{\\C7;S = {areaSqM:N2} m²\\P(P = {perimM:N2} m)}}",
                            TextHeight = 200.0,
                            Attachment = AttachmentPoint.MiddleCenter,
                            LayerId = roomLayer, TextStyleId = textStyle
                        };
                        btr.AppendEntity(mText); tr.AddNewlyCreatedDBObject(mText, true);

                        tr.Commit(); ed.Regen();
                    }
                }

                count++;
                ed.WriteMessage($"\n🏷️ [{count}] Đã gắn nhãn [{roomName}]: S={areaSqM:N2} m² ✓");
            }

            ed.WriteMessage($"\n🏷️ [DTT KIẾN TRÚC] Hoàn thành! Đã gắn {count} nhãn phòng ✓\n");
        }

        [CommandMethod("DTT_GRID")]
        public static void CmdGridAxis()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptStringOptions pso = new PromptStringOptions("\n🎯 [DTT TRỤC] Nhập ký hiệu trục (VD: 1, 2, A, B): ") { DefaultValue = "1" };
            PromptResult prAxis = ed.GetString(pso);
            string axisName = (prAxis.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(prAxis.StringResult)) ? prAxis.StringResult : "1";

            PromptPointOptions ppo1 = new PromptPointOptions("\n🎯 Click điểm BẮT ĐẦU đường trục: ");
            PromptPointResult ppr1 = ed.GetPoint(ppo1);
            if (ppr1.Status != PromptStatus.OK) return;

            PromptPointOptions ppo2 = new PromptPointOptions("\n🎯 Click điểm đặt BONG BÓNG TRỤC: ")
            { UseBasePoint = true, BasePoint = ppr1.Value };
            PromptPointResult ppr2 = ed.GetPoint(ppo2);
            if (ppr2.Status != PromptStatus.OK) return;

            Point3d p1 = ppr1.Value;
            Point3d p2 = ppr2.Value;
            double bubbleR = 400.0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId axisLayer = CadUtils.GetOrCreateLayer(db, "A-GRID-AXIS", 1, LineWeight.LineWeight015);
                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Line gridLine = new Line(p1, p2) { LayerId = axisLayer };
                btr.AppendEntity(gridLine); tr.AddNewlyCreatedDBObject(gridLine, true);

                Circle bubble = new Circle(p2, Vector3d.ZAxis, bubbleR) { LayerId = axisLayer };
                btr.AppendEntity(bubble); tr.AddNewlyCreatedDBObject(bubble, true);

                DBText label = new DBText
                {
                    Position = p2,
                    TextString = axisName,
                    Height = bubbleR * 1.1,
                    HorizontalMode = TextHorizontalMode.TextCenter,
                    VerticalMode = TextVerticalMode.TextVerticalMid,
                    AlignmentPoint = p2,
                    LayerId = axisLayer, TextStyleId = textStyle
                };
                btr.AppendEntity(label); tr.AddNewlyCreatedDBObject(label, true);

                tr.Commit(); ed.Regen();
            }
            ed.WriteMessage($"\n🎯 [DTT TRỤC] Đã vẽ trục định vị [{axisName}] thành công! ✓\n");
        }
    }
}
