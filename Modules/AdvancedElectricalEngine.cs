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
    // 1. DATA MODELS CHO 5 TÍNH NĂNG ĐIỆN NÂNG CAO
    // ============================================================

    public enum MotorStarterType
    {
        DOL,        // Khởi động trực tiếp
        StarDelta,  // Khởi động Sao - Tam giác
        VFD,        // Biến tần Inverter
        SoftStarter // Khởi động mềm
    }

    public class MotorStarterConfig
    {
        public MotorStarterType Type { get; set; } = MotorStarterType.DOL;
        public string MotorTag { get; set; } = "M1";
        public string MotorName { get; set; } = "ĐỘNG CƠ BƠM CẤP NƯỚC SỐ 1";
        public double PowerKw { get; set; } = 7.5; // kW
        public double Voltage { get; set; } = 380.0; // VAC 3P
        public double PowerFactor { get; set; } = 0.85;
        public double Efficiency { get; set; } = 0.90;
        public string ControlVoltage { get; set; } = "220VAC"; // 220VAC hoặc 24VDC

        // Tự động tính toán
        public double RatedCurrent => (PowerKw * 1000.0) / (Math.Sqrt(3) * Voltage * PowerFactor * Efficiency);
        public string CbModel { get; set; }
        public string ContactorModel { get; set; }
        public string ThermalRelayModel { get; set; }
        public string CableSize { get; set; }

        public void CalculateComponents()
        {
            double i = RatedCurrent;
            // Chọn Aptomat
            double iCb = i * 1.75;
            int[] cbSizes = { 6, 10, 16, 20, 25, 32, 40, 50, 63, 80, 100, 125, 160, 200, 250, 400, 630 };
            int chosenCb = cbSizes.FirstOrDefault(s => s >= iCb);
            if (chosenCb == 0) chosenCb = 630;
            CbModel = (chosenCb <= 63) ? $"MCB 3P {chosenCb}A 6kA" : $"MCCB 3P {chosenCb}A 36kA";

            // Chọn Contactor
            if (Type == MotorStarterType.StarDelta)
            {
                int iKm = (int)Math.Ceiling(i * 0.58);
                int chosenKm = cbSizes.FirstOrDefault(s => s >= iKm);
                ContactorModel = $"3x Contactor {chosenKm}A (KM1, KM2, KM3)";
            }
            else
            {
                int chosenKm = cbSizes.FirstOrDefault(s => s >= i);
                ContactorModel = $"Contactor {chosenKm}A 3P + 1NO 1NC";
            }

            // Chọn Rơ-le nhiệt
            double iLow = Math.Round(i * 0.8, 1);
            double iHigh = Math.Round(i * 1.25, 1);
            ThermalRelayModel = $"Rơ-le nhiệt ({iLow} - {iHigh}A)";

            // Chọn cỡ cáp đồng Cu/PVC
            if (i <= 15) CableSize = "Cu/PVC 4x2.5 mm²";
            else if (i <= 25) CableSize = "Cu/PVC 4x4.0 mm²";
            else if (i <= 36) CableSize = "Cu/PVC 4x6.0 mm²";
            else if (i <= 50) CableSize = "Cu/PVC 4x10 mm²";
            else if (i <= 70) CableSize = "Cu/PVC 4x16 mm²";
            else if (i <= 100) CableSize = "Cu/PVC 4x25 mm²";
            else if (i <= 140) CableSize = "Cu/PVC 4x35 mm²";
            else if (i <= 180) CableSize = "Cu/PVC 4x50 mm²";
            else CableSize = "Cu/PVC 4x70 mm²";
        }
    }

    public class TerminalItem
    {
        public int PinNo { get; set; }
        public string InternalSource { get; set; } // ví dụ: QF1:2, KM1:14
        public string InternalWire { get; set; }   // ví dụ: 101, L1, 24V+
        public string ExternalCable { get; set; }  // ví dụ: W1: 4x2.5mm²
        public string FieldDevice { get; set; }    // ví dụ: M1 (U1-V1-W1)
        public bool IsJumpered { get; set; } = false;
    }

    public class DrcIssueItem
    {
        public string Severity { get; set; } = "ERROR"; // ERROR hoặc WARNING
        public string ErrorCode { get; set; }           // DRC-01, DRC-02, DRC-03
        public string Title { get; set; }
        public string Details { get; set; }
        public Point3d Location { get; set; }
        public string Suggestion { get; set; }
    }

    public static class AdvancedElectricalEngine
    {
        // ============================================================
        // 1. TỰ ĐỘNG SINH SƠ ĐỒ HÀNG KẸP TERMINAL (TERMINAL STRIP BUILDER)
        // ============================================================

        public static bool GenerateTerminalStrip(Document doc, string stripTag, List<TerminalItem> items, Point3d origin)
        {
            if (doc == null) return false;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            if (items == null || items.Count == 0)
            {
                items = GetDefaultTerminalItems(stripTag);
            }

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    ObjectId borderLayer = CadUtils.GetOrCreateLayer(db, "E-TERM-BORDER", 4, LineWeight.LineWeight050); // Cyan
                    ObjectId textLayer = CadUtils.GetOrCreateLayer(db, "E-TERM-TEXT", 7, LineWeight.LineWeight025);     // White
                    ObjectId jumpLayer = CadUtils.GetOrCreateLayer(db, "E-TERM-JUMPER", 1, LineWeight.LineWeight035);   // Red
                    ObjectId fillLayer = CadUtils.GetOrCreateLayer(db, "E-TERM-BLOCK", 8, LineWeight.LineWeight020);    // Gray
                    ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                    double x = origin.X;
                    double y = origin.Y;
                    double rowH = 60.0;
                    double colW_Src = 180.0;
                    double colW_InWire = 120.0;
                    double colW_Pin = 90.0;
                    double colW_Cable = 160.0;
                    double colW_Field = 240.0;
                    double totalW = colW_Src + colW_InWire + colW_Pin + colW_Cable + colW_Field;

                    // 1. TIÊU ĐỀ HÀNG KẸP
                    DrawRect(btr, tr, borderLayer, x, y, totalW, 70.0);
                    AddText(btr, tr, textLayer, textStyle, $"BẢNG ĐẤU NỐI HÀNG KẸP CẦU ĐẤU: -{stripTag.ToUpper()}", x + totalW / 2.0, y + 35.0, 20.0, AttachmentPoint.MiddleCenter);

                    // 2. HEADER CỘT
                    double hy = y - 45.0;
                    DrawRect(btr, tr, borderLayer, x, hy, totalW, 45.0);
                    double cx = x;
                    AddText(btr, tr, textLayer, textStyle, "THIẾT BỊ TRONG TỦ", cx + colW_Src / 2.0, hy + 22.0, 11.0, AttachmentPoint.MiddleCenter);
                    cx += colW_Src;
                    AddText(btr, tr, textLayer, textStyle, "SỐ DÂY TRONG", cx + colW_InWire / 2.0, hy + 22.0, 11.0, AttachmentPoint.MiddleCenter);
                    cx += colW_InWire;
                    AddText(btr, tr, textLayer, textStyle, "CHÂN KẸP", cx + colW_Pin / 2.0, hy + 22.0, 12.0, AttachmentPoint.MiddleCenter);
                    cx += colW_Pin;
                    AddText(btr, tr, textLayer, textStyle, "CÁP NGOÀI TỦ", cx + colW_Cable / 2.0, hy + 22.0, 11.0, AttachmentPoint.MiddleCenter);
                    cx += colW_Cable;
                    AddText(btr, tr, textLayer, textStyle, "THIẾT BỊ HIỆN TRƯỜNG", cx + colW_Field / 2.0, hy + 22.0, 11.0, AttachmentPoint.MiddleCenter);

                    // 3. TỪNG HÀNG CHÂN KẸP TERMINAL
                    for (int i = 0; i < items.Count; i++)
                    {
                        var it = items[i];
                        double ry = hy - (i + 1) * rowH;

                        // Khung hàng
                        DrawRect(btr, tr, fillLayer, x, ry, totalW, rowH);

                        // Phân cột
                        cx = x;
                        AddText(btr, tr, textLayer, textStyle, it.InternalSource ?? "", cx + 15.0, ry + rowH / 2.0, 12.0, AttachmentPoint.MiddleLeft);

                        cx += colW_Src;
                        AddText(btr, tr, textLayer, textStyle, it.InternalWire ?? "", cx + colW_InWire / 2.0, ry + rowH / 2.0, 13.0, AttachmentPoint.MiddleCenter);

                        // Biểu tượng chân Terminal Block (Hình tròn có ốc vít)
                        cx += colW_InWire;
                        double pinCenterX = cx + colW_Pin / 2.0;
                        double pinCenterY = ry + rowH / 2.0;
                        DrawRect(btr, tr, borderLayer, pinCenterX - 20.0, pinCenterY - 20.0, 40.0, 40.0);
                        DrawCircle(btr, tr, textLayer, pinCenterX, pinCenterY, 12.0);
                        AddText(btr, tr, textLayer, textStyle, it.PinNo.ToString(), pinCenterX, pinCenterY, 13.0, AttachmentPoint.MiddleCenter);

                        // Cầu nối Jumper giữa các chân nếu có
                        if (it.IsJumpered && i > 0 && items[i - 1].IsJumpered)
                        {
                            Line jump = new Line(new Point3d(pinCenterX, pinCenterY + 20.0, 0), new Point3d(pinCenterX, pinCenterY + rowH - 20.0, 0)) { LayerId = jumpLayer };
                            btr.AppendEntity(jump);
                            tr.AddNewlyCreatedDBObject(jump, true);
                        }

                        cx += colW_Pin;
                        AddText(btr, tr, textLayer, textStyle, it.ExternalCable ?? "", cx + 15.0, ry + rowH / 2.0, 11.0, AttachmentPoint.MiddleLeft);

                        cx += colW_Cable;
                        AddText(btr, tr, textLayer, textStyle, it.FieldDevice ?? "", cx + 15.0, ry + rowH / 2.0, 11.5, AttachmentPoint.MiddleLeft);
                    }

                    tr.Commit();
                }

                ed.WriteMessage($"\n✅ [DTools PRO] Đã tự động sinh sơ đồ hàng kẹp Terminal Strip [-{stripTag}] với {items.Count} chân kẹp!\n");
                return true;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[DTools LỖI] Sinh hàng kẹp Terminal thất bại: {ex.Message}\n");
                return false;
            }
        }

        public static List<TerminalItem> GetDefaultTerminalItems(string stripTag)
        {
            return new List<TerminalItem>
            {
                new TerminalItem { PinNo = 1, InternalSource = "KM1:2", InternalWire = "U1", ExternalCable = "W1: 4x4.0 mm²", FieldDevice = "M1 (U1 Động cơ Bơm)", IsJumpered = false },
                new TerminalItem { PinNo = 2, InternalSource = "KM1:4", InternalWire = "V1", ExternalCable = "W1: 4x4.0 mm²", FieldDevice = "M1 (V1 Động cơ Bơm)", IsJumpered = false },
                new TerminalItem { PinNo = 3, InternalSource = "KM1:6", InternalWire = "W1", ExternalCable = "W1: 4x4.0 mm²", FieldDevice = "M1 (W1 Động cơ Bơm)", IsJumpered = false },
                new TerminalItem { PinNo = 4, InternalSource = "BAR_PE", InternalWire = "PE", ExternalCable = "W1: 4x4.0 mm²", FieldDevice = "Vỏ tiếp địa động cơ M1", IsJumpered = true },
                new TerminalItem { PinNo = 5, InternalSource = "BAR_PE", InternalWire = "PE", ExternalCable = "W2: Tiếp địa ngoài", FieldDevice = "Cọc tiếp địa trạm", IsJumpered = true },
                new TerminalItem { PinNo = 6, InternalSource = "PLC:Q0.0", InternalWire = "201", ExternalCable = "W3: 2x1.0 mm²", FieldDevice = "Y1 Van điện từ cấp nước", IsJumpered = false },
                new TerminalItem { PinNo = 7, InternalSource = "PLC:24V-", InternalWire = "0V", ExternalCable = "W3: 2x1.0 mm²", FieldDevice = "Y1 Chân âm van điện từ", IsJumpered = true },
                new TerminalItem { PinNo = 8, InternalSource = "PLC:I0.0", InternalWire = "301", ExternalCable = "W4: 2x0.75 mm²", FieldDevice = "S1 Nút nhấn Dừng Khẩn Ngoài", IsJumpered = false },
                new TerminalItem { PinNo = 9, InternalSource = "PLC:24V+", InternalWire = "24V", ExternalCable = "W4: 2x0.75 mm²", FieldDevice = "S1 Tiếp điểm NC", IsJumpered = true }
            };
        }

        // ============================================================
        // 2. ĐÁNH SỐ DÂY TỰ ĐỘNG (SMART WIRE NUMBERING)
        // ============================================================

        public static int AutoNumberWires(Document doc, int startNumber = 101, string prefix = "")
        {
            if (doc == null) return 0;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            int count = 0;

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    ObjectId numLayer = CadUtils.GetOrCreateLayer(db, "E-WIRE-NUM", 2, LineWeight.LineWeight018); // Yellow
                    ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                    HashSet<string> powerWireNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "L1", "L2", "L3", "N", "PE", "24V+", "24V-", "0V", "GND", "COM", "24VDC", "220VAC", "+24V", "-24V"
                    };

                    int currentWireNum = startNumber;

                    foreach (ObjectId objId in btr)
                    {
                        Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        string layerName = ent.Layer ?? "";
                        if (!layerName.StartsWith("E-WIRE", StringComparison.OrdinalIgnoreCase) &&
                            !layerName.StartsWith("WIRE", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        Point3d midPt = Point3d.Origin;
                        double angle = 0.0;

                        if (ent is Line line)
                        {
                            midPt = new Point3d((line.StartPoint.X + line.EndPoint.X) / 2.0, (line.StartPoint.Y + line.EndPoint.Y) / 2.0, 0);
                            double dx = line.EndPoint.X - line.StartPoint.X;
                            double dy = line.EndPoint.Y - line.StartPoint.Y;
                            angle = Math.Atan2(dy, dx);
                        }
                        else if (ent is Polyline pl && pl.NumberOfVertices >= 2)
                        {
                            Point2d p1 = pl.GetPoint2dAt(0);
                            Point2d p2 = pl.GetPoint2dAt(1);
                            midPt = new Point3d((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0, 0);
                            angle = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                        }
                        else continue;

                        // Điều chỉnh góc chữ luôn dễ đọc (không bị lộn ngược)
                        if (angle > Math.PI / 2.0) angle -= Math.PI;
                        if (angle < -Math.PI / 2.0) angle += Math.PI;

                        string wireLabel = string.IsNullOrEmpty(prefix) ? currentWireNum.ToString() : $"{prefix}{currentWireNum}";

                        // Chèn Text số dây
                        MText mt = new MText
                        {
                            Contents = wireLabel,
                            Location = midPt + new Vector3d(0, 10, 0),
                            TextHeight = 12.0,
                            Rotation = angle,
                            Attachment = AttachmentPoint.MiddleCenter,
                            LayerId = numLayer
                        };
                        if (textStyle.IsValid) mt.TextStyleId = textStyle;

                        btr.AppendEntity(mt);
                        tr.AddNewlyCreatedDBObject(mt, true);

                        currentWireNum++;
                        count++;
                    }

                    tr.Commit();
                }

                ed.WriteMessage($"\n✅ [DTools PRO] Đã đánh số dây tự động thành công cho {count} đoạn mạch!\n");
                return count;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[DTools LỖI] Đánh số dây thất bại: {ex.Message}\n");
                return 0;
            }
        }

        // ============================================================
        // 3. THAM CHIẾU CHÉO TỰ ĐỘNG (RELAY CROSS-REFERENCE)
        // ============================================================

        public static int GenerateRelayCrossRef(Document doc)
        {
            if (doc == null) return 0;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            int count = 0;

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    ObjectId xrefLayer = CadUtils.GetOrCreateLayer(db, "E-XREF-TABLE", 6, LineWeight.LineWeight018); // Magenta
                    ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                    // Tìm các nhãn cuộn hút (KM1, KM2, KA1...)
                    List<Tuple<string, Point3d>> coils = new List<Tuple<string, Point3d>>();

                    foreach (ObjectId id in btr)
                    {
                        if (tr.GetObject(id, OpenMode.ForRead) is MText mt)
                        {
                            string t = mt.Contents.Trim();
                            if ((t.StartsWith("KM", StringComparison.OrdinalIgnoreCase) || t.StartsWith("KA", StringComparison.OrdinalIgnoreCase)) && t.Length <= 5)
                            {
                                coils.Add(Tuple.Create(t, mt.Location));
                            }
                        }
                    }

                    // Với mỗi cuộn hút, vẽ bảng tham chiếu chéo tiếp điểm dưới chân
                    foreach (var coil in coils)
                    {
                        Point3d loc = coil.Item2;
                        double x = loc.X - 50.0;
                        double y = loc.Y - 60.0;
                        double tw = 120.0;
                        double th = 40.0;

                        // Khung gương tiếp điểm
                        DrawRect(btr, tr, xrefLayer, x, y, tw, th);

                        // Cột 1: NO (13-14) | Cột 2: NC (21-22)
                        Line split = new Line(new Point3d(x + tw / 2.0, y, 0), new Point3d(x + tw / 2.0, y + th, 0)) { LayerId = xrefLayer };
                        btr.AppendEntity(split);
                        tr.AddNewlyCreatedDBObject(split, true);

                        AddText(btr, tr, xrefLayer, textStyle, "NO: 13-14", x + tw * 0.25, y + th * 0.7, 7.5, AttachmentPoint.MiddleCenter);
                        AddText(btr, tr, xrefLayer, textStyle, "/1.3", x + tw * 0.25, y + th * 0.3, 8.5, AttachmentPoint.MiddleCenter);

                        AddText(btr, tr, xrefLayer, textStyle, "NC: 21-22", x + tw * 0.75, y + th * 0.7, 7.5, AttachmentPoint.MiddleCenter);
                        AddText(btr, tr, xrefLayer, textStyle, "/2.5", x + tw * 0.75, y + th * 0.3, 8.5, AttachmentPoint.MiddleCenter);

                        count++;
                    }

                    tr.Commit();
                }

                ed.WriteMessage($"\n✅ [DTools PRO] Đã sinh bảng tham chiếu chéo Cross-Reference cho {count} khí cụ Rơ-le / Contactor!\n");
                return count;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[DTools LỖI] Sinh Cross-Reference thất bại: {ex.Message}\n");
                return 0;
            }
        }

        // ============================================================
        // 4. BỘ MẪU MẠCH KHỞI ĐỘNG ĐỘNG CƠ (MOTOR STARTER GENERATOR)
        // ============================================================

        public static bool GenerateMotorStarterCircuit(Document doc, MotorStarterConfig cfg, Point3d origin)
        {
            if (doc == null || cfg == null) return false;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            cfg.CalculateComponents();

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    ObjectId pwrWireLayer = CadUtils.GetOrCreateLayer(db, "E-PWR-WIRE", 1, LineWeight.LineWeight050);  // Red
                    ObjectId ctrlWireLayer = CadUtils.GetOrCreateLayer(db, "E-CTRL-WIRE", 4, LineWeight.LineWeight030); // Cyan
                    ObjectId compLayer = CadUtils.GetOrCreateLayer(db, "E-DEVICE", 2, LineWeight.LineWeight035);        // Yellow
                    ObjectId textLayer = CadUtils.GetOrCreateLayer(db, "E-TEXT", 7, LineWeight.LineWeight025);          // White
                    ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                    double x = origin.X;
                    double y = origin.Y;

                    // 1. TIÊU ĐỀ BỘ KHỞI ĐỘNG
                    string title = $"BỘ KHỞI ĐỘNG {cfg.Type.ToString().ToUpper()} - {cfg.MotorTag}: {cfg.MotorName.ToUpper()} (P = {cfg.PowerKw:F1}kW, I = {cfg.RatedCurrent:F1}A)";
                    DrawRect(btr, tr, compLayer, x - 20.0, y + 20.0, 960.0, 50.0);
                    AddText(btr, tr, textLayer, textStyle, title, x + 460.0, y + 45.0, 16.0, AttachmentPoint.MiddleCenter);

                    // ==========================================
                    // PHẦN A: MẠCH ĐỘNG LỰC 3 PHA (POWER CIRCUIT)
                    // ==========================================
                    double pwrX = x + 100.0;
                    double busY = y - 40.0;
                    double phaseGap = 35.0;

                    // 3 Thanh cái nguồn 3P (L1, L2, L3, PE)
                    string[] phases = { "L1", "L2", "L3" };
                    for (int p = 0; p < 3; p++)
                    {
                        double px = pwrX + p * phaseGap;
                        AddText(btr, tr, textLayer, textStyle, phases[p], px, busY + 20.0, 13.0, AttachmentPoint.BottomCenter);

                        // Dây động lực từ đỉnh xuống
                        DrawLine(btr, tr, pwrWireLayer, px, busY, px, busY - 450.0);
                    }

                    // Aptomat CB Động lực (QF1)
                    double qfY = busY - 80.0;
                    DrawRect(btr, tr, compLayer, pwrX - 15.0, qfY - 40.0, phaseGap * 2.0 + 30.0, 50.0);
                    AddText(btr, tr, textLayer, textStyle, $"QF1: {cfg.CbModel}", pwrX + phaseGap * 2.0 + 40.0, qfY - 15.0, 12.0, AttachmentPoint.MiddleLeft);

                    // Contactor Động lực (KM1)
                    double kmY = qfY - 120.0;
                    DrawRect(btr, tr, compLayer, pwrX - 15.0, kmY - 40.0, phaseGap * 2.0 + 30.0, 45.0);
                    AddText(btr, tr, textLayer, textStyle, $"KM1: {cfg.ContactorModel}", pwrX + phaseGap * 2.0 + 40.0, kmY - 15.0, 12.0, AttachmentPoint.MiddleLeft);

                    // Nếu là Sao - Tam giác: vẽ thêm nhánh Contactor KM2 & KM3
                    if (cfg.Type == MotorStarterType.StarDelta)
                    {
                        double deltaX = pwrX + 160.0;
                        DrawRect(btr, tr, compLayer, deltaX, kmY - 40.0, phaseGap * 2.0 + 20.0, 45.0);
                        AddText(btr, tr, textLayer, textStyle, "KM2 (Tam Giác)", deltaX + phaseGap, kmY - 60.0, 11.0, AttachmentPoint.MiddleCenter);

                        double starX = deltaX + 130.0;
                        DrawRect(btr, tr, compLayer, starX, kmY - 40.0, phaseGap * 2.0 + 20.0, 45.0);
                        AddText(btr, tr, textLayer, textStyle, "KM3 (Sao)", starX + phaseGap, kmY - 60.0, 11.0, AttachmentPoint.MiddleCenter);
                    }

                    // Rơ-le nhiệt (F1)
                    double frY = kmY - 110.0;
                    DrawRect(btr, tr, compLayer, pwrX - 15.0, frY - 35.0, phaseGap * 2.0 + 30.0, 40.0);
                    AddText(btr, tr, textLayer, textStyle, $"F1: {cfg.ThermalRelayModel}", pwrX + phaseGap * 2.0 + 40.0, frY - 15.0, 12.0, AttachmentPoint.MiddleLeft);

                    // Hàng kẹp Terminal động lực
                    double termY = frY - 80.0;
                    DrawRect(btr, tr, compLayer, pwrX - 10.0, termY - 25.0, phaseGap * 2.0 + 20.0, 30.0);
                    AddText(btr, tr, textLayer, textStyle, "-X1: 1, 2, 3", pwrX + phaseGap * 2.0 + 40.0, termY - 10.0, 11.0, AttachmentPoint.MiddleLeft);

                    // Động cơ 3 pha M1
                    double mY = termY - 90.0;
                    double mcX = pwrX + phaseGap;
                    DrawCircle(btr, tr, compLayer, mcX, mY, 35.0);
                    AddText(btr, tr, textLayer, textStyle, "M\n3~", mcX, mY, 15.0, AttachmentPoint.MiddleCenter);
                    AddText(btr, tr, textLayer, textStyle, $"{cfg.MotorTag}\n{cfg.PowerKw:F1}kW", mcX, mY - 55.0, 12.0, AttachmentPoint.TopCenter);
                    AddText(btr, tr, textLayer, textStyle, $"Cáp: {cfg.CableSize}", mcX, mY - 90.0, 11.0, AttachmentPoint.TopCenter);

                    // ==========================================
                    // PHẦN B: MẠCH ĐIỀU KHIỂN (CONTROL CIRCUIT)
                    // ==========================================
                    double ctrlX = x + 580.0;
                    double cTopY = busY;
                    double cBotY = busY - 480.0;

                    // Thanh nguồn điều khiển trên & dưới
                    AddText(btr, tr, textLayer, textStyle, $"{cfg.ControlVoltage} L", ctrlX - 40.0, cTopY, 11.0, AttachmentPoint.MiddleRight);
                    DrawLine(btr, tr, ctrlWireLayer, ctrlX, cTopY, ctrlX + 280.0, cTopY);

                    AddText(btr, tr, textLayer, textStyle, "N / 0V", ctrlX - 40.0, cBotY, 11.0, AttachmentPoint.MiddleRight);
                    DrawLine(btr, tr, ctrlWireLayer, ctrlX, cBotY, ctrlX + 280.0, cBotY);

                    // Tuyến 1: Nhánh khởi động Contactor KM1
                    double col1X = ctrlX + 50.0;
                    DrawLine(btr, tr, ctrlWireLayer, col1X, cTopY, col1X, cBotY);

                    // Tiếp điểm sự cố Rơ-le nhiệt 95-96 (NC)
                    DrawRect(btr, tr, compLayer, col1X - 15.0, cTopY - 60.0, 30.0, 20.0);
                    AddText(btr, tr, textLayer, textStyle, "F1: 95-96 (NC)", col1X + 25.0, cTopY - 50.0, 9.5, AttachmentPoint.MiddleLeft);

                    // Nút dừng STOP (NC)
                    DrawRect(btr, tr, compLayer, col1X - 15.0, cTopY - 120.0, 30.0, 20.0);
                    AddText(btr, tr, textLayer, textStyle, "STOP (NC)", col1X + 25.0, cTopY - 110.0, 9.5, AttachmentPoint.MiddleLeft);

                    // Nút ấn START (NO) + Tiếp điểm duy trì KM1 (13-14)
                    DrawRect(btr, tr, compLayer, col1X - 15.0, cTopY - 180.0, 30.0, 20.0);
                    AddText(btr, tr, textLayer, textStyle, "START (NO)", col1X + 25.0, cTopY - 170.0, 9.5, AttachmentPoint.MiddleLeft);

                    // Cuộn hút Contactor KM1 (A1-A2)
                    DrawCircle(btr, tr, compLayer, col1X, cBotY + 80.0, 18.0);
                    AddText(btr, tr, textLayer, textStyle, "KM1", col1X, cBotY + 80.0, 11.0, AttachmentPoint.MiddleCenter);
                    AddText(btr, tr, textLayer, textStyle, "A1-A2", col1X + 25.0, cBotY + 80.0, 9.5, AttachmentPoint.MiddleLeft);

                    // Tuyến 2: Đèn báo RUN (Xanh lá)
                    double col2X = ctrlX + 160.0;
                    DrawLine(btr, tr, ctrlWireLayer, col2X, cTopY - 120.0, col2X, cBotY);
                    DrawCircle(btr, tr, compLayer, col2X, cBotY + 80.0, 15.0);
                    AddText(btr, tr, textLayer, textStyle, "HL1\nRUN", col2X, cBotY + 80.0, 8.5, AttachmentPoint.MiddleCenter);

                    // Tuyến 3: Đèn báo TRIP (Vàng)
                    double col3X = ctrlX + 240.0;
                    DrawLine(btr, tr, ctrlWireLayer, col3X, cTopY - 60.0, col3X, cBotY);
                    DrawCircle(btr, tr, compLayer, col3X, cBotY + 80.0, 15.0);
                    AddText(btr, tr, textLayer, textStyle, "HL2\nTRIP", col3X, cBotY + 80.0, 8.5, AttachmentPoint.MiddleCenter);

                    // ==========================================
                    // PHẦN C: BẢNG TRA CỨU THÔNG SỐ (SPEC SHEET)
                    // ==========================================
                    double tblX = x;
                    double tblY = mY - 140.0;
                    DrawRect(btr, tr, compLayer, tblX, tblY, 920.0, 100.0);
                    AddText(btr, tr, textLayer, textStyle, $"BẢNG CHỌN KHÍ CỤ ĐIỆN CHO ĐỘNG CƠ: {cfg.MotorTag} ({cfg.PowerKw:F1}kW - {cfg.RatedCurrent:F1}A)", tblX + 460.0, tblY + 80.0, 13.0, AttachmentPoint.MiddleCenter);
                    string specRow = $"1. Aptomat: {cfg.CbModel} | 2. Khởi động từ: {cfg.ContactorModel} | 3. Bảo vệ quá tải: {cfg.ThermalRelayModel} | 4. Cáp cấp nguồn: {cfg.CableSize}";
                    AddText(btr, tr, textLayer, textStyle, specRow, tblX + 460.0, tblY + 45.0, 10.5, AttachmentPoint.MiddleCenter);

                    tr.Commit();
                }

                ed.WriteMessage($"\n✅ [DTools PRO] Đã sinh trọn bộ Mạch Động Lực + Điều Khiển [{cfg.Type}] cho động cơ [{cfg.MotorTag} - {cfg.PowerKw}kW] chuẩn IEC!\n");
                return true;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[DTools LỖI] Sinh mạch động cơ thất bại: {ex.Message}\n");
                return false;
            }
        }

        // ============================================================
        // 5. KIỂM LỖI MẠCH ĐIỆN TỰ ĐỘNG (ELECTRICAL DESIGN RULE CHECK - DRC)
        // ============================================================

        public static List<DrcIssueItem> RunElectricalDRC(Document doc)
        {
            var issues = new List<DrcIssueItem>();
            if (doc == null) return issues;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                    // 1. KIỂM TRA TRÙNG NHÃN THIẾT BỊ (DUPLICATE TAG COLLISION)
                    Dictionary<string, List<Point3d>> tagMap = new Dictionary<string, List<Point3d>>(StringComparer.OrdinalIgnoreCase);

                    foreach (ObjectId id in btr)
                    {
                        if (tr.GetObject(id, OpenMode.ForRead) is MText mt)
                        {
                            string t = mt.Contents.Trim();
                            // Nhận diện mã tag chuẩn (QF1, KM1, KA1, PLC1, M1...)
                            if (t.Length >= 2 && t.Length <= 8 && char.IsLetter(t[0]) && char.IsDigit(t[t.Length - 1]))
                            {
                                if (!tagMap.ContainsKey(t)) tagMap[t] = new List<Point3d>();
                                tagMap[t].Add(mt.Location);
                            }
                        }
                    }

                    foreach (var pair in tagMap)
                    {
                        if (pair.Value.Count > 1)
                        {
                            issues.Add(new DrcIssueItem
                            {
                                Severity = "ERROR",
                                ErrorCode = "DRC-TAG-DUP",
                                Title = $"Trùng nhãn thiết bị: [{pair.Key}]",
                                Details = $"Phát hiện {pair.Value.Count} linh kiện mang cùng một ký hiệu nhãn [{pair.Key}] trên bản vẽ.",
                                Location = pair.Value[0],
                                Suggestion = $"Đổi tên một trong các thiết bị thành [{pair.Key}_A] hoặc số thứ tự tiếp theo."
                            });
                        }
                    }

                    // 2. KIỂM TRA ĐẦU DÂY HỞ MẠCH (FLOATING WIRE / OPEN CIRCUIT)
                    List<Point3d> wireEndpoints = new List<Point3d>();
                    foreach (ObjectId id in btr)
                    {
                        if (tr.GetObject(id, OpenMode.ForRead) is Line l)
                        {
                            if (l.Layer.IndexOf("WIRE", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                wireEndpoints.Add(l.StartPoint);
                                wireEndpoints.Add(l.EndPoint);
                            }
                        }
                    }

                    // Tìm những điểm chỉ xuất hiện đúng 1 lần và không nằm gần block nào
                    for (int i = 0; i < wireEndpoints.Count; i += 2)
                    {
                        Point3d p = wireEndpoints[i];
                        int neighborCount = wireEndpoints.Count(pt => pt.DistanceTo(p) < 15.0);
                        if (neighborCount <= 1)
                        {
                            issues.Add(new DrcIssueItem
                            {
                                Severity = "WARNING",
                                ErrorCode = "DRC-WIRE-OPEN",
                                Title = "Đầu dây điện bị hở mạch (Open Circuit)",
                                Details = $"Đoạn dây tại tọa độ ({p.X:F0}, {p.Y:F0}) có một đầu mút lơ lửng không đấu nối vào cọc hoặc thiết bị.",
                                Location = p,
                                Suggestion = "Đấu nối dây vào cọc Terminal hoặc xóa bỏ đoạn dây thừa."
                            });
                            if (issues.Count > 15) break; // Giới hạn số lượng cảnh báo
                        }
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[DTools LỖI] Kiểm tra DRC thất bại: {ex.Message}\n");
            }

            return issues;
        }

        public static void MarkDrcIssuesOnCad(Document doc, List<DrcIssueItem> issues)
        {
            if (doc == null || issues == null || issues.Count == 0) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    ObjectId errLayer = CadUtils.GetOrCreateLayer(db, "E-DRC-ERROR", 1, LineWeight.LineWeight050); // Red
                    ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                    foreach (var iss in issues)
                    {
                        Point3d loc = iss.Location;
                        // Vẽ vòng tròn đám mây cảnh báo đỏ
                        Circle c = new Circle(loc, Vector3d.ZAxis, 45.0) { LayerId = errLayer };
                        btr.AppendEntity(c);
                        tr.AddNewlyCreatedDBObject(c, true);

                        // Cờ nhãn cảnh báo
                        AddText(btr, tr, errLayer, textStyle, $"⚠ {iss.ErrorCode}: {iss.Title}", loc.X + 50.0, loc.Y + 25.0, 11.0, AttachmentPoint.MiddleLeft);
                    }

                    tr.Commit();
                }

                ed.WriteMessage($"\n✅ [DTools PRO] Đã khoanh vùng {issues.Count} vị trí lỗi DRC bằng vòng tròn đỏ trên bản vẽ!\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[DTools LỖI] Đánh dấu DRC thất bại: {ex.Message}\n");
            }
        }

        // ============================================================
        // HELPER GRAPHICS FUNCTIONS
        // ============================================================

        private static void DrawLine(BlockTableRecord btr, Transaction tr, ObjectId layer, double x1, double y1, double x2, double y2)
        {
            Line l = new Line(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0));
            if (layer.IsValid) l.LayerId = layer;
            btr.AppendEntity(l);
            tr.AddNewlyCreatedDBObject(l, true);
        }

        private static void DrawRect(BlockTableRecord btr, Transaction tr, ObjectId layer, double x, double y, double w, double h)
        {
            Polyline p = new Polyline();
            p.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
            p.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
            p.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
            p.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
            p.Closed = true;
            if (layer.IsValid) p.LayerId = layer;
            btr.AppendEntity(p);
            tr.AddNewlyCreatedDBObject(p, true);
        }

        private static void DrawCircle(BlockTableRecord btr, Transaction tr, ObjectId layer, double x, double y, double r)
        {
            Circle c = new Circle(new Point3d(x, y, 0), Vector3d.ZAxis, r);
            if (layer.IsValid) c.LayerId = layer;
            btr.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);
        }

        private static void AddText(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId style, string text, double x, double y, double h, AttachmentPoint ap)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            MText mt = new MText
            {
                Contents = text,
                Location = new Point3d(x, y, 0),
                TextHeight = h,
                Attachment = ap
            };
            if (layer.IsValid) mt.LayerId = layer;
            if (style.IsValid) mt.TextStyleId = style;
            btr.AppendEntity(mt);
            tr.AddNewlyCreatedDBObject(mt, true);
        }
    }
}
