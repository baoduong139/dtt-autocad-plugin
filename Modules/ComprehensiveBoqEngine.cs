using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.Modules
{
    public class BoqItem
    {
        public int Stt { get; set; }
        public string Category { get; set; } = "ĐIỆN";
        public string ItemCode { get; set; } = "";
        public string ItemDescription { get; set; } = "";
        public string Unit { get; set; } = "Bộ"; // Cái, Bộ, Mét, m², m³
        public double Quantity { get; set; } = 1.0;
        public double UnitPriceVnd { get; set; } = 0.0;
        public double TotalPriceVnd => Quantity * UnitPriceVnd;
        public string Note { get; set; } = "";
    }

    public static class ComprehensiveBoqEngine
    {
        /// <summary>
        /// Quét toàn diện bản vẽ CAD để tự động bóc tách khối lượng (BOQ/BOM)
        /// </summary>
        public static List<BoqItem> ScanDrawingForBoq(Database db)
        {
            List<BoqItem> items = new List<BoqItem>();
            if (db == null) return items;

            // Bộ đếm theo Block Name
            Dictionary<string, int> blockCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            // Bộ đếm độ dài dây/ống theo Layer
            Dictionary<string, double> layerLengths = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in modelSpace)
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // 1. Thống kê Block References
                    if (ent is BlockReference blkRef)
                    {
                        string name = blkRef.Name;
                        if (name.StartsWith("*")) // Dynamic block anonymous name
                        {
                            BlockTableRecord btrOrig = tr.GetObject(blkRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                            if (btrOrig != null) name = btrOrig.Name;
                        }

                        if (!blockCounts.ContainsKey(name)) blockCounts[name] = 0;
                        blockCounts[name]++;
                    }
                    // 2. Thống kê mét dài Dây điện / Tuyến cáp / Tuyến ống
                    else if (ent is Curve curve)
                    {
                        string layer = ent.Layer ?? "0";
                        try
                        {
                            double len = curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam);
                            if (len > 0)
                            {
                                if (!layerLengths.ContainsKey(layer)) layerLengths[layer] = 0;
                                layerLengths[layer] += len;
                            }
                        }
                        catch { }
                    }
                }

                tr.Commit();
            }

            int stt = 1;

            // A. PHÂN LOẠI HẠNG MỤC THIẾT BỊ ĐIỆN & TỰ ĐỘNG HÓA
            foreach (var kvp in blockCounts)
            {
                string name = kvp.Key;
                int qty = kvp.Value;
                var item = MapBlockNameToBoqItem(name, qty, ref stt);
                if (item != null) items.Add(item);
            }

            // B. PHÂN LOẠI HẠNG MỤC DÂY DẪN & ĐƯỜNG ỐNG (ĐỔI TỪ MM SANG MÉT)
            foreach (var kvp in layerLengths)
            {
                string layer = kvp.Key;
                double lenM = Math.Round(kvp.Value / 1000.0, 1);
                if (lenM <= 0.5) continue;

                var item = MapLayerToBoqItem(layer, lenM, ref stt);
                if (item != null) items.Add(item);
            }

            // Nếu bản vẽ mới chưa có nhiều block, nạp sẵn bộ danh mục mẫu đầy đủ chuẩn dự toán
            if (items.Count == 0)
            {
                items = GetDefaultSampleBoq();
            }

            return items;
        }

        private static BoqItem MapBlockNameToBoqItem(string blkName, int qty, ref int stt)
        {
            string upper = blkName.ToUpperInvariant();

            if (upper.Contains("LIGHT") || upper.Contains("DOWNLIGHT") || upper.Contains("ĐÈN"))
            {
                return new BoqItem
                {
                    Stt = stt++,
                    Category = "1. HỆ THỐNG ĐIỆN",
                    ItemCode = "EL-LT01",
                    ItemDescription = $"Đèn LED Downlight âm trần 12W 6500K ({blkName})",
                    Unit = "Bộ",
                    Quantity = qty,
                    UnitPriceVnd = 185000,
                    Note = "Rạng Đông / Philips"
                };
            }
            if (upper.Contains("SOCKET") || upper.Contains("Ổ CẮM"))
            {
                return new BoqItem
                {
                    Stt = stt++,
                    Category = "1. HỆ THỐNG ĐIỆN",
                    ItemCode = "EL-SK01",
                    ItemDescription = $"Ổ cắm đôi 3 chấu đa năng có màng che 16A ({blkName})",
                    Unit = "Bộ",
                    Quantity = qty,
                    UnitPriceVnd = 95000,
                    Note = "Panasonic Wide Series"
                };
            }
            if (upper.Contains("SWITCH") || upper.Contains("CÔNG TẮC"))
            {
                return new BoqItem
                {
                    Stt = stt++,
                    Category = "1. HỆ THỐNG ĐIỆN",
                    ItemCode = "EL-SW01",
                    ItemDescription = $"Công tắc đơn/đôi 1 chiều 16A ({blkName})",
                    Unit = "Bộ",
                    Quantity = qty,
                    UnitPriceVnd = 65000,
                    Note = "Panasonic Wide Series"
                };
            }
            if (upper.Contains("PANEL") || upper.Contains("TỦ ĐIỆN") || upper.Contains("DB") || upper.Contains("MSB"))
            {
                return new BoqItem
                {
                    Stt = stt++,
                    Category = "1. HỆ THỐNG ĐIỆN",
                    ItemCode = "EL-PN01",
                    ItemDescription = $"Tủ điện phân phối sơn tĩnh điện kèm thiết bị đóng cắt ({blkName})",
                    Unit = "Tủ",
                    Quantity = qty,
                    UnitPriceVnd = 4500000,
                    Note = "DTT Cabinet Pro"
                };
            }
            if (upper.Contains("DIFFUSER") || upper.Contains("MIỆNG GIÓ"))
            {
                return new BoqItem
                {
                    Stt = stt++,
                    Category = "2. THÔNG GIÓ HVAC",
                    ItemCode = "ME-DF01",
                    ItemDescription = $"Cửa gió khuếch tán 4 hướng 600x600 nhôm định hình ({blkName})",
                    Unit = "Bộ",
                    Quantity = qty,
                    UnitPriceVnd = 320000,
                    Note = "Sơn tĩnh điện trắng"
                };
            }
            if (upper.Contains("DRAIN") || upper.Contains("THU SÀN"))
            {
                return new BoqItem
                {
                    Stt = stt++,
                    Category = "3. CẤP THOÁT NƯỚC",
                    ItemCode = "PL-FD01",
                    ItemDescription = "Phễu thu sàn chống hôi Inox 304 D60/90",
                    Unit = "Bộ",
                    Quantity = qty,
                    UnitPriceVnd = 150000,
                    Note = "Inox 304 dày 2mm"
                };
            }

            return new BoqItem
            {
                Stt = stt++,
                Category = "4. THIẾT BỊ KHÁC",
                ItemCode = "MISC-01",
                ItemDescription = $"Thiết bị / Ký hiệu Block: {blkName}",
                Unit = "Cái",
                Quantity = qty,
                UnitPriceVnd = 120000,
                Note = "Bản vẽ CAD"
            };
        }

        private static BoqItem MapLayerToBoqItem(string layer, double lenM, ref int stt)
        {
            string upper = layer.ToUpperInvariant();

            if (upper.Contains("E-WIRE") || upper.Contains("DÂY") || upper.Contains("ELEC"))
            {
                return new BoqItem
                {
                    Stt = stt++,
                    Category = "1. HỆ THỐNG ĐIỆN",
                    ItemCode = "EL-WR01",
                    ItemDescription = $"Dây điện đồng đơn mềm Cu/PVC 1x2.5mm² (Layer: {layer})",
                    Unit = "Mét",
                    Quantity = lenM,
                    UnitPriceVnd = 12500,
                    Note = "Cadivi / Trần Phú"
                };
            }
            if (upper.Contains("P-CW") || upper.Contains("CẤP NƯỚC"))
            {
                return new BoqItem
                {
                    Stt = stt++,
                    Category = "3. CẤP THOÁT NƯỚC",
                    ItemCode = "PL-PPR01",
                    ItemDescription = $"Ống cấp nước nóng/lạnh PPR D25 PN20 (Layer: {layer})",
                    Unit = "Mét",
                    Quantity = lenM,
                    UnitPriceVnd = 45000,
                    Note = "Tiền Phong / Vesbo"
                };
            }
            if (upper.Contains("P-SOIL") || upper.Contains("P-WASTE") || upper.Contains("THOÁT"))
            {
                return new BoqItem
                {
                    Stt = stt++,
                    Category = "3. CẤP THOÁT NƯỚC",
                    ItemCode = "PL-PVC01",
                    ItemDescription = $"Ống thoát nước thải uPVC D90 Class 2 (Layer: {layer})",
                    Unit = "Mét",
                    Quantity = lenM,
                    UnitPriceVnd = 72000,
                    Note = "Nhựa Bình Minh / Tiền Phong"
                };
            }
            if (upper.Contains("HVAC") || upper.Contains("DUCT") || upper.Contains("ỐNG GIÓ"))
            {
                return new BoqItem
                {
                    Stt = stt++,
                    Category = "2. THÔNG GIÓ HVAC",
                    ItemCode = "ME-DC01",
                    ItemDescription = $"Ống gió tôn mạ kẽm độ dày 0.75mm có bọc cách nhiệt PE (Layer: {layer})",
                    Unit = "Mét",
                    Quantity = lenM,
                    UnitPriceVnd = 280000,
                    Note = "Tôn mạ kẽm Z80"
                };
            }
            if (upper.Contains("WALL") || upper.Contains("TƯỜNG"))
            {
                return new BoqItem
                {
                    Stt = stt++,
                    Category = "5. KIẾN TRÚC XÂY DỰNG",
                    ItemCode = "AR-WL01",
                    ItemDescription = $"Khối xây tường gạch ống tuynel (Layer: {layer})",
                    Unit = "Mét",
                    Quantity = lenM,
                    UnitPriceVnd = 180000,
                    Note = "Vữa xi măng M75"
                };
            }

            return null;
        }

        private static List<BoqItem> GetDefaultSampleBoq()
        {
            return new List<BoqItem>
            {
                new BoqItem { Stt = 1, Category = "1. HỆ THỐNG ĐIỆN", ItemCode = "EL-PN01", ItemDescription = "Tủ điện phân phối chính MSB 400A kèm phụ kiện", Unit = "Tủ", Quantity = 1, UnitPriceVnd = 28000000, Note = "DTT Cabinet Pro" },
                new BoqItem { Stt = 2, Category = "1. HỆ THỐNG ĐIỆN", ItemCode = "EL-LT01", ItemDescription = "Đèn LED Downlight âm trần 12W Rạng Đông", Unit = "Bộ", Quantity = 48, UnitPriceVnd = 185000, Note = "Bảo hành 2 năm" },
                new BoqItem { Stt = 3, Category = "1. HỆ THỐNG ĐIỆN", ItemCode = "EL-SK01", ItemDescription = "Ổ cắm đôi 3 chấu đa năng Panasonic 16A", Unit = "Bộ", Quantity = 32, UnitPriceVnd = 95000, Note = "Panasonic Wide" },
                new BoqItem { Stt = 4, Category = "1. HỆ THỐNG ĐIỆN", ItemCode = "EL-WR01", ItemDescription = "Dây điện Cu/PVC 1x2.5mm² luồn ống ghen chống cháy", Unit = "Mét", Quantity = 350, UnitPriceVnd = 14500, Note = "Cadivi" },
                new BoqItem { Stt = 5, Category = "2. THÔNG GIÓ HVAC", ItemCode = "ME-DC01", ItemDescription = "Ống gió vuông tôn mạ kẽm bọc cách nhiệt 600x300", Unit = "Mét", Quantity = 65, UnitPriceVnd = 320000, Note = "Tôn tráng kẽm 0.75mm" },
                new BoqItem { Stt = 6, Category = "2. THÔNG GIÓ HVAC", ItemCode = "ME-DF01", ItemDescription = "Cửa gió khuếch tán 4 hướng 600x600 nhôm định hình", Unit = "Bộ", Quantity = 16, UnitPriceVnd = 340000, Note = "Sơn tĩnh điện" },
                new BoqItem { Stt = 7, Category = "3. CẤP THOÁT NƯỚC", ItemCode = "PL-PPR01", ItemDescription = "Ống cấp nước sạch PPR D25 hàn nhiệt PN20", Unit = "Mét", Quantity = 85, UnitPriceVnd = 45000, Note = "Tiền Phong" },
                new BoqItem { Stt = 8, Category = "3. CẤP THOÁT NƯỚC", ItemCode = "PL-PVC01", ItemDescription = "Ống thoát nước thải uPVC D90 Class 2", Unit = "Mét", Quantity = 60, UnitPriceVnd = 72000, Note = "Bình Minh" }
            };
        }

        /// <summary>
        /// Xuất file Excel (.csv UTF-8 kèm BOM) chuẩn dự toán có công thức tính tiền
        /// </summary>
        public static string ExportBoqToCsvExcel(List<BoqItem> items, string filePath = null)
        {
            if (items == null || items.Count == 0) return null;

            if (string.IsNullOrEmpty(filePath))
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                filePath = Path.Combine(docs, $"DTools_BocTach_BOQ_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }

            StringBuilder sb = new StringBuilder();
            // Header báo cáo
            sb.AppendLine("BẢNG TỔNG HỢP DỰ TOÁN BÓC TÁCH KHỐI LƯỢNG VẬT TƯ & THIẾT BỊ (BOQ)");
            sb.AppendLine($"Dự án: DTools Pro CAD Master | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine("");
            sb.AppendLine("STT,HẠNG MỤC,MÃ HIỆU,MÔ TẢ VẬT TƯ THIẾT BỊ,ĐVT,KHỐI LƯỢNG,ĐƠN GIÁ (VNĐ),THÀNH TIỀN (VNĐ),GHI CHÚ");

            int rowStart = 5;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                int currentRow = rowStart + i;
                // Sử dụng công thức Excel tính thành tiền: =F{row}*G{row}
                sb.AppendLine($"{it.Stt},\"{it.Category}\",\"{it.ItemCode}\",\"{it.ItemDescription}\",\"{it.Unit}\",{it.Quantity:0.##},{it.UnitPriceVnd:0},\"=F{currentRow}*G{currentRow}\",\"{it.Note}\"");
            }

            // Dòng tổng cộng
            int totalRow = rowStart + items.Count;
            sb.AppendLine($",,,\"TỔNG CỘNG TRƯỚC THUẾ (VNĐ)\",,,,\"=SUM(H5:H{totalRow - 1})\",");
            sb.AppendLine($",,,\"THUẾ VAT (8%)\",,,,\"=H{totalRow}*0.08\",");
            sb.AppendLine($",,,\"TỔNG GIÁ TRỊ DỰ TOÁN SAU THUẾ (VNĐ)\",,,,\"=H{totalRow}+H{totalRow + 1}\",");

            // Lưu UTF-8 có BOM để Excel hiển thị tiếng Việt có dấu 100% chuẩn
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            return filePath;
        }

        /// <summary>
        /// Chèn Bảng thống kê khối lượng (AutoCAD Table) trực tiếp vào bản vẽ
        /// </summary>
        public static void InsertBoqTableToCad(Database db, Point3d insertPt, List<BoqItem> items)
        {
            if (db == null || items == null || items.Count == 0) return;

            ObjectId layerTable = CadUtils.GetOrCreateLayer(db, "A-TABLE-BOQ", 7);
            ObjectId styleId = CadUtils.GetOrCreateUnicodeStyle(db);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                int numRows = items.Count + 3; // Tiêu đề + Header + Data + Total
                int numCols = 7;

                Table table = new Table();
                table.TableStyle = db.Tablestyle;
                table.Position = insertPt;
                table.SetSize(numRows, numCols);
                table.LayerId = layerTable;

                // Chiều rộng các cột
                double[] colWidths = { 120, 320, 750, 150, 200, 300, 380 };
                for (int c = 0; c < numCols; c++)
                {
                    table.Columns[c].Width = colWidths[c];
                }

                // Tiêu đề bảng lớn
                table.Cells[0, 0].TextString = "BẢNG BÓC TÁCH KHỐI LƯỢNG VẬT TƯ & THIẾT BỊ (BOQ - DTOOLS PRO)";
                table.Cells[0, 0].TextHeight = 70;
                table.Cells[0, 0].Alignment = CellAlignment.MiddleCenter;

                // Header các cột
                string[] headers = { "STT", "HẠNG MỤC", "TÊN VẬT TƯ / THIẾT BỊ", "ĐVT", "SỐ LƯỢNG", "ĐƠN GIÁ (VNĐ)", "THÀNH TIỀN (VNĐ)" };
                for (int c = 0; c < numCols; c++)
                {
                    table.Cells[1, c].TextString = headers[c];
                    table.Cells[1, c].TextHeight = 45;
                    table.Cells[1, c].Alignment = CellAlignment.MiddleCenter;
                }

                // Điền dữ liệu
                double grandTotal = 0;
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    int r = i + 2;
                    double total = it.Quantity * it.UnitPriceVnd;
                    grandTotal += total;

                    table.Cells[r, 0].TextString = it.Stt.ToString();
                    table.Cells[r, 0].Alignment = CellAlignment.MiddleCenter;

                    table.Cells[r, 1].TextString = it.Category;
                    table.Cells[r, 1].Alignment = CellAlignment.MiddleLeft;

                    table.Cells[r, 2].TextString = it.ItemDescription;
                    table.Cells[r, 2].Alignment = CellAlignment.MiddleLeft;

                    table.Cells[r, 3].TextString = it.Unit;
                    table.Cells[r, 3].Alignment = CellAlignment.MiddleCenter;

                    table.Cells[r, 4].TextString = $"{it.Quantity:0.##}";
                    table.Cells[r, 4].Alignment = CellAlignment.MiddleRight;

                    table.Cells[r, 5].TextString = $"{it.UnitPriceVnd:N0}";
                    table.Cells[r, 5].Alignment = CellAlignment.MiddleRight;

                    table.Cells[r, 6].TextString = $"{total:N0}";
                    table.Cells[r, 6].Alignment = CellAlignment.MiddleRight;

                    for (int c = 0; c < numCols; c++)
                    {
                        table.Cells[r, c].TextHeight = 40;
                    }
                }

                // Dòng tổng cộng cuối cùng
                int lastRow = numRows - 1;
                table.Cells[lastRow, 2].TextString = "TỔNG GIÁ TRỊ DỰ TOÁN (VNĐ):";
                table.Cells[lastRow, 2].Alignment = CellAlignment.MiddleRight;
                table.Cells[lastRow, 2].TextHeight = 50;

                table.Cells[lastRow, 6].TextString = $"{grandTotal:N0} đ";
                table.Cells[lastRow, 6].Alignment = CellAlignment.MiddleRight;
                table.Cells[lastRow, 6].TextHeight = 55;

                btr.AppendEntity(table);
                tr.AddNewlyCreatedDBObject(table, true);

                tr.Commit();
            }
        }
    }
}
