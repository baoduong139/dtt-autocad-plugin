using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.Modules
{
    public static class MepTools
    {
        [CommandMethod("DTT_MEP_MENU")]
        public static void CmdShowMepMenu()
        {
            var pr = UI.DTools_FloatingDialog.Prompt(
                "HỆ THỐNG ĐƯỜNG ỐNG MEP & PCCC",
                "Chọn loại đường ống thoát nước, đầu phun sprinkler, ống gió",
                "💧",
                UI.PreviewKind.DrainPipe,
                new[] {
                    new UI.ParameterField("Type", "Loại hệ thống:", "Ống thoát nước dốc", null,
                        new[] { "Ống thoát nước dốc", "Đầu phun PCCC Sprinkler", "Ống gió điều hòa HVAC" }),
                    new UI.ParameterField("Dia", "Đường kính ống D:", "110", "mm",
                        new[] { "60", "90", "110", "140", "160", "200" }),
                    new UI.ParameterField("Slope", "Độ dốc thoát nước i (%):", "2.0", "%",
                        new[] { "1.0", "1.5", "2.0", "3.0" })
                });
            if (pr == null) return;
            string type = pr.GetString("Type", "Ống thoát nước dốc");
            if (type.Contains("thoát")) CmdDrainPipe();
            else if (type.Contains("PCCC")) CmdSprinkler();
            else CmdDuct();
        }

        [CommandMethod("DTT_DRAINPIPE")]
        public static void CmdDrainPipe()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var pr = UI.DTools_FloatingDialog.Prompt(
                "VẼ ĐƯỜNG ỐNG THOÁT NƯỚC DỐC",
                "Tự động tính độ chênh cao delta Z và gắn nhãn độ dốc",
                "💧",
                UI.PreviewKind.DrainPipe,
                new[] {
                    new UI.ParameterField("Dia", "Đường kính ống D:", "110", "mm",
                        new[] { "60", "90", "110", "140", "160", "200" }),
                    new UI.ParameterField("Slope", "Độ dốc thoát nước i (%):", "2.0", "%",
                        new[] { "1.0", "1.5", "2.0", "2.5", "3.0" })
                });
            if (pr == null) return;

            CadUtils.FocusDwgView();

            double slope = pr.GetDouble("Slope", 2.0);
            double dia = pr.GetDouble("Dia", 110.0);

            PromptPointOptions ppo1 = new PromptPointOptions("\nChọn điểm ĐẦU ống thoát (Cao độ nguồn): ");
            PromptPointResult ppr1 = ed.GetPoint(ppo1);
            if (ppr1.Status != PromptStatus.OK) return;

            PromptPointOptions ppo2 = new PromptPointOptions("\nChọn điểm CUỐI ống thoát (Hướng thoát): ")
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
            double len2D = Math.Sqrt(dx * dx + dy * dy);
            if (len2D <= 10.0) return;

            double deltaZ = (len2D * slope) / 100.0;
            double zStart = p1.Z;
            double zEnd = zStart - deltaZ;

            double nx = -dy / len2D;
            double ny = dx / len2D;
            double half = dia / 2.0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId pipeLayer = CadUtils.GetOrCreateLayer(db, "P-PIPE-DRAIN", 6, LineWeight.LineWeight035); // Magenta
                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Line centerLine = new Line(new Point3d(p1.X, p1.Y, zStart), new Point3d(p2.X, p2.Y, zEnd)) { LayerId = pipeLayer };
                btr.AppendEntity(centerLine);
                tr.AddNewlyCreatedDBObject(centerLine, true);

                Line wallL = new Line(new Point3d(p1.X + nx * half, p1.Y + ny * half, zStart),
                                      new Point3d(p2.X + nx * half, p2.Y + ny * half, zEnd)) { LayerId = pipeLayer };
                btr.AppendEntity(wallL);
                tr.AddNewlyCreatedDBObject(wallL, true);

                Line wallR = new Line(new Point3d(p1.X - nx * half, p1.Y - ny * half, zStart),
                                      new Point3d(p2.X - nx * half, p2.Y - ny * half, zEnd)) { LayerId = pipeLayer };
                btr.AppendEntity(wallR);
                tr.AddNewlyCreatedDBObject(wallR, true);

                Point3d midPt = new Point3d((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0, (zStart + zEnd) / 2.0);
                double textAngle = Math.Atan2(dy, dx);
                if (textAngle > Math.PI / 2 || textAngle < -Math.PI / 2) textAngle += Math.PI;

                DBText label = new DBText
                {
                    Position = new Point3d(midPt.X + nx * (half + 120), midPt.Y + ny * (half + 120), 0),
                    TextString = $"uPVC D{dia:0} (i={slope:0.#}%, L={len2D/1000.0:0.##}m, dZ=-{deltaZ:0.#}mm)",
                    Height = 140.0,
                    Rotation = textAngle,
                    LayerId = pipeLayer,
                    TextStyleId = textStyle
                };
                btr.AppendEntity(label);
                tr.AddNewlyCreatedDBObject(label, true);

                tr.Commit();
                ed.Regen();
            }

            ed.WriteMessage($"\n[DTT MEP] Đã vẽ ống thoát D{dia:0} dốc {slope}% (Dài {len2D/1000.0:N2}m, deltaZ = -{deltaZ:N1}mm) thành công!\n");
        }

        [CommandMethod("DTT_SPRINKLER")]
        public static void CmdSprinkler()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptPointOptions ppo = new PromptPointOptions("\n[DTT PCCC] Chọn vị trí đặt đầu phun Sprinkler (Vùng phủ R=2100mm): ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d pt = ppr.Value;
            double radiusMm = 2100.0;
            double headR = 100.0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId headLayer = CadUtils.GetOrCreateLayer(db, "FP-SPRINKLER-HEAD", 1, LineWeight.LineWeight030); // Đỏ
                ObjectId covLayer = CadUtils.GetOrCreateLayer(db, "FP-SPRINKLER-COVERAGE", 4, LineWeight.LineWeight015); // Cyan

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Circle headCircle = new Circle(pt, Vector3d.ZAxis, headR) { LayerId = headLayer };
                btr.AppendEntity(headCircle);
                tr.AddNewlyCreatedDBObject(headCircle, true);

                Line crossH = new Line(new Point3d(pt.X - headR * 1.5, pt.Y, 0), new Point3d(pt.X + headR * 1.5, pt.Y, 0)) { LayerId = headLayer };
                btr.AppendEntity(crossH);
                tr.AddNewlyCreatedDBObject(crossH, true);

                Line crossV = new Line(new Point3d(pt.X, pt.Y - headR * 1.5, 0), new Point3d(pt.X, pt.Y + headR * 1.5, 0)) { LayerId = headLayer };
                btr.AppendEntity(crossV);
                tr.AddNewlyCreatedDBObject(crossV, true);

                Circle covCircle = new Circle(pt, Vector3d.ZAxis, radiusMm) { LayerId = covLayer };
                btr.AppendEntity(covCircle);
                tr.AddNewlyCreatedDBObject(covCircle, true);

                tr.Commit();
                ed.Regen();
            }

            ed.WriteMessage($"\n[DTT PCCC] Đã bố trí đầu phun Sprinkler và vòng phủ R={radiusMm:0}mm (Diện tích ~13.8 m²)!\n");
        }

        [CommandMethod("DTT_DUCT")]
        public static void CmdDuct()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            CadUtils.FocusDwgView();

            PromptDoubleOptions pdoW = new PromptDoubleOptions("\n[DTT HVAC] Nhập chiều rộng ống gió W (mm): ") { DefaultValue = 400.0 };
            PromptDoubleResult pdrW = ed.GetDouble(pdoW);
            if (pdrW.Status != PromptStatus.OK) return;

            PromptDoubleOptions pdoH = new PromptDoubleOptions("\n[DTT HVAC] Nhập chiều cao ống gió H (mm): ") { DefaultValue = 250.0 };
            PromptDoubleResult pdrH = ed.GetDouble(pdoH);
            if (pdrH.Status != PromptStatus.OK) return;

            PromptPointOptions ppo1 = new PromptPointOptions("\nChọn điểm BẮT ĐẦU tuyến ống gió: ");
            PromptPointResult ppr1 = ed.GetPoint(ppo1);
            if (ppr1.Status != PromptStatus.OK) return;

            PromptPointOptions ppo2 = new PromptPointOptions("\nChọn điểm KẾT THÚC tuyến ống gió: ")
            {
                UseBasePoint = true,
                BasePoint = ppr1.Value
            };
            PromptPointResult ppr2 = ed.GetPoint(ppo2);
            if (ppr2.Status != PromptStatus.OK) return;

            Point3d p1 = ppr1.Value;
            Point3d p2 = ppr2.Value;
            double w = pdrW.Value;
            double h = pdrH.Value;

            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len <= 10.0) return;

            double nx = -dy / len;
            double ny = dx / len;
            double halfW = w / 2.0;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId ductLayer = CadUtils.GetOrCreateLayer(db, "M-DUCT", 3, LineWeight.LineWeight035); // Xanh lá
                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Line cl = new Line(p1, p2) { LayerId = ductLayer };
                btr.AppendEntity(cl);
                tr.AddNewlyCreatedDBObject(cl, true);

                Line side1 = new Line(new Point3d(p1.X + nx * halfW, p1.Y + ny * halfW, 0),
                                      new Point3d(p2.X + nx * halfW, p2.Y + ny * halfW, 0)) { LayerId = ductLayer };
                btr.AppendEntity(side1);
                tr.AddNewlyCreatedDBObject(side1, true);

                Line side2 = new Line(new Point3d(p1.X - nx * halfW, p1.Y - ny * halfW, 0),
                                      new Point3d(p2.X - nx * halfW, p2.Y - ny * halfW, 0)) { LayerId = ductLayer };
                btr.AppendEntity(side2);
                tr.AddNewlyCreatedDBObject(side2, true);

                Point3d midPt = new Point3d((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0, 0);
                double textAngle = Math.Atan2(dy, dx);
                if (textAngle > Math.PI / 2 || textAngle < -Math.PI / 2) textAngle += Math.PI;

                DBText label = new DBText
                {
                    Position = new Point3d(midPt.X + nx * (halfW + 100), midPt.Y + ny * (halfW + 100), 0),
                    TextString = $"{w:0}x{h:0} (L={len/1000.0:0.##}m)",
                    Height = 150.0,
                    Rotation = textAngle,
                    LayerId = ductLayer,
                    TextStyleId = textStyle
                };
                btr.AppendEntity(label);
                tr.AddNewlyCreatedDBObject(label, true);

                tr.Commit();
                ed.Regen();
            }

            ed.WriteMessage($"\n[DTT HVAC] Đã vẽ ống gió {w:0}x{h:0}mm (Dài {len/1000.0:N2}m) thành công!\n");
        }
    }
}
