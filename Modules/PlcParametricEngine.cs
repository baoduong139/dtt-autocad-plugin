using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.Modules
{
    public enum PlcBrand
    {
        Siemens_S7_1200,
        Siemens_S7_1500,
        Mitsubishi_FX5U,
        Omron_CP1H,
        Schneider_M221
    }

    public class PlcParametricConfig
    {
        public PlcBrand Brand { get; set; } = PlcBrand.Siemens_S7_1200;
        public string ModelName { get; set; } = "CPU 1214C DC/DC/DC";
        public string TagName { get; set; } = "PLC-01";
        public string PowerSupply { get; set; } = "24VDC (L+ / M)"; // hoặc 220VAC
        public int DigitalInputsCount { get; set; } = 14;
        public int DigitalOutputsCount { get; set; } = 10;
        public int AnalogInputsCount { get; set; } = 2;
        public int AnalogOutputsCount { get; set; } = 0;
        public string StartDiAddress { get; set; } = "I0.0";
        public string StartDoAddress { get; set; } = "Q0.0";
        public double TerminalSpacing { get; set; } = 25.0; // mm
    }

    public static class PlcParametricEngine
    {
        // ============================================================
        // HÀM SINH SƠ ĐỒ NGUYÊN LÝ PLC PARAMETRIC SCHEMATIC
        // ============================================================

        public static bool GeneratePlcBlock(Document doc, PlcParametricConfig cfg, Point3d insertPt)
        {
            if (doc == null || cfg == null) return false;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // 1. TẠO CÁC LAYER CHUẨN KỸ THUẬT ĐIỆN TỰ ĐỘNG HÓA
                    ObjectId bodyLayer = CadUtils.GetOrCreateLayer(db, "E-PLC-BODY", 8, LineWeight.LineWeight035);      // RAL 7035 Xám đậm
                    ObjectId borderLayer = CadUtils.GetOrCreateLayer(db, "E-PLC-BORDER", 4, LineWeight.LineWeight050);  // Cyan viền dày
                    ObjectId terminalLayer = CadUtils.GetOrCreateLayer(db, "E-PLC-TERM", 7, LineWeight.LineWeight025);  // Trắng ốc vít cọc đấu
                    ObjectId textLayer = CadUtils.GetOrCreateLayer(db, "E-PLC-TEXT", 2, LineWeight.LineWeight025);      // Vàng nhãn mác I/O
                    ObjectId ledLayer = CadUtils.GetOrCreateLayer(db, "E-PLC-LED", 3, LineWeight.LineWeight030);        // Xanh lá LED Run/Stop
                    ObjectId pwrLayer = CadUtils.GetOrCreateLayer(db, "E-PLC-PWR", 1, LineWeight.LineWeight035);        // Đỏ nguồn cấp

                    ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                    // 2. TÍNH TOÁN KÍCH THƯỚC PARAMETRIC CO DÃN THEO SỐ ĐIỂM I/O
                    int maxPins = Math.Max(cfg.DigitalInputsCount + cfg.AnalogInputsCount + 3, cfg.DigitalOutputsCount + cfg.AnalogOutputsCount + 3);
                    double pinStep = cfg.TerminalSpacing;
                    double bodyH = Math.Max(400.0, (maxPins + 4) * pinStep);
                    double bodyW = 280.0;
                    double x = insertPt.X;
                    double y = insertPt.Y;

                    // 3. VẼ KHỐI THÂN PLC (MODULE BODY & HEADER)
                    DrawRect(btr, tr, bodyLayer, x, y, bodyW, bodyH);
                    DrawRect(btr, tr, borderLayer, x - 2.0, y - 2.0, bodyW + 4.0, bodyH + 4.0);

                    // Thanh Header thương hiệu & mã dòng PLC
                    double headerH = 70.0;
                    DrawRect(btr, tr, borderLayer, x, y + bodyH - headerH, bodyW, headerH);

                    string brandName = GetBrandDisplayName(cfg.Brand);
                    AddText(btr, tr, textLayer, textStyle, brandName, x + bodyW / 2.0, y + bodyH - 22.0, 16.0, AttachmentPoint.MiddleCenter);
                    AddText(btr, tr, textLayer, textStyle, $"{cfg.ModelName} [{cfg.TagName}]", x + bodyW / 2.0, y + bodyH - 48.0, 12.0, AttachmentPoint.MiddleCenter);

                    // Đèn LED chỉ thị CPU (RUN / STOP / ERROR / MAINT)
                    DrawLedDot(btr, tr, ledLayer, textStyle, x + 25.0, y + bodyH - headerH - 20.0, "RUN", 3);
                    DrawLedDot(btr, tr, pwrLayer, textStyle, x + 65.0, y + bodyH - headerH - 20.0, "STOP", 1);
                    DrawLedDot(btr, tr, pwrLayer, textStyle, x + 115.0, y + bodyH - headerH - 20.0, "ERR", 1);
                    DrawLedDot(btr, tr, terminalLayer, textStyle, x + 165.0, y + bodyH - headerH - 20.0, "MAINT", 2);

                    // 4. CHÂN NGUỒN CẤP (POWER SUPPLY PINS)
                    double topPinsY = y + bodyH - headerH - 60.0;

                    // Chân L+ / L1
                    DrawTerminalPin(btr, tr, pwrLayer, textLayer, textStyle, x, topPinsY, "L+ / 24V", true);
                    // Chân M / N
                    DrawTerminalPin(btr, tr, bodyLayer, textLayer, textStyle, x, topPinsY - pinStep, "M / 0V", true);
                    // Chân PE / Mass
                    DrawTerminalPin(btr, tr, terminalLayer, textLayer, textStyle, x, topPinsY - pinStep * 2.0, "PE (⏚)", true);

                    // 5. CÁC CHÂN ĐẦU VÀO SỐ (DIGITAL INPUTS - BÊN TRÁI)
                    double inStartY = topPinsY - pinStep * 3.5;
                    // Chân chung 1M
                    DrawTerminalPin(btr, tr, bodyLayer, textLayer, textStyle, x, inStartY, "1M (COM)", true);

                    string diPrefix = (cfg.Brand == PlcBrand.Mitsubishi_FX5U) ? "X" : "I";
                    for (int i = 0; i < cfg.DigitalInputsCount; i++)
                    {
                        double py = inStartY - (i + 1) * pinStep;
                        string addr = FormatIoAddress(diPrefix, i, cfg.Brand);
                        DrawTerminalPin(btr, tr, terminalLayer, textLayer, textStyle, x, py, addr, true);
                        // Đèn LED xanh trạng thái Input
                        DrawCircle(btr, tr, ledLayer, x + 35.0, py, 2.5);
                    }

                    // Chân Analog Input (AI)
                    double aiStartY = inStartY - (cfg.DigitalInputsCount + 1) * pinStep;
                    for (int a = 0; a < cfg.AnalogInputsCount; a++)
                    {
                        double py = aiStartY - a * pinStep;
                        DrawTerminalPin(btr, tr, terminalLayer, textLayer, textStyle, x, py, $"AI{a} (0-10V)", true);
                    }

                    // 6. CÁC CHÂN ĐẦU RA SỐ (DIGITAL OUTPUTS - BÊN PHẢI)
                    double outStartY = topPinsY;
                    // Chân chung cấp nguồn output 1L+
                    DrawTerminalPin(btr, tr, pwrLayer, textLayer, textStyle, x + bodyW, outStartY, "1L+ (24V)", false);
                    DrawTerminalPin(btr, tr, bodyLayer, textLayer, textStyle, x + bodyW, outStartY - pinStep, "1M (0V)", false);

                    string doPrefix = (cfg.Brand == PlcBrand.Mitsubishi_FX5U) ? "Y" : "Q";
                    for (int j = 0; j < cfg.DigitalOutputsCount; j++)
                    {
                        double py = outStartY - (j + 2) * pinStep;
                        string addr = FormatIoAddress(doPrefix, j, cfg.Brand);
                        DrawTerminalPin(btr, tr, terminalLayer, textLayer, textStyle, x + bodyW, py, addr, false);
                        // Đèn LED trạng thái Output
                        DrawCircle(btr, tr, ledLayer, x + bodyW - 35.0, py, 2.5);
                    }

                    // Chân Analog Output (AO) nếu có
                    double aoStartY = outStartY - (cfg.DigitalOutputsCount + 2) * pinStep;
                    for (int b = 0; b < cfg.AnalogOutputsCount; b++)
                    {
                        double py = aoStartY - b * pinStep;
                        DrawTerminalPin(btr, tr, terminalLayer, textLayer, textStyle, x + bodyW, py, $"AQ{b} (4-20mA)", false);
                    }

                    // 7. CỔNG MẠNG TRUYỀN THÔNG (PROFINET / ETHERNET / RS485 RJ45)
                    double ethY = y + 40.0;
                    DrawEthernetPort(btr, tr, borderLayer, textLayer, textStyle, x + bodyW / 2.0, ethY, "PROFINET / ETH (RJ45)");

                    tr.Commit();
                }

                ed.WriteMessage($"\n✅ [DTools PLC] Đã tạo thành công khối nguyên lý PLC [{cfg.Brand} - {cfg.ModelName}] với {cfg.DigitalInputsCount} DI, {cfg.DigitalOutputsCount} DO, {cfg.AnalogInputsCount} AI!\n");
                return true;
            }
            catch (System.Exception ex)
            {
                ed?.WriteMessage($"\n[DTools LỖI] Tạo khối PLC thất bại: {ex.Message}\n");
                return false;
            }
        }

        // ============================================================
        // HÀM VẼ CHI TIẾT CỌC ĐẤU NỐI & DÂY (TERMINALS)
        // ============================================================

        private static void DrawTerminalPin(BlockTableRecord btr, Transaction tr, ObjectId pinLayer, ObjectId textLayer, ObjectId textStyle, double x, double y, string label, bool isLeft)
        {
            double r = 4.0;
            double wireLen = 22.0;

            // Vòng tròn ốc vít cọc đấu (Terminal screw)
            Circle c = new Circle(new Point3d(x, y, 0), Vector3d.ZAxis, r) { LayerId = pinLayer };
            btr.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);

            // Đoạn chân cọc thò ra ngoài để đấu dây
            double xOut = isLeft ? x - wireLen : x + wireLen;
            Line pinWire = new Line(new Point3d(x, y, 0), new Point3d(xOut, y, 0)) { LayerId = pinLayer };
            btr.AppendEntity(pinWire);
            tr.AddNewlyCreatedDBObject(pinWire, true);

            // Nhãn chữ tên chân
            double textX = isLeft ? x + 12.0 : x - 12.0;
            AttachmentPoint ap = isLeft ? AttachmentPoint.MiddleLeft : AttachmentPoint.MiddleRight;
            AddText(btr, tr, textLayer, textStyle, label, textX, y, 9.5, ap);
        }

        private static void DrawLedDot(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, double x, double y, string name, int colorIndex)
        {
            Circle led = new Circle(new Point3d(x, y, 0), Vector3d.ZAxis, 3.5)
            {
                LayerId = layer,
                Color = Color.FromColorIndex(ColorMethod.ByAci, (short)colorIndex)
            };
            btr.AppendEntity(led);
            tr.AddNewlyCreatedDBObject(led, true);

            AddText(btr, tr, layer, textStyle, name, x + 8.0, y, 8.0, AttachmentPoint.MiddleLeft);
        }

        private static void DrawEthernetPort(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textLayer, ObjectId textStyle, double cx, double cy, string label)
        {
            double pw = 60.0;
            double ph = 36.0;
            DrawRect(btr, tr, layer, cx - pw / 2.0, cy - ph / 2.0, pw, ph);
            AddText(btr, tr, textLayer, textStyle, label, cx, cy - ph / 2.0 - 12.0, 9.0, AttachmentPoint.TopCenter);
        }

        private static string FormatIoAddress(string prefix, int index, PlcBrand brand)
        {
            if (brand == PlcBrand.Mitsubishi_FX5U)
            {
                // Mitsubishi đánh số bát phân: 0..7, 10..17, 20..27
                int byteIdx = index / 8;
                int bitIdx = index % 8;
                return $"{prefix}{byteIdx * 10 + bitIdx}";
            }
            else
            {
                // Siemens / Omron / Schneider: Byte.Bit (I0.0 - I0.7, I1.0 - I1.7)
                int byteIdx = index / 8;
                int bitIdx = index % 8;
                return $"{prefix}{byteIdx}.{bitIdx}";
            }
        }

        private static string GetBrandDisplayName(PlcBrand brand)
        {
            switch (brand)
            {
                case PlcBrand.Siemens_S7_1200: return "SIEMENS SIMATIC S7-1200";
                case PlcBrand.Siemens_S7_1500: return "SIEMENS SIMATIC S7-1500";
                case PlcBrand.Mitsubishi_FX5U: return "MITSUBISHI MELSEC iQ-F";
                case PlcBrand.Omron_CP1H: return "OMRON SYSMAC CP1H";
                case PlcBrand.Schneider_M221: return "SCHNEIDER MODICON M221";
                default: return "PLC AUTOMATION";
            }
        }

        private static void DrawRect(BlockTableRecord btr, Transaction tr, ObjectId layer, double x, double y, double w, double h)
        {
            Polyline poly = new Polyline();
            poly.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
            poly.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
            poly.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
            poly.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
            poly.Closed = true;
            poly.LayerId = layer;
            btr.AppendEntity(poly);
            tr.AddNewlyCreatedDBObject(poly, true);
        }

        private static void DrawCircle(BlockTableRecord btr, Transaction tr, ObjectId layer, double x, double y, double r)
        {
            Circle c = new Circle(new Point3d(x, y, 0), Vector3d.ZAxis, r) { LayerId = layer };
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
            if (layer != ObjectId.Null && layer.IsValid) mt.LayerId = layer;
            if (style != ObjectId.Null && style.IsValid) mt.TextStyleId = style;
            btr.AppendEntity(mt);
            tr.AddNewlyCreatedDBObject(mt, true);
        }
    }
}
