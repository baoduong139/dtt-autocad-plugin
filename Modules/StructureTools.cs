using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.Modules
{
    public static class StructureTools
    {
        [CommandMethod("DTT_STRUCT_MENU")]
        public static void CmdShowStructMenu()
        {
            var pr = UI.DTools_FloatingDialog.Prompt(
                "KẾT CẤU BÊ TÔNG CỐT THÉP",
                "Chọn loại kết cấu, thông số thép & xem trước mô hình",
                "🏗️",
                UI.PreviewKind.BeamSection,
                new[] {
                    new UI.ParameterField("Type", "Loại cấu kiện:", "Mặt cắt dầm", null,
                        new[] { "Mặt cắt dầm", "Thép chủ móc 135°", "Thép chủ móc 90°", "Thép đai móc 135°" }),
                    new UI.ParameterField("Width", "Bề rộng B:", "300", "mm"),
                    new UI.ParameterField("Height", "Chiều cao H:", "500", "mm"),
                    new UI.ParameterField("Dia", "Đường kính thép Phi:", "18", "mm",
                        new[] { "10", "12", "14", "16", "18", "20", "22", "25" })
                });
            if (pr == null) return;
            string type = pr.GetString("Type", "Mặt cắt dầm");
            if (type.Contains("dầm")) CmdBeamSection();
            else if (type.Contains("135")) CmdRebar135();
            else if (type.Contains("90")) CmdRebar90();
            else CmdStirrup();
        }

        [CommandMethod("DTT_REBAR135")]
        public static void CmdRebar135()
        {
            DrawRebarWithHook(135.0);
        }

        [CommandMethod("DTT_REBAR90")]
        public static void CmdRebar90()
        {
            DrawRebarWithHook(90.0);
        }

        private static void DrawRebarWithHook(double hookAngleDeg)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var pr = UI.DTools_FloatingDialog.Prompt(
                $"VẼ THÉP CHỦ UỐN MÓC {hookAngleDeg:0}° KHÁNG CHẤN",
                "Nhập đường kính thép & xem trước hình dáng uốn móc",
                "🪝",
                UI.PreviewKind.Rebar,
                new[] {
                    new UI.ParameterField("Dia", "Đường kính thép Phi (D):", "20", "mm",
                        new[] { "10", "12", "14", "16", "18", "20", "22", "25", "28", "32" }),
                    new UI.ParameterField("Angle", "Góc uốn móc:", $"{hookAngleDeg:0}", "độ")
                });
            if (pr == null) return;

            CadUtils.FocusDwgView();

            double dia = pr.GetDouble("Dia", 20.0);

            PromptPointOptions ppo1 = new PromptPointOptions($"\nChọn điểm BẮT ĐẦU thân thép Phi {dia:0}: ");
            PromptPointResult ppr1 = ed.GetPoint(ppo1);
            if (ppr1.Status != PromptStatus.OK) return;

            PromptPointOptions ppo2 = new PromptPointOptions("\nChọn điểm KẾT THÚC thân thép: ")
            {
                UseBasePoint = true,
                BasePoint = ppr1.Value
            };
            PromptPointResult ppr2 = ed.GetPoint(ppo2);
            if (ppr2.Status != PromptStatus.OK) return;

            Point3d p1 = ppr1.Value;
            Point3d p2 = ppr2.Value;

            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double mainLen = Math.Sqrt(dx * dx + dy * dy);
            if (mainLen <= dia * 2)
            {
                ed.WriteMessage("\n[DTT LỖI] Chiều dài thanh thép quá ngắn!");
                return;
            }

            double hookLen = Math.Max(100.0, dia * 10.0);
            double theta = Math.Atan2(dy, dx);
            double hookRad = (180.0 - hookAngleDeg) * (Math.PI / 180.0);

            Point2d ptHookStart = new Point2d(p1.X + hookLen * Math.Cos(theta + Math.PI - hookRad),
                                              p1.Y + hookLen * Math.Sin(theta + Math.PI - hookRad));
            Point2d ptHookEnd = new Point2d(p2.X + hookLen * Math.Cos(theta - Math.PI + hookRad),
                                            p2.Y + hookLen * Math.Sin(theta - Math.PI + hookRad));

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId layerId = CadUtils.GetOrCreateLayer(db, "S-REBAR", 1, LineWeight.LineWeight050); // Màu đỏ
                ObjectId styleId = CadUtils.GetOrCreateUnicodeStyle(db);

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Polyline pl = new Polyline();
                pl.AddVertexAt(0, ptHookStart, 0, 0, 0);
                pl.AddVertexAt(1, new Point2d(p1.X, p1.Y), 0, 0, 0);
                pl.AddVertexAt(2, new Point2d(p2.X, p2.Y), 0, 0, 0);
                pl.AddVertexAt(3, ptHookEnd, 0, 0, 0);
                pl.LayerId = layerId;

                btr.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);

                Point3d midPt = new Point3d((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0, 0);
                double totalLen = mainLen + (hookLen * 2.0);

                DBText note = new DBText
                {
                    Position = new Point3d(midPt.X, midPt.Y + 120, 0),
                    TextString = $"1%%c{dia:0} L={totalLen:0} (Móc {hookAngleDeg:0}°)",
                    Height = 150.0,
                    LayerId = layerId,
                    TextStyleId = styleId
                };
                btr.AppendEntity(note);
                tr.AddNewlyCreatedDBObject(note, true);

                tr.Commit();
                ed.Regen();
            }

            ed.WriteMessage($"\n[DTT KẾT CẤU] Đã vẽ thép Phi {dia:0} uốn móc {hookAngleDeg:0}° (Tổng L={mainLen + hookLen * 2:N0}mm) thành công!\n");
        }

        [CommandMethod("DTT_STIRRUP")]
        public static void CmdStirrup()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptDoubleOptions pdoW = new PromptDoubleOptions("\n[DTT KẾT CẤU] Chiều rộng đai B (mm): ") { DefaultValue = 200.0 };
            PromptDoubleResult pdrW = ed.GetDouble(pdoW);
            if (pdrW.Status != PromptStatus.OK) return;

            PromptDoubleOptions pdoH = new PromptDoubleOptions("\n[DTT KẾT CẤU] Chiều cao đai H (mm): ") { DefaultValue = 350.0 };
            PromptDoubleResult pdrH = ed.GetDouble(pdoH);
            if (pdrH.Status != PromptStatus.OK) return;

            PromptPointOptions ppo = new PromptPointOptions("\nChọn góc dưới bên trái để đặt đai: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            double w = pdrW.Value;
            double h = pdrH.Value;
            Point3d pt = ppr.Value;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId layerId = CadUtils.GetOrCreateLayer(db, "S-REBAR-STIRRUP", 2, LineWeight.LineWeight035); // Vàng
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Polyline pl = new Polyline();
                pl.AddVertexAt(0, new Point2d(pt.X, pt.Y), 0, 0, 0);
                pl.AddVertexAt(1, new Point2d(pt.X + w, pt.Y), 0, 0, 0);
                pl.AddVertexAt(2, new Point2d(pt.X + w, pt.Y + h), 0, 0, 0);
                pl.AddVertexAt(3, new Point2d(pt.X, pt.Y + h), 0, 0, 0);
                // Móc 135 độ tại góc trên bên trái
                pl.AddVertexAt(4, new Point2d(pt.X + 80, pt.Y + h - 80), 0, 0, 0);
                pl.LayerId = layerId;

                btr.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);

                tr.Commit();
                ed.Regen();
            }

            ed.WriteMessage($"\n[DTT KẾT CẤU] Đã vẽ thép đai {w}x{h}mm uốn móc kháng chấn 135°!\n");
        }

        [CommandMethod("DTT_BEAMSEC")]
        public static void CmdBeamSection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var pr = UI.DTools_FloatingDialog.Prompt(
                "MẶT CẮT DẦM BÊ TÔNG CỐT THÉP",
                "Nhập kích thước tiết diện BxH và lớp bê tông bảo vệ",
                "🏛️",
                UI.PreviewKind.BeamSection,
                new[] {
                    new UI.ParameterField("Width", "Bề rộng tiết diện dầm B:", "300", "mm", new[] { "200", "220", "250", "300", "350", "400" }),
                    new UI.ParameterField("Height", "Chiều cao tiết diện dầm H:", "500", "mm", new[] { "350", "400", "450", "500", "600", "700" }),
                    new UI.ParameterField("Cover", "Lớp bê tông bảo vệ c:", "25", "mm"),
                    new UI.ParameterField("Rebar", "Ký hiệu cốt thép:", "4 Phi 18")
                });
            if (pr == null) return;

            CadUtils.FocusDwgView();

            PromptPointOptions ppo = new PromptPointOptions("\nChọn điểm đặt mặt cắt: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            double b = pr.GetDouble("Width", 300.0);
            double h = pr.GetDouble("Height", 500.0);
            double cover = pr.GetDouble("Cover", 25.0);
            Point3d pt = ppr.Value;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId concLayer = CadUtils.GetOrCreateLayer(db, "S-CONC", 7, LineWeight.LineWeight035);
                ObjectId rebarLayer = CadUtils.GetOrCreateLayer(db, "S-REBAR", 1, LineWeight.LineWeight050);
                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                BlockTable bTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 1. Viền bê tông
                Polyline concBox = new Polyline();
                concBox.AddVertexAt(0, new Point2d(pt.X, pt.Y), 0, 0, 0);
                concBox.AddVertexAt(1, new Point2d(pt.X + b, pt.Y), 0, 0, 0);
                concBox.AddVertexAt(2, new Point2d(pt.X + b, pt.Y + h), 0, 0, 0);
                concBox.AddVertexAt(3, new Point2d(pt.X, pt.Y + h), 0, 0, 0);
                concBox.Closed = true;
                concBox.LayerId = concLayer;
                btr.AppendEntity(concBox);
                tr.AddNewlyCreatedDBObject(concBox, true);

                // 2. Thép đai
                Polyline stirrup = new Polyline();
                stirrup.AddVertexAt(0, new Point2d(pt.X + cover, pt.Y + cover), 0, 0, 0);
                stirrup.AddVertexAt(1, new Point2d(pt.X + b - cover, pt.Y + cover), 0, 0, 0);
                stirrup.AddVertexAt(2, new Point2d(pt.X + b - cover, pt.Y + h - cover), 0, 0, 0);
                stirrup.AddVertexAt(3, new Point2d(pt.X + cover, pt.Y + h - cover), 0, 0, 0);
                stirrup.Closed = true;
                stirrup.LayerId = rebarLayer;
                btr.AppendEntity(stirrup);
                tr.AddNewlyCreatedDBObject(stirrup, true);

                // 3. Bốn cây thép chủ góc Phi 20
                double barR = 10.0;
                Point2d[] corners = new Point2d[]
                {
                    new Point2d(pt.X + cover + barR, pt.Y + cover + barR),
                    new Point2d(pt.X + b - cover - barR, pt.Y + cover + barR),
                    new Point2d(pt.X + b - cover - barR, pt.Y + h - cover - barR),
                    new Point2d(pt.X + cover + barR, pt.Y + h - cover - barR)
                };

                foreach (var cp in corners)
                {
                    Circle bar = new Circle(new Point3d(cp.X, cp.Y, 0), Vector3d.ZAxis, barR)
                    {
                        LayerId = rebarLayer
                    };
                    btr.AppendEntity(bar);
                    tr.AddNewlyCreatedDBObject(bar, true);
                }

                // 4. Nhãn kích thước mặt cắt
                DBText label = new DBText
                {
                    Position = new Point3d(pt.X + b / 2.0 - 80, pt.Y - 120, 0),
                    TextString = $"{b:0}x{h:0} (4%%c20)",
                    Height = 120.0,
                    LayerId = concLayer,
                    TextStyleId = textStyle
                };
                btr.AppendEntity(label);
                tr.AddNewlyCreatedDBObject(label, true);

                tr.Commit();
                ed.Regen();
            }

            ed.WriteMessage($"\n[DTT KẾT CẤU] Đã vẽ mặt cắt tiết diện {b}x{h}mm kèm cốt thép thành công!\n");
        }

        public class RebarScanItem
        {
            public string STT { get; set; }
            public string Member { get; set; }
            public string Shape { get; set; }
            public double Dia { get; set; }
            public double Length { get; set; }
            public int Qty { get; set; }
        }

        [CommandMethod("DTT_REBARTABLE")]
        public static void CmdRebarTable()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            // 1. Cho phép người dùng chọn thép trên màn hình hoặc nhấn ENTER để tự quét toàn bộ
            PromptSelectionOptions pso = new PromptSelectionOptions
            {
                MessageForAdding = "\n[DTT KẾT CẤU] Quét chọn các thanh thép / mặt cắt dầm cột (hoặc nhấn ENTER để tự động quét toàn bộ bản vẽ): "
            };
            PromptSelectionResult psr = ed.GetSelection(pso);

            List<RebarScanItem> scanList = new List<RebarScanItem>();

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectIdCollection targetIds = new ObjectIdCollection();
                if (psr.Status == PromptStatus.OK && psr.Value != null)
                {
                    foreach (SelectedObject so in psr.Value)
                    {
                        if (so != null && !so.ObjectId.IsNull) targetIds.Add(so.ObjectId);
                    }
                }
                else
                {
                    // Quét toàn bộ ModelSpace
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                    foreach (ObjectId id in btr) targetIds.Add(id);
                }

                bool isExplicitSelection = (psr.Status == PromptStatus.OK && psr.Value != null);

                // Từ điển gom nhóm các thanh thép theo (Member, Shape, Dia, Length)
                Dictionary<string, RebarScanItem> groupedRebar = new Dictionary<string, RebarScanItem>();

                foreach (ObjectId id in targetIds)
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    string layer = ent.Layer.ToUpper();
                    bool isRebarLayer = (layer.Contains("REBAR") || layer.Contains("THEP") || layer.Contains("STEEL") || layer.Contains("S-") || layer.Contains("KETCUA"));

                    // Trích xuất đường kính từ tên Layer (nếu có ví dụ: S-REBAR-D20 hoặc THEP_PHI16)
                    double layerDia = 0.0;
                    var matchDia = System.Text.RegularExpressions.Regex.Match(layer, @"(?:%%[cC]|Φ|PHI|[Dd])\s*(\d+)");
                    if (matchDia.Success && double.TryParse(matchDia.Groups[1].Value, out double ld) && ld >= 6.0 && ld <= 50.0)
                    {
                        layerDia = ld;
                    }

                    // A. Quét Polyline thép (Chấp nhận tất cả nếu người dùng quét chọn thủ công)
                    if (ent is Polyline pl && (isExplicitSelection || isRebarLayer))
                    {
                        double len = Math.Round(pl.Length, 0);
                        if (len < 50.0) continue; // Bỏ qua đoạn rác quá ngắn

                        string shape = "Thanh Thẳng";
                        double dia = layerDia > 0 ? layerDia : 18.0;
                        string member = "Dầm/Sàn";

                        if (pl.ConstantWidth > 0)
                        {
                            dia = Math.Max(6.0, Math.Round(pl.ConstantWidth));
                        }

                        if (pl.Closed || layer.Contains("STIRRUP") || layer.Contains("DAI"))
                        {
                            shape = "Thép Đai Kín (135°)";
                            if (layerDia <= 0 && pl.ConstantWidth <= 0) dia = 8.0;
                            member = "Đai Dầm/Cột";
                        }
                        else if (pl.NumberOfVertices >= 4)
                        {
                            shape = "Thép Chủ Móc 135°";
                            if (layerDia <= 0 && pl.ConstantWidth <= 0) dia = 20.0;
                            member = "Dầm D1";
                        }
                        else if (pl.NumberOfVertices == 3)
                        {
                            shape = "Thép Chủ Uốn 90°";
                            if (layerDia <= 0 && pl.ConstantWidth <= 0) dia = 18.0;
                            member = "Cột C1";
                        }

                        string key = $"{member}_{shape}_{dia}_{len}";
                        if (groupedRebar.ContainsKey(key))
                        {
                            groupedRebar[key].Qty++;
                        }
                        else
                        {
                            groupedRebar[key] = new RebarScanItem
                            {
                                Member = member,
                                Shape = shape,
                                Dia = dia,
                                Length = len,
                                Qty = 1
                            };
                        }
                    }
                    // B. Quét Line (Thép đoạn thẳng vẽ bằng Line)
                    else if (ent is Line line && (isExplicitSelection || isRebarLayer))
                    {
                        double len = Math.Round(line.Length, 0);
                        if (len < 50.0) continue;

                        double dia = layerDia > 0 ? layerDia : 16.0;
                        string shape = "Thép Chủ Thẳng";
                        string member = "Thép Dầm/Sàn";

                        string key = $"{member}_{shape}_{dia}_{len}";
                        if (groupedRebar.ContainsKey(key))
                        {
                            groupedRebar[key].Qty++;
                        }
                        else
                        {
                            groupedRebar[key] = new RebarScanItem
                            {
                                Member = member,
                                Shape = shape,
                                Dia = dia,
                                Length = len,
                                Qty = 1
                            };
                        }
                    }
                    // C. Quét Nhãn Chữ Text / MText ghi thông số thép (Ví dụ: 4%%c20 L=5800 hoặc 3 Phi 18)
                    else if ((ent is DBText || ent is MText) && (isExplicitSelection || isRebarLayer))
                    {
                        string txt = (ent is DBText dbt) ? dbt.TextString : ((MText)ent).Contents;
                        if (string.IsNullOrWhiteSpace(txt)) continue;

                        var match = System.Text.RegularExpressions.Regex.Match(
                            txt,
                            @"(?:(\d+)\s*)?(?:%%[cC]|Φ|Phi|[Dd])\s*(\d+)(?:.*?L\s*=\s*(\d+))?",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                        if (match.Success)
                        {
                            int qty = 1;
                            if (!string.IsNullOrEmpty(match.Groups[1].Value))
                                int.TryParse(match.Groups[1].Value, out qty);

                            double dia = 18.0;
                            if (!string.IsNullOrEmpty(match.Groups[2].Value))
                                double.TryParse(match.Groups[2].Value, out dia);

                            double len = 4500.0;
                            if (!string.IsNullOrEmpty(match.Groups[3].Value))
                                double.TryParse(match.Groups[3].Value, out len);

                            string shape = txt.Contains("135") ? "Móc 135°" : txt.Contains("90") ? "Uốn 90°" : "Thanh Thẳng";
                            string member = txt.Contains("Đai") ? "Đai Dầm" : "Thép Dầm/Cột";

                            string key = $"TXT_{member}_{shape}_{dia}_{len}";
                            if (groupedRebar.ContainsKey(key))
                            {
                                groupedRebar[key].Qty += qty;
                            }
                            else
                            {
                                groupedRebar[key] = new RebarScanItem
                                {
                                    Member = member,
                                    Shape = shape,
                                    Dia = dia,
                                    Length = len,
                                    Qty = qty
                                };
                            }
                        }
                    }
                }

                // Chuyển kết quả sang danh sách có thứ tự STT
                int sttIdx = 1;
                foreach (var item in groupedRebar.Values)
                {
                    item.STT = sttIdx.ToString();
                    scanList.Add(item);
                    sttIdx++;
                }

                tr.Commit();
            }

            // Nếu không quét được thanh thép nào, báo cho người dùng và KHÔNG sinh số mặc định
            if (scanList.Count == 0)
            {
                ed.WriteMessage("\n⚠️ [DTT KẾT CẤU] Không tìm thấy thanh thép hoặc ký hiệu thép nào trong đối tượng đã chọn hoặc bản vẽ CAD.\n💡 Vui lòng vẽ thanh thép bằng lệnh DTT_REBAR135, DTT_REBAR90 hoặc quét chọn các thanh thép trên bản vẽ.\n");
                return;
            }

            PromptPointOptions ppo = new PromptPointOptions("\n[DTT KẾT CẤU] Chọn điểm đặt BẢNG THỐNG KÊ THÉP TỰ ĐỘNG: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d insertPt = ppr.Value;

            int numRows = scanList.Count + 3;
            int numCols = 8;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId tableLayer = CadUtils.GetOrCreateLayer(db, "S-REBAR-TABLE", 7, LineWeight.LineWeight025);
                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Table tbl = new Table();
                tbl.SetSize(numRows, numCols);
                tbl.Position = insertPt;
                tbl.LayerId = tableLayer;

                tbl.Columns[0].Width = 500;  // STT
                tbl.Columns[1].Width = 1400; // Cấu kiện
                tbl.Columns[2].Width = 1200; // Hình dáng
                tbl.Columns[3].Width = 800;  // ĐK
                tbl.Columns[4].Width = 1000; // Chiều dài 1 thanh
                tbl.Columns[5].Width = 700;  // Số lượng
                tbl.Columns[6].Width = 1100; // Tổng L (m)
                tbl.Columns[7].Width = 1300; // Tổng KL (kg)

                for (int r = 0; r < numRows; r++) tbl.Rows[r].Height = 320;
                tbl.Rows[0].Height = 450;

                tbl.Cells[0, 0].TextString = "BẢNG THỐNG KÊ CỐT THÉP (TCVN 5574:2018 - QUÉT TỰ ĐỘNG THỰC TẾ)";
                tbl.Cells[0, 0].TextHeight = 220;
                tbl.Cells[0, 0].Alignment = CellAlignment.MiddleCenter;

                string[] headers = { "STT", "Cấu kiện", "Hình dáng", "ĐK (Φ)", "Dài (mm)", "SL", "Tổng L (m)", "Tổng KL (kg)" };
                for (int c = 0; c < numCols; c++)
                {
                    Cell cell = tbl.Cells[1, c];
                    cell.TextString = headers[c];
                    cell.TextHeight = 150;
                    cell.Alignment = CellAlignment.MiddleCenter;
                    cell.BackgroundColor = Color.FromRgb(2, 132, 199); // Màu xanh dương chuyên nghiệp sáng
                    cell.ContentColor = Color.FromRgb(255, 255, 255);
                }

                double grandTotalKg = 0.0;
                for (int i = 0; i < scanList.Count; i++)
                {
                    int r = i + 2;
                    var it = scanList[i];
                    double totM = (it.Length * it.Qty) / 1000.0;
                    // Công thức định mức khối lượng theo TCVN 5574: P = d^2 / 162 (kg/m)
                    double unitWeight = (it.Dia * it.Dia) / 162.0;
                    double totalKg = totM * unitWeight;
                    grandTotalKg += totalKg;

                    tbl.Cells[r, 0].TextString = it.STT;
                    tbl.Cells[r, 1].TextString = it.Member;
                    tbl.Cells[r, 2].TextString = it.Shape;
                    tbl.Cells[r, 3].TextString = $"Φ{it.Dia:0}";
                    tbl.Cells[r, 4].TextString = it.Length.ToString("N0");
                    tbl.Cells[r, 5].TextString = it.Qty.ToString();
                    tbl.Cells[r, 6].TextString = totM.ToString("N2");
                    tbl.Cells[r, 7].TextString = totalKg.ToString("N2");

                    for (int c = 0; c < numCols; c++)
                    {
                        tbl.Cells[r, c].TextHeight = 140;
                        tbl.Cells[r, c].Alignment = (c == 1 || c == 2) ? CellAlignment.MiddleLeft : CellAlignment.MiddleCenter;
                    }
                }

                int lastRow = numRows - 1;
                tbl.Cells[lastRow, 0].TextString = "TỔNG KHỐI LƯỢNG";
                tbl.Cells[lastRow, 0].TextHeight = 160;
                tbl.Cells[lastRow, 0].Alignment = CellAlignment.MiddleCenter;
                tbl.Cells[lastRow, 7].TextString = $"{grandTotalKg:N2} kg";
                tbl.Cells[lastRow, 7].TextHeight = 170;
                tbl.Cells[lastRow, 7].Alignment = CellAlignment.MiddleCenter;
                tbl.Cells[lastRow, 7].ContentColor = Color.FromRgb(255, 215, 0);

                btr.AppendEntity(tbl);
                tr.AddNewlyCreatedDBObject(tbl, true);

                tr.Commit();
                ed.Regen();
            }

            ed.WriteMessage($"\n[DTT KẾT CẤU] Đã quét {scanList.Count} chủng loại thép và xuất Bảng Thống Kê Thép tự động thành công!\n");
        }
    }
}
