using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.Modules
{
    public enum PipeSystemType
    {
        ColdWater,   // Nước cấp sinh hoạt (PPR D20, D25, D32)
        HotWater,    // Nước nóng sinh hoạt (PPR-UV D20, D25)
        DrainageSoil,// Thoát phân bồn cầu (uPVC D114, D90)
        DrainageWaste,// Thoát nước sinh hoạt lavabo, sàn (uPVC D60, D90)
        StormWater   // Thoát nước mưa ban công, mái (uPVC D90, D114)
    }

    public enum HvacDuctType
    {
        SupplyAir,   // Ống gió cấp (SAD)
        ReturnAir,   // Ống gió hồi (RAD)
        ExhaustAir,  // Ống gió thải/hút mùi (EAD)
        FreshAir     // Ống lấy gió tươi (FAD)
    }

    public static class PlumbingAndHvacEngine
    {
        // ============================================================
        // 1. CẤP THOÁT NƯỚC (PLUMBING & SANITARY)
        // ============================================================

        public static void DrawPlumbingPipeline(Database db, Point3d startPt, Point3d endPt, PipeSystemType sysType, int diameterMm, double slopePct = 1.5)
        {
            if (db == null) return;

            string layerName;
            short colorIndex;
            string sysLabel;

            switch (sysType)
            {
                case PipeSystemType.ColdWater:
                    layerName = "P-CW-PIPE";
                    colorIndex = 5; // Xanh dương
                    sysLabel = $"ỐNG CẤP NƯỚC LẠNH PPR D{diameterMm}";
                    break;
                case PipeSystemType.HotWater:
                    layerName = "P-HW-PIPE";
                    colorIndex = 1; // Đỏ
                    sysLabel = $"ỐNG NƯỚC NÓNG PPR D{diameterMm}";
                    break;
                case PipeSystemType.DrainageSoil:
                    layerName = "P-SOIL-PIPE";
                    colorIndex = 6; // Magenta
                    sysLabel = $"ỐNG THOÁT PHÂN uPVC D{diameterMm} (i={slopePct}%)";
                    break;
                case PipeSystemType.DrainageWaste:
                    layerName = "P-WASTE-PIPE";
                    colorIndex = 30; // Cam
                    sysLabel = $"ỐNG THOÁT SÀN uPVC D{diameterMm} (i={slopePct}%)";
                    break;
                default:
                    layerName = "P-STORM-PIPE";
                    colorIndex = 4; // Cyan
                    sysLabel = $"ỐNG THOÁT NƯỚC MƯA uPVC D{diameterMm}";
                    break;
            }

            ObjectId layerId = CadUtils.GetOrCreateLayer(db, layerName, colorIndex, LineWeight.LineWeight035);
            ObjectId styleId = CadUtils.GetOrCreateUnicodeStyle(db);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                // Đường ống chính
                Line pipe = new Line(startPt, endPt) { LayerId = layerId };
                btr.AppendEntity(pipe); tr.AddNewlyCreatedDBObject(pipe, true);

                // Điểm bắt đầu & kết thúc (Fittings)
                Circle startFitting = new Circle(startPt, Vector3d.ZAxis, 12) { LayerId = layerId };
                Circle endFitting = new Circle(endPt, Vector3d.ZAxis, 12) { LayerId = layerId };
                btr.AppendEntity(startFitting); tr.AddNewlyCreatedDBObject(startFitting, true);
                btr.AppendEntity(endFitting); tr.AddNewlyCreatedDBObject(endFitting, true);

                // Nhãn chữ thông số ống nằm dọc theo tuyến ống
                Point3d midPt = new Point3d((startPt.X + endPt.X) / 2, (startPt.Y + endPt.Y) / 2 + 35, 0);
                double angle = Math.Atan2(endPt.Y - startPt.Y, endPt.X - startPt.X);
                if (angle > Math.PI / 2 || angle < -Math.PI / 2) angle += Math.PI;

                DBText label = new DBText
                {
                    Position = midPt,
                    Height = 70,
                    TextString = sysLabel,
                    Rotation = angle,
                    TextStyleId = styleId,
                    LayerId = layerId
                };
                btr.AppendEntity(label); tr.AddNewlyCreatedDBObject(label, true);

                tr.Commit();
            }
        }

        public static void InsertPlumbingAccessory(Database db, Point3d pt, string accessoryType)
        {
            if (db == null) return;
            ObjectId layerId = CadUtils.GetOrCreateLayer(db, "P-EQUIP", 2); // Vàng
            ObjectId styleId = CadUtils.GetOrCreateUnicodeStyle(db);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                if (accessoryType.Contains("Phễu") || accessoryType.Contains("FloorDrain"))
                {
                    // Phễu thu sàn chống hôi (Square with circle inside)
                    Polyline fd = new Polyline();
                    fd.AddVertexAt(0, new Point2d(pt.X - 100, pt.Y - 100), 0, 0, 0);
                    fd.AddVertexAt(1, new Point2d(pt.X + 100, pt.Y - 100), 0, 0, 0);
                    fd.AddVertexAt(2, new Point2d(pt.X + 100, pt.Y + 100), 0, 0, 0);
                    fd.AddVertexAt(3, new Point2d(pt.X - 100, pt.Y + 100), 0, 0, 0);
                    fd.Closed = true;
                    fd.LayerId = layerId;
                    btr.AppendEntity(fd); tr.AddNewlyCreatedDBObject(fd, true);

                    Circle c = new Circle(pt, Vector3d.ZAxis, 70) { LayerId = layerId };
                    btr.AppendEntity(c); tr.AddNewlyCreatedDBObject(c, true);

                    DBText txt = new DBText { Position = new Point3d(pt.X + 120, pt.Y - 30, 0), Height = 65, TextString = "FD (Thu Sàn)", TextStyleId = styleId, LayerId = layerId };
                    btr.AppendEntity(txt); tr.AddNewlyCreatedDBObject(txt, true);
                }
                else if (accessoryType.Contains("Van") || accessoryType.Contains("Valve"))
                {
                    // Van khóa 2 cánh bướm
                    Polyline valve = new Polyline();
                    valve.AddVertexAt(0, new Point2d(pt.X - 80, pt.Y - 50), 0, 0, 0);
                    valve.AddVertexAt(1, new Point2d(pt.X - 80, pt.Y + 50), 0, 0, 0);
                    valve.AddVertexAt(2, new Point2d(pt.X + 80, pt.Y - 50), 0, 0, 0);
                    valve.AddVertexAt(3, new Point2d(pt.X + 80, pt.Y + 50), 0, 0, 0);
                    valve.Closed = true;
                    valve.LayerId = layerId;
                    btr.AppendEntity(valve); tr.AddNewlyCreatedDBObject(valve, true);

                    DBText txt = new DBText { Position = new Point3d(pt.X - 40, pt.Y + 70, 0), Height = 60, TextString = "GV (Van Cửa)", TextStyleId = styleId, LayerId = layerId };
                    btr.AppendEntity(txt); tr.AddNewlyCreatedDBObject(txt, true);
                }
                else
                {
                    // Hố ga hoặc hố ga thu nước (Inspection Chamber)
                    Polyline ic = new Polyline();
                    ic.AddVertexAt(0, new Point2d(pt.X - 250, pt.Y - 250), 0, 0, 0);
                    ic.AddVertexAt(1, new Point2d(pt.X + 250, pt.Y - 250), 0, 0, 0);
                    ic.AddVertexAt(2, new Point2d(pt.X + 250, pt.Y + 250), 0, 0, 0);
                    ic.AddVertexAt(3, new Point2d(pt.X - 250, pt.Y + 250), 0, 0, 0);
                    ic.Closed = true;
                    ic.LayerId = layerId;
                    btr.AppendEntity(ic); tr.AddNewlyCreatedDBObject(ic, true);

                    Line diag1 = new Line(new Point3d(pt.X - 250, pt.Y - 250, 0), new Point3d(pt.X + 250, pt.Y + 250, 0)) { LayerId = layerId };
                    Line diag2 = new Line(new Point3d(pt.X - 250, pt.Y + 250, 0), new Point3d(pt.X + 250, pt.Y - 250, 0)) { LayerId = layerId };
                    btr.AppendEntity(diag1); tr.AddNewlyCreatedDBObject(diag1, true);
                    btr.AppendEntity(diag2); tr.AddNewlyCreatedDBObject(diag2, true);

                    DBText txt = new DBText { Position = new Point3d(pt.X - 100, pt.Y - 350, 0), Height = 75, TextString = "HỐ GA 500x500", TextStyleId = styleId, LayerId = layerId };
                    btr.AppendEntity(txt); tr.AddNewlyCreatedDBObject(txt, true);
                }

                tr.Commit();
            }
        }

        // ============================================================
        // 2. THÔNG GIÓ & ĐIỀU HÒA KHÔNG KHÍ (HVAC DUCTS & DIFFUSERS)
        // ============================================================

        public static void DrawHvacDuct(Database db, Point3d startPt, Point3d endPt, double widthMm, double heightMm, HvacDuctType ductType)
        {
            if (db == null) return;

            string layerName;
            short colorIndex;
            string prefix;

            switch (ductType)
            {
                case HvacDuctType.SupplyAir:
                    layerName = "M-HVAC-SUPPLY";
                    colorIndex = 4; // Cyan
                    prefix = "SAD";
                    break;
                case HvacDuctType.ReturnAir:
                    layerName = "M-HVAC-RETURN";
                    colorIndex = 140; // Xanh lam
                    prefix = "RAD";
                    break;
                case HvacDuctType.ExhaustAir:
                    layerName = "M-HVAC-EXHAUST";
                    colorIndex = 30; // Cam nâu
                    prefix = "EAD";
                    break;
                default:
                    layerName = "M-HVAC-FRESH";
                    colorIndex = 3; // Xanh lá
                    prefix = "FAD";
                    break;
            }

            ObjectId layerId = CadUtils.GetOrCreateLayer(db, layerName, colorIndex, LineWeight.LineWeight035);
            ObjectId layerCenter = CadUtils.GetOrCreateLayer(db, "M-HVAC-CLINE", 8);
            ObjectId linetypeCenter = CadUtils.EnsureLinetype(db, "CENTER");
            ObjectId styleId = CadUtils.GetOrCreateUnicodeStyle(db);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                Vector3d dir = (endPt - startPt).GetNormal();
                Vector3d perp = new Vector3d(-dir.Y, dir.X, 0) * (widthMm / 2.0);

                // Đường biên 1
                Point3d p1Start = startPt + perp;
                Point3d p1End = endPt + perp;
                Line wall1 = new Line(p1Start, p1End) { LayerId = layerId };
                btr.AppendEntity(wall1); tr.AddNewlyCreatedDBObject(wall1, true);

                // Đường biên 2
                Point3d p2Start = startPt - perp;
                Point3d p2End = endPt - perp;
                Line wall2 = new Line(p2Start, p2End) { LayerId = layerId };
                btr.AppendEntity(wall2); tr.AddNewlyCreatedDBObject(wall2, true);

                // Đường tim ống gió nét chấm gạch
                Line centerLine = new Line(startPt, endPt)
                {
                    LayerId = layerCenter,
                    LinetypeId = linetypeCenter,
                    LinetypeScale = 10.0
                };
                btr.AppendEntity(centerLine); tr.AddNewlyCreatedDBObject(centerLine, true);

                // Nhãn ghi chú kích thước ống gió W x H
                Point3d midPt = new Point3d((startPt.X + endPt.X) / 2, (startPt.Y + endPt.Y) / 2, 0);
                double angle = Math.Atan2(dir.Y, dir.X);
                if (angle > Math.PI / 2 || angle < -Math.PI / 2) angle += Math.PI;

                DBText txtDuct = new DBText
                {
                    Position = midPt + new Vector3d(-dir.Y, dir.X, 0) * (widthMm / 2.0 + 35),
                    Height = 85,
                    TextString = $"{prefix} {widthMm:0}x{heightMm:0}",
                    Rotation = angle,
                    TextStyleId = styleId,
                    LayerId = layerId
                };
                btr.AppendEntity(txtDuct); tr.AddNewlyCreatedDBObject(txtDuct, true);

                tr.Commit();
            }
        }

        public static void InsertCeilingDiffuser(Database db, Point3d pt, double sizeMm = 600, double airFlowCfm = 350)
        {
            if (db == null) return;

            ObjectId layerId = CadUtils.GetOrCreateLayer(db, "M-HVAC-DIFFUSER", 4); // Cyan
            ObjectId layerText = CadUtils.GetOrCreateLayer(db, "M-HVAC-TEXT", 7);
            ObjectId styleId = CadUtils.GetOrCreateUnicodeStyle(db);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                double half = sizeMm / 2.0;

                // Khung ngoài miệng gió khuếch tán 4 hướng
                Polyline outer = new Polyline();
                outer.AddVertexAt(0, new Point2d(pt.X - half, pt.Y - half), 0, 0, 0);
                outer.AddVertexAt(1, new Point2d(pt.X + half, pt.Y - half), 0, 0, 0);
                outer.AddVertexAt(2, new Point2d(pt.X + half, pt.Y + half), 0, 0, 0);
                outer.AddVertexAt(3, new Point2d(pt.X - half, pt.Y + half), 0, 0, 0);
                outer.Closed = true;
                outer.LayerId = layerId;
                btr.AppendEntity(outer); tr.AddNewlyCreatedDBObject(outer, true);

                // Khung cánh bên trong (Inner Louvers)
                double innerHalf = half * 0.55;
                Polyline inner = new Polyline();
                inner.AddVertexAt(0, new Point2d(pt.X - innerHalf, pt.Y - innerHalf), 0, 0, 0);
                inner.AddVertexAt(1, new Point2d(pt.X + innerHalf, pt.Y - innerHalf), 0, 0, 0);
                inner.AddVertexAt(2, new Point2d(pt.X + innerHalf, pt.Y + innerHalf), 0, 0, 0);
                inner.AddVertexAt(3, new Point2d(pt.X - innerHalf, pt.Y + innerHalf), 0, 0, 0);
                inner.Closed = true;
                inner.LayerId = layerId;
                btr.AppendEntity(inner); tr.AddNewlyCreatedDBObject(inner, true);

                // 4 Nét chéo chỉ hướng thổi 4 phía
                Line d1 = new Line(new Point3d(pt.X - half, pt.Y - half, 0), new Point3d(pt.X - innerHalf, pt.Y - innerHalf, 0)) { LayerId = layerId };
                Line d2 = new Line(new Point3d(pt.X + half, pt.Y - half, 0), new Point3d(pt.X + innerHalf, pt.Y - innerHalf, 0)) { LayerId = layerId };
                Line d3 = new Line(new Point3d(pt.X + half, pt.Y + half, 0), new Point3d(pt.X + innerHalf, pt.Y + innerHalf, 0)) { LayerId = layerId };
                Line d4 = new Line(new Point3d(pt.X - half, pt.Y + half, 0), new Point3d(pt.X - innerHalf, pt.Y + innerHalf, 0)) { LayerId = layerId };
                btr.AppendEntity(d1); tr.AddNewlyCreatedDBObject(d1, true);
                btr.AppendEntity(d2); tr.AddNewlyCreatedDBObject(d2, true);
                btr.AppendEntity(d3); tr.AddNewlyCreatedDBObject(d3, true);
                btr.AppendEntity(d4); tr.AddNewlyCreatedDBObject(d4, true);

                // Nhãn lưu lượng gió
                DBText txt = new DBText
                {
                    Position = new Point3d(pt.X - 180, pt.Y - half - 80, 0),
                    Height = 65,
                    TextString = $"SCD {sizeMm:0}x{sizeMm:0} ({airFlowCfm:0} CFM)",
                    TextStyleId = styleId,
                    LayerId = layerText
                };
                btr.AppendEntity(txt); tr.AddNewlyCreatedDBObject(txt, true);

                tr.Commit();
            }
        }
    }
}
