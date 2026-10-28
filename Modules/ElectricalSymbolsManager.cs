using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.Modules
{
    public class ElectricalSymbolItem
    {
        public string Id { get; set; }
        public string NameEn { get; set; }
        public string NameVi { get; set; }
        public string Category { get; set; }
        public string DwgFile { get; set; }
        public string PreviewImage { get; set; }
        public string SymbolType { get; set; }
        public string Icon { get; set; }
        public string DefaultSpec { get; set; } = "";
        public double DefaultHeight { get; set; } = 0.0;

        public string DisplayName => $"{Icon} {NameVi} ({NameEn})";
    }

    /// <summary>
    /// Ngữ cảnh lưu trữ thông tin ký hiệu điện đang chọn để đặt liên tục trên CAD
    /// </summary>
    public static class ElectricalPlacementContext
    {
        public static ElectricalSymbolItem TargetSymbol { get; set; }
        public static double Scale { get; set; } = 1.0;
        public static double Rotation { get; set; } = 0.0;
    }

    public static class ElectricalSymbolsManager
    {
        private static List<ElectricalSymbolItem> _cachedSymbols = null;

        public static List<ElectricalSymbolItem> GetAllSymbols()
        {
            if (_cachedSymbols != null && _cachedSymbols.Count > 0)
                return _cachedSymbols;

            _cachedSymbols = LoadSymbolsFromFile();
            return _cachedSymbols;
        }

        public static List<ElectricalSymbolItem> SearchSymbols(string keyword, string category = null)
        {
            var list = GetAllSymbols();
            if (list == null) return new List<ElectricalSymbolItem>();

            var filtered = list.AsEnumerable();

            if (!string.IsNullOrEmpty(category) && category != "All" && category != "Tất cả")
            {
                filtered = filtered.Where(s => string.Equals(s.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = RemoveDiacritics(keyword.Trim().ToLower());
                filtered = filtered.Where(s =>
                    RemoveDiacritics((s.NameVi ?? "").ToLower()).Contains(kw) ||
                    (s.NameEn ?? "").ToLower().Contains(kw) ||
                    (s.Id ?? "").ToLower().Contains(kw) ||
                    (s.Category ?? "").ToLower().Contains(kw));
            }

            return filtered.ToList();
        }

        private static List<ElectricalSymbolItem> LoadSymbolsFromFile()
        {
            try
            {
                string jsonPath = DTools.Core.ResourceLocator.GetSymbolsDatabasePath();
                if (File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath, Encoding.UTF8);
                    var list = ParseJsonSymbols(json);
                    if (list != null && list.Count > 0)
                    {
                        return list;
                    }
                }
            }
            catch { }

            return GetFallbackBuiltinSymbols();
        }

        private static List<ElectricalSymbolItem> ParseJsonSymbols(string json)
        {
            var items = new List<ElectricalSymbolItem>();
            try
            {
                // Tiền xử lý: loại bỏ comment và trailing comma nếu có
                string cleanJson = Regex.Replace(json, @",\s*([}\]])", "$1");

                // Parse danh sách object JSON
                var regex = new Regex(@"\{[^{}]*\}", RegexOptions.Singleline);
                var matches = regex.Matches(cleanJson);

                foreach (Match m in matches)
                {
                    string block = m.Value;
                    string id = ExtractJsonField(block, "id");
                    if (string.IsNullOrEmpty(id)) continue;

                    double defHeight = 0.0;
                    string hStr = ExtractJsonField(block, "defaultHeight");
                    if (!string.IsNullOrEmpty(hStr)) double.TryParse(hStr, out defHeight);

                    items.Add(new ElectricalSymbolItem
                    {
                        Id = id,
                        NameEn = ExtractJsonField(block, "nameEn"),
                        NameVi = ExtractJsonField(block, "nameVi"),
                        Category = ExtractJsonField(block, "category"),
                        DwgFile = ExtractJsonField(block, "dwgFile"),
                        PreviewImage = ExtractJsonField(block, "previewImage"),
                        SymbolType = ExtractJsonField(block, "symbolType"),
                        DefaultSpec = ExtractJsonField(block, "defaultSpec"),
                        Icon = ExtractJsonField(block, "icon"),
                        DefaultHeight = defHeight
                    });
                }
            }
            catch { }

            return items;
        }

        private static string ExtractJsonField(string block, string key)
        {
            // Hỗ trợ cả trường số và chuỗi
            var r = new Regex($@"""{key}""\s*:\s*(?:""([^""]*)""|(-?\d+(?:\.\d+)?))", RegexOptions.IgnoreCase);
            var m = r.Match(block);
            if (!m.Success) return "";
            return !string.IsNullOrEmpty(m.Groups[1].Value) ? m.Groups[1].Value : m.Groups[2].Value;
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC).Replace("đ", "d").Replace("Đ", "D");
        }

        private static List<ElectricalSymbolItem> GetFallbackBuiltinSymbols()
        {
            return new List<ElectricalSymbolItem>
            {
                new ElectricalSymbolItem { Id = "SYM_OUTLET_1P", NameEn = "Single Socket Outlet 1P 10A", NameVi = "Ổ cắm đơn 1P 2 chấu 10A", Category = "OUTLET", SymbolType = "OutletSingle", Icon = "🔌" },
                new ElectricalSymbolItem { Id = "SYM_OUTLET_2P_E", NameEn = "Double Socket Outlet with Earth 16A", NameVi = "Ổ cắm đôi 3 chấu có tiếp địa 16A", Category = "OUTLET", SymbolType = "OutletDouble", Icon = "🔌" },
                new ElectricalSymbolItem { Id = "SYM_OUTLET_WP", NameEn = "Waterproof Socket Outlet IP55", NameVi = "Ổ cắm chống thấm nắp che IP55", Category = "OUTLET", SymbolType = "OutletWaterproof", Icon = "💧" },
                new ElectricalSymbolItem { Id = "SYM_LIGHT_DOWNLIGHT", NameEn = "LED Recessed Downlight 12W", NameVi = "Đèn Downlight LED âm trần D110 12W", Category = "LIGHT", SymbolType = "LightDownlight", Icon = "💡" },
                new ElectricalSymbolItem { Id = "SYM_LIGHT_TUBE", NameEn = "LED Tube Light 1.2m T8 18W", NameVi = "Bộ đèn tuýp LED 1m2 T8/T5 18W", Category = "LIGHT", SymbolType = "LightTube", Icon = "💡" },
                new ElectricalSymbolItem { Id = "SYM_LIGHT_PANEL", NameEn = "LED Panel Light 600x600 48W", NameVi = "Đèn Panel LED tấm vuông 600x600 48W", Category = "LIGHT", SymbolType = "LightPanel", Icon = "🔲" },
                new ElectricalSymbolItem { Id = "SYM_SWITCH_1G", NameEn = "1-Gang 1-Way Switch 16A", NameVi = "Công tắc đơn 1 hạt 1 chiều", Category = "SWITCH", SymbolType = "SwitchSingle", Icon = "🔘" },
                new ElectricalSymbolItem { Id = "SYM_SWITCH_2G", NameEn = "2-Gang 1-Way Switch 16A", NameVi = "Công tắc đôi 2 hạt 1 chiều", Category = "SWITCH", SymbolType = "SwitchDouble", Icon = "🔘" },
                new ElectricalSymbolItem { Id = "SYM_EARTH", NameEn = "Earth Potential", NameVi = "Tiếp địa / Nối đất bảo vệ PE", Category = "SWITCH", SymbolType = "Earth", Icon = "⏚" },
                new ElectricalSymbolItem { Id = "SYM_FUSE", NameEn = "Electrical Fuse", NameVi = "Cầu chì hạ thế", Category = "SWITCH", SymbolType = "Fuse", Icon = "⚡" },
                new ElectricalSymbolItem { Id = "SYM_MCB_1P", NameEn = "MCB 1P", NameVi = "Aptomat tép 1 pha (MCB)", Category = "SWITCH", SymbolType = "MCB1P", Icon = "🕹️" },
                new ElectricalSymbolItem { Id = "SYM_MCB_3P", NameEn = "MCB 3P", NameVi = "Aptomat tép 3 pha (MCB 3P)", Category = "SWITCH", SymbolType = "MCB3P", Icon = "🕹️" },
                new ElectricalSymbolItem { Id = "SYM_MCCB_3P", NameEn = "MCCB 3P", NameVi = "Aptomat khối 3 pha (MCCB)", Category = "PANEL", SymbolType = "MCCB3P", Icon = "🎛️" },
                new ElectricalSymbolItem { Id = "SYM_CABINET_DB", NameEn = "Distribution Board (DB)", NameVi = "Tủ điện phân phối tầng (DB)", Category = "PANEL", SymbolType = "CabinetDB", Icon = "📦" },
                new ElectricalSymbolItem { Id = "SYM_CABINET_MDB", NameEn = "Main Distribution Board (MDB)", NameVi = "Tủ điện tổng chính tòa nhà (MDB)", Category = "PANEL", SymbolType = "CabinetMDB", Icon = "⚡" }
            };
        }

        // ============================================================
        // LỆNH ĐẶT KÝ HIỆU LIÊN TỤC TRÊN CAD (AUTOCAD COMMAND CONTEXT)
        // ============================================================

        [CommandMethod("DTT_PLACE_SYMBOL")]
        [CommandMethod("DT_PLACE_SYMBOL")]
        [CommandMethod("PLACE_SYMBOL")]
        public static void CmdPlaceSymbol()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var sym = ElectricalPlacementContext.TargetSymbol;
            if (sym == null)
            {
                ed.WriteMessage("\n[DTools] Chưa chọn ký hiệu điện. Vui lòng chọn ký hiệu từ bảng Palette hoặc lệnh DTT_ELEC_MENU.\n");
                return;
            }

            double scale = ElectricalPlacementContext.Scale > 0 ? ElectricalPlacementContext.Scale : 1.0;
            double rot = ElectricalPlacementContext.Rotation;

            PromptPointOptions ppo = new PromptPointOptions($"\n⚡ [DTools] Click chọn điểm đặt ký hiệu [{sym.NameVi}] [Enter/Esc để kết thúc]: ");
            ppo.AllowNone = true;

            int count = 0;
            while (true)
            {
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) break;

                bool ok = InsertSymbolToCad(sym, ppr.Value, scale, rot);
                if (!ok) break;

                count++;
                ppo.Message = $"\n⚡ [DTools] Đã đặt {count} ký hiệu [{sym.NameVi}]. Click điểm tiếp theo [Enter/Esc để kết thúc]: ";
            }

            if (count > 0)
            {
                ed.WriteMessage($"\n✅ [DTools Pro] Đã hoàn thành chèn {count} ký hiệu [{sym.NameVi}] vào bản vẽ.\n");
            }
        }

        // ============================================================
        // HÀM CHÈN KÝ HIỆU VÀO AUTOCAD DƯỚI DẠNG BLOCK REFERENCE CHUẨN
        // ============================================================

        public static bool InsertSymbolToCad(ElectricalSymbolItem item, Point3d insertPt, double scale = 1.0, double rotationDeg = 0.0)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || item == null) return false;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            string blockName = $"DTT_SYM_{item.Id}";

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    ObjectId blockId = ObjectId.Null;

                    // 1. Kiểm tra block đã có trong database chưa
                    if (bt.Has(blockName))
                    {
                        blockId = bt[blockName];
                    }
                    else
                    {
                        // 2. Thử load từ file .dwg nếu có
                        string asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        string fullDwgPath = Path.Combine(asmDir, item.DwgFile ?? "");

                        if (!string.IsNullOrEmpty(item.DwgFile) && File.Exists(fullDwgPath))
                        {
                            try
                            {
                                using (Database extDb = new Database(false, true))
                                {
                                    extDb.ReadDwgFile(fullDwgPath, FileOpenMode.OpenForReadAndAllShare, true, "");
                                    blockId = db.Insert(blockName, extDb, true);
                                }
                            }
                            catch { }
                        }

                        // 3. Nếu chưa có file .dwg, tự động sinh Block bằng Native AutoCAD API 100% chuẩn TCVN/IEC!
                        if (blockId.IsNull || !blockId.IsValid)
                        {
                            if (!bt.IsWriteEnabled) bt.UpgradeOpen();
                            blockId = CreateNativeSymbolBlock(db, tr, bt, blockName, item);
                        }
                    }

                    if (blockId.IsNull || !blockId.IsValid)
                    {
                        ed.WriteMessage($"\n[DTools ERROR] Không thể tạo khối cho ký hiệu {item.NameVi}!\n");
                        return false;
                    }

                    // 4. Chèn BlockReference vào ModelSpace
                    BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    string layerName = (item.Category == "ARCH") ? "A-FURN" : "E-SYMBOLS";
                    short layerColor = (item.Category == "ARCH") ? (short)3 : (short)2; // 2: Yellow, 3: Green
                    ObjectId symbolLayer = CadUtils.GetOrCreateLayer(db, layerName, layerColor, LineWeight.LineWeight025);

                    BlockReference br = new BlockReference(insertPt, blockId)
                    {
                        ScaleFactors = new Scale3d(scale),
                        Rotation = rotationDeg * (Math.PI / 180.0),
                        LayerId = symbolLayer
                    };

                    modelSpace.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);

                    tr.Commit();
                }

                ed.WriteMessage($"\n✅ [DTools Pro] Đã chèn ký hiệu: {item.NameVi} tại ({insertPt.X:F1}, {insertPt.Y:F1})\n");
                return true;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[DTools ERROR] Lỗi khi chèn ký hiệu: {ex.Message}\n");
                return false;
            }
        }

        // ============================================================
        // BỘ ENGINE SINH HÌNH HỌC KÝ HIỆU ĐIỆN VÀ THIẾT BỊ NATIVE 100%
        // CHUẨN IEC / TCVN / BS CHO TOÀN BỘ DANH MỤC THƯ VIỆN DTOOLS
        // ============================================================

        private static ObjectId CreateNativeSymbolBlock(Database db, Transaction tr, BlockTable bt, string blockName, ElectricalSymbolItem item)
        {
            if (!bt.IsWriteEnabled) bt.UpgradeOpen();

            BlockTableRecord btr = new BlockTableRecord { Name = blockName };
            ObjectId btrId = bt.Add(btr);
            tr.AddNewlyCreatedDBObject(btr, true);

            ObjectId zeroLayer = db.LayerZero;
            string type = item.SymbolType ?? "";

            switch (type)
            {
                // ==================== 1. Ổ CẮM ĐIỆN (OUTLETS / SOCKETS) ====================
                case "OutletSingle":
                    // Ổ cắm đơn: Nửa hình tròn + 1 chân cắm giữa
                    AddArc(btr, tr, 0, 0, 15, 0, Math.PI, zeroLayer);
                    AddLine(btr, tr, -15, 0, 15, 0, zeroLayer);
                    AddLine(btr, tr, 0, 0, 0, 15, zeroLayer);
                    AddText(btr, tr, db, "1P", -3, -7, 4.5);
                    break;

                case "OutletDouble":
                    // Ổ cắm đôi: Nửa hình tròn + 2 chân cắm đối xứng
                    AddArc(btr, tr, 0, 0, 15, 0, Math.PI, zeroLayer);
                    AddLine(btr, tr, -15, 0, 15, 0, zeroLayer);
                    AddLine(btr, tr, -6, 0, -6, 15, zeroLayer);
                    AddLine(btr, tr, 6, 0, 6, 15, zeroLayer);
                    AddText(btr, tr, db, "2P", -3, -7, 4.5);
                    break;

                case "OutletWaterproof":
                    // Ổ cắm chống thấm nắp che IP55: Ổ cắm đôi + Mái che tam giác chống nước
                    AddArc(btr, tr, 0, 0, 15, 0, Math.PI, zeroLayer);
                    AddLine(btr, tr, -15, 0, 15, 0, zeroLayer);
                    AddLine(btr, tr, -6, 0, -6, 15, zeroLayer);
                    AddLine(btr, tr, 6, 0, 6, 15, zeroLayer);
                    AddPolyline(btr, tr, zeroLayer, true, new Point2d(-18, 0), new Point2d(0, 22), new Point2d(18, 0));
                    AddText(btr, tr, db, "IP55", -7, -7, 4.5);
                    break;

                case "OutletFloor":
                    // Ổ cắm âm sàn bật nắp Pop-up: Khung vuông bao ngoài ổ cắm
                    AddRect(btr, tr, zeroLayer, -20, -10, 40, 32);
                    AddArc(btr, tr, 0, 0, 12, 0, Math.PI, zeroLayer);
                    AddLine(btr, tr, -12, 0, 12, 0, zeroLayer);
                    AddLine(btr, tr, -5, 0, -5, 12, zeroLayer);
                    AddLine(btr, tr, 5, 0, 5, 12, zeroLayer);
                    AddText(btr, tr, db, "FL", -4, -7, 4.5);
                    break;

                // ==================== 2. ĐÈN CHIẾU SÁNG (LIGHT FIXTURES) ====================
                case "LightDownlight":
                    // Đèn Downlight LED âm trần: Vòng tròn ngoài, vòng trong và chữ thập
                    AddCircle(btr, tr, 0, 0, 15, zeroLayer);
                    AddCircle(btr, tr, 0, 0, 7, zeroLayer);
                    AddLine(btr, tr, -15, 0, 15, 0, zeroLayer);
                    AddLine(btr, tr, 0, -15, 0, 15, zeroLayer);
                    break;

                case "LightTube":
                    // Đèn tuýp LED 1.2m: Hộp dài có vạch chéo biểu diễn bóng tuýp
                    AddRect(btr, tr, zeroLayer, -35, -6, 70, 12);
                    AddLine(btr, tr, -35, -6, 35, 6, zeroLayer);
                    AddLine(btr, tr, -35, 6, 35, -6, zeroLayer);
                    AddText(btr, tr, db, "1.2m", -8, -2, 4);
                    break;

                case "LightPanel":
                    // Đèn Panel LED tấm 600x600: Vuông có 2 đường chéo X
                    AddRect(btr, tr, zeroLayer, -22, -22, 44, 44);
                    AddLine(btr, tr, -22, -22, 22, 22, zeroLayer);
                    AddLine(btr, tr, -22, 22, 22, -22, zeroLayer);
                    AddText(btr, tr, db, "600x600", -14, -2, 4);
                    break;

                case "LightExit":
                    // Đèn chỉ hướng thoát hiểm EXIT: Biển hình chữ nhật có khung và chữ EXIT + Mũi tên
                    AddRect(btr, tr, zeroLayer, -26, -11, 52, 22);
                    AddRect(btr, tr, zeroLayer, -23, -8, 46, 16);
                    AddText(btr, tr, db, "EXIT", -14, -3, 6);
                    AddLine(btr, tr, 10, 0, 18, 0, zeroLayer);
                    AddLine(btr, tr, 14, 3, 18, 0, zeroLayer);
                    AddLine(btr, tr, 14, -3, 18, 0, zeroLayer);
                    break;

                case "PilotLampRed":
                case "PilotLampYellow":
                case "PilotLampGreen":
                    // Đèn báo pha tròn có dấu X bên trong
                    AddCircle(btr, tr, 0, 0, 12, zeroLayer);
                    double dl = 12.0 * Math.Cos(Math.PI / 4.0);
                    AddLine(btr, tr, -dl, -dl, dl, dl, zeroLayer);
                    AddLine(btr, tr, -dl, dl, dl, -dl, zeroLayer);
                    AddLine(btr, tr, 0, -12, 0, -18, zeroLayer);
                    AddLine(btr, tr, 0, 12, 0, 18, zeroLayer);
                    string phaseLetter = type.Contains("Red") ? "L1" : (type.Contains("Yellow") ? "L2" : "L3");
                    AddText(btr, tr, db, phaseLetter, -4, -3, 6);
                    break;

                // ==================== 3. CÔNG TẮC & NÚT BẤM (SWITCHES) ====================
                case "SwitchSingle":
                    // Công tắc đơn 1 hạt: Chấm tròn và 1 cánh gạt có móc
                    AddCircle(btr, tr, 0, 0, 4.5, zeroLayer);
                    AddLine(btr, tr, 0, 0, 14, 14, zeroLayer);
                    AddLine(btr, tr, 14, 14, 18, 10, zeroLayer);
                    break;

                case "SwitchDouble":
                    // Công tắc đôi 2 hạt: Chấm tròn và 2 cánh móc
                    AddCircle(btr, tr, 0, 0, 4.5, zeroLayer);
                    AddLine(btr, tr, 0, 0, 14, 14, zeroLayer);
                    AddLine(btr, tr, 14, 14, 18, 10, zeroLayer);
                    AddLine(btr, tr, 10, 10, 14, 6, zeroLayer);
                    break;

                case "Switch2Way":
                    // Công tắc đảo chiều cầu thang: 2 cánh gạt 2 phía đối diện
                    AddCircle(btr, tr, 0, 0, 4.5, zeroLayer);
                    AddLine(btr, tr, 0, 0, 14, 14, zeroLayer);
                    AddLine(btr, tr, 14, 14, 18, 10, zeroLayer);
                    AddLine(btr, tr, 0, 0, -14, -14, zeroLayer);
                    AddLine(btr, tr, -14, -14, -18, -10, zeroLayer);
                    break;

                case "PushButton":
                    // Nút ấn đơn điều khiển Start/Stop
                    AddCircle(btr, tr, 0, 0, 10, zeroLayer);
                    AddCircle(btr, tr, 0, 0, 6, zeroLayer);
                    AddLine(btr, tr, -15, 0, -10, 0, zeroLayer);
                    AddLine(btr, tr, 10, 0, 15, 0, zeroLayer);
                    AddText(btr, tr, db, "PB", -4, -3, 5);
                    break;

                case "EmergencyStop":
                    // Nút dừng khẩn cấp E-Stop nấm đỏ
                    AddCircle(btr, tr, 0, 0, 15, zeroLayer);
                    AddCircle(btr, tr, 0, 0, 10, zeroLayer);
                    AddText(btr, tr, db, "STOP", -8, -3, 5);
                    break;

                // ==================== 4. KHÍ CỤ ĐÓNG CẮT & BẢO VỆ ====================
                case "Earth":
                    // Đường trục đứng & 3 thanh ngang thuôn dần
                    AddLine(btr, tr, 0, 0, 0, -10, zeroLayer);
                    AddLine(btr, tr, -14, -10, 14, -10, zeroLayer);
                    AddLine(btr, tr, -9, -14, 9, -14, zeroLayer);
                    AddLine(btr, tr, -4, -18, 4, -18, zeroLayer);
                    break;

                case "Battery":
                    // Cực dương (+) dài mỏng, cực âm (-) ngắn dày
                    AddLine(btr, tr, -15, 0, -6, 0, zeroLayer);
                    AddLine(btr, tr, -6, 12, -6, -12, zeroLayer); // Cực +
                    AddLine(btr, tr, 0, 6, 0, -6, zeroLayer);     // Cực -
                    AddLine(btr, tr, 0, 0, 15, 0, zeroLayer);
                    AddText(btr, tr, db, "+", -10, 8, 5);
                    AddText(btr, tr, db, "-", 4, 8, 5);
                    break;

                case "Fuse":
                    // Cầu chì hạ thế
                    AddLine(btr, tr, -18, 0, 18, 0, zeroLayer);
                    AddRect(btr, tr, zeroLayer, -12, -5, 24, 10);
                    break;

                case "RelayCoil":
                    // Cuộn hút Rơ-le trung gian
                    AddLine(btr, tr, -15, 0, -8, 0, zeroLayer);
                    AddLine(btr, tr, 8, 0, 15, 0, zeroLayer);
                    AddRect(btr, tr, zeroLayer, -8, -10, 16, 20);
                    AddLine(btr, tr, -8, -10, 8, 10, zeroLayer);
                    AddText(btr, tr, db, "K", -2, -3, 6);
                    break;

                case "ContactNO":
                    // Tiếp điểm thường mở NO
                    AddLine(btr, tr, -15, 0, -4, 0, zeroLayer);
                    AddCircle(btr, tr, -4, 0, 1.5, zeroLayer);
                    AddLine(btr, tr, -3, 2, 4, 10, zeroLayer);
                    AddCircle(btr, tr, 4, 0, 1.5, zeroLayer);
                    AddLine(btr, tr, 4, 0, 15, 0, zeroLayer);
                    break;

                case "ContactNC":
                    // Tiếp điểm thường đóng NC
                    AddLine(btr, tr, -15, 0, -4, 0, zeroLayer);
                    AddCircle(btr, tr, -4, 0, 1.5, zeroLayer);
                    AddLine(btr, tr, -4, 2, 4, 0, zeroLayer);
                    AddCircle(btr, tr, 4, 0, 1.5, zeroLayer);
                    AddLine(btr, tr, 4, 0, 15, 0, zeroLayer);
                    break;

                case "MCB1P":
                    // Aptomat tép MCB 1P DIN Rail
                    AddRect(btr, tr, zeroLayer, -8, -25, 16, 50);
                    AddRect(btr, tr, zeroLayer, -4, -5, 8, 10);
                    AddCircle(btr, tr, 0, 18, 2, zeroLayer);
                    AddCircle(btr, tr, 0, -18, 2, zeroLayer);
                    AddText(btr, tr, db, "MCB", -6, 6, 4);
                    break;

                case "MCB2P":
                    // Aptomat tép MCB 2P DIN Rail
                    AddRect(btr, tr, zeroLayer, -16, -25, 32, 50);
                    AddRect(btr, tr, zeroLayer, -10, -5, 8, 10);
                    AddRect(btr, tr, zeroLayer, 2, -5, 8, 10);
                    AddCircle(btr, tr, -6, 18, 2, zeroLayer);
                    AddCircle(btr, tr, 6, 18, 2, zeroLayer);
                    AddCircle(btr, tr, -6, -18, 2, zeroLayer);
                    AddCircle(btr, tr, 6, -18, 2, zeroLayer);
                    AddText(btr, tr, db, "2P", -3, 6, 4);
                    break;

                case "MCB3P":
                    // Aptomat tép MCB 3P DIN Rail
                    AddRect(btr, tr, zeroLayer, -24, -25, 48, 50);
                    AddRect(btr, tr, zeroLayer, -16, -5, 32, 10);
                    AddCircle(btr, tr, -14, 18, 2, zeroLayer);
                    AddCircle(btr, tr, 0, 18, 2, zeroLayer);
                    AddCircle(btr, tr, 14, 18, 2, zeroLayer);
                    AddCircle(btr, tr, -14, -18, 2, zeroLayer);
                    AddCircle(btr, tr, 0, -18, 2, zeroLayer);
                    AddCircle(btr, tr, 14, -18, 2, zeroLayer);
                    AddText(btr, tr, db, "MCB 3P", -14, 6, 4.5);
                    break;

                case "MCCB3P":
                    // Aptomat khối MCCB 3 pha tổng
                    AddRect(btr, tr, zeroLayer, -25, -35, 50, 70);
                    AddRect(btr, tr, zeroLayer, -8, -10, 16, 20); // Tay gạt
                    AddCircle(btr, tr, -16, 25, 3, zeroLayer);
                    AddCircle(btr, tr, 0, 25, 3, zeroLayer);
                    AddCircle(btr, tr, 16, 25, 3, zeroLayer);
                    AddCircle(btr, tr, -16, -25, 3, zeroLayer);
                    AddCircle(btr, tr, 0, -25, 3, zeroLayer);
                    AddCircle(btr, tr, 16, -25, 3, zeroLayer);
                    AddText(btr, tr, db, "MCCB 3P", -18, 4, 5.5);
                    break;

                case "MCCB4P":
                    // Aptomat khối MCCB 4 pha (3P+N)
                    AddRect(btr, tr, zeroLayer, -32, -35, 64, 70);
                    AddRect(btr, tr, zeroLayer, -10, -10, 20, 20);
                    AddCircle(btr, tr, -22, 25, 3, zeroLayer);
                    AddCircle(btr, tr, -7, 25, 3, zeroLayer);
                    AddCircle(btr, tr, 7, 25, 3, zeroLayer);
                    AddCircle(btr, tr, 22, 25, 3, zeroLayer);
                    AddCircle(btr, tr, -22, -25, 3, zeroLayer);
                    AddCircle(btr, tr, -7, -25, 3, zeroLayer);
                    AddCircle(btr, tr, 7, -25, 3, zeroLayer);
                    AddCircle(btr, tr, 22, -25, 3, zeroLayer);
                    AddText(btr, tr, db, "MCCB 4P", -20, 4, 5.5);
                    break;

                case "RCCB":
                    // Aptomat chống giật chống rò ELCB / RCBO
                    AddRect(btr, tr, zeroLayer, -16, -25, 32, 50);
                    AddCircle(btr, tr, 0, 0, 7, zeroLayer);
                    AddRect(btr, tr, zeroLayer, 4, 12, 6, 6); // Nút Test T
                    AddText(btr, tr, db, "T", 5.5, 13.5, 3.5);
                    AddText(btr, tr, db, "RCBO", -12, -18, 4);
                    break;

                // ==================== 5. TỦ ĐIỆN & BIẾN TẦN & ĐỘNG LỰC ====================
                case "CabinetDB":
                    // Tủ điện phân phối tầng (DB) chuẩn bản vẽ M&E
                    AddRect(btr, tr, zeroLayer, -24, -18, 48, 36);
                    AddLine(btr, tr, -24, -18, 24, 18, zeroLayer);
                    AddPolyline(btr, tr, zeroLayer, true, new Point2d(-24, -18), new Point2d(-24, 18), new Point2d(24, 18));
                    AddText(btr, tr, db, "DB", -7, -10, 7);
                    break;

                case "CabinetMDB":
                    // Tủ điện tổng chính tòa nhà (MDB): Khung đôi + Dấu X kép
                    AddRect(btr, tr, zeroLayer, -30, -22, 60, 44);
                    AddRect(btr, tr, zeroLayer, -26, -18, 52, 36);
                    AddLine(btr, tr, -26, -18, 26, 18, zeroLayer);
                    AddLine(btr, tr, -26, 18, 26, -18, zeroLayer);
                    AddText(btr, tr, db, "MDB", -14, -4, 8);
                    break;

                case "VFD":
                case "SoftStarter":
                    // Biến tần điều khiển động cơ (VFD) / Khởi động mềm
                    AddRect(btr, tr, zeroLayer, -25, -35, 50, 70);
                    AddRect(btr, tr, zeroLayer, -18, 12, 36, 16);
                    AddText(btr, tr, db, "50.0Hz", -12, 16, 5);
                    AddCircle(btr, tr, -10, -5, 4, zeroLayer);
                    AddCircle(btr, tr, 10, -5, 4, zeroLayer);
                    AddText(btr, tr, db, type == "VFD" ? "INVERTER" : "SOFT START", -18, -22, 4.5);
                    break;

                case "UPS":
                    // Bộ lưu điện liên tục Online UPS
                    AddRect(btr, tr, zeroLayer, -24, -16, 48, 32);
                    AddLine(btr, tr, -24, -16, 24, 16, zeroLayer);
                    AddText(btr, tr, db, "UPS", -11, -4, 7);
                    break;

                case "Transformer":
                    // Máy biến áp / Biến dòng CT
                    AddCircle(btr, tr, -7, 0, 10, zeroLayer);
                    AddCircle(btr, tr, 7, 0, 10, zeroLayer);
                    AddLine(btr, tr, -7, 10, -7, 18, zeroLayer);
                    AddLine(btr, tr, 7, -10, 7, -18, zeroLayer);
                    AddText(btr, tr, db, "TR", -5, -16, 4.5);
                    break;

                case "Bell":
                    // Chuông còi báo động sự cố
                    AddArc(btr, tr, 0, 0, 14, 0, Math.PI, zeroLayer);
                    AddLine(btr, tr, -14, 0, 14, 0, zeroLayer);
                    AddLine(btr, tr, 0, 0, 0, -8, zeroLayer);
                    AddCircle(btr, tr, 0, -11, 3, zeroLayer);
                    break;

                case "TerminalBlock":
                    // Cầu đấu dây gắn ray TB
                    AddRect(btr, tr, zeroLayer, -6, -18, 12, 36);
                    AddCircle(btr, tr, 0, 10, 2.5, zeroLayer);
                    AddCircle(btr, tr, 0, -10, 2.5, zeroLayer);
                    AddLine(btr, tr, 0, 7, 0, -7, zeroLayer);
                    break;

                case "TerminalStrip":
                    // Khối hàng kẹp cầu đấu đa chân
                    AddRect(btr, tr, zeroLayer, -40, -12, 80, 24);
                    for (int x = -30; x <= 30; x += 15)
                    {
                        AddLine(btr, tr, x, -12, x, 12, zeroLayer);
                        AddCircle(btr, tr, x - 7.5, 6, 2, zeroLayer);
                        AddCircle(btr, tr, x - 7.5, -6, 2, zeroLayer);
                    }
                    AddText(btr, tr, db, "-X1", -8, -4, 6);
                    break;

                // ==================== CÁC LINH KIỆN MẠCH ĐỘNG LỰC & ĐIỀU KHIỂN ====================
                case "ContactorCoil":
                    // Cuộn hút Contactor KM (A1 - A2)
                    AddRect(btr, tr, zeroLayer, -12, -15, 24, 30);
                    AddLine(btr, tr, 0, 15, 0, 22, zeroLayer);
                    AddLine(btr, tr, 0, -15, 0, -22, zeroLayer);
                    AddCircle(btr, tr, 0, 22, 1.5, zeroLayer);
                    AddCircle(btr, tr, 0, -22, 1.5, zeroLayer);
                    AddText(btr, tr, db, "A1", 3, 17, 3.5);
                    AddText(btr, tr, db, "A2", 3, -21, 3.5);
                    AddText(btr, tr, db, "KM", -6, -3, 6);
                    break;

                case "ContactorPower3P":
                    // 3 tiếp điểm động lực Contactor 3 pha (1-2, 3-4, 5-6)
                    for (int i = 0; i < 3; i++)
                    {
                        double px = -20 + i * 20;
                        AddLine(btr, tr, px, 20, px, 10, zeroLayer);
                        AddCircle(btr, tr, px, 10, 1.5, zeroLayer);
                        AddLine(btr, tr, px, 9, px + 7, -8, zeroLayer); // Cánh tiếp điểm mở
                        AddCircle(btr, tr, px, -10, 1.5, zeroLayer);
                        AddLine(btr, tr, px, -10, px, -20, zeroLayer);
                        AddText(btr, tr, db, (2 * i + 1).ToString(), px - 8, 12, 3.5);
                        AddText(btr, tr, db, (2 * i + 2).ToString(), px - 8, -18, 3.5);
                    }
                    AddLine(btr, tr, -22, 0, 22, 0, zeroLayer); // Nét gạch liên kết 3 cực
                    AddText(btr, tr, db, "KM", 25, -3, 5.5);
                    break;

                case "OverloadRelay3P":
                    // Rơ le nhiệt 3 pha bảo vệ quá tải TH (Thermal Overload)
                    for (int i = 0; i < 3; i++)
                    {
                        double px = -20 + i * 20;
                        AddLine(btr, tr, px, 18, px, 6, zeroLayer);
                        AddArc(btr, tr, px, 0, 6, -Math.PI / 2, Math.PI / 2, zeroLayer); // Ký hiệu lưỡng kim nhiệt
                        AddLine(btr, tr, px, -6, px, -18, zeroLayer);
                    }
                    AddText(btr, tr, db, "TH / OL", 25, -3, 5);
                    break;

                case "TimerOnDelay":
                    // Rơ-le thời gian Timer ON-Delay T1
                    AddRect(btr, tr, zeroLayer, -15, -18, 30, 36);
                    AddLine(btr, tr, -15, -18, 15, 18, zeroLayer);
                    AddArc(btr, tr, 6, 9, 5, 0, Math.PI, zeroLayer); // Ký hiệu dù trễ
                    AddLine(btr, tr, 0, 18, 0, 24, zeroLayer);
                    AddLine(btr, tr, 0, -18, 0, -24, zeroLayer);
                    AddText(btr, tr, db, "T1", -5, -4, 6);
                    break;

                case "Motor3P_6Pins":
                    // Động cơ không đồng bộ 3 pha 6 đầu dây (U1-V1-W1 / U2-V2-W2)
                    AddCircle(btr, tr, 0, 0, 24, zeroLayer);
                    AddCircle(btr, tr, 0, 0, 21, zeroLayer);
                    AddText(btr, tr, db, "M", -5, 2, 8);
                    AddText(btr, tr, db, "3 ~", -6, -8, 5);
                    // 3 đầu cực trên: U1, V1, W1
                    AddLine(btr, tr, -12, 21, -12, 32, zeroLayer);
                    AddLine(btr, tr, 0, 24, 0, 32, zeroLayer);
                    AddLine(btr, tr, 12, 21, 12, 32, zeroLayer);
                    AddText(btr, tr, db, "U1", -15, 34, 3.5);
                    AddText(btr, tr, db, "V1", -3, 34, 3.5);
                    AddText(btr, tr, db, "W1", 9, 34, 3.5);
                    // 3 đầu cực dưới: U2, V2, W2
                    AddLine(btr, tr, -12, -21, -12, -32, zeroLayer);
                    AddLine(btr, tr, 0, -24, 0, -32, zeroLayer);
                    AddLine(btr, tr, 12, -21, 12, -32, zeroLayer);
                    AddText(btr, tr, db, "U2", -15, -38, 3.5);
                    AddText(btr, tr, db, "V2", -3, -38, 3.5);
                    AddText(btr, tr, db, "W2", 9, -38, 3.5);
                    break;

                case "PowerSource3P":
                    // Nguồn 3 pha L1, L2, L3, N, PE
                    for (int i = 0; i < 3; i++)
                    {
                        double px = -20 + i * 20;
                        AddLine(btr, tr, px, 20, px, -20, zeroLayer);
                        AddText(btr, tr, db, $"L{i + 1}", px - 5, 22, 4.5);
                    }
                    AddLine(btr, tr, 40, 20, 40, -20, zeroLayer);
                    AddText(btr, tr, db, "N", 37, 22, 4.5);
                    break;

                case "MechInterlock":
                    // Khóa liên động cơ khí giữa Contactor Sao và Tam giác
                    AddLine(btr, tr, -25, 0, 25, 0, zeroLayer);
                    AddPolyline(btr, tr, zeroLayer, true, new Point2d(-6, 6), new Point2d(0, 0), new Point2d(-6, -6));
                    AddPolyline(btr, tr, zeroLayer, true, new Point2d(6, 6), new Point2d(0, 0), new Point2d(6, -6));
                    AddText(btr, tr, db, "INTERLOCK", -16, 8, 4);
                    break;

                case "MotorStarter":
                    // Mạch khởi động động cơ (Contactor + Overload Relay + Động cơ)
                    AddRect(btr, tr, zeroLayer, -25, -35, 50, 70);
                    AddRect(btr, tr, zeroLayer, -16, 12, 32, 16);
                    AddText(btr, tr, db, "KM", -7, 16, 5);
                    AddRect(btr, tr, zeroLayer, -16, -6, 32, 14);
                    AddText(btr, tr, db, "OL", -6, -2, 5);
                    AddCircle(btr, tr, 0, -22, 9, zeroLayer);
                    AddText(btr, tr, db, "M3~", -7, -24, 4);
                    AddLine(btr, tr, 0, 12, 0, 8, zeroLayer);
                    AddLine(btr, tr, 0, -6, 0, -13, zeroLayer);
                    break;

                case "PlcController":
                    // Bộ điều khiển PLC công nghiệp (Siemens / Mitsubishi / Omron / Schneider)
                    AddRect(btr, tr, zeroLayer, -35, -22, 70, 44);
                    // Dãy chân đấu dây I/O trên
                    for (int x = -28; x <= 28; x += 8) AddCircle(btr, tr, x, 16, 1.8, zeroLayer);
                    // Dãy chân đấu dây I/O dưới
                    for (int x = -28; x <= 28; x += 8) AddCircle(btr, tr, x, -16, 1.8, zeroLayer);
                    // Dãy đèn LED trạng thái
                    for (int x = -24; x <= -8; x += 4) AddCircle(btr, tr, x, 4, 1.2, zeroLayer);
                    string plcLabel = item.NameVi.Contains("1200") ? "S7-1200" :
                                      (item.NameVi.Contains("1500") ? "S7-1500" :
                                      (item.NameVi.Contains("FX5U") ? "FX5U" :
                                      (item.NameVi.Contains("CP1H") ? "CP1H" : "PLC CPU")));
                    AddText(btr, tr, db, plcLabel, -16, -6, 5.5);
                    break;

                // ==================== 6. ĐIỆN NHẸ ELV & THIẾT BỊ PHỤ TRỢ ====================
                case "DataOutlet":
                    // Ổ cắm mạng LAN RJ45 Cat6: Tam giác có vạch chia đôi
                    AddPolyline(btr, tr, zeroLayer, true, new Point2d(-15, -10), new Point2d(15, -10), new Point2d(0, 16));
                    AddLine(btr, tr, 0, -10, 0, 16, zeroLayer);
                    AddText(btr, tr, db, "DATA", -9, -17, 4.5);
                    break;

                case "TvOutlet":
                    // Ổ cắm truyền hình Tivi: Tam giác kèm chữ TV
                    AddPolyline(btr, tr, zeroLayer, true, new Point2d(-15, -10), new Point2d(15, -10), new Point2d(0, 16));
                    AddText(btr, tr, db, "TV", -5, -5, 6);
                    break;

                case "SmokeDetector":
                    // Đầu báo khói quang điện gắn trần
                    AddCircle(btr, tr, 0, 0, 15, zeroLayer);
                    AddCircle(btr, tr, 0, 0, 10, zeroLayer);
                    AddText(btr, tr, db, "S", -4, -4, 8);
                    break;

                case "IpCamera":
                    // Camera an ninh IP Dome bán cầu
                    AddCircle(btr, tr, 0, 0, 15, zeroLayer);
                    AddPolyline(btr, tr, zeroLayer, true, new Point2d(-8, -6), new Point2d(8, -6), new Point2d(5, 7), new Point2d(-5, 7));
                    AddCircle(btr, tr, 0, -6, 3, zeroLayer);
                    AddText(btr, tr, db, "CAM", -7, 8, 3.5);
                    break;

                case "AcPoint":
                    // Điểm cấp nguồn Điều hòa nhiệt độ AC
                    AddRect(btr, tr, zeroLayer, -20, -14, 40, 28);
                    AddLine(btr, tr, -6, 14, -6, 19, zeroLayer);
                    AddLine(btr, tr, 6, 14, 6, 19, zeroLayer);
                    AddText(btr, tr, db, "AC", -7, -4, 8);
                    break;

                case "WaterHeaterPoint":
                    // Điểm cấp nguồn Bình nóng lạnh
                    AddRect(btr, tr, zeroLayer, -20, -14, 40, 28);
                    AddText(btr, tr, db, "NL", -7, -4, 8);
                    break;

                case "CeilingFanPoint":
                    // Điểm cấp nguồn Quạt trần
                    AddCircle(btr, tr, 0, 0, 6, zeroLayer);
                    AddArc(btr, tr, 0, 0, 18, 0, Math.PI * 0.6, zeroLayer);
                    AddArc(btr, tr, 0, 0, 18, Math.PI * 0.7, Math.PI * 1.3, zeroLayer);
                    AddArc(btr, tr, 0, 0, 18, Math.PI * 1.4, Math.PI * 2.0, zeroLayer);
                    break;

                case "CabinetLock":
                    // Khóa tay nắm xoay
                    AddCircle(btr, tr, 0, 0, 10, zeroLayer);
                    AddRect(btr, tr, zeroLayer, -3, -18, 6, 36);
                    AddCircle(btr, tr, 0, 0, 4, zeroLayer);
                    break;

                case "Voltmeter":
                    AddCircle(btr, tr, 0, 0, 14, zeroLayer);
                    AddText(btr, tr, db, "V", -4, -4, 9);
                    break;

                case "Ammeter":
                    AddCircle(btr, tr, 0, 0, 14, zeroLayer);
                    AddText(btr, tr, db, "A", -4, -4, 9);
                    break;

                case "WiringDuct":
                    AddRect(btr, tr, zeroLayer, -12, -40, 24, 80);
                    for (int y = -30; y <= 30; y += 10)
                    {
                        AddLine(btr, tr, -12, y, -6, y, zeroLayer);
                        AddLine(btr, tr, 6, y, 12, y, zeroLayer);
                    }
                    break;

                // ==================== 7. KIẾN TRÚC & KẾT CẤU (ARCHITECTURAL) ====================
                case "ArchColumn":
                    // Cột bê tông cốt thép 220x220mm kèm 4 thép góc
                    AddRect(btr, tr, zeroLayer, -15, -15, 30, 30);
                    AddRect(btr, tr, zeroLayer, -10, -10, 20, 20);
                    AddCircle(btr, tr, -10, -10, 2.5, zeroLayer);
                    AddCircle(btr, tr, 10, -10, 2.5, zeroLayer);
                    AddCircle(btr, tr, 10, 10, 2.5, zeroLayer);
                    AddCircle(btr, tr, -10, 10, 2.5, zeroLayer);
                    AddLine(btr, tr, -15, -15, 15, 15, zeroLayer);
                    AddLine(btr, tr, -15, 15, 15, -15, zeroLayer);
                    break;

                case "ArchDoor":
                    // Cửa đi 1 cánh mở quay 90 độ
                    AddRect(btr, tr, zeroLayer, -25, -5, 5, 10);
                    AddRect(btr, tr, zeroLayer, 20, -5, 5, 10);
                    AddLine(btr, tr, -25, 0, -25, 45, zeroLayer);
                    AddArc(btr, tr, -25, 0, 45, 0, Math.PI / 2.0, zeroLayer);
                    break;

                case "ArchWindow":
                    // Cửa sổ 2 cánh
                    AddLine(btr, tr, -30, -5, 30, -5, zeroLayer);
                    AddLine(btr, tr, -30, 5, 30, 5, zeroLayer);
                    AddLine(btr, tr, -30, -5, -30, 5, zeroLayer);
                    AddLine(btr, tr, 30, -5, 30, 5, zeroLayer);
                    AddLine(btr, tr, -28, -1.5, 28, -1.5, zeroLayer);
                    AddLine(btr, tr, -28, 1.5, 28, 1.5, zeroLayer);
                    break;

                case "ArchGrid":
                    // Trục định vị & bong bóng trục
                    AddLine(btr, tr, -30, 0, 30, 0, zeroLayer);
                    AddLine(btr, tr, 0, -30, 0, 30, zeroLayer);
                    AddCircle(btr, tr, 0, 25, 12, zeroLayer);
                    AddText(btr, tr, db, "A", -3, 21, 8);
                    break;

                default:
                    // Khối mặc định hình chữ nhật bo + Text
                    AddRect(btr, tr, zeroLayer, -15, -15, 30, 30);
                    AddText(btr, tr, db, item.Id.Replace("SYM_", ""), -12, -3, 4);
                    break;
            }

            return btrId;
        }

        private static void AddLine(BlockTableRecord btr, Transaction tr, double x1, double y1, double x2, double y2, ObjectId layerId)
        {
            Line line = new Line(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0)) { LayerId = layerId };
            btr.AppendEntity(line);
            tr.AddNewlyCreatedDBObject(line, true);
        }

        private static void AddCircle(BlockTableRecord btr, Transaction tr, double cx, double cy, double r, ObjectId layerId)
        {
            Circle c = new Circle(new Point3d(cx, cy, 0), Vector3d.ZAxis, r) { LayerId = layerId };
            btr.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);
        }

        private static void AddArc(BlockTableRecord btr, Transaction tr, double cx, double cy, double r, double startAngleRad, double endAngleRad, ObjectId layerId)
        {
            Arc arc = new Arc(new Point3d(cx, cy, 0), r, startAngleRad, endAngleRad) { LayerId = layerId };
            btr.AppendEntity(arc);
            tr.AddNewlyCreatedDBObject(arc, true);
        }

        private static void AddRect(BlockTableRecord btr, Transaction tr, ObjectId layerId, double x, double y, double w, double h)
        {
            Polyline pl = new Polyline();
            pl.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
            pl.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
            pl.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
            pl.Closed = true;
            pl.LayerId = layerId;
            btr.AppendEntity(pl);
            tr.AddNewlyCreatedDBObject(pl, true);
        }

        private static void AddPolyline(BlockTableRecord btr, Transaction tr, ObjectId layerId, bool closed, params Point2d[] pts)
        {
            if (pts == null || pts.Length < 2) return;
            Polyline pl = new Polyline();
            for (int i = 0; i < pts.Length; i++)
            {
                pl.AddVertexAt(i, pts[i], 0, 0, 0);
            }
            pl.Closed = closed;
            pl.LayerId = layerId;
            btr.AppendEntity(pl);
            tr.AddNewlyCreatedDBObject(pl, true);
        }

        private static void AddText(BlockTableRecord btr, Transaction tr, Database db, string text, double x, double y, double height)
        {
            ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);
            DBText txt = new DBText
            {
                Position = new Point3d(x, y, 0),
                Height = height,
                TextString = text,
                TextStyleId = textStyle,
                LayerId = db.LayerZero
            };
            btr.AppendEntity(txt);
            tr.AddNewlyCreatedDBObject(txt, true);
        }
    }
}
