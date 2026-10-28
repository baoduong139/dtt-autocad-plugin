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
    public static class ElectricalTools
    {
        // ============================================================
        // LỆNH MỞ MENU POPUP THIẾT BỊ ĐIỆN PRO
        // ============================================================

        [CommandMethod("DTT_ELEC_MENU")]
        public static void CmdShowElecMenu()
        {
            try
            {
                UI.DTT_QuickMenuDialog.ShowElecDeviceMenu();
            }
            catch (System.Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    $"\n[DTools LỖI] Không thể mở thư viện điện: {ex.Message}\n");
            }
        }

        // ============================================================
        // VẼ CỤM DÂY 3 PHA CHUYÊN NGHIỆP (FIX 100% LỖI AEMULTIWIRE)
        // ============================================================

        [CommandMethod("DTT_ELEC_3PHASE")]
        public static void CmdDrawThreePhaseWire()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var pr = DTools_FloatingDialog.Prompt(
                "VẼ CỤM DÂY 3 PHA CHUYÊN NGHIỆP",
                "Tuyến cáp nguồn động lực & chiếu sáng 3 pha chuẩn TCVN / IEC",
                "⚡⚡⚡",
                PreviewKind.ThreePhaseWire,
                new[] {
                    new ParameterField("Mode", "Kiểu thể hiện tuyến dây:", "1 - Cáp vạch pha chuẩn (3P+N+PE)", null,
                        new[] {
                            "1 - Cáp vạch pha chuẩn (3P+N+PE)",
                            "2 - Ba dây song song (L1-L2-L3)",
                            "3 - Bốn dây song song (3P+N)",
                            "4 - Năm dây song song (3P+N+PE)"
                        }),
                    new ParameterField("Section", "Quy cách / Tiết diện cáp:", "4x6+1x4 mm²"),
                    new ParameterField("Spacing", "Khoảng cách giữa các dây song song:", "60", "mm"),
                    new ParameterField("Continuous", "Vẽ liên tục nhiều đoạn:", "Có", null, new[] { "Có", "Không" })
                });

            if (pr == null) return;

            CadUtils.FocusDwgView();

            string mode = pr.GetString("Mode", "1");
            string section = pr.GetString("Section", "4x6+1x4 mm²");
            double spacing = pr.GetDouble("Spacing", 60.0);
            bool continuous = pr.GetBool("Continuous", true);

            PromptPointOptions ppo1 = new PromptPointOptions("\n⚡ [DTools 3 PHA] Click điểm BẮT ĐẦU tuyến dây 3 pha: ");
            PromptPointResult ppr1 = ed.GetPoint(ppo1);
            if (ppr1.Status != PromptStatus.OK) return;

            Point3d ptStart = ppr1.Value;

            while (true)
            {
                PromptPointOptions ppo2 = new PromptPointOptions("\n⚡ Click điểm TIẾP THEO của tuyến dây 3 pha (Enter/ESC kết thúc): ")
                {
                    UseBasePoint = true,
                    BasePoint = ptStart
                };
                PromptPointResult ppr2 = ed.GetPoint(ppo2);
                if (ppr2.Status != PromptStatus.OK) break;

                Point3d ptEnd = ppr2.Value;
                if (ptStart.DistanceTo(ptEnd) < 5.0) continue;

                DrawThreePhaseSegment(doc, db, ptStart, ptEnd, mode, section, spacing);

                if (!continuous) break;
                ptStart = ptEnd;
            }

            ed.WriteMessage("\n✅ [DTools Pro] Hoàn tất vẽ cụm dây 3 pha!\n");
        }

        private static void DrawThreePhaseSegment(Document doc, Database db, Point3d p1, Point3d p2, string mode, string section, double spacing)
        {
            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Vector3d dir = (p2 - p1).GetNormal();
                Vector3d norm = new Vector3d(-dir.Y, dir.X, 0).GetNormal(); // Pháp tuyến vuông góc

                if (mode.Contains("2") || mode.Contains("3") || mode.Contains("4") || mode.Contains("Song Song") || mode.Contains("Dây"))
                {
                    // 1. VẼ CÁC ĐƯỜNG DÂY SONG SONG TÁCH RỜI
                    int wireCount = 3;
                    if (mode.Contains("4") || mode.Contains("Bốn")) wireCount = 4;
                    if (mode.Contains("5") || mode.Contains("Năm")) wireCount = 5;

                    short[] colors = new short[] { 1, 2, 3, 5, 82 }; // 1=Đỏ(L1), 2=Vàng(L2), 3=Xanh lá(L3), 5=Xanh dương(N), 82=Vàng xanh(PE)
                    string[] layers = new string[] { "E-WIRE-L1", "E-WIRE-L2", "E-WIRE-L3", "E-WIRE-N", "E-WIRE-PE" };
                    string[] tags = new string[] { "L1", "L2", "L3", "N", "PE" };

                    double totalOffset = (wireCount - 1) * spacing;
                    Point3d base1 = p1 - norm * (totalOffset / 2.0);
                    Point3d base2 = p2 - norm * (totalOffset / 2.0);

                    for (int i = 0; i < wireCount; i++)
                    {
                        Point3d wStart = base1 + norm * (i * spacing);
                        Point3d wEnd = base2 + norm * (i * spacing);

                        ObjectId wireLayer = CadUtils.GetOrCreateLayer(db, layers[i], colors[i], LineWeight.LineWeight035);

                        Line line = new Line(wStart, wEnd) { LayerId = wireLayer };
                        btr.AppendEntity(line);
                        tr.AddNewlyCreatedDBObject(line, true);

                        // Nhãn pha ở đầu tuyến dây
                        DBText txt = new DBText
                        {
                            Position = wStart + norm * 8,
                            TextString = tags[i],
                            Height = 22.0,
                            LayerId = wireLayer,
                            TextStyleId = textStyle
                        };
                        btr.AppendEntity(txt);
                        tr.AddNewlyCreatedDBObject(txt, true);
                    }
                }
                else
                {
                    // 2. VẼ 1 CÁP CHÍNH KÈM VẠCH KÝ HIỆU 3P+N+PE
                    ObjectId mainLayer = CadUtils.GetOrCreateLayer(db, "E-WIRE-POWER", 4, LineWeight.LineWeight035);
                    ObjectId textLayer = CadUtils.GetOrCreateLayer(db, "E-TEXT", 2, LineWeight.LineWeight015);

                    Line cable = new Line(p1, p2) { LayerId = mainLayer };
                    btr.AppendEntity(cable);
                    tr.AddNewlyCreatedDBObject(cable, true);

                    // Điểm chính giữa đoạn dây
                    Point3d mid = p1 + (p2 - p1) * 0.5;
                    double tickLen = 35.0;

                    // 3 vạch pha xiên /// (L1, L2, L3)
                    for (int i = -1; i <= 1; i++)
                    {
                        Point3d tickBase = mid + dir * (i * 20.0);
                        Point3d t1 = tickBase - norm * (tickLen * 0.5) - dir * 10;
                        Point3d t2 = tickBase + norm * (tickLen * 0.5) + dir * 10;

                        Line tick = new Line(t1, t2) { LayerId = mainLayer, Color = Color.FromColorIndex(ColorMethod.ByAci, 1) };
                        btr.AppendEntity(tick);
                        tr.AddNewlyCreatedDBObject(tick, true);
                    }

                    // 1 vạch trung tính N (có chấm tròn)
                    Point3d nBase = mid + dir * 40.0;
                    Point3d nt1 = nBase - norm * (tickLen * 0.5) - dir * 10;
                    Point3d nt2 = nBase + norm * (tickLen * 0.5) + dir * 10;
                    Line nTick = new Line(nt1, nt2) { LayerId = mainLayer, Color = Color.FromColorIndex(ColorMethod.ByAci, 5) };
                    btr.AppendEntity(nTick);
                    tr.AddNewlyCreatedDBObject(nTick, true);

                    Circle nDot = new Circle(nt2, Vector3d.ZAxis, 6.0) { LayerId = mainLayer, Color = Color.FromColorIndex(ColorMethod.ByAci, 5) };
                    btr.AppendEntity(nDot);
                    tr.AddNewlyCreatedDBObject(nDot, true);

                    // 1 vạch tiếp địa PE (có gạch chéo)
                    Point3d peBase = mid + dir * 60.0;
                    Point3d pet1 = peBase - norm * (tickLen * 0.5) - dir * 10;
                    Point3d pet2 = peBase + norm * (tickLen * 0.5) + dir * 10;
                    Line peTick = new Line(pet1, pet2) { LayerId = mainLayer, Color = Color.FromColorIndex(ColorMethod.ByAci, 3) };
                    btr.AppendEntity(peTick);
                    tr.AddNewlyCreatedDBObject(peTick, true);

                    Line peCross = new Line(pet2 - dir * 6, pet2 + dir * 6 + norm * 6) { LayerId = mainLayer, Color = Color.FromColorIndex(ColorMethod.ByAci, 3) };
                    btr.AppendEntity(peCross);
                    tr.AddNewlyCreatedDBObject(peCross, true);

                    // Ghi chú tiết diện cáp
                    DBText secText = new DBText
                    {
                        Position = mid + norm * (tickLen * 0.7),
                        TextString = $"3P+N+PE ({section})",
                        Height = 25.0,
                        LayerId = textLayer,
                        TextStyleId = textStyle
                    };
                    btr.AppendEntity(secText);
                    tr.AddNewlyCreatedDBObject(secText, true);
                }

                tr.Commit();
            }
        }

        // ============================================================
        // VẼ DÂY ĐIỆN ĐƠN (NATIVE CHAY THAY THẾ AEWIRE)
        // ============================================================

        [CommandMethod("DTT_ELEC_WIRE")]
        public static void CmdDrawSingleWire()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var pr = DTools_FloatingDialog.Prompt(
                "VẼ DÂY ĐIỆN CHIẾU SÁNG & ĐỘNG LỰC",
                "Đường dây đơn pha L / N / PE tiêu chuẩn",
                "⚡",
                PreviewKind.Generic,
                new[] {
                    new ParameterField("Type", "Loại dây:", "Pha L (Đỏ/Vàng/Xanh)", null,
                        new[] { "Pha L (Đỏ)", "Trung Tính N (Xanh dương)", "Tiếp Địa PE (Vàng xanh)", "Dây Điều Khiển (Cam)" }),
                    new ParameterField("Section", "Tiết diện dây:", "1x2.5 mm²"),
                    new ParameterField("Continuous", "Chế độ vẽ liên tục:", "Có", null, new[] { "Có", "Không" })
                });

            if (pr == null) return;

            CadUtils.FocusDwgView();

            string type = pr.GetString("Type", "Pha L");
            short color = 1;
            string layerName = "E-WIRE-PHASE";
            if (type.Contains("N") || type.Contains("Trung")) { color = 5; layerName = "E-WIRE-N"; }
            else if (type.Contains("PE") || type.Contains("Tiếp")) { color = 82; layerName = "E-WIRE-PE"; }
            else if (type.Contains("Điều")) { color = 30; layerName = "E-WIRE-CTRL"; }

            PromptPointOptions ppo1 = new PromptPointOptions("\n⚡ [DTools DÂY ĐIỆN] Click điểm ĐẦU: ");
            PromptPointResult ppr1 = ed.GetPoint(ppo1);
            if (ppr1.Status != PromptStatus.OK) return;

            Point3d ptStart = ppr1.Value;
            bool continuous = pr.GetBool("Continuous", true);

            while (true)
            {
                PromptPointOptions ppo2 = new PromptPointOptions("\n⚡ Click điểm TIẾP THEO (Enter kết thúc): ")
                {
                    UseBasePoint = true,
                    BasePoint = ptStart
                };
                PromptPointResult ppr2 = ed.GetPoint(ppo2);
                if (ppr2.Status != PromptStatus.OK) break;

                Point3d ptEnd = ppr2.Value;

                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ObjectId wireLayer = CadUtils.GetOrCreateLayer(db, layerName, color, LineWeight.LineWeight035);
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    Line line = new Line(ptStart, ptEnd) { LayerId = wireLayer };
                    btr.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);
                    tr.Commit();
                }

                if (!continuous) break;
                ptStart = ptEnd;
            }
        }

        // ============================================================
        // ĐÁNH SỐ DÂY TỰ ĐỘNG (THAY THẾ AEWIRENUM)
        // ============================================================

        [CommandMethod("DTT_ELEC_WIRENUM")]
        public static void CmdWireNumbering()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var pr = DTools_FloatingDialog.Prompt(
                "ĐÁNH SỐ DÂY & MẠCH TỰ ĐỘNG",
                "Gắn nhãn số thứ tự dây dẫn tăng dần",
                "🔢",
                PreviewKind.Generic,
                new[] {
                    new ParameterField("Prefix", "Tiền tố ký hiệu:", "W-"),
                    new ParameterField("Start", "Số bắt đầu:", "101"),
                    new ParameterField("Step", "Bước tăng số:", "1")
                });

            if (pr == null) return;

            CadUtils.FocusDwgView();

            string prefix = pr.GetString("Prefix", "W-");
            int currentNum = pr.GetInt("Start", 101);
            int step = pr.GetInt("Step", 1);

            while (true)
            {
                PromptPointOptions ppo = new PromptPointOptions($"\n🔢 Click vị trí gắn số dây [{prefix}{currentNum}] (ESC để dừng): ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) break;

                Point3d pt = ppr.Value;

                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ObjectId textLayer = CadUtils.GetOrCreateLayer(db, "E-TEXT", 2, LineWeight.LineWeight015);
                    ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    string wireText = $"{prefix}{currentNum}";
                    DBText txt = new DBText
                    {
                        Position = pt,
                        TextString = wireText,
                        Height = 25.0,
                        LayerId = textLayer,
                        TextStyleId = textStyle
                    };
                    btr.AppendEntity(txt);
                    tr.AddNewlyCreatedDBObject(txt, true);

                    // Khung bo nhẹ quanh số dây
                    double boxW = wireText.Length * 16.0 + 10.0;
                    double boxH = 32.0;
                    DrawRect(btr, tr, textLayer, pt.X - 5, pt.Y - 5, boxW, boxH);

                    tr.Commit();
                }

                currentNum += step;
            }
        }

        // ============================================================
        // TẠO CẦU VƯỢT GIAO DÂY (THAY THẾ AEGAP)
        // ============================================================

        [CommandMethod("DTT_ELEC_GAP")]
        public static void CmdWireJumper()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptPointOptions ppo = new PromptPointOptions("\n🌉 Click TÂM giao nhau giữa 2 đường dây điện để tạo cầu nhảy: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d pt = ppr.Value;
            double radius = 15.0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId wireLayer = CadUtils.GetOrCreateLayer(db, "E-WIRE-POWER", 4, LineWeight.LineWeight035);
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Cung bán nguyệt vồng lên
                Arc jumper = new Arc(pt, radius, 0, Math.PI) { LayerId = wireLayer };
                btr.AppendEntity(jumper);
                tr.AddNewlyCreatedDBObject(jumper, true);

                tr.Commit();
            }

            ed.WriteMessage("\n✅ [DTools Pro] Đã tạo cầu vượt giao dây điện (Wire Jumper) thành công!\n");
        }

        // ============================================================
        // ⚡ BỘ THIẾT KẾ & SINH BẢN VẼ TỦ ĐIỆN VẠN NĂNG (DYNAMIC BUILDER)
        // ============================================================

        [CommandMethod("DTT_CABINET_BUILDER")]
        [CommandMethod("DTT_CABINET")]
        [CommandMethod("DTOOLS_CABINET")]
        public static void CmdCabinetBuilder()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            bool is3D = false;
            var cfg = DTools_CabinetBuilderDialog.PromptCabinetConfig(out is3D);
            if (cfg == null) return;

            CadUtils.FocusDwgView();

            if (is3D)
            {
                PromptPointOptions ppo3d = new PromptPointOptions($"\n🧊 [DTools 3D CABINET] Click điểm đặt mô hình 3D Solid [{cfg.CabinetName}]: ");
                PromptPointResult ppr3d = ed.GetPoint(ppo3d);
                if (ppr3d.Status != PromptStatus.OK) return;

                Cabinet3DBuilderEngine.Generate3DCabinet(doc, cfg, ppr3d.Value);
                Cabinet3DBuilderEngine.SwitchTo3DQuickView(doc);
                return;
            }

            PromptPointOptions ppo = new PromptPointOptions($"\n⚡ [DTools TỦ ĐIỆN VẠN NĂNG] Click điểm đặt góc dưới bên trái bản vẽ [{cfg.CabinetName}]: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            CabinetBuilderEngine.GenerateCabinetDrawings(doc, cfg, ppr.Value);
        }

        // ============================================================
        // LỆNH TỰ ĐỘNG DỰNG MÔ HÌNH 3D SOLID TỦ ĐIỆN & QUICK VIEW
        // ============================================================

        [CommandMethod("DTT_CABINET_3D")]
        [CommandMethod("DTOOLS_CABINET_3D")]
        public static void CmdCabinet3DBuilder()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var cfg = DTools_CabinetBuilderDialog.PromptCabinetConfig();
            if (cfg == null) return;

            CadUtils.FocusDwgView();

            PromptPointOptions ppo = new PromptPointOptions($"\n🧊 [DTools 3D CABINET] Click điểm đặt mô hình 3D Solid [{cfg.CabinetName}]: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Cabinet3DBuilderEngine.Generate3DCabinet(doc, cfg, ppr.Value);
            Cabinet3DBuilderEngine.SwitchTo3DQuickView(doc);
        }

        [CommandMethod("DTT_3D_VIEW")]
        [CommandMethod("DTOOLS_3D_VIEW")]
        public static void CmdSwitch3DView()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Cabinet3DBuilderEngine.SwitchTo3DQuickView(doc);
        }

        [CommandMethod("DTT_2D_VIEW")]
        public static void CmdSwitch2DView()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Cabinet3DBuilderEngine.SwitchToTopView(doc);
        }

        // ============================================================
        // LỆNH QUÉT VÀ XUẤT BẢNG BÓC TÁCH VẬT TƯ (BOM) SANG EXCEL (.XLSX)
        // ============================================================

        [CommandMethod("DTT_EXPORT_BOM")]
        [CommandMethod("DTT_BOM")]
        [CommandMethod("DTOOLS_BOM")]
        public static void CmdExportBomToExcel()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            BomExporterModule.ScanAndExportBom(doc);
        }

        // ============================================================
        // LỆNH VẼ MẶT BẰNG ĐIỆN 2D FULL TÍNH NĂNG (PRO FLOOR PLAN)
        // ============================================================

        [CommandMethod("DTT_ELEC_FLOORPLAN")]
        [CommandMethod("DTT_FLOORPLAN")]
        [CommandMethod("DTOOLS_FLOORPLAN")]
        public static void CmdFloorPlanElectricalBuilder()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            bool exportBom = false;
            var cfg = DTools_FloorPlanElecDialog.PromptConfig(out exportBom);
            if (cfg == null) return;

            CadUtils.FocusDwgView();

            Polyline boundaryPoly = null;
            Point3d startPt = Point3d.Origin;

            if (cfg.Shape == RoomShapeKind.PickBoundary)
            {
                PromptEntityOptions peo = new PromptEntityOptions("\n👉 [DTools MẶT BẰNG] Chọn đường Polyline ranh giới phòng trên bản vẽ CAD: ");
                peo.SetRejectMessage("\nChỉ chấp nhận đối tượng Polyline khép kín!");
                peo.AddAllowedClass(typeof(Polyline), true);
                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK) return;

                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    boundaryPoly = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                    if (boundaryPoly != null && boundaryPoly.NumberOfVertices > 0)
                    {
                        startPt = new Point3d(boundaryPoly.GetPoint2dAt(0).X, boundaryPoly.GetPoint2dAt(0).Y, 0);
                    }
                    tr.Commit();
                }
            }
            else
            {
                PromptPointOptions ppo = new PromptPointOptions($"\n👉 [DTools MẶT BẰNG ĐIỆN PRO] Click điểm đặt góc dưới bên trái phòng [{cfg.RoomName}]: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) return;
                startPt = ppr.Value;
            }

            bool ok = FloorPlanElectricalEngine.GenerateFloorPlan(doc, cfg, startPt, boundaryPoly);

            if (ok && exportBom)
            {
                BomExporterModule.ScanAndExportBom(doc);
            }
        }

        // ============================================================
        // LỆNH THƯ VIỆN PLC I/O PARAMETRIC & SMART SYNC TỦ ĐIỆN
        // ============================================================

        [CommandMethod("DTT_PLC_BUILDER")]
        [CommandMethod("DTT_PLC")]
        [CommandMethod("DTOOLS_PLC")]
        public static void CmdPlcParametricBuilder()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            bool autoSync = false;
            List<SchematicComponentItem> components = null;
            var plcCfg = DTools_PlcBuilderDialog.PromptPlcBuilder(out autoSync, out components);

            CadUtils.FocusDwgView();

            if (autoSync)
            {
                CmdSmartSyncPanel();
                return;
            }

            if (plcCfg == null) return;

            PromptPointOptions ppo = new PromptPointOptions($"\n👉 [DTools PLC PARAMETRIC] Click điểm đặt khối sơ đồ nguyên lý [{plcCfg.ModelName}]: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            PlcParametricEngine.GeneratePlcBlock(doc, plcCfg, ppr.Value);
        }

        [CommandMethod("DTT_SMART_SYNC")]
        [CommandMethod("DTT_SYNC_PANEL")]
        public static void CmdSmartSyncPanel()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n🔍 [DTools SMART SYNC] Đang quét toàn bộ thiết bị linh kiện trên sơ đồ nguyên lý...");
            var components = SmartSyncEngine.ScanSchematicComponents(doc);

            var cabCfg = new CabinetConfig { Width = 800.0, Height = 1200.0, Depth = 300.0 };

            PromptPointOptions ppo = new PromptPointOptions($"\n⚡ [DTools SMART SYNC] Click điểm đặt Tấm Panel gá xếp {components.Count} linh kiện tự động: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            SmartSyncEngine.AutoLayoutComponentsOnPanel(doc, cabCfg, ppr.Value, components);
        }

        // ============================================================
        // CHÈN KÝ HIỆU ĐIỆN THEO ID HOẶC MỞ KHO THƯ VIỆN KÝ HIỆU
        // ============================================================

        [CommandMethod("DTT_SYMBOLS")]
        public static void CmdShowSymbolsLibrary()
        {
            DttPaletteManager.ShowPalette();
        }

        [CommandMethod("DTT_INSERT_SYMBOL")]
        public static void CmdInsertSymbolById()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptStringOptions pso = new PromptStringOptions("\nNhập ID ký hiệu cần chèn (ví dụ: SYM_EARTH, SYM_MCCB_3P...): ") { AllowSpaces = false };
            PromptResult pr = ed.GetString(pso);
            if (pr.Status != PromptStatus.OK) return;

            string id = pr.StringResult.Trim();
            var all = ElectricalSymbolsManager.GetAllSymbols();
            var item = all.Find(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                ed.WriteMessage($"\n[DTools ERROR] Không tìm thấy ký hiệu có ID '{id}' trong thư viện!\n");
                return;
            }

            PromptPointOptions ppo = new PromptPointOptions($"\nClick điểm đặt ký hiệu [{item.NameVi}]: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            ElectricalSymbolsManager.InsertSymbolToCad(item, ppr.Value);
        }

        // ============================================================
        // VẼ TỦ ĐIỆN DB - SỬ DỤNG FLOATING PARAMETER DIALOG & PREVIEW
        // ============================================================

        [CommandMethod("DTT_ELEC_DB")]
        public static void CmdDrawDistributionBoard()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var pr = DTools_FloatingDialog.Prompt(
                "VẼ TỦ ĐIỆN PHÂN PHỐI DB",
                "Nhập kích thước vỏ tủ & số mạch CB nhánh",
                "📦",
                PreviewKind.PanelBoard,
                new[] {
                    new ParameterField("Width", "Chiều rộng tủ W", "400", "mm"),
                    new ParameterField("Height", "Chiều cao tủ H", "600", "mm"),
                    new ParameterField("Circuits", "Số mạch CB nhánh", "8", "mạch"),
                    new ParameterField("Name", "Ký hiệu tên tủ", "DB-01"),
                    new ParameterField("Continuous", "Chế độ vẽ liên tục", "Không", null, new[] { "Có", "Không" })
                });

            if (pr == null) return;

            CadUtils.FocusDwgView();

            double w = pr.GetDouble("Width", 400.0);
            double h = pr.GetDouble("Height", 600.0);
            int circuits = pr.GetInt("Circuits", 8);
            string panelName = pr.GetString("Name", "DB-01");
            bool continuous = pr.GetBool("Continuous", false);

            do
            {
                PromptPointOptions ppo = new PromptPointOptions($"\n⚡ [DTools TỦ ĐIỆN] Click điểm đặt góc DƯỚI TRÁI tủ [{panelName}]: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) break;

                Point3d pt = ppr.Value;

                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ObjectId elecLayer = CadUtils.GetOrCreateLayer(db, "E-PANEL", 4, LineWeight.LineWeight035);
                    ObjectId textLayer = CadUtils.GetOrCreateLayer(db, "E-TEXT", 2, LineWeight.LineWeight015);
                    ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // 1. Vỏ tủ ngoài
                    DrawRect(btr, tr, elecLayer, pt.X, pt.Y, w, h);

                    // 2. Cửa tủ trong
                    double margin = 20.0;
                    DrawRect(btr, tr, elecLayer, pt.X + margin, pt.Y + margin, w - margin * 2, h - margin * 2);

                    // 3. Đường chéo nửa tủ (chuẩn ký hiệu tủ điện TCVN)
                    Line diag = new Line(new Point3d(pt.X, pt.Y, 0), new Point3d(pt.X + w, pt.Y + h, 0)) { LayerId = elecLayer };
                    btr.AppendEntity(diag); tr.AddNewlyCreatedDBObject(diag, true);

                    // 4. Aptomat tổng (Main Breaker)
                    double cbW = (w - margin * 2) * 0.45;
                    double cbH = 45.0;
                    DrawCB(btr, tr, elecLayer, new Point3d(pt.X + w / 2 - cbW / 2, pt.Y + h - margin - cbH - 25, 0), cbW, cbH, "MCB TỔNG 63A");

                    // 5. Thanh cái Busbar
                    double busbarY = pt.Y + h - margin - cbH - 50;
                    Line busbar = new Line(new Point3d(pt.X + margin + 15, busbarY, 0), new Point3d(pt.X + w - margin - 15, busbarY, 0))
                    {
                        LayerId = elecLayer,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, 1),
                        LineWeight = LineWeight.LineWeight050
                    };
                    btr.AppendEntity(busbar); tr.AddNewlyCreatedDBObject(busbar, true);

                    // 6. Các CB nhánh
                    int rows = (circuits + 1) / 2;
                    double branchW = (w - margin * 2 - 30) / 2.0;
                    double branchH = 30.0;
                    double startY = busbarY - 30;
                    double stepY = Math.Min(40.0, (startY - pt.Y - margin - 35) / Math.Max(rows, 1));

                    for (int i = 0; i < circuits; i++)
                    {
                        int row = i / 2;
                        int col = i % 2;
                        double bx = (col == 0) ? (pt.X + margin + 10) : (pt.X + w / 2 + 5);
                        double by = startY - row * stepY - branchH;
                        DrawCB(btr, tr, elecLayer, new Point3d(bx, by, 0), branchW, branchH, $"L{i + 1}");
                    }

                    // 7. Nhãn tên tủ & thông số
                    DBText lblName = new DBText
                    {
                        Position = new Point3d(pt.X + w / 2 - 40, pt.Y + h + 15, 0),
                        TextString = $"TỦ ĐIỆN {panelName}",
                        Height = 35.0,
                        LayerId = textLayer,
                        TextStyleId = textStyle
                    };
                    btr.AppendEntity(lblName); tr.AddNewlyCreatedDBObject(lblName, true);

                    tr.Commit();
                }

                ed.WriteMessage($"\n⚡ [DTools] Đã vẽ tủ điện [{panelName}] thành công!\n");
            } while (continuous);
        }

        // ============================================================
        // VẼ TỦ ĐIỆN TỔNG MDB 3 PHA (Main Distribution Board)
        // ============================================================

        [CommandMethod("DTT_ELEC_MDB")]
        public static void CmdDrawMainDistributionBoard()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var pr = DTools_FloatingDialog.Prompt(
                "VẼ TỦ ĐIỆN TỔNG MDB 3 PHA",
                "Tủ phân phối chính cho toàn bộ công trình",
                "⚡",
                PreviewKind.PanelBoard,
                new[] {
                    new ParameterField("Width", "Chiều rộng tủ MDB W", "800", "mm"),
                    new ParameterField("Height", "Chiều cao tủ MDB H", "1200", "mm"),
                    new ParameterField("Current", "Dòng điện định mức", "400", "A"),
                    new ParameterField("Name", "Ký hiệu tủ", "MDB-01")
                });

            if (pr == null) return;

            CadUtils.FocusDwgView();

            double w = pr.GetDouble("Width", 800.0);
            double h = pr.GetDouble("Height", 1200.0);
            double current = pr.GetDouble("Current", 400.0);
            string panelName = pr.GetString("Name", "MDB-01");

            PromptPointOptions ppo = new PromptPointOptions($"\n⚡ [DTools MDB] Click điểm đặt góc DƯỚI TRÁI tủ tổng [{panelName}]: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d pt = ppr.Value;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId elecLayer = CadUtils.GetOrCreateLayer(db, "E-PANEL", 1, LineWeight.LineWeight050); // Red
                ObjectId textLayer = CadUtils.GetOrCreateLayer(db, "E-TEXT", 2, LineWeight.LineWeight015);
                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Khung vỏ tủ MDB
                DrawRect(btr, tr, elecLayer, pt.X, pt.Y, w, h);
                DrawRect(btr, tr, elecLayer, pt.X + 30, pt.Y + 30, w - 60, h - 60);

                // Khoang đồng hồ Volt/Ampe
                double meterH = 140.0;
                DrawRect(btr, tr, elecLayer, pt.X + 40, pt.Y + h - 40 - meterH, w - 80, meterH);

                // 3 đồng hồ V, A, F
                double mw = (w - 120) / 3.0;
                for (int i = 0; i < 3; i++)
                {
                    double mx = pt.X + 50 + i * (mw + 10);
                    double my = pt.Y + h - 40 - meterH + 20;
                    Circle c = new Circle(new Point3d(mx + mw / 2, my + 50, 0), Vector3d.ZAxis, 35) { LayerId = elecLayer };
                    btr.AppendEntity(c); tr.AddNewlyCreatedDBObject(c, true);

                    string lbl = (i == 0) ? "V" : (i == 1 ? "A" : "Hz");
                    DBText txt = new DBText { Position = new Point3d(mx + mw / 2 - 10, my + 40, 0), TextString = lbl, Height = 25.0, LayerId = textLayer, TextStyleId = textStyle };
                    btr.AppendEntity(txt); tr.AddNewlyCreatedDBObject(txt, true);
                }

                // Aptomat khối MCCB 3 Pha Tổng
                double mccbW = 160.0;
                double mccbH = 90.0;
                DrawCB(btr, tr, elecLayer, new Point3d(pt.X + w / 2 - mccbW / 2, pt.Y + h - meterH - 180, 0), mccbW, mccbH, $"MCCB 3P {current}A");

                // Thanh cái đồng 3 pha Cu Busbar
                double busY = pt.Y + h - meterH - 220;
                for (int p = 0; p < 3; p++)
                {
                    short color = (short)(p == 0 ? 1 : (p == 1 ? 2 : 3)); // Đỏ, Vàng, Xanh lá
                    Line b = new Line(new Point3d(pt.X + 50, busY - p * 15, 0), new Point3d(pt.X + w - 50, busY - p * 15, 0))
                    {
                        LayerId = elecLayer,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, color),
                        LineWeight = LineWeight.LineWeight050
                    };
                    btr.AppendEntity(b); tr.AddNewlyCreatedDBObject(b, true);
                }

                // Tên tủ
                DBText title = new DBText
                {
                    Position = new Point3d(pt.X + w / 2 - 120, pt.Y + h + 25, 0),
                    TextString = $"TỦ TỔNG {panelName} - {current}A 3P 380V",
                    Height = 40.0,
                    LayerId = textLayer,
                    TextStyleId = textStyle
                };
                btr.AppendEntity(title); tr.AddNewlyCreatedDBObject(title, true);

                tr.Commit();
            }

            ed.WriteMessage($"\n⚡ [DTools] Đã vẽ tủ điện tổng MDB [{panelName}] thành công!\n");
        }

        // ============================================================
        // VẼ SƠ ĐỒ NGUYÊN LÝ ĐƠN TUYẾN (SINGLE-LINE DIAGRAM)
        // ============================================================

        [CommandMethod("DTT_ELEC_SINGLELINE")]
        public static void CmdDrawSingleLineDiagram()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var pr = DTools_FloatingDialog.Prompt(
                "VẼ SƠ ĐỒ NGUYÊN LÝ ĐƠN TUYẾN 1 SỢI",
                "Tự động sinh sơ đồ cấp nguồn và các lộ nhánh phụ tải",
                "🔀",
                PreviewKind.SingleLine,
                new[] {
                    new ParameterField("Branches", "Số lộ nhánh xuất tuyến:", "6", "lộ"),
                    new ParameterField("Spacing", "Khoảng cách giữa các lộ:", "250", "mm"),
                    new ParameterField("Title", "Tên sơ đồ nguyên lý:", "SƠ ĐỒ CẤP ĐIỆN TỔNG THỂ")
                });

            if (pr == null) return;

            CadUtils.FocusDwgView();

            int branches = pr.GetInt("Branches", 6);
            double spacing = pr.GetDouble("Spacing", 250.0);
            string titleText = pr.GetString("Title", "SƠ ĐỒ CẤP ĐIỆN TỔNG THỂ");

            PromptPointOptions ppo = new PromptPointOptions("\n🔀 Click điểm đặt GÓC TRÊN TRÁI của Sơ Đồ Đơn Tuyến: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d origin = ppr.Value;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId lineLayer = CadUtils.GetOrCreateLayer(db, "E-SINGLELINE", 4, LineWeight.LineWeight035);
                ObjectId textLayer = CadUtils.GetOrCreateLayer(db, "E-TEXT", 2, LineWeight.LineWeight015);
                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 1. Tiêu đề sơ đồ
                DBText title = new DBText
                {
                    Position = new Point3d(origin.X + 50, origin.Y + 80, 0),
                    TextString = titleText,
                    Height = 45.0,
                    LayerId = textLayer,
                    TextStyleId = textStyle
                };
                btr.AppendEntity(title); tr.AddNewlyCreatedDBObject(title, true);

                // 2. Nguồn vào
                DrawACSource(btr, tr, lineLayer, new Point3d(origin.X + 150, origin.Y, 0));

                // 3. Đoạn dẫn xuống MCCB tổng
                Line feedLine = new Line(new Point3d(origin.X + 150, origin.Y - 150, 0), new Point3d(origin.X + 150, origin.Y - 260, 0)) { LayerId = lineLayer };
                btr.AppendEntity(feedLine); tr.AddNewlyCreatedDBObject(feedLine, true);

                // 4. MCCB tổng
                DrawCB(btr, tr, lineLayer, new Point3d(origin.X + 100, origin.Y - 330, 0), 100, 70, "MCCB TỔNG\n3P 250A");

                // 5. Nối xuống thanh cái
                Line toBus = new Line(new Point3d(origin.X + 150, origin.Y - 330, 0), new Point3d(origin.X + 150, origin.Y - 420, 0)) { LayerId = lineLayer };
                btr.AppendEntity(toBus); tr.AddNewlyCreatedDBObject(toBus, true);

                // 6. Thanh cái chính
                double busWidth = Math.Max(branches * spacing + 100, 600.0);
                double busY = origin.Y - 420;
                Line busbar = new Line(new Point3d(origin.X + 50, busY, 0), new Point3d(origin.X + 50 + busWidth, busY, 0))
                {
                    LayerId = lineLayer,
                    Color = Color.FromColorIndex(ColorMethod.ByAci, 1),
                    LineWeight = LineWeight.LineWeight070
                };
                btr.AppendEntity(busbar); tr.AddNewlyCreatedDBObject(busbar, true);

                // 7. Các lộ nhánh xuất tuyến
                string[] loads = new string[] { "Chiếu Sáng Tầng 1", "Ổ Cắm Tầng 1", "Điều Hòa Tầng 1", "Chiếu Sáng Tầng 2", "Ổ Cắm Tầng 2", "Bơm Nước PCCC", "Dự Phòng", "Thang Máy" };
                for (int b = 0; b < branches; b++)
                {
                    double bx = origin.X + 100 + b * spacing;

                    // Đoạn rẽ từ thanh cái
                    Line drop = new Line(new Point3d(bx, busY, 0), new Point3d(bx, busY - 70, 0)) { LayerId = lineLayer };
                    btr.AppendEntity(drop); tr.AddNewlyCreatedDBObject(drop, true);

                    // CB nhánh
                    DrawCB(btr, tr, lineLayer, new Point3d(bx - 35, busY - 140, 0), 70, 60, $"MCB-{b + 1}");

                    // Tuyến cáp ra tải
                    Line toLoad = new Line(new Point3d(bx, busY - 140, 0), new Point3d(bx, busY - 260, 0)) { LayerId = lineLayer };
                    btr.AppendEntity(toLoad); tr.AddNewlyCreatedDBObject(toLoad, true);

                    // Biểu tượng phụ tải
                    DrawLoadSymbol(btr, tr, lineLayer, new Point3d(bx, busY - 260, 0));

                    // Tên phụ tải
                    string loadName = loads[b % loads.Length];
                    DBText loadTxt = new DBText
                    {
                        Position = new Point3d(bx - 40, busY - 330, 0),
                        TextString = loadName,
                        Height = 22.0,
                        LayerId = textLayer,
                        TextStyleId = textStyle
                    };
                    btr.AppendEntity(loadTxt); tr.AddNewlyCreatedDBObject(loadTxt, true);
                }

                tr.Commit();
            }

            ed.WriteMessage($"\n🔀 [DTools] Đã vẽ Sơ Đồ Đơn Tuyến ({branches} lộ nhánh) thành công!\n");
        }

        // ============================================================
        // CHÈN KÝ HIỆU THIẾT BỊ ĐIỆN
        // ============================================================

        public static void InsertDeviceSymbol(string category, string name, string spec, double height, double scale = 1.0)
        {
            string symbolType = "SOCKET_DOUBLE";
            string search = (name + " " + category).ToLowerInvariant();
            if (search.Contains("đơn")) symbolType = "SOCKET_SINGLE";
            else if (search.Contains("sàn")) symbolType = "SOCKET_FLOOR";
            else if (search.Contains("nước")) symbolType = "SOCKET_WATERPROOF";
            else if (search.Contains("downlight") || search.Contains("âm trần")) symbolType = "LIGHT_DOWNLIGHT";
            else if (search.Contains("panel")) symbolType = "LIGHT_PANEL";
            else if (search.Contains("tuýp")) symbolType = "LIGHT_TUBE";
            else if (search.Contains("sự cố") || search.Contains("exit")) symbolType = "LIGHT_EMERGENCY";
            else if (search.Contains("1 hạt") || search.Contains("1g")) symbolType = "SWITCH_1G";
            else if (search.Contains("2 hạt") || search.Contains("2g")) symbolType = "SWITCH_2G";
            else if (search.Contains("3 hạt") || search.Contains("3g")) symbolType = "SWITCH_3G";
            else if (search.Contains("dimmer")) symbolType = "SWITCH_DIMMER";
            else if (search.Contains("khói")) symbolType = "SMOKE_DETECTOR";
            else if (search.Contains("quạt")) symbolType = "FAN_CEILING";
            else if (search.Contains("bình")) symbolType = "HEATER";

            InsertSymbol(symbolType, scale, height);
        }

        public static void InsertDeviceSymbol(string symbolType, double scale = 1.0, double zHeight = 0.0)
        {
            InsertSymbol(symbolType, scale, zHeight);
        }

        public static void InsertSymbol(string symbolType, double scale = 1.0, double zHeight = 0.0)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptPointOptions ppo = new PromptPointOptions($"\n⚡ [DTools ĐIỆN] Click vị trí đặt [{symbolType}]: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d pt = new Point3d(ppr.Value.X, ppr.Value.Y, zHeight);

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId elecLayer = CadUtils.GetOrCreateLayer(db, "E-DEVICE", 4, LineWeight.LineWeight025);
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                switch (symbolType)
                {
                    case "SOCKET_DOUBLE":
                    case "SOCKET_SINGLE":
                    case "SOCKET_FLOOR":
                    case "SOCKET_WATERPROOF":
                        DrawSocketSymbol(btr, tr, elecLayer, pt, 50.0 * scale, symbolType);
                        break;

                    case "LIGHT_DOWNLIGHT":
                    case "LIGHT_PANEL":
                    case "LIGHT_TUBE":
                    case "LIGHT_EMERGENCY":
                        DrawLightSymbol(btr, tr, elecLayer, pt, 60.0 * scale, symbolType);
                        break;

                    case "SWITCH_1G":
                    case "SWITCH_2G":
                    case "SWITCH_3G":
                    case "SWITCH_DIMMER":
                        DrawSwitchSymbol(btr, tr, elecLayer, pt, 45.0 * scale, symbolType);
                        break;

                    case "SMOKE_DETECTOR":
                    case "FAN_CEILING":
                    case "HEATER":
                        DrawAuxSymbol(btr, tr, elecLayer, pt, 55.0 * scale, symbolType);
                        break;

                    default:
                        DrawGenericSymbol(btr, tr, elecLayer, pt, 50.0 * scale, symbolType);
                        break;
                }

                tr.Commit();
            }

            ed.WriteMessage($"\n⚡ [DTools] Đã chèn ký hiệu [{symbolType}] thành công! ✓\n");
        }

        // ============================================================
        // CÁC HÀM VẼ KÝ HIỆU CHUẨN TCVN / IEC
        // ============================================================

        private static void DrawSocketSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, Point3d pt, double s, string type)
        {
            Arc arc = new Arc(pt, s, 0, Math.PI) { LayerId = layer };
            btr.AppendEntity(arc); tr.AddNewlyCreatedDBObject(arc, true);

            Line baseLine = new Line(new Point3d(pt.X - s, pt.Y, 0), new Point3d(pt.X + s, pt.Y, 0)) { LayerId = layer };
            btr.AppendEntity(baseLine); tr.AddNewlyCreatedDBObject(baseLine, true);

            if (type == "SOCKET_DOUBLE" || type == "SOCKET_FLOOR")
            {
                Line pin1 = new Line(new Point3d(pt.X - s * 0.4, pt.Y, 0), new Point3d(pt.X - s * 0.4, pt.Y + s * 0.9, 0)) { LayerId = layer };
                Line pin2 = new Line(new Point3d(pt.X + s * 0.4, pt.Y, 0), new Point3d(pt.X + s * 0.4, pt.Y + s * 0.9, 0)) { LayerId = layer };
                btr.AppendEntity(pin1); tr.AddNewlyCreatedDBObject(pin1, true);
                btr.AppendEntity(pin2); tr.AddNewlyCreatedDBObject(pin2, true);
            }
            else
            {
                Line pin = new Line(new Point3d(pt.X, pt.Y, 0), new Point3d(pt.X, pt.Y + s * 0.95, 0)) { LayerId = layer };
                btr.AppendEntity(pin); tr.AddNewlyCreatedDBObject(pin, true);
            }
        }

        private static void DrawLightSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, Point3d pt, double s, string type)
        {
            Circle circle = new Circle(pt, Vector3d.ZAxis, s) { LayerId = layer };
            btr.AppendEntity(circle); tr.AddNewlyCreatedDBObject(circle, true);

            double d = s * 0.7071;
            Line x1 = new Line(new Point3d(pt.X - d, pt.Y - d, 0), new Point3d(pt.X + d, pt.Y + d, 0)) { LayerId = layer };
            Line x2 = new Line(new Point3d(pt.X - d, pt.Y + d, 0), new Point3d(pt.X + d, pt.Y - d, 0)) { LayerId = layer };
            btr.AppendEntity(x1); tr.AddNewlyCreatedDBObject(x1, true);
            btr.AppendEntity(x2); tr.AddNewlyCreatedDBObject(x2, true);
        }

        private static void DrawSwitchSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, Point3d pt, double s, string type)
        {
            Circle dot = new Circle(pt, Vector3d.ZAxis, s * 0.3) { LayerId = layer };
            btr.AppendEntity(dot); tr.AddNewlyCreatedDBObject(dot, true);

            Line arm = new Line(pt, new Point3d(pt.X + s, pt.Y + s, 0)) { LayerId = layer };
            btr.AppendEntity(arm); tr.AddNewlyCreatedDBObject(arm, true);

            int gangs = (type == "SWITCH_2G") ? 2 : ((type == "SWITCH_3G") ? 3 : 1);
            for (int i = 0; i < gangs; i++)
            {
                double off = i * (s * 0.25);
                Line tick = new Line(new Point3d(pt.X + s - off, pt.Y + s, 0), new Point3d(pt.X + s - off + s * 0.25, pt.Y + s - s * 0.25, 0)) { LayerId = layer };
                btr.AppendEntity(tick); tr.AddNewlyCreatedDBObject(tick, true);
            }
        }

        private static void DrawAuxSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, Point3d pt, double s, string type)
        {
            double w = s * 1.5;
            double h = s * 1.5;
            DrawRect(btr, tr, layer, pt.X - w / 2, pt.Y - h / 2, w, h);

            Line diag1 = new Line(new Point3d(pt.X - w / 2, pt.Y - h / 2, 0), new Point3d(pt.X + w / 2, pt.Y + h / 2, 0)) { LayerId = layer };
            btr.AppendEntity(diag1); tr.AddNewlyCreatedDBObject(diag1, true);

            Line diag2 = new Line(new Point3d(pt.X - w / 2, pt.Y + h / 2, 0), new Point3d(pt.X + w / 2, pt.Y - h / 2, 0)) { LayerId = layer };
            btr.AppendEntity(diag2); tr.AddNewlyCreatedDBObject(diag2, true);
        }

        private static void DrawGenericSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, Point3d pt, double s, string type)
        {
            Circle c = new Circle(pt, Vector3d.ZAxis, s) { LayerId = layer };
            btr.AppendEntity(c); tr.AddNewlyCreatedDBObject(c, true);
        }

        // ============================================================
        // UTILITY SHAPES
        // ============================================================

        private static void DrawRect(BlockTableRecord btr, Transaction tr, ObjectId layer,
                                      double x, double y, double w, double h)
        {
            Polyline rect = new Polyline();
            rect.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
            rect.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
            rect.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
            rect.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
            rect.Closed = true;
            rect.LayerId = layer;
            btr.AppendEntity(rect); tr.AddNewlyCreatedDBObject(rect, true);
        }

        private static void DrawCB(BlockTableRecord btr, Transaction tr, ObjectId layer,
                                    Point3d pt, double w, double h, string label)
        {
            DrawRect(btr, tr, layer, pt.X, pt.Y, w, h);

            double mx = pt.X + w / 2;
            double my = pt.Y + h / 2;
            double sz = Math.Min(w, h) * 0.25;

            Polyline zz = new Polyline();
            zz.AddVertexAt(0, new Point2d(mx - sz, my - sz), 0, 0, 0);
            zz.AddVertexAt(1, new Point2d(mx, my + sz), 0, 0, 0);
            zz.AddVertexAt(2, new Point2d(mx + sz, my - sz), 0, 0, 0);
            zz.LayerId = layer;
            btr.AppendEntity(zz); tr.AddNewlyCreatedDBObject(zz, true);

            if (!string.IsNullOrEmpty(label))
            {
                try
                {
                    ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(
                        Application.DocumentManager.MdiActiveDocument.Database);
                    DBText cbLabel = new DBText
                    {
                        Position = new Point3d(pt.X + w + 10, pt.Y + h * 0.4, 0),
                        TextString = label.Replace("\n", " "),
                        Height = Math.Max(h * 0.22, 20.0),
                        LayerId = layer,
                        TextStyleId = textStyle
                    };
                    btr.AppendEntity(cbLabel); tr.AddNewlyCreatedDBObject(cbLabel, true);
                }
                catch { }
            }
        }

        private static void DrawACSource(BlockTableRecord btr, Transaction tr, ObjectId layer, Point3d pt)
        {
            Circle src = new Circle(pt, Vector3d.ZAxis, 150) { LayerId = layer };
            btr.AppendEntity(src); tr.AddNewlyCreatedDBObject(src, true);

            Arc wave1 = new Arc(new Point3d(pt.X - 75, pt.Y, 0), 75, 0, Math.PI) { LayerId = layer };
            btr.AppendEntity(wave1); tr.AddNewlyCreatedDBObject(wave1, true);

            Arc wave2 = new Arc(new Point3d(pt.X + 75, pt.Y, 0), 75, Math.PI, 2 * Math.PI) { LayerId = layer };
            btr.AppendEntity(wave2); tr.AddNewlyCreatedDBObject(wave2, true);
        }

        private static void DrawLoadSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, Point3d pt)
        {
            double sz = 100.0;
            Polyline tri = new Polyline();
            tri.AddVertexAt(0, new Point2d(pt.X - sz, pt.Y + sz), 0, 0, 0);
            tri.AddVertexAt(1, new Point2d(pt.X + sz, pt.Y + sz), 0, 0, 0);
            tri.AddVertexAt(2, new Point2d(pt.X, pt.Y), 0, 0, 0);
            tri.Closed = true;
            tri.LayerId = layer;
            btr.AppendEntity(tri); tr.AddNewlyCreatedDBObject(tri, true);
        }

        // ============================================================
        // 5 TÍNH NĂNG CHUYÊN SÂU NGHÀNH ĐIỆN NÂNG CAO (ADVANCED SUITE)
        // ============================================================

        [CommandMethod("DTT_TERMINAL_STRIP")]
        [CommandMethod("DTT_TERMINAL")]
        public static void CmdGenerateTerminalStrip()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptStringOptions pso = new PromptStringOptions("\n👉 [DTools TERMINAL] Nhập mã hiệu hàng kẹp cầu đấu (ví dụ: X1, X2, TB1): ") { DefaultValue = "X1" };
            PromptResult pr = ed.GetString(pso);
            if (pr.Status != PromptStatus.OK) return;
            string stripTag = string.IsNullOrWhiteSpace(pr.StringResult) ? "X1" : pr.StringResult.Trim();

            PromptPointOptions ppo = new PromptPointOptions($"\n👉 [DTools TERMINAL] Click điểm đặt bảng hàng kẹp [-{stripTag}]: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            AdvancedElectricalEngine.GenerateTerminalStrip(doc, stripTag, null, ppr.Value);
        }

        [CommandMethod("DTT_SMART_WIRENUM")]
        [CommandMethod("DTT_WIRENUM_AUTO")]
        public static void CmdSmartWireNumbering()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptIntegerOptions pio = new PromptIntegerOptions("\n👉 [DTools ĐÁNH SỐ DÂY] Nhập số thứ tự dây bắt đầu: ") { DefaultValue = 101 };
            PromptIntegerResult pir = ed.GetInteger(pio);
            int startNum = (pir.Status == PromptStatus.OK) ? pir.Value : 101;

            AdvancedElectricalEngine.AutoNumberWires(doc, startNum);
        }

        [CommandMethod("DTT_CROSS_REF")]
        [CommandMethod("DTT_RELAY_XREF")]
        public static void CmdRelayCrossRef()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            AdvancedElectricalEngine.GenerateRelayCrossRef(doc);
        }

        [CommandMethod("DTT_MOTOR_STARTER")]
        [CommandMethod("DTT_STARTER")]
        public static void CmdMotorStarterBuilder()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var cfg = DTools_MotorStarterDialog.PromptConfig();
            if (cfg == null) return;

            CadUtils.FocusDwgView();

            PromptPointOptions ppo = new PromptPointOptions($"\n👉 [DTools MOTOR STARTER] Click điểm đặt mạch khởi động [{cfg.Type} - {cfg.MotorTag}]: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            AdvancedElectricalEngine.GenerateMotorStarterCircuit(doc, cfg, ppr.Value);
        }

        [CommandMethod("DTT_ELEC_DRC")]
        [CommandMethod("DTT_DRC")]
        public static void CmdElectricalDRC()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var issues = AdvancedElectricalEngine.RunElectricalDRC(doc);
            bool markOnCad = DTools_DrcReportDialog.ShowReport(issues);

            if (markOnCad && issues.Count > 0)
            {
                CadUtils.FocusDwgView();
                AdvancedElectricalEngine.MarkDrcIssuesOnCad(doc, issues);
            }
        }
    }
}
