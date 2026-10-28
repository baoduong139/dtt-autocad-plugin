using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.Modules
{
    // ============================================================
    // DTO & DỮ LIỆU TÍNH TOÁN CÁP & SƠ ĐỒ ĐƠN TUYẾN
    // ============================================================

    public class CableCalculationParam
    {
        public string CircuitName { get; set; } = "LỘ CẤP NGUỒN TỦ ĐIỆN TẦNG 1";
        public double PowerKw { get; set; } = 35.0; // kW
        public double VoltageV { get; set; } = 380.0; // 380V (3 pha) hoặc 220V (1 pha)
        public double PowerFactor { get; set; } = 0.85; // cos phi
        public double LengthM { get; set; } = 65.0; // Mét chiều dài
        public double MaxVoltageDropPercent { get; set; } = 2.5; // % sụt áp cho phép (2.5% cho chiếu sáng, 5% cho động lực)
        public string InsulationType { get; set; } = "XLPE"; // XLPE hoặc PVC
        public string InstallationMethod { get; set; } = "Máng cáp đục lỗ / Thang cáp"; // Phương pháp lắp đặt
        public double TempDeratingFactor { get; set; } = 0.94; // k1 nhiệt độ môi trường 35-40 độ C
        public double GroupDeratingFactor { get; set; } = 0.80; // k2 đi chung máng

        // Kết quả tính toán
        public double CurrentA { get; set; }
        public double DesignCurrentA { get; set; }
        public double CableSizeMm2 { get; set; }
        public string CableSpecText { get; set; }
        public string BreakerSpecText { get; set; }
        public double ActualVoltageDropV { get; set; }
        public double ActualVoltageDropPercent { get; set; }
        public bool IsPass { get; set; }
    }

    public class SldFeederItem
    {
        public string Tag { get; set; } = "F1";
        public string LoadName { get; set; } = "CHIẾU SÁNG & Ổ CẮM TẦNG 1";
        public double PowerKw { get; set; } = 12.5;
        public int PhaseCount { get; set; } = 3; // 1 hoặc 3
        public string BreakerRating { get; set; } = "MCCB 3P 32A 25kA";
        public string CableSpec { get; set; } = "Cu/XLPE/PVC (4x6+1x6)mm²";
        public double LengthM { get; set; } = 35.0;
        public double VoltageDropPct { get; set; } = 1.15;
    }

    public class SldMainPanelConfig
    {
        public string PanelTag { get; set; } = "MSB-01";
        public string PanelName { get; set; } = "TỦ PHÂN PHỐI TỔNG (MAIN SWITCHBOARD)";
        public string IncomerSource { get; set; } = "TRẠM BIẾN ÁP 400kVA & MÁY PHÁT DỰ PHÒNG";
        public double TotalPowerKw { get; set; } = 180.0;
        public double CoincidenceFactor { get; set; } = 0.85; // Hệ số đồng thời Kd
        public double PowerFactor { get; set; } = 0.88;
        public string MainBreakerRating { get; set; } = "ACB 3P 400A 50kA (MicroLogic 2.0)";
        public string BusbarRating { get; set; } = "ĐỒNG THANH CÁI CU 50x5mm (630A)";
        public List<SldFeederItem> Feeders { get; set; } = new List<SldFeederItem>();
    }

    // ============================================================
    // ĐỘNG CƠ TÍNH TOÁN CÁP & SINH SƠ ĐỒ NGUYÊN LÝ ĐƠN TUYẾN SLD
    // ============================================================

    public static class ElectricalCalculationEngine
    {
        // Bảng dòng mang tải cho phép (Ampacity) theo IEC 60364-5-52 / TCVN 7447 cho cáp đồng Cu/XLPE
        private static readonly Dictionary<double, double> XlpeAmpacityMap = new Dictionary<double, double>
        {
            { 1.5, 23.0 },
            { 2.5, 31.0 },
            { 4.0, 42.0 },
            { 6.0, 54.0 },
            { 10.0, 75.0 },
            { 16.0, 100.0 },
            { 25.0, 133.0 },
            { 35.0, 164.0 },
            { 50.0, 198.0 },
            { 70.0, 253.0 },
            { 95.0, 306.0 },
            { 120.0, 354.0 },
            { 150.0, 407.0 },
            { 185.0, 464.0 },
            { 240.0, 546.0 },
            { 300.0, 628.0 },
            { 400.0, 728.0 }
        };

        // Điện trở xoay chiều r (Ohm/km) của ruột đồng ở 90 độ C
        private static readonly Dictionary<double, double> CableResistanceMap = new Dictionary<double, double>
        {
            { 1.5, 14.8 },
            { 2.5, 9.08 },
            { 4.0, 5.68 },
            { 6.0, 3.77 },
            { 10.0, 2.24 },
            { 16.0, 1.41 },
            { 25.0, 0.884 },
            { 35.0, 0.635 },
            { 50.0, 0.473 },
            { 70.0, 0.321 },
            { 95.0, 0.232 },
            { 120.0, 0.184 },
            { 150.0, 0.150 },
            { 185.0, 0.120 },
            { 240.0, 0.091 },
            { 300.0, 0.075 },
            { 400.0, 0.060 }
        };

        private const double ReactancePerKm = 0.08; // X cảm kháng trung bình Ohm/km cho cáp hạ thế

        /// <summary>
        /// Tính toán chọn cáp điện và kiểm tra sụt áp tiêu chuẩn TCVN / IEC
        /// </summary>
        public static CableCalculationParam CalculateCableAndBreaker(CableCalculationParam p)
        {
            if (p == null) return null;

            // 1. Tính dòng điện làm việc định mức
            bool is3Phase = p.VoltageV >= 300.0;
            if (is3Phase)
            {
                p.CurrentA = (p.PowerKw * 1000.0) / (Math.Sqrt(3) * p.VoltageV * p.PowerFactor);
            }
            else
            {
                p.CurrentA = (p.PowerKw * 1000.0) / (p.VoltageV * p.PowerFactor);
            }

            // Dòng điện tính toán hiệu chỉnh theo hệ số k1 k2
            double totalK = Math.Max(0.5, p.TempDeratingFactor * p.GroupDeratingFactor);
            p.DesignCurrentA = p.CurrentA / totalK;

            // 2. Chọn Aptomat (Breaker) bảo vệ
            int[] standardBreakers = { 10, 16, 20, 25, 32, 40, 50, 63, 80, 100, 125, 160, 200, 250, 315, 400, 500, 630, 800, 1000 };
            double targetCbRating = p.CurrentA * 1.25;
            int chosenCb = standardBreakers.FirstOrDefault(b => b >= targetCbRating);
            if (chosenCb == 0) chosenCb = 1000;

            if (is3Phase)
            {
                p.BreakerSpecText = (chosenCb <= 63)
                    ? $"MCB 3P {chosenCb}A 10kA"
                    : $"MCCB 3P {chosenCb}A 36kA (Icu=36kA, Ics=100% Icu)";
            }
            else
            {
                p.BreakerSpecText = $"MCB 2P {chosenCb}A 6kA (220VAC)";
            }

            // 3. Tìm tiết diện cáp thỏa mãn cả dòng phát nóng và sụt áp
            double sinPhi = Math.Sqrt(Math.Max(0, 1.0 - p.PowerFactor * p.PowerFactor));
            double chosenSection = 1.5;
            bool found = false;

            foreach (var kvp in XlpeAmpacityMap)
            {
                double s = kvp.Key;
                double iCp = kvp.Value;

                // Điều kiện 1: Dòng mang tải phát nóng
                if (iCp < p.DesignCurrentA) continue;

                // Điều kiện 2: Kiểm tra sụt áp delta U
                double r = CableResistanceMap.ContainsKey(s) ? CableResistanceMap[s] : 0.1;
                double deltaUVolt = 0.0;

                if (is3Phase)
                {
                    // Delta U 3 pha = sqrt(3) * I * (L/1000) * (r*cos + x*sin)
                    deltaUVolt = Math.Sqrt(3) * p.CurrentA * (p.LengthM / 1000.0) * (r * p.PowerFactor + ReactancePerKm * sinPhi);
                }
                else
                {
                    // Delta U 1 pha = 2 * I * (L/1000) * (r*cos + x*sin)
                    deltaUVolt = 2.0 * p.CurrentA * (p.LengthM / 1000.0) * (r * p.PowerFactor + ReactancePerKm * sinPhi);
                }

                double deltaUPct = (deltaUVolt / p.VoltageV) * 100.0;

                if (deltaUPct <= p.MaxVoltageDropPercent)
                {
                    chosenSection = s;
                    p.ActualVoltageDropV = Math.Round(deltaUVolt, 2);
                    p.ActualVoltageDropPercent = Math.Round(deltaUPct, 2);
                    p.IsPass = true;
                    found = true;
                    break;
                }
            }

            // Nếu không tìm thấy trong bảng đơn, lấy tiết diện lớn nhất
            if (!found)
            {
                chosenSection = 400.0;
                p.ActualVoltageDropPercent = p.MaxVoltageDropPercent;
                p.IsPass = false;
            }

            p.CableSizeMm2 = chosenSection;
            if (is3Phase)
            {
                double neutralSize = chosenSection <= 16 ? chosenSection : chosenSection / 2.0;
                double peSize = chosenSection <= 16 ? chosenSection : (chosenSection <= 35 ? 16 : chosenSection / 2.0);
                p.CableSpecText = $"Cu/{p.InsulationType}/PVC (3x{chosenSection:0.#} + 1x{neutralSize:0.#} + 1x{peSize:0.#})mm² - Cadivi";
            }
            else
            {
                p.CableSpecText = $"Cu/{p.InsulationType}/PVC (2x{chosenSection:0.#} + 1x{chosenSection:0.#}E)mm² - Cadivi";
            }

            return p;
        }

        /// <summary>
        /// Tự động sinh Sơ Đồ Nguyên Lý Đơn Tuyến (Single Line Diagram) đầy đủ ra bản vẽ AutoCAD
        /// </summary>
        public static void GenerateSingleLineDiagram(Database db, Point3d startPt, SldMainPanelConfig cfg)
        {
            if (db == null || cfg == null) return;

            ObjectId layerMain = CadUtils.GetOrCreateLayer(db, "E-SLD-MAIN", 1);     // Đỏ: Lộ tổng & Thanh cái
            ObjectId layerFeeder = CadUtils.GetOrCreateLayer(db, "E-SLD-FEEDER", 3); // Xanh lá: Lộ nhánh
            ObjectId layerText = CadUtils.GetOrCreateLayer(db, "E-SLD-TEXT", 7);     // Trắng: Chữ ghi chú
            ObjectId layerDim = CadUtils.GetOrCreateLayer(db, "E-SLD-DIM", 4);       // Cyan: Khung & Phụ trợ
            ObjectId styleId = CadUtils.GetOrCreateUnicodeStyle(db);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                double baseX = startPt.X;
                double baseY = startPt.Y;

                // 1. TIÊU ĐỀ KHỐI BẢN VẼ SƠ ĐỒ ĐƠN TUYẾN
                DBText txtTitle = new DBText
                {
                    Position = new Point3d(baseX, baseY + 1800, 0),
                    Height = 220,
                    TextString = $"SƠ ĐỒ NGUYÊN LÝ ĐƠN TUYẾN (SINGLE LINE DIAGRAM) - {cfg.PanelTag}",
                    TextStyleId = styleId,
                    LayerId = layerMain
                };
                btr.AppendEntity(txtTitle);
                tr.AddNewlyCreatedDBObject(txtTitle, true);

                DBText txtSub = new DBText
                {
                    Position = new Point3d(baseX, baseY + 1550, 0),
                    Height = 120,
                    TextString = $"{cfg.PanelName} | P_đặt={cfg.TotalPowerKw:0.#}kW | Kd={cfg.CoincidenceFactor:0.##} | cosφ={cfg.PowerFactor:0.##}",
                    TextStyleId = styleId,
                    LayerId = layerText
                };
                btr.AppendEntity(txtSub);
                tr.AddNewlyCreatedDBObject(txtSub, true);

                // 2. VẼ ĐẦU VÀO NGUỒN CẤP (INCOMER): MÁY BIẾN ÁP HOẶC ATS
                Point3d inTop = new Point3d(baseX + 400, baseY + 1300, 0);
                Point3d inBottom = new Point3d(baseX + 400, baseY + 900, 0);

                Line lineIn = new Line(inTop, inBottom) { LayerId = layerMain };
                btr.AppendEntity(lineIn);
                tr.AddNewlyCreatedDBObject(lineIn, true);

                // Ký hiệu nguồn cấp Incomer Box
                DrawIncomerSymbol(btr, tr, inTop, cfg.IncomerSource, styleId, layerText, layerMain);

                // Ký hiệu Aptomat Tổng (ACB / MCCB)
                Point3d acbPt = new Point3d(baseX + 400, baseY + 900, 0);
                DrawCircuitBreakerSymbol(btr, tr, acbPt, cfg.MainBreakerRating, isMain: true, styleId, layerMain, layerText);

                // 3. VẼ THANH CÁI ĐỒNG CHÍNH (MAIN COPPER BUSBAR)
                int feederCount = (cfg.Feeders != null && cfg.Feeders.Count > 0) ? cfg.Feeders.Count : 6;
                double feederSpacing = 450.0;
                double busbarStartX = baseX + 250;
                double busbarEndX = busbarStartX + (feederCount + 1) * feederSpacing;
                double busbarY = baseY + 600;

                Polyline busbar = new Polyline();
                busbar.AddVertexAt(0, new Point2d(busbarStartX, busbarY), 0, 18, 18); // Nét thanh cái dày 18mm
                busbar.AddVertexAt(1, new Point2d(busbarEndX, busbarY), 0, 18, 18);
                busbar.LayerId = layerMain;
                btr.AppendEntity(busbar);
                tr.AddNewlyCreatedDBObject(busbar, true);

                DBText busbarLabel = new DBText
                {
                    Position = new Point3d(busbarStartX - 20, busbarY + 30, 0),
                    Height = 90,
                    TextString = $"MAIN BUSBAR: {cfg.BusbarRating}",
                    TextStyleId = styleId,
                    LayerId = layerMain
                };
                btr.AppendEntity(busbarLabel);
                tr.AddNewlyCreatedDBObject(busbarLabel, true);

                // 4. VẼ TỪNG LỘ NHÁNH XUẤT PHÁT TỪ THANH CÁI XUỐNG PHỤ TẢI
                var feeders = cfg.Feeders;
                if (feeders == null || feeders.Count == 0)
                {
                    feeders = GetDefaultSampleFeeders();
                }

                for (int i = 0; i < feeders.Count; i++)
                {
                    var f = feeders[i];
                    double fx = busbarStartX + (i + 1) * feederSpacing;

                    // Tuyến thẳng đứng từ Busbar xuống
                    Point3d pBus = new Point3d(fx, busbarY, 0);
                    Point3d pEnd = new Point3d(fx, baseY - 650, 0);

                    Line feederLine = new Line(pBus, pEnd) { LayerId = layerFeeder };
                    btr.AppendEntity(feederLine);
                    tr.AddNewlyCreatedDBObject(feederLine, true);

                    // Nốt tiếp xúc (Contact Dot) tại thanh cái
                    Circle dot = new Circle(pBus, Vector3d.ZAxis, 8) { LayerId = layerMain };
                    btr.AppendEntity(dot);
                    tr.AddNewlyCreatedDBObject(dot, true);

                    // Ký hiệu Aptomat nhánh (MCB / MCCB)
                    Point3d breakerPt = new Point3d(fx, baseY + 400, 0);
                    DrawCircuitBreakerSymbol(btr, tr, breakerPt, f.BreakerRating, isMain: false, styleId, layerFeeder, layerText);

                    // Ký hiệu Cáp & Thông số sụt áp
                    Point3d cablePt = new Point3d(fx, baseY + 100, 0);
                    DrawCableSpecAnnotation(btr, tr, cablePt, f, styleId, layerText);

                    // Ký hiệu Phụ tải đầu cuối (Tủ điện tầng / Thiết bị)
                    Point3d loadPt = new Point3d(fx, baseY - 650, 0);
                    DrawLoadTerminalBlock(btr, tr, loadPt, f, styleId, layerFeeder, layerText);
                }

                // 5. VẼ KHUNG BẢNG TỔNG QUÁT BẢO VỆ & TIÊU CHUẨN KỸ THUẬT
                DrawStandardLegendBox(btr, tr, new Point3d(busbarEndX + 100, baseY + 1400, 0), styleId, layerDim, layerText);

                tr.Commit();
            }
        }

        private static List<SldFeederItem> GetDefaultSampleFeeders()
        {
            return new List<SldFeederItem>
            {
                new SldFeederItem { Tag = "L1", LoadName = "TỦ ĐIỆN TẦNG 1 (DB-01)", PowerKw = 25.0, BreakerRating = "MCCB 3P 50A 25kA", CableSpec = "Cu/XLPE (4x16+1x10)mm²", LengthM = 30, VoltageDropPct = 1.05 },
                new SldFeederItem { Tag = "L2", LoadName = "TỦ ĐIỆN TẦNG 2 (DB-02)", PowerKw = 22.0, BreakerRating = "MCCB 3P 50A 25kA", CableSpec = "Cu/XLPE (4x16+1x10)mm²", LengthM = 45, VoltageDropPct = 1.48 },
                new SldFeederItem { Tag = "L3", LoadName = "HỆ THỐNG ĐIỀU HÒA HVAC", PowerKw = 45.0, BreakerRating = "MCCB 3P 100A 36kA", CableSpec = "Cu/XLPE (3x35+1x16)mm²", LengthM = 55, VoltageDropPct = 1.82 },
                new SldFeederItem { Tag = "L4", LoadName = "TRẠM BƠM CẤP NƯỚC & PCCC", PowerKw = 30.0, BreakerRating = "MCCB 3P 63A 36kA", CableSpec = "Cu/XLPE (3x25+1x16)mm²", LengthM = 25, VoltageDropPct = 0.95 },
                new SldFeederItem { Tag = "L5", LoadName = "THANG MÁY CHỞ KHÁCH 1000KG", PowerKw = 15.0, BreakerRating = "MCCB 3P 40A 25kA", CableSpec = "Cu/XLPE (4x10+1x6)mm²", LengthM = 40, VoltageDropPct = 1.20 },
                new SldFeederItem { Tag = "L6", LoadName = "DỰ PHÒNG MỞ RỘNG (SPARE)", PowerKw = 20.0, BreakerRating = "MCCB 3P 50A 25kA", CableSpec = "Cáp chờ dự phòng", LengthM = 0, VoltageDropPct = 0.0 }
            };
        }

        private static void DrawIncomerSymbol(BlockTableRecord btr, Transaction tr, Point3d pt, string sourceName, ObjectId styleId, ObjectId layerText, ObjectId layerMain)
        {
            // Vòng tròn đôi máy biến áp (Transformer symbol)
            Circle c1 = new Circle(new Point3d(pt.X, pt.Y + 90, 0), Vector3d.ZAxis, 60) { LayerId = layerMain };
            Circle c2 = new Circle(new Point3d(pt.X, pt.Y + 180, 0), Vector3d.ZAxis, 60) { LayerId = layerMain };
            btr.AppendEntity(c1); tr.AddNewlyCreatedDBObject(c1, true);
            btr.AppendEntity(c2); tr.AddNewlyCreatedDBObject(c2, true);

            DBText txtSrc = new DBText
            {
                Position = new Point3d(pt.X + 80, pt.Y + 140, 0),
                Height = 85,
                TextString = $"NGUỒN CẤP: {sourceName}",
                TextStyleId = styleId,
                LayerId = layerText
            };
            btr.AppendEntity(txtSrc); tr.AddNewlyCreatedDBObject(txtSrc, true);
        }

        private static void DrawCircuitBreakerSymbol(BlockTableRecord btr, Transaction tr, Point3d pt, string breakerName, bool isMain, ObjectId styleId, ObjectId layerSymbol, ObjectId layerText)
        {
            double w = isMain ? 50 : 35;
            double h = isMain ? 90 : 70;

            // Hình chữ nhật đại diện Aptomat
            Polyline box = new Polyline();
            box.AddVertexAt(0, new Point2d(pt.X - w / 2, pt.Y - h / 2), 0, 0, 0);
            box.AddVertexAt(1, new Point2d(pt.X + w / 2, pt.Y - h / 2), 0, 0, 0);
            box.AddVertexAt(2, new Point2d(pt.X + w / 2, pt.Y + h / 2), 0, 0, 0);
            box.AddVertexAt(3, new Point2d(pt.X - w / 2, pt.Y + h / 2), 0, 0, 0);
            box.Closed = true;
            box.LayerId = layerSymbol;
            btr.AppendEntity(box); tr.AddNewlyCreatedDBObject(box, true);

            // Gạch chéo bảo vệ quá tải & ngắn mạch
            Line cross = new Line(new Point3d(pt.X - w / 2, pt.Y - h / 2, 0), new Point3d(pt.X + w / 2, pt.Y + h / 2, 0)) { LayerId = layerSymbol };
            btr.AppendEntity(cross); tr.AddNewlyCreatedDBObject(cross, true);

            // Nhãn tên thiết bị
            DBText txt = new DBText
            {
                Position = new Point3d(pt.X + w / 2 + 15, pt.Y - 20, 0),
                Height = isMain ? 80 : 65,
                TextString = breakerName,
                TextStyleId = styleId,
                LayerId = layerText
            };
            btr.AppendEntity(txt); tr.AddNewlyCreatedDBObject(txt, true);
        }

        private static void DrawCableSpecAnnotation(BlockTableRecord btr, Transaction tr, Point3d pt, SldFeederItem f, ObjectId styleId, ObjectId layerText)
        {
            // Nhánh dẫn ghi chú cáp (Leader slash)
            Line slash = new Line(new Point3d(pt.X - 15, pt.Y - 15, 0), new Point3d(pt.X + 15, pt.Y + 15, 0)) { LayerId = layerText };
            btr.AppendEntity(slash); tr.AddNewlyCreatedDBObject(slash, true);

            DBText txtCable = new DBText
            {
                Position = new Point3d(pt.X + 25, pt.Y + 10, 0),
                Height = 65,
                TextString = f.CableSpec,
                TextStyleId = styleId,
                LayerId = layerText
            };
            btr.AppendEntity(txtCable); tr.AddNewlyCreatedDBObject(txtCable, true);

            DBText txtDrop = new DBText
            {
                Position = new Point3d(pt.X + 25, pt.Y - 60, 0),
                Height = 60,
                TextString = $"L={f.LengthM:0}m | ΔU={f.VoltageDropPct:0.##}% | P={f.PowerKw:0.#}kW",
                TextStyleId = styleId,
                LayerId = layerText
            };
            btr.AppendEntity(txtDrop); tr.AddNewlyCreatedDBObject(txtDrop, true);
        }

        private static void DrawLoadTerminalBlock(BlockTableRecord btr, Transaction tr, Point3d pt, SldFeederItem f, ObjectId styleId, ObjectId layerBox, ObjectId layerText)
        {
            double w = 220;
            double h = 90;

            Polyline box = new Polyline();
            box.AddVertexAt(0, new Point2d(pt.X - w / 2, pt.Y - h), 0, 0, 0);
            box.AddVertexAt(1, new Point2d(pt.X + w / 2, pt.Y - h), 0, 0, 0);
            box.AddVertexAt(2, new Point2d(pt.X + w / 2, pt.Y), 0, 0, 0);
            box.AddVertexAt(3, new Point2d(pt.X - w / 2, pt.Y), 0, 0, 0);
            box.Closed = true;
            box.LayerId = layerBox;
            btr.AppendEntity(box); tr.AddNewlyCreatedDBObject(box, true);

            DBText txtTag = new DBText
            {
                Position = new Point3d(pt.X - w / 2 + 15, pt.Y - 35, 0),
                Height = 70,
                TextString = f.Tag,
                TextStyleId = styleId,
                LayerId = layerText
            };
            btr.AppendEntity(txtTag); tr.AddNewlyCreatedDBObject(txtTag, true);

            DBText txtName = new DBText
            {
                Position = new Point3d(pt.X - w / 2 + 15, pt.Y - 75, 0),
                Height = 55,
                TextString = f.LoadName,
                TextStyleId = styleId,
                LayerId = layerText
            };
            btr.AppendEntity(txtName); tr.AddNewlyCreatedDBObject(txtName, true);
        }

        private static void DrawStandardLegendBox(BlockTableRecord btr, Transaction tr, Point3d pt, ObjectId styleId, ObjectId layerDim, ObjectId layerText)
        {
            double w = 700;
            double h = 550;

            Polyline frame = new Polyline();
            frame.AddVertexAt(0, new Point2d(pt.X, pt.Y - h), 0, 0, 0);
            frame.AddVertexAt(1, new Point2d(pt.X + w, pt.Y - h), 0, 0, 0);
            frame.AddVertexAt(2, new Point2d(pt.X + w, pt.Y), 0, 0, 0);
            frame.AddVertexAt(3, new Point2d(pt.X, pt.Y), 0, 0, 0);
            frame.Closed = true;
            frame.LayerId = layerDim;
            btr.AppendEntity(frame); tr.AddNewlyCreatedDBObject(frame, true);

            string[] notes = new string[]
            {
                "GHI CHÚ KỸ THUẬT TIÊU CHUẨN TCVN / IEC:",
                "1. Điện áp hệ thống: 3 Pha 4 Dây 380/220VAC, 50Hz, TN-S.",
                "2. Cáp điện sử dụng đồng nguyên chất Cu/XLPE/PVC - Cadivi.",
                "3. Độ sụt áp cho phép: Chiếu sáng <= 2.5%, Động lực <= 5.0%.",
                "4. Dòng cắt ngắn mạch Icu: Tủ tổng MSB >= 36kA, Tủ tầng DB >= 10kA.",
                "5. Tiếp địa an toàn PE nối chung hệ bãi cọc đồng R <= 4 Ohm.",
                "6. Tất cả thiết bị tuân thủ IEC 60947-2, IEC 60898 và TCVN 7447."
            };

            for (int i = 0; i < notes.Length; i++)
            {
                DBText txt = new DBText
                {
                    Position = new Point3d(pt.X + 25, pt.Y - 50 - i * 65, 0),
                    Height = (i == 0) ? 75 : 55,
                    TextString = notes[i],
                    TextStyleId = styleId,
                    LayerId = layerText
                };
                btr.AppendEntity(txt); tr.AddNewlyCreatedDBObject(txt, true);
            }
        }
    }
}
