using System;
using System.IO;
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
    public static class ProExpansionCommands
    {
        // ============================================================
        // 1. MODULE TÍNH TOÁN CHỌN CÁP & SƠ ĐỒ ĐƠN TUYẾN SLD
        // ============================================================

        [CommandMethod("DTT_CABLE_CALC")]
        public static void CmdCableCalculation()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                var pr = DTools_FloatingDialog.Prompt(
                    "TÍNH CHỌN CÁP ĐIỆN & KIỂM TRA SỤT ÁP (TCVN/IEC)",
                    "Nhập công suất phụ tải, chiều dài và kiểm tra độ sụt áp %",
                    "⚡",
                    PreviewKind.SingleLine,
                    new[]
                    {
                        new ParameterField("PowerKw", "Công suất phụ tải P (kW):", "35", "kW"),
                        new ParameterField("VoltageV", "Điện áp hệ thống:", "380", "V", new[] { "380", "220" }),
                        new ParameterField("CosPhi", "Hệ số công suất cosφ:", "0.85", null, new[] { "0.85", "0.80", "0.90", "0.95" }),
                        new ParameterField("LengthM", "Chiều dài tuyến cáp L (m):", "65", "m"),
                        new ParameterField("MaxDrop", "Độ sụt áp cho phép ΔU:", "2.5", "%", new[] { "2.5", "5.0", "3.0", "4.0" }),
                        new ParameterField("Insulation", "Vật liệu cách điện cáp:", "XLPE", null, new[] { "XLPE", "PVC" })
                    });

                if (pr == null) return;

                var param = new CableCalculationParam
                {
                    PowerKw = pr.GetDouble("PowerKw", 35.0),
                    VoltageV = pr.GetDouble("VoltageV", 380.0),
                    PowerFactor = pr.GetDouble("CosPhi", 0.85),
                    LengthM = pr.GetDouble("LengthM", 65.0),
                    MaxVoltageDropPercent = pr.GetDouble("MaxDrop", 2.5),
                    InsulationType = pr.GetString("Insulation", "XLPE")
                };

                var res = ElectricalCalculationEngine.CalculateCableAndBreaker(param);

                ed.WriteMessage("\n=======================================================");
                ed.WriteMessage("\n⚡ KẾT QUẢ TÍNH CHỌN CÁP & APTOMAT - DTOOLS PRO:");
                ed.WriteMessage($"\n- Dòng điện định mức I_dm: {res.CurrentA:0.##} A (Dòng tính toán Itt: {res.DesignCurrentA:0.##} A)");
                ed.WriteMessage($"\n- Aptomat bảo vệ đề xuất:  {res.BreakerSpecText}");
                ed.WriteMessage($"\n- Cáp điện Cadivi đề xuất:  {res.CableSpecText}");
                ed.WriteMessage($"\n- Độ sụt áp tính toán ΔU:  {res.ActualVoltageDropV:0.##} V ({res.ActualVoltageDropPercent:0.##}%) -> {(res.IsPass ? "✅ ĐẠT TIÊU CHUẨN" : "⚠️ VƯỢT QUÁ SỤT ÁP CHO PHÉP")}");
                ed.WriteMessage("\n=======================================================\n");

                PromptPointOptions ppo = new PromptPointOptions("\n[DTT] Chọn điểm chèn Bảng kết quả tính cáp ra bản vẽ: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status == PromptStatus.OK)
                {
                    using (doc.LockDocument())
                    {
                        InsertCableResultNote(doc.Database, ppr.Value, res);
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [DTools Lỗi] Tính chọn cáp: {ex.Message}\n");
            }
        }

        private static void InsertCableResultNote(Database db, Point3d pt, CableCalculationParam res)
        {
            ObjectId layerId = CadUtils.GetOrCreateLayer(db, "E-CALC-NOTE", 1);
            ObjectId styleId = CadUtils.GetOrCreateUnicodeStyle(db);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                string[] lines = new string[]
                {
                    "KẾT QUẢ TÍNH CHỌN CÁP & THIẾT BỊ BẢO VỆ (TCVN / IEC):",
                    $"• Phụ tải: P={res.PowerKw:0.#}kW, U={res.VoltageV:0}V, L={res.LengthM:0}m, cosφ={res.PowerFactor:0.##}",
                    $"• Dòng điện tính toán: I_tt = {res.CurrentA:0.#} A",
                    $"• Thiết bị đóng cắt:  {res.BreakerSpecText}",
                    $"• Tiết diện cáp đề xuất: {res.CableSpecText}",
                    $"• Độ sụt áp thực tế:   ΔU = {res.ActualVoltageDropPercent:0.##}% (Cho phép <= {res.MaxVoltageDropPercent:0.##}%) -> ĐẠT"
                };

                for (int i = 0; i < lines.Length; i++)
                {
                    DBText t = new DBText
                    {
                        Position = new Point3d(pt.X, pt.Y - i * 85, 0),
                        Height = (i == 0) ? 90 : 65,
                        TextString = lines[i],
                        TextStyleId = styleId,
                        LayerId = layerId
                    };
                    btr.AppendEntity(t);
                    tr.AddNewlyCreatedDBObject(t, true);
                }

                tr.Commit();
            }
        }

        [CommandMethod("DTT_SLD_BUILDER")]
        public static void CmdSingleLineDiagram()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                var pr = DTools_FloatingDialog.Prompt(
                    "SINH SƠ ĐỒ NGUYÊN LÝ ĐƠN TUYẾN (SLD)",
                    "Tự động vẽ Busbar, Incomer, các lộ nhánh và cáp cấp nguồn",
                    "📊",
                    PreviewKind.PanelBoard,
                    new[]
                    {
                        new ParameterField("PanelTag", "Ký hiệu tủ điện:", "MSB-01"),
                        new ParameterField("PowerKw", "Tổng công suất P (kW):", "180", "kW"),
                        new ParameterField("Incomer", "Nguồn cấp vào:", "TRẠM BIẾN ÁP 400kVA & ATS"),
                        new ParameterField("Feeders", "Số lượng lộ nhánh ra:", "6", "lộ", new[] { "4", "6", "8", "10", "12" })
                    });

                if (pr == null) return;

                PromptPointOptions ppo = new PromptPointOptions("\n[DTT] Chọn điểm đặt góc trên bên trái Sơ đồ đơn tuyến: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) return;

                var cfg = new SldMainPanelConfig
                {
                    PanelTag = pr.GetString("PanelTag", "MSB-01"),
                    TotalPowerKw = pr.GetDouble("PowerKw", 180.0),
                    IncomerSource = pr.GetString("Incomer", "TRẠM BIẾN ÁP 400kVA & ATS")
                };

                using (doc.LockDocument())
                {
                    ElectricalCalculationEngine.GenerateSingleLineDiagram(doc.Database, ppr.Value, cfg);
                }

                ed.WriteMessage("\n✅ [DTools] Đã sinh Sơ đồ nguyên lý đơn tuyến (Single Line Diagram) thành công!\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [DTools Lỗi] Sơ đồ đơn tuyến: {ex.Message}\n");
            }
        }

        // ============================================================
        // 2. MODULE CẦU THANG PHONG THỦY & NỘI THẤT KIẾN TRÚC
        // ============================================================

        [CommandMethod("DTT_STAIRCASE")]
        public static void CmdStaircase()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                var pr = DTools_FloatingDialog.Prompt(
                    "VẼ CẦU THANG KIẾN TRÚC PHONG THỦY",
                    "Tự động tính bậc Cung Sinh (17, 21, 25 bậc) và vẽ chi tiết vế thang",
                    "🪜",
                    PreviewKind.Generic,
                    new[]
                    {
                        new ParameterField("Steps", "Số bậc phong thủy (Cung Sinh):", "21", "bậc", new[] { "21", "17", "25", "19", "23" }),
                        new ParameterField("Type", "Dạng thang:", "2 Vế Chữ U (Có Chiếu Nghỉ)", null, new[] { "2 Vế Chữ U (Có Chiếu Nghỉ)", "1 Vế Thẳng Chữ I" }),
                        new ParameterField("Width", "Chiều rộng vế thang W:", "1000", "mm", new[] { "900", "1000", "1100", "1200" }),
                        new ParameterField("Tread", "Chiều rộng mặt bậc b:", "260", "mm", new[] { "250", "260", "270", "280" }),
                        new ParameterField("Landing", "Chiều sâu chiếu nghỉ:", "1000", "mm")
                    });

                if (pr == null) return;

                PromptPointOptions ppo = new PromptPointOptions("\n[DTT] Chọn điểm đặt góc chân cầu thang: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) return;

                string typeStr = pr.GetString("Type", "2 Vế Chữ U");
                var cfg = new StaircaseConfig
                {
                    Type = typeStr.Contains("1 Vế") ? StaircaseType.Straight1Flight : StaircaseType.UTurn2Flights,
                    TotalSteps = pr.GetInt("Steps", 21),
                    FlightWidth = pr.GetDouble("Width", 1000.0),
                    TreadDepth = pr.GetDouble("Tread", 260.0),
                    LandingDepth = pr.GetDouble("Landing", 1000.0),
                    ShowStepNumbers = true
                };

                using (doc.LockDocument())
                {
                    InteriorAndStaircaseEngine.GenerateStaircase(doc.Database, ppr.Value, cfg);
                }

                ed.WriteMessage($"\n✅ [DTools] Đã vẽ Cầu thang phong thủy {cfg.TotalSteps} bậc (Cung Sinh) thành công!\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [DTools Lỗi] Vẽ cầu thang: {ex.Message}\n");
            }
        }

        [CommandMethod("DTT_FURNITURE")]
        public static void CmdFurniture()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                var pr = DTools_FloatingDialog.Prompt(
                    "BỐ TRÍ NỘI THẤT HOÀN THIỆN TỰ ĐỘNG",
                    "Chọn không gian chức năng để bố trí trọn bộ nội thất chuẩn kiến trúc",
                    "🛋️",
                    PreviewKind.Generic,
                    new[]
                    {
                        new ParameterField("RoomType", "Không gian chức năng:", "Phòng Khách (Sofa Góc + Kệ TV)", null,
                            new[] { "Phòng Khách (Sofa Góc + Kệ TV)", "Phòng Bếp & Ăn (Bàn ăn 6 Ghế + Tủ bếp L)", "Phòng Ngủ Master (Giường King + Tab + Tủ)", "Phòng Vệ Sinh WC (Lavabo + Bồn cầu + Tắm)", "Phòng Làm Việc (Bàn + Ghế xoay + Kệ sách)" })
                    });

                if (pr == null) return;

                PromptPointOptions ppo = new PromptPointOptions("\n[DTT] Chọn tâm phòng để bố trí nội thất: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) return;

                string sel = pr.GetString("RoomType", "Phòng Khách");
                FurnitureRoomType rType = FurnitureRoomType.LivingRoom;
                if (sel.Contains("Bếp")) rType = FurnitureRoomType.KitchenDining;
                else if (sel.Contains("Ngủ")) rType = FurnitureRoomType.MasterBedroom;
                else if (sel.Contains("Vệ Sinh") || sel.Contains("WC")) rType = FurnitureRoomType.RestroomWc;
                else if (sel.Contains("Làm Việc")) rType = FurnitureRoomType.HomeOffice;

                using (doc.LockDocument())
                {
                    InteriorAndStaircaseEngine.GenerateRoomFurniture(doc.Database, ppr.Value, rType, 5000, 4000);
                }

                ed.WriteMessage("\n✅ [DTools] Đã bố trí cụm nội thất kiến trúc thành công!\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [DTools Lỗi] Bố trí nội thất: {ex.Message}\n");
            }
        }

        // ============================================================
        // 3. MODULE CẤP THOÁT NƯỚC & THÔNG GIÓ HVAC
        // ============================================================

        [CommandMethod("DTT_PLUMBING")]
        public static void CmdPlumbing()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                var pr = DTools_FloatingDialog.Prompt(
                    "VẼ TUYẾN ỐNG CẤP THOÁT NƯỚC",
                    "Chọn hệ thống ống cấp nước lạnh/nóng hoặc thoát sàn/phân",
                    "🚰",
                    PreviewKind.DrainPipe,
                    new[]
                    {
                        new ParameterField("SysType", "Hệ thống đường ống:", "Cấp Nước Lạnh PPR (D25)", null,
                            new[] { "Cấp Nước Lạnh PPR (D25)", "Cấp Nước Nóng PPR (D20)", "Thoát Phân Bồn Cầu uPVC (D114)", "Thoát Sàn & Lavabo uPVC (D90)", "Thoát Nước Mưa uPVC (D90)" }),
                        new ParameterField("Slope", "Độ dốc thoát nước i (%):", "1.5", "%", new[] { "1.5", "2.0", "1.0" })
                    });

                if (pr == null) return;

                PromptPointOptions ppo1 = new PromptPointOptions("\n[DTT] Chọn điểm đầu tuyến ống: ");
                PromptPointResult ppr1 = ed.GetPoint(ppo1);
                if (ppr1.Status != PromptStatus.OK) return;

                PromptPointOptions ppo2 = new PromptPointOptions("\n[DTT] Chọn điểm cuối tuyến ống: ");
                ppo2.BasePoint = ppr1.Value;
                ppo2.UseBasePoint = true;
                PromptPointResult ppr2 = ed.GetPoint(ppo2);
                if (ppr2.Status != PromptStatus.OK) return;

                string sel = pr.GetString("SysType", "Cấp Nước Lạnh");
                PipeSystemType pType = PipeSystemType.ColdWater;
                int d = 25;

                if (sel.Contains("Nóng")) { pType = PipeSystemType.HotWater; d = 20; }
                else if (sel.Contains("Phân")) { pType = PipeSystemType.DrainageSoil; d = 114; }
                else if (sel.Contains("Thoát Sàn")) { pType = PipeSystemType.DrainageWaste; d = 90; }
                else if (sel.Contains("Nước Mưa")) { pType = PipeSystemType.StormWater; d = 90; }

                double slope = pr.GetDouble("Slope", 1.5);

                using (doc.LockDocument())
                {
                    PlumbingAndHvacEngine.DrawPlumbingPipeline(doc.Database, ppr1.Value, ppr2.Value, pType, d, slope);
                }

                ed.WriteMessage("\n✅ [DTools] Đã vẽ tuyến ống cấp thoát nước thành công!\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [DTools Lỗi] Tuyến ống nước: {ex.Message}\n");
            }
        }

        [CommandMethod("DTT_HVAC_DUCT")]
        public static void CmdHvacDuct()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                var pr = DTools_FloatingDialog.Prompt(
                    "VẼ TUYẾN ỐNG GIÓ THÔNG GIÓ & ĐIỀU HÒA (HVAC)",
                    "Nhập kích thước miệng ống Rộng (W) x Cao (H) và loại ống gió",
                    "🌬️",
                    PreviewKind.Duct,
                    new[]
                    {
                        new ParameterField("Type", "Loại ống gió:", "Ống Gió Cấp SAD (Supply Air)", null,
                            new[] { "Ống Gió Cấp SAD (Supply Air)", "Ống Gió Hồi RAD (Return Air)", "Ống Gió Thải EAD (Exhaust Air)", "Ống Gió Tươi FAD (Fresh Air)" }),
                        new ParameterField("Width", "Chiều rộng ống W (mm):", "600", "mm", new[] { "400", "500", "600", "800", "1000", "1200" }),
                        new ParameterField("Height", "Chiều cao ống H (mm):", "300", "mm", new[] { "250", "300", "350", "400", "500" })
                    });

                if (pr == null) return;

                PromptPointOptions ppo1 = new PromptPointOptions("\n[DTT] Chọn điểm đầu tuyến ống gió: ");
                PromptPointResult ppr1 = ed.GetPoint(ppo1);
                if (ppr1.Status != PromptStatus.OK) return;

                PromptPointOptions ppo2 = new PromptPointOptions("\n[DTT] Chọn điểm cuối tuyến ống gió: ");
                ppo2.BasePoint = ppr1.Value;
                ppo2.UseBasePoint = true;
                PromptPointResult ppr2 = ed.GetPoint(ppo2);
                if (ppr2.Status != PromptStatus.OK) return;

                string sel = pr.GetString("Type", "Cấp");
                HvacDuctType dt = HvacDuctType.SupplyAir;
                if (sel.Contains("Hồi")) dt = HvacDuctType.ReturnAir;
                else if (sel.Contains("Thải")) dt = HvacDuctType.ExhaustAir;
                else if (sel.Contains("Tươi")) dt = HvacDuctType.FreshAir;

                double w = pr.GetDouble("Width", 600.0);
                double h = pr.GetDouble("Height", 300.0);

                using (doc.LockDocument())
                {
                    PlumbingAndHvacEngine.DrawHvacDuct(doc.Database, ppr1.Value, ppr2.Value, w, h, dt);
                }

                ed.WriteMessage("\n✅ [DTools] Đã vẽ tuyến ống gió HVAC 2D thành công!\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [DTools Lỗi] Ống gió HVAC: {ex.Message}\n");
            }
        }

        [CommandMethod("DTT_HVAC_DIFFUSER")]
        public static void CmdHvacDiffuser()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                PromptPointOptions ppo = new PromptPointOptions("\n[DTT] Chọn vị trí đặt miệng gió khuếch tán: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) return;

                using (doc.LockDocument())
                {
                    PlumbingAndHvacEngine.InsertCeilingDiffuser(doc.Database, ppr.Value, 600, 350);
                }

                ed.WriteMessage("\n✅ [DTools] Đã chèn Miệng gió khuếch tán 600x600 thành công!\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [DTools Lỗi] Miệng gió: {ex.Message}\n");
            }
        }

        // ============================================================
        // 4. MODULE IN ẤN HÀNG LOẠT (BATCH PLOT) & ĐÁNH SỐ BẢN VẼ
        // ============================================================

        [CommandMethod("DTT_RENUMBER_SHEETS")]
        public static void CmdRenumberSheets()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                var pr = DTools_FloatingDialog.Prompt(
                    "ĐÁNH SỐ THỨ TỰ BẢN VẼ TỰ ĐỘNG",
                    "Quét các khung tên từ trên xuống dưới, từ trái sang phải để đánh số",
                    "🔢",
                    PreviewKind.Generic,
                    new[]
                    {
                        new ParameterField("Prefix", "Tiền tố ký hiệu bản vẽ:", "E-", null, new[] { "E-", "A-", "M-", "S-", "KT-", "Đ-" }),
                        new ParameterField("StartNo", "Số thứ tự bắt đầu:", "1", null, new[] { "1", "01" })
                    });

                if (pr == null) return;

                string prefix = pr.GetString("Prefix", "E-");
                int startNo = pr.GetInt("StartNo", 1);

                using (doc.LockDocument())
                {
                    int total = BatchPlotAndSheetEngine.AutoRenumberSheets(doc.Database, prefix, startNo);
                    if (total > 0)
                    {
                        ed.WriteMessage($"\n✅ [DTools] Đã đánh số thứ tự tự động cho {total} khung bản vẽ thành công!\n");
                    }
                    else
                    {
                        ed.WriteMessage("\n⚠️ [DTools] Không tìm thấy khung bản vẽ phù hợp trong Model Space (Yêu cầu khung hình chữ nhật tỉ lệ A3/A4/A1).\n");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [DTools Lỗi] Đánh số bản vẽ: {ex.Message}\n");
            }
        }

        [CommandMethod("DTT_BATCH_PLOT")]
        public static void CmdBatchPlot()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                var frames = BatchPlotAndSheetEngine.DetectSheetFramesInModel(doc.Database);
                if (frames.Count == 0)
                {
                    ed.WriteMessage("\n⚠️ [DTools] Không tìm thấy khung bản vẽ trong Model. Hãy kiểm tra các Block khung tên hoặc Polyline khung A3/A4/A1!\n");
                    return;
                }

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string outDir = Path.Combine(desktop, "DTools_Plots_PDF");

                ed.WriteMessage($"\n[DTools] Đã tìm thấy {frames.Count} khung bản vẽ. Đang tiến hành xuất sang PDF tại: {outDir}...");

                int count = BatchPlotAndSheetEngine.ExecuteBatchPlotToPdf(doc.Database, outDir, "BanVe_");
                ed.WriteMessage($"\n✅ [DTools] Hoàn tất in hàng loạt! Đã tạo thành công {count} file PDF trong thư mục: {outDir}\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [DTools Lỗi] In hàng loạt: {ex.Message}\n");
            }
        }

        // ============================================================
        // 5. MODULE BÓC TÁCH KHỐI LƯỢNG TOÀN DIỆN (BOQ EXPORT)
        // ============================================================

        [CommandMethod("DTT_BOQ_FULL_EXPORT")]
        [CommandMethod("DTT_BOQ_EXPORT")]
        public static void CmdBoqExport()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n🔍 [DTools] Đang quét toàn diện các thiết bị, khối block và chiều dài tuyến dây/ống trong bản vẽ...");
                var items = ComprehensiveBoqEngine.ScanDrawingForBoq(doc.Database);

                // 1. Xuất file Excel CSV
                string csvPath = ComprehensiveBoqEngine.ExportBoqToCsvExcel(items);
                ed.WriteMessage($"\n✅ [DTools] Đã xuất file Bóc tách dự toán Excel thành công: {csvPath}");

                // 2. Chèn bảng Table CAD
                PromptPointOptions ppo = new PromptPointOptions("\n[DTT] Chọn điểm chèn Bảng Bóc Tách Khối Lượng (Table) vào bản vẽ CAD: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status == PromptStatus.OK)
                {
                    using (doc.LockDocument())
                    {
                        ComprehensiveBoqEngine.InsertBoqTableToCad(doc.Database, ppr.Value, items);
                    }
                    ed.WriteMessage("\n✅ [DTools] Đã chèn Bảng Table Thống kê Khối lượng thành công vào bản vẽ!\n");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [DTools Lỗi] Bóc tách khối lượng: {ex.Message}\n");
            }
        }
    }
}
