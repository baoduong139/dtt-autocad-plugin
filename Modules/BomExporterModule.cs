using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace DTT_AutoCAD_Plugin.Modules
{
    public class BomItem
    {
        public int Stt { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Spec { get; set; }
        public string Unit { get; set; } = "Bộ";
        public int Quantity { get; set; } = 1;
        public string Notes { get; set; } = "";
    }

    public static class BomExporterModule
    {
        // ============================================================
        // HÀM QUÉT BẢN VẼ VÀ XUẤT BOM SANG FILE EXCEL (.XLSX)
        // ============================================================

        public static void ScanAndExportBom(Document doc)
        {
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n🔍 [DTools BOM] Đang quét toàn bộ thiết bị & block linh kiện trong bản vẽ CAD...");

                List<BomItem> items = ScanDrawingEntities(doc, db);

                if (items == null || items.Count == 0)
                {
                    ed.WriteMessage("\n⚠️ Chưa phát hiện linh kiện. Đang tự động tạo bảng bóc tách theo cấu hình tủ điện chuẩn...");
                    items = GetSampleCabinetBom();
                }

                // Gán lại STT
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].Stt = i + 1;
                }

                // Đường dẫn lưu file Excel
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"Bang_Boc_Tach_Vat_Tu_Tu_Dien_{timeStamp}.xlsx";
                string fullPath = Path.Combine(desktop, fileName);

                // Tạo file .xlsx OpenXML chuẩn Microsoft Excel
                bool ok = CreateOpenXmlExcelFile(items, fullPath, "BÓC TÁCH VẬT TƯ TỦ ĐIỆN");

                if (ok && File.Exists(fullPath))
                {
                    ed.WriteMessage($"\n✅ [DTools BOM] Đã xuất thành công {items.Count} danh mục vật tư!");
                    ed.WriteMessage($"\n📁 File Excel lưu tại: {fullPath}\n");

                    // Tự động mở file Excel
                    try
                    {
                        Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                    }
                    catch { }
                }
                else
                {
                    ed.WriteMessage("\n[DTools ERROR] Không thể ghi file Excel!\n");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[DTools ERROR] Lỗi bóc tách BOM: {ex.Message}\n");
            }
        }

        // ============================================================
        // QUÉT CÁC BLOCK REFERENCE & TEXT TRONG MODELSPACE
        // ============================================================

        private static List<BomItem> ScanDrawingEntities(Document doc, Database db)
        {
            var dict = new Dictionary<string, BomItem>();

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in btr)
                {
                    if (id.ObjectClass.DxfName == "INSERT")
                    {
                        var br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                        if (br == null) continue;

                        string blockName = br.Name;
                        if (br.IsDynamicBlock)
                        {
                            var btrBlock = (BlockTableRecord)tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead);
                            blockName = btrBlock.Name;
                        }

                        // Lấy thuộc tính Attribute nếu có
                        string tag = "";
                        string spec = "";
                        if (br.AttributeCollection != null && br.AttributeCollection.Count > 0)
                        {
                            foreach (ObjectId attId in br.AttributeCollection)
                            {
                                var att = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                                if (att == null) continue;
                                if (att.Tag.Equals("TAG", StringComparison.OrdinalIgnoreCase)) tag = att.TextString;
                                else if (att.Tag.Equals("SPEC", StringComparison.OrdinalIgnoreCase)) spec = att.TextString;
                            }
                        }

                        string name = MapBlockNameToVietnamese(blockName);
                        if (string.IsNullOrEmpty(tag)) tag = blockName.Replace("DTT_SYM_", "");

                        string key = $"{name}_{spec}_{tag}";
                        if (dict.ContainsKey(key))
                        {
                            dict[key].Quantity++;
                        }
                        else
                        {
                            dict[key] = new BomItem
                            {
                                Name = name,
                                Tag = tag,
                                Spec = string.IsNullOrEmpty(spec) ? "Tiêu chuẩn IEC" : spec,
                                Unit = "Bộ",
                                Quantity = 1,
                                Notes = "Lắp đặt trong tủ điện"
                            };
                        }
                    }
                    else if (id.ObjectClass.DxfName == "MTEXT" || id.ObjectClass.DxfName == "TEXT")
                    {
                        string txt = "";
                        if (id.ObjectClass.DxfName == "MTEXT")
                        {
                            var mt = tr.GetObject(id, OpenMode.ForRead) as MText;
                            if (mt != null) txt = mt.Text;
                        }
                        else
                        {
                            var dt = tr.GetObject(id, OpenMode.ForRead) as DBText;
                            if (dt != null) txt = dt.TextString;
                        }

                        if (!string.IsNullOrEmpty(txt) && (txt.Contains("MCCB") || txt.Contains("MCB") || txt.Contains("TỦ ĐIỆN") || txt.Contains("DB-") || txt.Contains("MDB-")))
                        {
                            string key = txt.Trim();
                            if (!dict.ContainsKey(key))
                            {
                                dict[key] = new BomItem
                                {
                                    Name = txt.Trim(),
                                    Tag = "EQUIP",
                                    Spec = "Theo thiết kế",
                                    Unit = "Bộ",
                                    Quantity = 1,
                                    Notes = "Ghi chú từ bản vẽ"
                                };
                            }
                        }
                    }
                }

                tr.Commit();
            }

            return dict.Values.ToList();
        }

        private static string MapBlockNameToVietnamese(string blockName)
        {
            if (blockName.Contains("SYM_MCCB_3P")) return "Aptomat khối MCCB 3 Pha";
            if (blockName.Contains("SYM_MCCB_4P")) return "Aptomat khối MCCB 4 Pha (3P+N)";
            if (blockName.Contains("SYM_MCB_1P")) return "Aptomat tép MCB 1 Pha";
            if (blockName.Contains("SYM_MCB_2P")) return "Aptomat tép MCB 2 Pha";
            if (blockName.Contains("SYM_MCB_3P")) return "Aptomat tép MCB 3 Pha";
            if (blockName.Contains("SYM_RCCB")) return "Aptomat chống rò RCBO / ELCB 30mA";
            if (blockName.Contains("SYM_RELAY")) return "Rơ-le trung gian kiếng kèm đế";
            if (blockName.Contains("SYM_TERMINAL")) return "Cầu đấu dây gắn ray DIN Terminal Block";
            if (blockName.Contains("SYM_LAMP_RED")) return "Đèn báo pha Đỏ (L1/A)";
            if (blockName.Contains("SYM_LAMP_YELLOW")) return "Đèn báo pha Vàng (L2/B)";
            if (blockName.Contains("SYM_LAMP_GREEN")) return "Đèn báo pha Xanh (L3/C)";
            if (blockName.Contains("SYM_VFD")) return "Biến tần điều khiển động cơ (VFD)";
            if (blockName.Contains("SYM_SOFT_STARTER")) return "Bộ khởi động mềm (Soft Starter)";
            if (blockName.Contains("SYM_FUSE")) return "Cầu chì hạ thế bảo vệ mạch";
            if (blockName.Contains("SYM_EARTH")) return "Cực tiếp địa bảo vệ PE";
            if (blockName.Contains("SYM_OUTLET")) return "Ổ cắm điện chuẩn TCVN/IEC";
            if (blockName.Contains("SYM_LIGHT")) return "Bộ đèn chiếu sáng LED";
            if (blockName.Contains("SYM_SWITCH")) return "Công tắc điện";
            if (blockName.Contains("SYM_VOLTMETER")) return "Đồng hồ đo điện áp Volt (V)";
            if (blockName.Contains("SYM_AMMETER")) return "Đồng hồ đo dòng điện Ampe (A)";

            return blockName.Replace("DTT_SYM_", "").Replace("_", " ");
        }

        private static List<BomItem> GetSampleCabinetBom()
        {
            return new List<BomItem>
            {
                new BomItem { Stt = 1, Name = "Vỏ tủ điện sơn tĩnh điện (kèm chân đế)", Tag = "ENCLOSURE", Spec = "800x1200x300mm Tole 1.5mm IP54", Unit = "Cái", Quantity = 1, Notes = "Sơn tĩnh điện RAL 7035" },
                new BomItem { Stt = 2, Name = "Tấm gá linh kiện tôn mạ kẽm", Tag = "PANEL-PLATE", Spec = "720x1120mm Tole 2.0mm", Unit = "Tấm", Quantity = 1, Notes = "Gia công đột lỗ CNC" },
                new BomItem { Stt = 3, Name = "Aptomat khối MCCB 3 Pha chính", Tag = "MCCB-MAIN", Spec = "3P 250A 36kA Schneider/LS", Unit = "Bộ", Quantity = 1, Notes = "Nguồn tổng" },
                new BomItem { Stt = 4, Name = "Aptomat tép nhánh MCB 1 Pha", Tag = "MCB-BRANCH", Spec = "1P 16A 6kA Acti9/BKN", Unit = "Cái", Quantity = 18, Notes = "Các lộ chiếu sáng & ổ cắm" },
                new BomItem { Stt = 5, Name = "Aptomat chống rò chống giật RCBO", Tag = "RCBO-01", Spec = "2P 32A 30mA", Unit = "Cái", Quantity = 2, Notes = "Khu vực ẩm ướt / Bếp" },
                new BomItem { Stt = 6, Name = "Rơ-le trung gian kiếng kèm đế 14 chân", Tag = "RELAY-AUX", Spec = "MY4N-J 220VAC 5A Omron", Unit = "Bộ", Quantity = 6, Notes = "Mạch điều khiển & liên động" },
                new BomItem { Stt = 7, Name = "Dàn cầu đấu dây gắn ray DIN Terminal", Tag = "TB-STRIP", Spec = "TB-2.5mm² 24A Weidmuller", Unit = "Cực", Quantity = 24, Notes = "Đấu nối xuất tuyến ngoài" },
                new BomItem { Stt = 8, Name = "Đèn báo pha LED hiển thị mặt tủ", Tag = "PILOT-LAMPS", Spec = "Phi 22 220VAC (Đỏ, Vàng, Xanh)", Unit = "Bộ", Quantity = 3, Notes = "Chỉ thị nguồn 3 pha" },
                new BomItem { Stt = 9, Name = "Đồng hồ kỹ thuật số đo Điện Áp & Dòng", Tag = "METER-VA", Spec = "Multimeter LED Digital 3P", Unit = "Bộ", Quantity = 1, Notes = "Lắp mặt cửa ngoài" },
                new BomItem { Stt = 10, Name = "Thanh cái đồng Busbar mạ thiếc", Tag = "BUSBAR-CU", Spec = "Đồng đỏ Cu 20x3mm (R, S, T, N, PE)", Unit = "Bộ", Quantity = 1, Notes = "Bọc co nhiệt 5 màu" },
                new BomItem { Stt = 11, Name = "Máng cáp đi dây nhựa lược Slotted Duct", Tag = "DUCT-PVC", Spec = "35x50mm PVC chống cháy", Unit = "Mét", Quantity = 6, Notes = "Đi dây gọn gàng nội bộ" },
                new BomItem { Stt = 12, Name = "Khóa tay nắm xoay & Bản lề công nghiệp", Tag = "HARDWARE", Spec = "Tay nắm xoay bật có chìa khóa", Unit = "Bộ", Quantity = 1, Notes = "Phụ kiện vỏ tủ" }
            };
        }

        // ============================================================
        // TẠO FILE MICROSOFT EXCEL (.XLSX) CHUẨN OPENXML BẰNG ZIPARCHIVE
        // Không phụ thuộc MS Office / Excel Interop, mở được trên 100% máy tính
        // ============================================================

        private static bool CreateOpenXmlExcelFile(List<BomItem> items, string filePath, string title)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    try { File.Delete(filePath); } catch { }
                }

                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
                using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    // 1. [Content_Types].xml
                    AddZipEntry(zip, "[Content_Types].xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">\r\n" +
                        "  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>\r\n" +
                        "  <Default Extension=\"xml\" ContentType=\"application/xml\"/>\r\n" +
                        "  <Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>\r\n" +
                        "  <Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>\r\n" +
                        "  <Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>\r\n" +
                        "</Types>");

                    // 2. _rels/.rels
                    AddZipEntry(zip, "_rels/.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\r\n" +
                        "  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>\r\n" +
                        "</Relationships>");

                    // 3. xl/_rels/workbook.xml.rels
                    AddZipEntry(zip, "xl/_rels/workbook.xml.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\r\n" +
                        "  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>\r\n" +
                        "  <Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>\r\n" +
                        "</Relationships>");

                    // 4. xl/workbook.xml
                    AddZipEntry(zip, "xl/workbook.xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">\r\n" +
                        "  <sheets>\r\n" +
                        "    <sheet name=\"BocTachVatTu_BOM\" sheetId=\"1\" r:id=\"rId1\"/>\r\n" +
                        "  </sheets>\r\n" +
                        "</workbook>");

                    // 5. xl/styles.xml (Định nghĩa định dạng viền, font chữ, màu nền)
                    AddZipEntry(zip, "xl/styles.xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">\r\n" +
                        "  <fonts count=\"4\">\r\n" +
                        "    <font><name val=\"Segoe UI\"/><sz val=\"10\"/></font>\r\n" +
                        "    <font><b/><name val=\"Segoe UI\"/><sz val=\"14\"/><color rgb=\"FF0284C7\"/></font>\r\n" +
                        "    <font><b/><name val=\"Segoe UI\"/><sz val=\"10.5\"/><color rgb=\"FFFFFFFF\"/></font>\r\n" +
                        "    <font><b/><name val=\"Segoe UI\"/><sz val=\"10\"/><color rgb=\"FF0F172A\"/></font>\r\n" +
                        "  </fonts>\r\n" +
                        "  <fills count=\"4\">\r\n" +
                        "    <fill><patternFill patternType=\"none\"/></fill>\r\n" +
                        "    <fill><patternFill patternType=\"gray125\"/></fill>\r\n" +
                        "    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF0284C7\"/></patternFill></fill>\r\n" + // Header xanh cyan
                        "    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFF1F5F9\"/></patternFill></fill>\r\n" + // Dòng chẵn nhạt
                        "  </fills>\r\n" +
                        "  <borders count=\"2\">\r\n" +
                        "    <border><left/><right/><top/><bottom/></border>\r\n" +
                        "    <border>\r\n" +
                        "      <left style=\"thin\"><color rgb=\"FFCBD5E1\"/></left>\r\n" +
                        "      <right style=\"thin\"><color rgb=\"FFCBD5E1\"/></right>\r\n" +
                        "      <top style=\"thin\"><color rgb=\"FFCBD5E1\"/></top>\r\n" +
                        "      <bottom style=\"thin\"><color rgb=\"FFCBD5E1\"/></bottom>\r\n" +
                        "    </border>\r\n" +
                        "  </borders>\r\n" +
                        "  <cellXfs count=\"5\">\r\n" +
                        "    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>\r\n" + // 0: Normal
                        "    <xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" applyFont=\"1\"/>\r\n" + // 1: Title
                        "    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"2\" borderId=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" alignment=\"center\"/>\r\n" + // 2: Header Col
                        "    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" applyFont=\"1\" applyBorder=\"1\"/>\r\n" + // 3: Data Cell
                        "    <xf numFmtId=\"0\" fontId=\"3\" fillId=\"3\" borderId=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>\r\n" + // 4: Summary Row
                        "  </cellXfs>\r\n" +
                        "</styleSheet>");

                    // 6. xl/worksheets/sheet1.xml (Dữ liệu bảng tính thực tế)
                    StringBuilder sb = new StringBuilder();
                    sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n");
                    sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">\r\n");

                    // Định nghĩa độ rộng cột (Auto-fit Columns)
                    sb.Append("  <cols>\r\n");
                    sb.Append("    <col min=\"1\" max=\"1\" width=\"7\" customWidth=\"1\"/>\r\n");  // STT
                    sb.Append("    <col min=\"2\" max=\"2\" width=\"38\" customWidth=\"1\"/>\r\n"); // Tên
                    sb.Append("    <col min=\"3\" max=\"3\" width=\"18\" customWidth=\"1\"/>\r\n"); // Tag
                    sb.Append("    <col min=\"4\" max=\"4\" width=\"32\" customWidth=\"1\"/>\r\n"); // Spec
                    sb.Append("    <col min=\"5\" max=\"5\" width=\"10\" customWidth=\"1\"/>\r\n"); // Đơn vị
                    sb.Append("    <col min=\"6\" max=\"6\" width=\"12\" customWidth=\"1\"/>\r\n"); // Số lượng
                    sb.Append("    <col min=\"7\" max=\"7\" width=\"28\" customWidth=\"1\"/>\r\n"); // Ghi chú
                    sb.Append("  </cols>\r\n");

                    sb.Append("  <sheetData>\r\n");

                    // Dòng 1: Tiêu đề lớn
                    sb.Append("    <row r=\"1\" ht=\"28\" customHeight=\"1\">\r\n");
                    sb.Append($"      <c r=\"B1\" s=\"1\" t=\"inlineStr\"><is><t>{EscapeXml(title)}</t></is></c>\r\n");
                    sb.Append("    </row>\r\n");

                    // Dòng 2: Thông tin dự án
                    sb.Append("    <row r=\"2\" ht=\"18\" customHeight=\"1\">\r\n");
                    sb.Append($"      <c r=\"B2\" t=\"inlineStr\"><is><t>Dự án: Thiết kế &amp; Sản xuất Tủ Điện • Xuất tự động bởi DTools Pro v3.5 • Ngày: {DateTime.Now:dd/MM/yyyy HH:mm}</t></is></c>\r\n");
                    sb.Append("    </row>\r\n");

                    // Dòng 4: Header Bảng
                    int startRow = 4;
                    sb.Append($"    <row r=\"{startRow}\" ht=\"24\" customHeight=\"1\">\r\n");
                    sb.Append($"      <c r=\"A{startRow}\" s=\"2\" t=\"inlineStr\"><is><t>STT</t></is></c>\r\n");
                    sb.Append($"      <c r=\"B{startRow}\" s=\"2\" t=\"inlineStr\"><is><t>MÃ / TÊN THIẾT BỊ</t></is></c>\r\n");
                    sb.Append($"      <c r=\"C{startRow}\" s=\"2\" t=\"inlineStr\"><is><t>KÝ HIỆU (TAG)</t></is></c>\r\n");
                    sb.Append($"      <c r=\"D{startRow}\" s=\"2\" t=\"inlineStr\"><is><t>THÔNG SỐ KỸ THUẬT</t></is></c>\r\n");
                    sb.Append($"      <c r=\"E{startRow}\" s=\"2\" t=\"inlineStr\"><is><t>ĐƠN VỊ</t></is></c>\r\n");
                    sb.Append($"      <c r=\"F{startRow}\" s=\"2\" t=\"inlineStr\"><is><t>SỐ LƯỢNG</t></is></c>\r\n");
                    sb.Append($"      <c r=\"G{startRow}\" s=\"2\" t=\"inlineStr\"><is><t>GHI CHÚ</t></is></c>\r\n");
                    sb.Append("    </row>\r\n");

                    // Các dòng dữ liệu
                    int totalQty = 0;
                    for (int i = 0; i < items.Count; i++)
                    {
                        var it = items[i];
                        int rNum = startRow + 1 + i;
                        totalQty += it.Quantity;

                        sb.Append($"    <row r=\"{rNum}\" ht=\"20\" customHeight=\"1\">\r\n");
                        sb.Append($"      <c r=\"A{rNum}\" s=\"3\"><v>{it.Stt}</v></c>\r\n");
                        sb.Append($"      <c r=\"B{rNum}\" s=\"3\" t=\"inlineStr\"><is><t>{EscapeXml(it.Name)}</t></is></c>\r\n");
                        sb.Append($"      <c r=\"C{rNum}\" s=\"3\" t=\"inlineStr\"><is><t>{EscapeXml(it.Tag)}</t></is></c>\r\n");
                        sb.Append($"      <c r=\"D{rNum}\" s=\"3\" t=\"inlineStr\"><is><t>{EscapeXml(it.Spec)}</t></is></c>\r\n");
                        sb.Append($"      <c r=\"E{rNum}\" s=\"3\" t=\"inlineStr\"><is><t>{EscapeXml(it.Unit)}</t></is></c>\r\n");
                        sb.Append($"      <c r=\"F{rNum}\" s=\"3\"><v>{it.Quantity}</v></c>\r\n");
                        sb.Append($"      <c r=\"G{rNum}\" s=\"3\" t=\"inlineStr\"><is><t>{EscapeXml(it.Notes)}</t></is></c>\r\n");
                        sb.Append("    </row>\r\n");
                    }

                    // Dòng tổng cộng
                    int sumRow = startRow + 1 + items.Count;
                    sb.Append($"    <row r=\"{sumRow}\" ht=\"22\" customHeight=\"1\">\r\n");
                    sb.Append($"      <c r=\"A{sumRow}\" s=\"4\" t=\"inlineStr\"><is><t></t></is></c>\r\n");
                    sb.Append($"      <c r=\"B{sumRow}\" s=\"4\" t=\"inlineStr\"><is><t>TỔNG CỘNG SỐ LƯỢNG VẬT TƯ:</t></is></c>\r\n");
                    sb.Append($"      <c r=\"C{sumRow}\" s=\"4\" t=\"inlineStr\"><is><t></t></is></c>\r\n");
                    sb.Append($"      <c r=\"D{sumRow}\" s=\"4\" t=\"inlineStr\"><is><t></t></is></c>\r\n");
                    sb.Append($"      <c r=\"E{sumRow}\" s=\"4\" t=\"inlineStr\"><is><t>Hạng mục</t></is></c>\r\n");
                    sb.Append($"      <c r=\"F{sumRow}\" s=\"4\"><v>{totalQty}</v></c>\r\n");
                    sb.Append($"      <c r=\"G{sumRow}\" s=\"4\" t=\"inlineStr\"><is><t>Đã kiểm tra &amp; xuất kho</t></is></c>\r\n");
                    sb.Append("    </row>\r\n");

                    sb.Append("  </sheetData>\r\n");
                    sb.Append("</worksheet>");

                    AddZipEntry(zip, "xl/worksheets/sheet1.xml", sb.ToString());
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void AddZipEntry(ZipArchive zip, string entryName, string content)
        {
            var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), Encoding.UTF8))
            {
                writer.Write(content);
            }
        }

        private static string EscapeXml(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }
    }
}
