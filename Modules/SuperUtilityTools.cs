using System;
using System.Collections.Generic;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.Modules
{
    public static class SuperUtilityTools
    {
        [CommandMethod("DTT_TLEN")]
        public static void CmdTotalLength()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            TypedValue[] filter = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "LINE"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Start, "POLYLINE"),
                new TypedValue((int)DxfCode.Start, "ARC"),
                new TypedValue((int)DxfCode.Start, "CIRCLE"),
                new TypedValue((int)DxfCode.Start, "SPLINE"),
                new TypedValue((int)DxfCode.Start, "ELLIPSE"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            };

            PromptSelectionOptions pso = new PromptSelectionOptions
            {
                MessageForAdding = "\n[DTT TLEN] Quét chọn các đối tượng cần đo tổng chiều dài: "
            };

            PromptSelectionResult psr = ed.GetSelection(pso, new SelectionFilter(filter));
            if (psr.Status != PromptStatus.OK || psr.Value == null || psr.Value.Count == 0) return;

            double totalMm = 0.0;
            Dictionary<string, int> counts = new Dictionary<string, int>();

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in psr.Value)
                {
                    if (so == null) continue;
                    Curve c = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Curve;
                    if (c != null)
                    {
                        try
                        {
                            double l = c.GetDistanceAtParameter(c.EndParam) - c.GetDistanceAtParameter(c.StartParam);
                            totalMm += Math.Abs(l);

                            string tn = c.GetType().Name;
                            if (counts.ContainsKey(tn)) counts[tn]++;
                            else counts[tn] = 1;
                        }
                        catch { }
                    }
                }
                tr.Commit();
            }

            double totalM = totalMm / 1000.0;
            string details = string.Join(", ", counts);
            ed.WriteMessage("\n=======================================================");
            ed.WriteMessage($"\n[DTT TLEN] TỔNG CHIỀU DÀI: {totalM:N3} m ({totalMm:N1} mm)");
            ed.WriteMessage($"\n[DTT TLEN] Số lượng: {psr.Value.Count} đối tượng ({details})");
            ed.WriteMessage("\n=======================================================\n");

            // Hỏi người dùng có muốn đặt chữ kết quả lên bản vẽ không
            PromptKeywordOptions pko = new PromptKeywordOptions("\nBạn có muốn đặt chữ ghi kết quả lên bản vẽ? [Co/Khong]: ")
            {
                AllowNone = true
            };
            pko.Keywords.Add("Co");
            pko.Keywords.Add("Khong");
            pko.Keywords.Default = "Khong";
            PromptResult prKey = ed.GetKeywords(pko);

            if (prKey.Status == PromptStatus.OK && prKey.StringResult == "Co")
            {
                PromptPointOptions ppo = new PromptPointOptions("\nChọn điểm đặt chữ kết quả: ");
                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status == PromptStatus.OK)
                {
                    using (doc.LockDocument())
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        ObjectId utilLayer = CadUtils.GetOrCreateLayer(db, "UTIL-TEXT", 2, LineWeight.LineWeight025);
                        ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        DBText txt = new DBText
                        {
                            Position = ppr.Value,
                            TextString = $"L = {totalM:N2}m ({totalMm:N0}mm)",
                            Height = 250.0,
                            LayerId = utilLayer,
                            TextStyleId = textStyle
                        };
                        btr.AppendEntity(txt);
                        tr.AddNewlyCreatedDBObject(txt, true);

                        tr.Commit();
                        ed.Regen();
                    }
                }
            }
        }

        [CommandMethod("DTT_TAREA")]
        public static void CmdTotalArea()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            TypedValue[] filter = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Start, "POLYLINE"),
                new TypedValue((int)DxfCode.Start, "HATCH"),
                new TypedValue((int)DxfCode.Start, "REGION"),
                new TypedValue((int)DxfCode.Start, "CIRCLE"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            };

            PromptSelectionOptions pso = new PromptSelectionOptions
            {
                MessageForAdding = "\n[DTT TAREA] Quét chọn các Polyline khép kín, Hatch, Circle để tính diện tích: "
            };

            PromptSelectionResult psr = ed.GetSelection(pso, new SelectionFilter(filter));
            if (psr.Status != PromptStatus.OK || psr.Value == null || psr.Value.Count == 0) return;

            double totalMm2 = 0.0;
            int count = 0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in psr.Value)
                {
                    if (so == null) continue;
                    Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent is Polyline pl && pl.Closed)
                    {
                        totalMm2 += pl.Area;
                        count++;
                    }
                    else if (ent is Circle c)
                    {
                        totalMm2 += c.Area;
                        count++;
                    }
                    else if (ent is Hatch h)
                    {
                        totalMm2 += h.Area;
                        count++;
                    }
                    else if (ent is Region r)
                    {
                        totalMm2 += r.Area;
                        count++;
                    }
                }
                tr.Commit();
            }

            double totalM2 = totalMm2 / 1000000.0;
            ed.WriteMessage("\n=======================================================");
            ed.WriteMessage($"\n[DTT TAREA] TỔNG DIỆN TÍCH ({count} đối tượng): {totalM2:N2} m² ({totalMm2:N0} mm²)");
            ed.WriteMessage("\n=======================================================\n");
        }

        [CommandMethod("DTT_TCOUNT")]
        public static void CmdAutoIncrement()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptStringOptions pso = new PromptStringOptions("\n[DTT TCOUNT] Nhập tiền tố (Prefix): ") { DefaultValue = "T-" };
            PromptResult prPfx = ed.GetString(pso);
            string prefix = prPfx.Status == PromptStatus.OK ? prPfx.StringResult : "T-";

            PromptIntegerOptions pio = new PromptIntegerOptions("\nNhập số bắt đầu: ") { DefaultValue = 1 };
            PromptIntegerResult pir = ed.GetInteger(pio);
            int currentNum = pir.Status == PromptStatus.OK ? pir.Value : 1;

            int placed = 0;

            using (doc.LockDocument())
            {
                ObjectId utilLayer = CadUtils.GetOrCreateLayer(doc.Database, "UTIL-TEXT", 2, LineWeight.LineWeight025);
                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(doc.Database);

                while (true)
                {
                    string textStr = $"{prefix}{currentNum:00}";
                    PromptPointOptions ppo = new PromptPointOptions($"\nClick điểm đặt [{textStr}] (Enter/Esc để dừng): ");
                    PromptPointResult ppr = ed.GetPoint(ppo);
                    if (ppr.Status != PromptStatus.OK) break;

                    using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        DBText txt = new DBText
                        {
                            Position = ppr.Value,
                            TextString = textStr,
                            Height = 250.0,
                            LayerId = utilLayer,
                            TextStyleId = textStyle
                        };
                        btr.AppendEntity(txt);
                        tr.AddNewlyCreatedDBObject(txt, true);

                        tr.Commit();
                    }

                    ed.Regen();
                    placed++;
                    currentNum++;
                }
            }

            ed.WriteMessage($"\n[DTT TCOUNT] Đã đánh xong {placed} nhãn số tự tăng!\n");
        }

        [CommandMethod("DTT_TOGGLEBG")]
        public static void CmdToggleBackground()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            try
            {
                object acadApp = Application.AcadApplication;
                if (acadApp == null) return;

                object preferences = acadApp.GetType().InvokeMember("Preferences", BindingFlags.GetProperty, null, acadApp, null);
                object displayPrefs = preferences.GetType().InvokeMember("Display", BindingFlags.GetProperty, null, preferences, null);
                object colorVal = displayPrefs.GetType().InvokeMember("GraphicsWinModelBackgrndColor", BindingFlags.GetProperty, null, displayPrefs, null);

                uint currentColor = Convert.ToUInt32(colorVal);
                byte r = (byte)(currentColor & 0xFF);
                byte g = (byte)((currentColor >> 8) & 0xFF);
                byte b = (byte)((currentColor >> 16) & 0xFF);
                double brightness = (r * 0.299 + g * 0.587 + b * 0.114);

                string modeName;
                uint newColor;
                if (brightness < 128)
                {
                    newColor = 0x00FFFFFF; // Trắng sáng
                    modeName = "TRẮNG SÁNG (White Paper Mode)";
                }
                else
                {
                    newColor = (uint)((48 << 16) | (40 << 8) | 33); // Đen CAD
                    modeName = "ĐEN ĐỒ HỌA (Dark CAD Mode)";
                }

                displayPrefs.GetType().InvokeMember("GraphicsWinModelBackgrndColor", BindingFlags.SetProperty, null, displayPrefs, new object[] { newColor });

                if (doc != null)
                {
                    doc.Editor.Regen();
                    doc.Editor.WriteMessage($"\n[DTT TIỆN ÍCH] Đã chuyển màu nền Model sang: {modeName}\n");
                }
            }
            catch (System.Exception ex)
            {
                doc?.Editor.WriteMessage("\n[DTT LỖI] Không thể đổi màu nền: " + ex.Message + "\n");
            }
        }
    }
}
