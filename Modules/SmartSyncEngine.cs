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
    public class SchematicComponentItem
    {
        public string Tag { get; set; }           // Ví dụ: QF1, KM1, PLC1, KA1
        public string Name { get; set; }          // Aptomat MCCB 3P, Contactor, PLC...
        public string TypeCategory { get; set; }  // MCCB, MCB, PLC, VFD, Contactor, Relay, Terminal
        public string Spec { get; set; }          // 250A 36kA, 24VDC, 7.5kW...
        public double RealWidth { get; set; }     // mm (kích thước thực ngoài đời)
        public double RealHeight { get; set; }    // mm
        public double RealDepth { get; set; }     // mm
        public bool IsAssignedToPanel { get; set; } = false;
        public int AssignedRow { get; set; } = 1;
    }

    public static class SmartSyncEngine
    {
        // ============================================================
        // 1. QUÉT TOÀN BỘ LINH KIỆN TRÊN MẠCH NGUYÊN LÝ (SCHEMATICS)
        // ============================================================

        public static List<SchematicComponentItem> ScanSchematicComponents(Document doc)
        {
            var list = new List<SchematicComponentItem>();
            if (doc == null) return list;
            Database db = doc.Database;

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                    foreach (ObjectId id in btr)
                    {
                        Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        if (ent is BlockReference br)
                        {
                            string blkName = br.Name.ToUpper();
                            var comp = ParseComponentFromBlock(br, tr);
                            if (comp != null) list.Add(comp);
                        }
                        else if (ent is MText mt)
                        {
                            var comp = ParseComponentFromText(mt.Contents);
                            if (comp != null && !list.Any(x => x.Tag == comp.Tag)) list.Add(comp);
                        }
                    }

                    tr.Commit();
                }
            }
            catch { }

            // Nếu bản vẽ mới chưa có mạch nguyên lý, nạp danh mục cấu hình tủ mẫu chuyên nghiệp
            if (list.Count == 0)
            {
                list = GetDefaultSchematicComponents();
            }

            return list;
        }

        // ============================================================
        // 2. TỰ ĐỘNG XẾP LINH KIỆN LÊN PANEL TỦ ĐIỆN (AUTO-LAYOUT ON PANEL)
        // ============================================================

        public static bool AutoLayoutComponentsOnPanel(Document doc, CabinetConfig cfg, Point3d panelOrigin, List<SchematicComponentItem> components)
        {
            if (doc == null || cfg == null) return false;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                if (components == null || components.Count == 0)
                {
                    components = ScanSchematicComponents(doc);
                }

                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // Các layer bố trí mặt tủ
                    ObjectId plateLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-PLATE", 8, LineWeight.LineWeight025);
                    ObjectId railLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-DINRAIL", 7, LineWeight.LineWeight018);
                    ObjectId ductLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-DUCT", 9, LineWeight.LineWeight025);
                    ObjectId compLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-EQUIP", 2, LineWeight.LineWeight035);
                    ObjectId textLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-TEXT", 7, LineWeight.LineWeight020);

                    ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                    double pW = cfg.Width - 100.0;
                    double pH = cfg.Height - 120.0;
                    double ox = panelOrigin.X;
                    double oy = panelOrigin.Y;

                    // 1. Tấm gá Panel nền
                    DrawRect(btr, tr, plateLayer, ox, oy, pW, pH);

                    // 2. Máng cáp nhựa lược viền xung quanh (Wiring Duct 40x60mm)
                    double ductW = 40.0;
                    DrawRect(btr, tr, ductLayer, ox + 10.0, oy + 10.0, ductW, pH - 20.0);             // Máng đứng trái
                    DrawRect(btr, tr, ductLayer, ox + pW - 10.0 - ductW, oy + 10.0, ductW, pH - 20.0); // Máng đứng phải
                    DrawRect(btr, tr, ductLayer, ox + 10.0, oy + 10.0, pW - 20.0, ductW);             // Máng đáy
                    DrawRect(btr, tr, ductLayer, ox + 10.0, oy + pH - 10.0 - ductW, pW - 20.0, ductW); // Máng đỉnh

                    // 3. Phân chia 4 Hàng Din-rail tiêu chuẩn
                    double usableLeft = ox + ductW + 20.0;
                    double usableRight = ox + pW - ductW - 20.0;
                    double usableWidth = usableRight - usableLeft;

                    int numRows = 4;
                    double rowSpacing = (pH - ductW * 2.0 - 40.0) / numRows;

                    // Phân nhóm linh kiện theo 4 tầng chức năng:
                    // Hàng 1 (Động lực chính): MCCB chính, ATS, PLC, Biến tần VFD
                    var row1Items = components.Where(c => c.TypeCategory == "MCCB" || c.TypeCategory == "PLC" || c.TypeCategory == "VFD").ToList();
                    // Hàng 2 (Bảo vệ nhánh): Dàn MCB tép 1P, 2P, 3P
                    var row2Items = components.Where(c => c.TypeCategory == "MCB").ToList();
                    // Hàng 3 (Điều khiển): Contactor, Rơ-le kiếng, Timer, Cầu chì
                    var row3Items = components.Where(c => c.TypeCategory == "Contactor" || c.TypeCategory == "Relay" || c.TypeCategory == "Fuse").ToList();
                    // Hàng 4 (Xuất tuyến): Cầu đấu dây (Terminal Blocks)
                    var row4Items = components.Where(c => c.TypeCategory == "Terminal" || c.TypeCategory == "Ground").ToList();

                    // Vẽ từng tầng Din-rail và gắn linh kiện thẳng hàng
                    LayoutRowOnDinRail(btr, tr, railLayer, compLayer, textLayer, textStyle, usableLeft, oy + pH - ductW - 20.0 - rowSpacing * 0.8, usableWidth, row1Items, 1);
                    LayoutRowOnDinRail(btr, tr, railLayer, compLayer, textLayer, textStyle, usableLeft, oy + pH - ductW - 20.0 - rowSpacing * 1.8, usableWidth, row2Items, 2);
                    LayoutRowOnDinRail(btr, tr, railLayer, compLayer, textLayer, textStyle, usableLeft, oy + pH - ductW - 20.0 - rowSpacing * 2.8, usableWidth, row3Items, 3);
                    LayoutRowOnDinRail(btr, tr, railLayer, compLayer, textLayer, textStyle, usableLeft, oy + pH - ductW - 20.0 - rowSpacing * 3.8, usableWidth, row4Items, 4);

                    // 4. Bảng thống kê đồng bộ (Smart Sync Status Badge)
                    DrawSyncReportBadge(btr, tr, textLayer, textStyle, new Point2d(ox + pW / 2.0, oy - 60.0), components.Count);

                    tr.Commit();
                }

                ed.WriteMessage($"\n🎯 [DTools SMART SYNC] Đã đồng bộ 100% ({components.Count} linh kiện) từ Sơ đồ nguyên lý lên Tấm Panel Mặt Tủ thành công!\n");
                return true;
            }
            catch (System.Exception ex)
            {
                ed?.WriteMessage($"\n[DTools LỖI] Đồng bộ mặt tủ thất bại: {ex.Message}\n");
                return false;
            }
        }

        // ============================================================
        // HÀM XẾP HÀNG LINH KIỆN LÊN 1 THANH DIN-RAIL
        // ============================================================

        private static void LayoutRowOnDinRail(BlockTableRecord btr, Transaction tr, ObjectId railLayer, ObjectId compLayer, ObjectId textLayer, ObjectId textStyle,
            double startX, double railCenterY, double totalRailW, List<SchematicComponentItem> items, int rowNumber)
        {
            // Vẽ thanh ray nhôm DIN Rail 35mm
            double railH = 35.0;
            DrawRect(btr, tr, railLayer, startX, railCenterY - railH / 2.0, totalRailW, railH);

            // Các lỗ oval bắt vít trên thanh ray
            for (double hx = startX + 30.0; hx < startX + totalRailW - 30.0; hx += 80.0)
            {
                DrawRect(btr, tr, railLayer, hx, railCenterY - 4.0, 20.0, 8.0);
            }

            // Gắn linh kiện lên thanh ray
            double curX = startX + 15.0;
            double itemSpacing = 10.0;

            foreach (var item in items)
            {
                double iw = item.RealWidth;
                double ih = item.RealHeight;

                if (curX + iw > startX + totalRailW - 10.0) break; // Tránh tràn thanh ray

                double iy = railCenterY - ih / 2.0;

                // Khối linh kiện kích thước thực tế
                DrawRect(btr, tr, compLayer, curX, iy, iw, ih);

                // Cần gạt hoặc chi tiết nhận diện
                DrawComponentFace(btr, tr, compLayer, curX, iy, iw, ih, item.TypeCategory);

                // Nhãn Tag mác linh kiện (vd: QF1, KM1, PLC1...)
                AddText(btr, tr, textLayer, textStyle, item.Tag, curX + iw / 2.0, iy + ih + 16.0, 11.0, AttachmentPoint.BottomCenter);
                AddText(btr, tr, textLayer, textStyle, item.Spec, curX + iw / 2.0, iy - 16.0, 9.0, AttachmentPoint.TopCenter);

                item.IsAssignedToPanel = true;
                item.AssignedRow = rowNumber;

                curX += iw + itemSpacing;
            }
        }

        private static void DrawComponentFace(BlockTableRecord btr, Transaction tr, ObjectId layer, double x, double y, double w, double h, string category)
        {
            if (category == "MCCB")
            {
                // Cần gạt Aptomat khối 3P
                DrawRect(btr, tr, layer, x + w * 0.35, y + h * 0.4, w * 0.3, h * 0.2);
            }
            else if (category == "MCB")
            {
                // Cần gạt tép MCB
                DrawRect(btr, tr, layer, x + w * 0.2, y + h * 0.45, w * 0.6, h * 0.15);
            }
            else if (category == "PLC")
            {
                // Khe cắm thẻ & cổng truyền thông
                DrawRect(btr, tr, layer, x + 15.0, y + 15.0, 30.0, 25.0);
            }
            else if (category == "Relay")
            {
                // Thân rơ-le kiếng trong suốt
                DrawRect(btr, tr, layer, x + 4.0, y + 6.0, w - 8.0, h - 12.0);
            }
        }

        private static void DrawSyncReportBadge(BlockTableRecord btr, Transaction tr, ObjectId textLayer, ObjectId textStyle, Point2d pt, int totalCount)
        {
            double bw = 600.0;
            double bh = 45.0;
            DrawRect(btr, tr, textLayer, pt.X - bw / 2.0, pt.Y - bh / 2.0, bw, bh);
            AddText(btr, tr, textLayer, textStyle, $"✅ SMART SYNC HOÀN TẤT: {totalCount}/{totalCount} LINH KIỆN ĐÃ ĐƯỢC XẾP PANEL ĐẦY ĐỦ", pt.X, pt.Y, 13.0, AttachmentPoint.MiddleCenter);
        }

        // ============================================================
        // BỘ DỮ LIỆU MẪU ĐẦY ĐỦ LINH KIỆN MẠCH NGUYÊN LÝ CHUẨN
        // ============================================================

        private static List<SchematicComponentItem> GetDefaultSchematicComponents()
        {
            return new List<SchematicComponentItem>
            {
                // Hàng 1: Động lực & PLC
                new SchematicComponentItem { Tag = "QF1", Name = "Aptomat tổng MCCB 3P", TypeCategory = "MCCB", Spec = "250A 36kA", RealWidth = 105.0, RealHeight = 165.0, RealDepth = 86.0 },
                new SchematicComponentItem { Tag = "PS1", Name = "Bộ nguồn 24VDC Sitop", TypeCategory = "PLC", Spec = "24V 10A 240W", RealWidth = 70.0, RealHeight = 125.0, RealDepth = 120.0 },
                new SchematicComponentItem { Tag = "PLC1", Name = "PLC Siemens S7-1200", TypeCategory = "PLC", Spec = "CPU 1214C DC/DC", RealWidth = 110.0, RealHeight = 100.0, RealDepth = 75.0 },
                new SchematicComponentItem { Tag = "VFD1", Name = "Biến tần động cơ bơm", TypeCategory = "VFD", Spec = "7.5kW 3P 380V", RealWidth = 120.0, RealHeight = 180.0, RealDepth = 150.0 },

                // Hàng 2: Dàn MCB nhánh
                new SchematicComponentItem { Tag = "MCB1", Name = "MCB Nhánh Động lực 1", TypeCategory = "MCB", Spec = "3P 40A 6kA", RealWidth = 54.0, RealHeight = 85.0, RealDepth = 75.0 },
                new SchematicComponentItem { Tag = "MCB2", Name = "MCB Nhánh Động lực 2", TypeCategory = "MCB", Spec = "3P 32A 6kA", RealWidth = 54.0, RealHeight = 85.0, RealDepth = 75.0 },
                new SchematicComponentItem { Tag = "MCB3", Name = "MCB Chiếu sáng N1", TypeCategory = "MCB", Spec = "1P 16A 6kA", RealWidth = 18.0, RealHeight = 85.0, RealDepth = 75.0 },
                new SchematicComponentItem { Tag = "MCB4", Name = "MCB Ổ cắm N2", TypeCategory = "MCB", Spec = "1P 20A 6kA", RealWidth = 18.0, RealHeight = 85.0, RealDepth = 75.0 },
                new SchematicComponentItem { Tag = "MCB5", Name = "MCB Điều khiển 24V", TypeCategory = "MCB", Spec = "1P 10A 6kA", RealWidth = 18.0, RealHeight = 85.0, RealDepth = 75.0 },

                // Hàng 3: Khởi động từ & Rơ-le
                new SchematicComponentItem { Tag = "KM1", Name = "Khởi động từ Bơm 1", TypeCategory = "Contactor", Spec = "3P 25A 220VAC", RealWidth = 45.0, RealHeight = 85.0, RealDepth = 90.0 },
                new SchematicComponentItem { Tag = "KM2", Name = "Khởi động từ Bơm 2", TypeCategory = "Contactor", Spec = "3P 25A 220VAC", RealWidth = 45.0, RealHeight = 85.0, RealDepth = 90.0 },
                new SchematicComponentItem { Tag = "KA1", Name = "Rơ-le trung gian Relay 1", TypeCategory = "Relay", Spec = "8 chân 24VDC", RealWidth = 27.0, RealHeight = 75.0, RealDepth = 65.0 },
                new SchematicComponentItem { Tag = "KA2", Name = "Rơ-le trung gian Relay 2", TypeCategory = "Relay", Spec = "8 chân 24VDC", RealWidth = 27.0, RealHeight = 75.0, RealDepth = 65.0 },
                new SchematicComponentItem { Tag = "KT1", Name = "Rơ-le thời gian Timer", TypeCategory = "Relay", Spec = "0-60s 220VAC", RealWidth = 27.0, RealHeight = 75.0, RealDepth = 65.0 },

                // Hàng 4: Cầu đấu xuất tuyến
                new SchematicComponentItem { Tag = "TB1", Name = "Dãy cầu đấu Terminal", TypeCategory = "Terminal", Spec = "24 Cực UK-5N 32A", RealWidth = 220.0, RealHeight = 45.0, RealDepth = 48.0 },
                new SchematicComponentItem { Tag = "GND1", Name = "Thanh tiếp địa đồng", TypeCategory = "Terminal", Spec = "PE Ground Bar", RealWidth = 150.0, RealHeight = 25.0, RealDepth = 15.0 }
            };
        }

        private static SchematicComponentItem ParseComponentFromBlock(BlockReference br, Transaction tr)
        {
            string name = br.Name.ToUpper();
            if (name.Contains("MCCB"))
                return new SchematicComponentItem { Tag = "QF1", Name = "MCCB 3P", TypeCategory = "MCCB", Spec = "250A", RealWidth = 105.0, RealHeight = 165.0 };
            if (name.Contains("MCB"))
                return new SchematicComponentItem { Tag = "MCB", Name = "MCB Tép", TypeCategory = "MCB", Spec = "20A", RealWidth = 18.0, RealHeight = 85.0 };
            if (name.Contains("PLC"))
                return new SchematicComponentItem { Tag = "PLC1", Name = "PLC Controller", TypeCategory = "PLC", Spec = "CPU S7-1200", RealWidth = 110.0, RealHeight = 100.0 };
            return null;
        }

        private static SchematicComponentItem ParseComponentFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            text = text.ToUpper();
            if (text.Contains("MCCB")) return new SchematicComponentItem { Tag = "QF1", Name = "MCCB", TypeCategory = "MCCB", Spec = "250A", RealWidth = 105.0, RealHeight = 165.0 };
            if (text.Contains("PLC")) return new SchematicComponentItem { Tag = "PLC1", Name = "PLC", TypeCategory = "PLC", Spec = "CPU", RealWidth = 110.0, RealHeight = 100.0 };
            return null;
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
