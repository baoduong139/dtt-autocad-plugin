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
    public enum HouseShapeKind
    {
        Rectangle,
        LShape,
        TShape,
        PickBoundary
    }

    public enum ArchDoorKind
    {
        SingleSwing900,
        DoubleSwing1400,
        Sliding1200
    }

    public class HousePlanConfig
    {
        // 1. Hình dáng & Kích thước
        public HouseShapeKind Shape { get; set; } = HouseShapeKind.Rectangle;
        public string RoomName { get; set; } = "PHÒNG KHÁCH & SINH HOẠT CHUNG";
        public double Length { get; set; } = 8000.0; // mm
        public double Width { get; set; } = 5000.0;  // mm
        public double BranchL { get; set; } = 4000.0; // mm (Cho chữ L, T)
        public double BranchW { get; set; } = 3000.0; // mm

        // 2. Kết cấu
        public double WallOuterT { get; set; } = 220.0; // mm (220 hoặc 300)
        public double WallInnerT { get; set; } = 110.0; // mm
        public bool HasColumns { get; set; } = true;
        public double ColumnSize { get; set; } = 220.0; // mm
        public double CeilingHeight { get; set; } = 3300.0; // mm

        // 3. Cửa đi & Cửa sổ
        public bool HasDoor { get; set; } = true;
        public ArchDoorKind DoorType { get; set; } = ArchDoorKind.SingleSwing900;
        public bool HasWindow { get; set; } = true;
        public double WindowWidth { get; set; } = 1600.0; // mm

        // 4. Hệ Trục & Kích thước
        public bool GenerateGrids { get; set; } = true;
        public bool Generate3LayerDims { get; set; } = true;

        // 5. Phủ hệ thống Điện (Electrical MEP Overlay)
        public bool OverlayElectrical { get; set; } = true;
        public int LightCount { get; set; } = 6;
        public bool PlaceDbPanel { get; set; } = true;
    }

    public static class ArchitecturalHousePlanEngine
    {
        public static bool GenerateHousePlan(Database db, Point3d startPt, HousePlanConfig cfg, Polyline boundaryPoly = null)
        {
            if (db == null || cfg == null) return false;
            Editor ed = Application.DocumentManager.MdiActiveDocument?.Editor;

            try
            {
                // 1. Chuẩn bị Layer & TextStyle trước ngoài transaction vẽ
                ObjectId wallLayer = CadUtils.GetOrCreateLayer(db, "A-WALL", 7, LineWeight.LineWeight050); // Nét tường cắt đậm
                ObjectId colLayer = CadUtils.GetOrCreateLayer(db, "A-COLS", 1, LineWeight.LineWeight050); // Cột bê tông đỏ
                ObjectId doorLayer = CadUtils.GetOrCreateLayer(db, "A-DOOR", 2, LineWeight.LineWeight025); // Cửa vàng
                ObjectId winLayer = CadUtils.GetOrCreateLayer(db, "A-GLAZ", 4, LineWeight.LineWeight025); // Kính xanh cyan
                ObjectId gridLayer = CadUtils.GetOrCreateLayer(db, "A-GRID", 1, LineWeight.LineWeight015); // Trục đỏ nét chấm gạch
                ObjectId dimLayer = CadUtils.GetOrCreateLayer(db, "A-DIMS", 3, LineWeight.LineWeight018); // Kích thước xanh lá
                ObjectId textLayer = CadUtils.GetOrCreateLayer(db, "A-TEXT", 7, LineWeight.LineWeight020); // Chữ trắng

                ObjectId lightLayer = CadUtils.GetOrCreateLayer(db, "E-LGT-FIXT", 2, LineWeight.LineWeight035);
                ObjectId wireLayer = CadUtils.GetOrCreateLayer(db, "E-LGT-WIRE", 1, LineWeight.LineWeight020);
                ObjectId panelLayer = CadUtils.GetOrCreateLayer(db, "E-PWR-PNL", 6, LineWeight.LineWeight040);

                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    double ox = startPt.X;
                    double oy = startPt.Y;
                    double L = Math.Max(1000.0, cfg.Length);
                    double W = Math.Max(1000.0, cfg.Width);
                    double bL = cfg.BranchL > 0 ? cfg.BranchL : L * 0.5;
                    double bW = cfg.BranchW > 0 ? cfg.BranchW : W * 0.5;
                    double wallT = cfg.WallOuterT > 0 ? cfg.WallOuterT : 220.0;
                    double colS = cfg.ColumnSize > 0 ? cfg.ColumnSize : 220.0;

                    // 2. TÍNH TOÁN CÁC ĐỈNH RANH GIỚI TƯỜNG NGOÀI & TƯỜNG TRONG BẰNG HÌNH HỌC THUẦN (100% AN TOÀN)
                    List<Point2d> outlineOuter = new List<Point2d>();
                    List<Point2d> outlineInner = new List<Point2d>();

                    if (cfg.Shape == HouseShapeKind.LShape)
                    {
                        // Đỉnh ngoài chữ L
                        outlineOuter.Add(new Point2d(ox, oy));
                        outlineOuter.Add(new Point2d(ox + L, oy));
                        outlineOuter.Add(new Point2d(ox + L, oy + bW));
                        outlineOuter.Add(new Point2d(ox + bL, oy + bW));
                        outlineOuter.Add(new Point2d(ox + bL, oy + W));
                        outlineOuter.Add(new Point2d(ox, oy + W));

                        // Đỉnh trong chữ L
                        outlineInner.Add(new Point2d(ox + wallT, oy + wallT));
                        outlineInner.Add(new Point2d(ox + L - wallT, oy + wallT));
                        outlineInner.Add(new Point2d(ox + L - wallT, oy + bW - wallT));
                        outlineInner.Add(new Point2d(ox + bL + wallT, oy + bW - wallT));
                        outlineInner.Add(new Point2d(ox + bL + wallT, oy + W - wallT));
                        outlineInner.Add(new Point2d(ox + wallT, oy + W - wallT));
                    }
                    else if (cfg.Shape == HouseShapeKind.TShape)
                    {
                        double stepX = (L - bL) / 2.0;
                        outlineOuter.Add(new Point2d(ox + stepX, oy));
                        outlineOuter.Add(new Point2d(ox + stepX + bL, oy));
                        outlineOuter.Add(new Point2d(ox + stepX + bL, oy + bW));
                        outlineOuter.Add(new Point2d(ox + L, oy + bW));
                        outlineOuter.Add(new Point2d(ox + L, oy + W));
                        outlineOuter.Add(new Point2d(ox, oy + W));
                        outlineOuter.Add(new Point2d(ox, oy + bW));
                        outlineOuter.Add(new Point2d(ox + stepX, oy + bW));

                        outlineInner.Add(new Point2d(ox + stepX + wallT, oy + wallT));
                        outlineInner.Add(new Point2d(ox + stepX + bL - wallT, oy + wallT));
                        outlineInner.Add(new Point2d(ox + stepX + bL - wallT, oy + bW + wallT));
                        outlineInner.Add(new Point2d(ox + L - wallT, oy + bW + wallT));
                        outlineInner.Add(new Point2d(ox + L - wallT, oy + W - wallT));
                        outlineInner.Add(new Point2d(ox + wallT, oy + W - wallT));
                        outlineInner.Add(new Point2d(ox + wallT, oy + bW + wallT));
                        outlineInner.Add(new Point2d(ox + stepX + wallT, oy + bW + wallT));
                    }
                    else if (cfg.Shape == HouseShapeKind.PickBoundary && boundaryPoly != null && boundaryPoly.NumberOfVertices >= 3)
                    {
                        for (int i = 0; i < boundaryPoly.NumberOfVertices; i++)
                            outlineOuter.Add(boundaryPoly.GetPoint2dAt(i));
                    }
                    else // Mặc định: Hình Chữ Nhật
                    {
                        outlineOuter.Add(new Point2d(ox, oy));
                        outlineOuter.Add(new Point2d(ox + L, oy));
                        outlineOuter.Add(new Point2d(ox + L, oy + W));
                        outlineOuter.Add(new Point2d(ox, oy + W));

                        outlineInner.Add(new Point2d(ox + wallT, oy + wallT));
                        outlineInner.Add(new Point2d(ox + L - wallT, oy + wallT));
                        outlineInner.Add(new Point2d(ox + L - wallT, oy + W - wallT));
                        outlineInner.Add(new Point2d(ox + wallT, oy + W - wallT));
                    }

                    // 3. VẼ ĐƯỜNG BAO TƯỜNG NGOÀI
                    Polyline polyOuter = new Polyline();
                    for (int i = 0; i < outlineOuter.Count; i++)
                        polyOuter.AddVertexAt(i, outlineOuter[i], 0, 0, 0);
                    polyOuter.Closed = true;
                    polyOuter.LayerId = wallLayer;
                    btr.AppendEntity(polyOuter);
                    tr.AddNewlyCreatedDBObject(polyOuter, true);

                    // VẼ ĐƯỜNG BAO TƯỜNG TRONG
                    if (outlineInner.Count >= 3)
                    {
                        Polyline polyInner = new Polyline();
                        for (int i = 0; i < outlineInner.Count; i++)
                            polyInner.AddVertexAt(i, outlineInner[i], 0, 0, 0);
                        polyInner.Closed = true;
                        polyInner.LayerId = wallLayer;
                        btr.AppendEntity(polyInner);
                        tr.AddNewlyCreatedDBObject(polyInner, true);
                    }

                    // 4. VẼ CỘT BÊ TÔNG BTCT
                    if (cfg.HasColumns)
                    {
                        foreach (var pt in outlineOuter)
                        {
                            DrawConcreteColumn(btr, tr, colLayer, pt.X - colS / 2.0, pt.Y - colS / 2.0, colS, colS);
                        }
                        if (cfg.Shape == HouseShapeKind.Rectangle && L > 4500.0)
                        {
                            DrawConcreteColumn(btr, tr, colLayer, ox + L / 2.0 - colS / 2.0, oy - colS / 2.0, colS, colS);
                            DrawConcreteColumn(btr, tr, colLayer, ox + L / 2.0 - colS / 2.0, oy + W - colS / 2.0, colS, colS);
                        }
                    }

                    // 5. TRỔ CỬA ĐI & CỬA SỔ
                    double doorW = (cfg.DoorType == ArchDoorKind.DoubleSwing1400) ? 1400.0 :
                                   (cfg.DoorType == ArchDoorKind.Sliding1200) ? 1200.0 : 900.0;
                    Point2d doorLoc = new Point2d(ox + colS + 300.0, oy);
                    if (cfg.HasDoor)
                    {
                        DrawArchDoor(btr, tr, doorLayer, doorLoc, doorW, cfg.DoorType, wallT);
                    }

                    if (cfg.HasWindow)
                    {
                        double winW = cfg.WindowWidth > 0 ? cfg.WindowWidth : 1600.0;
                        Point2d winLoc = new Point2d(ox + L / 2.0 - winW / 2.0, oy + W);
                        DrawArchWindow(btr, tr, winLayer, winLoc, winW, wallT);
                    }

                    // 6. GHI NHÃN PHÒNG & TÍNH DIỆN TÍCH
                    double areaM2 = (L * W) / 1000000.0;
                    if (cfg.Shape == HouseShapeKind.LShape) areaM2 *= 0.75;
                    else if (cfg.Shape == HouseShapeKind.TShape) areaM2 *= 0.65;

                    Point3d textCenter = new Point3d(ox + L / 2.0, oy + W / 2.0, 0);
                    DrawMText(btr, tr, textLayer, textStyle, cfg.RoomName.ToUpper(), textCenter + new Vector3d(0, 100, 0), 220.0, AttachmentPoint.MiddleCenter);
                    DrawMText(btr, tr, textLayer, textStyle, $"S = {areaM2:F1} m²  |  H_trần = {cfg.CeilingHeight / 1000.0:F2}m", textCenter - new Vector3d(0, 120, 0), 140.0, AttachmentPoint.MiddleCenter);

                    // 7. HỆ TRỤC KIẾN TRÚC
                    if (cfg.GenerateGrids)
                    {
                        DrawGridSystem(btr, tr, gridLayer, textStyle, ox, oy, L, W, db);
                    }

                    // 8. 3 LỚP ĐƯỜNG KÍCH THƯỚC
                    if (cfg.Generate3LayerDims)
                    {
                        Draw3LayerDimensions(btr, tr, dimLayer, textStyle, ox, oy, L, W, doorLoc, doorW, cfg.HasDoor);
                    }

                    // 9. PHỦ HỆ THỐNG ĐIỆN MEP OVERLAY
                    if (cfg.OverlayElectrical)
                    {
                        DrawElectricalOverlay(btr, tr, lightLayer, wireLayer, panelLayer, textStyle, ox, oy, L, W, cfg, db);
                    }

                    tr.Commit();
                }

                ed?.WriteMessage($"\n✅ [DTools PRO] Đã hoàn thành vẽ mặt bằng nhà kiến trúc [{cfg.RoomName}] ({cfg.Length:0}x{cfg.Width:0}mm) thành công!\n");
                return true;
            }
            catch (System.Exception ex)
            {
                ed?.WriteMessage($"\n[DTools LỖI] Sinh mặt bằng kiến trúc thất bại: {ex.Message}\n");
                return false;
            }
        }

        // ============================================================
        // TỰ ĐỘNG SINH MẶT BẰNG TẦNG ĐIỂN HÌNH HOÀN CHỈNH (4 PHÒNG)
        // ============================================================
        public static bool GenerateStandardFloorPlan(Database db, Point3d startPt, double totalL, double totalW, double wallT)
        {
            if (db == null) return false;
            Editor ed = Application.DocumentManager.MdiActiveDocument?.Editor;

            try
            {
                ObjectId wallLayer = CadUtils.GetOrCreateLayer(db, "A-WALL", 7, LineWeight.LineWeight050);
                ObjectId colLayer = CadUtils.GetOrCreateLayer(db, "A-COLS", 1, LineWeight.LineWeight050);
                ObjectId doorLayer = CadUtils.GetOrCreateLayer(db, "A-DOOR", 2, LineWeight.LineWeight025);
                ObjectId winLayer = CadUtils.GetOrCreateLayer(db, "A-GLAZ", 4, LineWeight.LineWeight025);
                ObjectId gridLayer = CadUtils.GetOrCreateLayer(db, "A-GRID", 1, LineWeight.LineWeight015);
                ObjectId dimLayer = CadUtils.GetOrCreateLayer(db, "A-DIMS", 3, LineWeight.LineWeight018);
                ObjectId textLayer = CadUtils.GetOrCreateLayer(db, "A-TEXT", 7, LineWeight.LineWeight020);
                ObjectId stairLayer = CadUtils.GetOrCreateLayer(db, "A-STRS", 6, LineWeight.LineWeight025);

                ObjectId lightLayer = CadUtils.GetOrCreateLayer(db, "E-LGT-FIXT", 2, LineWeight.LineWeight035);
                ObjectId panelLayer = CadUtils.GetOrCreateLayer(db, "E-PWR-PNL", 6, LineWeight.LineWeight040);

                ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    double ox = startPt.X;
                    double oy = startPt.Y;
                    double L = Math.Max(6000.0, totalL);
                    double W = Math.Max(4000.0, totalW);
                    double wt = wallT > 0 ? wallT : 220.0;
                    double innerT = 110.0;
                    double colS = 220.0;

                    // 1. Tường bao ngoài (Outer & Inner boundary)
                    DrawRect(btr, tr, wallLayer, ox, oy, L, W);
                    DrawRect(btr, tr, wallLayer, ox + wt, oy + wt, L - 2 * wt, W - 2 * wt);

                    // Phân chia không gian 4 phòng:
                    // Trục ngăn dọc: tách Vế Trái (Khách + Bếp) và Vế Phải (Ngủ + WC + Cầu thang)
                    double splitX = ox + Math.Round(L * 0.55);
                    // Trục ngăn ngang Vế Trái (tách Khách và Bếp)
                    double splitY1 = oy + Math.Round(W * 0.52);
                    // Trục ngăn ngang Vế Phải (tách Ngủ và WC/Cầu thang)
                    double splitY2 = oy + Math.Round(W * 0.48);

                    // 2. Tường ngăn trong (Inner Partition Walls)
                    DrawRect(btr, tr, wallLayer, splitX - innerT / 2.0, oy + wt, innerT, W - 2 * wt);
                    DrawRect(btr, tr, wallLayer, ox + wt, splitY1 - innerT / 2.0, (splitX - innerT / 2.0) - (ox + wt), innerT);
                    DrawRect(btr, tr, wallLayer, splitX + innerT / 2.0, splitY2 - innerT / 2.0, (ox + L - wt) - (splitX + innerT / 2.0), innerT);

                    // Tường ngăn phụ giữa WC và Cầu thang
                    double splitX2 = splitX + innerT / 2.0 + ((ox + L - wt) - (splitX + innerT / 2.0)) * 0.45;
                    DrawRect(btr, tr, wallLayer, splitX2 - innerT / 2.0, splitY2 + innerT / 2.0, innerT, (oy + W - wt) - (splitY2 + innerT / 2.0));

                    // 3. Cột Bê Tông (220x220) tại các góc và giao điểm
                    double halfCol = colS / 2.0;
                    DrawConcreteColumn(btr, tr, colLayer, ox, oy, colS, colS);
                    DrawConcreteColumn(btr, tr, colLayer, ox + L - colS, oy, colS, colS);
                    DrawConcreteColumn(btr, tr, colLayer, ox + L - colS, oy + W - colS, colS, colS);
                    DrawConcreteColumn(btr, tr, colLayer, ox, oy + W - colS, colS, colS);

                    DrawConcreteColumn(btr, tr, colLayer, splitX - halfCol, oy, colS, colS);
                    DrawConcreteColumn(btr, tr, colLayer, splitX - halfCol, oy + W - colS, colS, colS);
                    DrawConcreteColumn(btr, tr, colLayer, ox, splitY1 - halfCol, colS, colS);
                    DrawConcreteColumn(btr, tr, colLayer, ox + L - colS, splitY2 - halfCol, colS, colS);
                    DrawConcreteColumn(btr, tr, colLayer, splitX - halfCol, splitY1 - halfCol, colS, colS);

                    // 4. Cửa Đi
                    double mainDoorW = 1400.0;
                    double mainDoorX = ox + ((splitX - ox) - mainDoorW) / 2.0;
                    DrawArchDoor(btr, tr, doorLayer, new Point2d(mainDoorX, oy + wt), mainDoorW, ArchDoorKind.DoubleSwing1400, wt);

                    double bedDoorW = 900.0;
                    Point2d bedDoorPt = new Point2d(splitX + innerT / 2.0 + 200.0, splitY2 - innerT / 2.0);
                    DrawArchDoor(btr, tr, doorLayer, bedDoorPt, bedDoorW, ArchDoorKind.SingleSwing900, innerT);

                    double wcDoorW = 750.0;
                    Point2d wcDoorPt = new Point2d(splitX + innerT / 2.0 + 150.0, splitY2 + innerT / 2.0);
                    DrawArchDoor(btr, tr, doorLayer, wcDoorPt, wcDoorW, ArchDoorKind.SingleSwing900, innerT);

                    double backDoorW = 900.0;
                    Point2d backDoorPt = new Point2d(ox + ((splitX - ox) - backDoorW) / 2.0, oy + W - wt);
                    DrawArchDoor(btr, tr, doorLayer, backDoorPt, backDoorW, ArchDoorKind.SingleSwing900, wt);

                    // 5. Cửa Sổ
                    DrawArchWindow(btr, tr, winLayer, new Point2d(ox + 400.0, oy + wt), 1600.0, wt);
                    DrawArchWindow(btr, tr, winLayer, new Point2d(splitX + ((ox + L - splitX) - 1600.0) / 2.0, oy + wt), 1600.0, wt);
                    DrawArchWindow(btr, tr, winLayer, new Point2d(ox + 500.0, oy + W), 1400.0, wt);
                    DrawArchWindow(btr, tr, winLayer, new Point2d(ox + L - wt, splitY2 + 500.0), 600.0, wt);

                    // 6. Cầu Thang Bộ
                    double stairX1 = splitX + innerT / 2.0 + 80.0;
                    double stairX2 = splitX2 - innerT / 2.0 - 80.0;
                    double stairY1 = splitY2 + innerT / 2.0 + 100.0;
                    double stairY2 = oy + W - wt - 100.0;
                    double stairW = stairX2 - stairX1;
                    double stairH = stairY2 - stairY1;

                    if (stairW > 600 && stairH > 1200)
                    {
                        DrawRect(btr, tr, stairLayer, stairX1, stairY1, stairW, stairH);
                        double midStairX = (stairX1 + stairX2) / 2.0;
                        Line spine = new Line(new Point3d(midStairX, stairY1, 0), new Point3d(midStairX, stairY2 - 700.0, 0)) { LayerId = stairLayer };
                        btr.AppendEntity(spine);
                        tr.AddNewlyCreatedDBObject(spine, true);

                        DrawRect(btr, tr, stairLayer, stairX1, stairY2 - 700.0, stairW, 700.0);
                        DrawMText(btr, tr, stairLayer, textStyle, "CHIẾU NGHỈ", new Point3d(midStairX, stairY2 - 350.0, 0), 100.0, AttachmentPoint.MiddleCenter);

                        int numSteps = 7;
                        double stepD = (stairH - 700.0) / numSteps;
                        for (int s = 1; s < numSteps; s++)
                        {
                            double sy = stairY1 + s * stepD;
                            Line st1 = new Line(new Point3d(stairX1, sy, 0), new Point3d(midStairX, sy, 0)) { LayerId = stairLayer };
                            Line st2 = new Line(new Point3d(midStairX, sy, 0), new Point3d(stairX2, sy, 0)) { LayerId = stairLayer };
                            btr.AppendEntity(st1);
                            tr.AddNewlyCreatedDBObject(st1, true);
                            btr.AppendEntity(st2);
                            tr.AddNewlyCreatedDBObject(st2, true);
                        }

                        Line arr = new Line(new Point3d((stairX1 + midStairX) / 2.0, stairY1 + 100.0, 0), new Point3d((stairX1 + midStairX) / 2.0, stairY2 - 800.0, 0)) { LayerId = stairLayer };
                        btr.AppendEntity(arr);
                        tr.AddNewlyCreatedDBObject(arr, true);
                        DrawMText(btr, tr, stairLayer, textStyle, "▲ LÊN TẦNG", new Point3d((stairX1 + midStairX) / 2.0 - 40.0, stairY1 + stairH * 0.3, 0), 90.0, AttachmentPoint.MiddleRight);
                    }

                    // 7. Nhãn Phòng & Diện Tích
                    double sLiving = Math.Max(0.1, ((splitX - ox - wt) * (splitY1 - oy - wt)) / 1e6);
                    double sKitchen = Math.Max(0.1, ((splitX - ox - wt) * (oy + W - wt - splitY1)) / 1e6);
                    double sBed = Math.Max(0.1, ((ox + L - wt - splitX) * (splitY2 - oy - wt)) / 1e6);
                    double sWc = Math.Max(0.1, ((ox + L - wt - splitX2) * (oy + W - wt - splitY2)) / 1e6);

                    Point3d pLiving = new Point3d(ox + (splitX - ox) / 2.0, oy + (splitY1 - oy) / 2.0, 0);
                    DrawMText(btr, tr, textLayer, textStyle, "PHÒNG KHÁCH", pLiving, 240.0, AttachmentPoint.MiddleCenter);
                    DrawMText(btr, tr, textLayer, textStyle, $"S = {sLiving:F1} m²", new Point3d(pLiving.X, pLiving.Y - 260.0, 0), 160.0, AttachmentPoint.MiddleCenter);

                    Point3d pKitchen = new Point3d(ox + (splitX - ox) / 2.0, splitY1 + (oy + W - splitY1) / 2.0, 0);
                    DrawMText(btr, tr, textLayer, textStyle, "BẾP & PHÒNG ĂN", pKitchen, 240.0, AttachmentPoint.MiddleCenter);
                    DrawMText(btr, tr, textLayer, textStyle, $"S = {sKitchen:F1} m²", new Point3d(pKitchen.X, pKitchen.Y - 260.0, 0), 160.0, AttachmentPoint.MiddleCenter);

                    Point3d pBed = new Point3d(splitX + (ox + L - splitX) / 2.0, oy + (splitY2 - oy) / 2.0, 0);
                    DrawMText(btr, tr, textLayer, textStyle, "PHÒNG NGỦ MASTER", pBed, 240.0, AttachmentPoint.MiddleCenter);
                    DrawMText(btr, tr, textLayer, textStyle, $"S = {sBed:F1} m²", new Point3d(pBed.X, pBed.Y - 260.0, 0), 160.0, AttachmentPoint.MiddleCenter);

                    Point3d pWc = new Point3d(splitX2 + (ox + L - splitX2) / 2.0, splitY2 + (oy + W - splitY2) / 2.0, 0);
                    DrawMText(btr, tr, textLayer, textStyle, "WC", pWc, 200.0, AttachmentPoint.MiddleCenter);
                    DrawMText(btr, tr, textLayer, textStyle, $"S = {sWc:F1} m²", new Point3d(pWc.X, pWc.Y - 220.0, 0), 140.0, AttachmentPoint.MiddleCenter);

                    // 8. Tủ Điện DB tổng & Đèn chiếu sáng
                    DrawRect(btr, tr, panelLayer, ox + wt + 80.0, oy + wt + 200.0, 450, 140);
                    DrawMText(btr, tr, panelLayer, textStyle, "TỦ ĐIỆN TỔNG DB-01", new Point3d(ox + wt + 305.0, oy + wt + 380.0, 0), 80.0, AttachmentPoint.BottomCenter);

                    Circle l1 = new Circle(pLiving, Vector3d.ZAxis, 120.0) { LayerId = lightLayer };
                    Circle l2 = new Circle(pKitchen, Vector3d.ZAxis, 120.0) { LayerId = lightLayer };
                    Circle l3 = new Circle(pBed, Vector3d.ZAxis, 120.0) { LayerId = lightLayer };
                    Circle l4 = new Circle(pWc, Vector3d.ZAxis, 90.0) { LayerId = lightLayer };
                    btr.AppendEntity(l1); tr.AddNewlyCreatedDBObject(l1, true);
                    btr.AppendEntity(l2); tr.AddNewlyCreatedDBObject(l2, true);
                    btr.AppendEntity(l3); tr.AddNewlyCreatedDBObject(l3, true);
                    btr.AppendEntity(l4); tr.AddNewlyCreatedDBObject(l4, true);

                    // 9. Hệ Trục Tọa Độ (Grid System)
                    DrawGridSystem(btr, tr, gridLayer, textStyle, ox, oy, L, W, db);

                    // 10. 3 Lớp Kích Thước (Dimensions)
                    Draw3LayerDimensions(btr, tr, dimLayer, textStyle, ox, oy, L, W, new Point2d(mainDoorX, oy), mainDoorW, true);

                    // Tiêu đề bản vẽ
                    Point3d pTitle = new Point3d(ox + L / 2.0, oy - 2800.0, 0);
                    DrawMText(btr, tr, textLayer, textStyle, "MẶT BẰNG TẦNG ĐIỂN HÌNH - KIẾN TRÚC & ĐIỆN", pTitle, 350.0, AttachmentPoint.MiddleCenter);
                    DrawMText(btr, tr, textLayer, textStyle, $"TỶ LỆ 1/100  |  KÍCH THƯỚC: {L:0} x {W:0} mm  |  DTT AUTO CAD TOOLS", new Point3d(pTitle.X, pTitle.Y - 380.0, 0), 160.0, AttachmentPoint.MiddleCenter);

                    tr.Commit();
                }

                ed?.WriteMessage($"\n✅ [DTools PRO] Đã hoàn thành vẽ Mặt bằng tầng điển hình ({totalL:0}x{totalW:0}mm, tường {wallT:0}mm) thành công rực rỡ!\n");
                return true;
            }
            catch (System.Exception ex)
            {
                ed?.WriteMessage($"\n❌ [DTools LỖI] Sinh mặt bằng tầng điển hình thất bại: {ex.Message}\n");
                return false;
            }
        }

        // ============================================================
        // CÁC HÀM HỖ TRỢ VẼ HÌNH HỌC AN TOÀN
        // ============================================================

        private static void DrawConcreteColumn(BlockTableRecord btr, Transaction tr, ObjectId layer, double x, double y, double w, double h)
        {
            try
            {
                Polyline colBox = new Polyline();
                colBox.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
                colBox.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
                colBox.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
                colBox.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
                colBox.Closed = true;
                colBox.LayerId = layer;
                btr.AppendEntity(colBox);
                tr.AddNewlyCreatedDBObject(colBox, true);

                Line l1 = new Line(new Point3d(x, y, 0), new Point3d(x + w, y + h, 0)) { LayerId = layer };
                Line l2 = new Line(new Point3d(x + w, y, 0), new Point3d(x, y + h, 0)) { LayerId = layer };
                btr.AppendEntity(l1);
                tr.AddNewlyCreatedDBObject(l1, true);
                btr.AppendEntity(l2);
                tr.AddNewlyCreatedDBObject(l2, true);
            }
            catch { }
        }

        private static void DrawArchDoor(BlockTableRecord btr, Transaction tr, ObjectId layer, Point2d pt, double width, ArchDoorKind kind, double wallT)
        {
            try
            {
                if (kind == ArchDoorKind.DoubleSwing1400)
                {
                    double halfW = width / 2.0;
                    Line leafL = new Line(new Point3d(pt.X, pt.Y, 0), new Point3d(pt.X, pt.Y + halfW, 0)) { LayerId = layer };
                    Arc arcL = new Arc(new Point3d(pt.X, pt.Y, 0), halfW, 0, Math.PI / 2.0) { LayerId = layer };
                    btr.AppendEntity(leafL);
                    tr.AddNewlyCreatedDBObject(leafL, true);
                    btr.AppendEntity(arcL);
                    tr.AddNewlyCreatedDBObject(arcL, true);

                    Line leafR = new Line(new Point3d(pt.X + width, pt.Y, 0), new Point3d(pt.X + width, pt.Y + halfW, 0)) { LayerId = layer };
                    Arc arcR = new Arc(new Point3d(pt.X + width, pt.Y, 0), halfW, Math.PI / 2.0, Math.PI) { LayerId = layer };
                    btr.AppendEntity(leafR);
                    tr.AddNewlyCreatedDBObject(leafR, true);
                    btr.AppendEntity(arcR);
                    tr.AddNewlyCreatedDBObject(arcR, true);
                }
                else if (kind == ArchDoorKind.Sliding1200)
                {
                    double halfW = width / 2.0;
                    Line rail = new Line(new Point3d(pt.X - 100, pt.Y + wallT / 2.0, 0), new Point3d(pt.X + width + 100, pt.Y + wallT / 2.0, 0)) { LayerId = layer };
                    btr.AppendEntity(rail);
                    tr.AddNewlyCreatedDBObject(rail, true);

                    DrawRect(btr, tr, layer, pt.X, pt.Y + wallT / 2.0 - 25, halfW + 40, 50);
                    DrawRect(btr, tr, layer, pt.X + halfW - 40, pt.Y + wallT / 2.0 - 55, halfW + 40, 50);
                }
                else
                {
                    Line doorLeaf = new Line(new Point3d(pt.X, pt.Y, 0), new Point3d(pt.X, pt.Y + width, 0)) { LayerId = layer };
                    btr.AppendEntity(doorLeaf);
                    tr.AddNewlyCreatedDBObject(doorLeaf, true);

                    Arc swing = new Arc(new Point3d(pt.X, pt.Y, 0), width, 0, Math.PI / 2.0) { LayerId = layer };
                    btr.AppendEntity(swing);
                    tr.AddNewlyCreatedDBObject(swing, true);
                }
            }
            catch { }
        }

        private static void DrawArchWindow(BlockTableRecord btr, Transaction tr, ObjectId layer, Point2d pt, double width, double wallT)
        {
            try
            {
                DrawRect(btr, tr, layer, pt.X, pt.Y - wallT, width, wallT);
                Line glass1 = new Line(new Point3d(pt.X, pt.Y - wallT * 0.35, 0), new Point3d(pt.X + width, pt.Y - wallT * 0.35, 0)) { LayerId = layer };
                Line glass2 = new Line(new Point3d(pt.X, pt.Y - wallT * 0.65, 0), new Point3d(pt.X + width, pt.Y - wallT * 0.65, 0)) { LayerId = layer };
                btr.AppendEntity(glass1);
                tr.AddNewlyCreatedDBObject(glass1, true);
                btr.AppendEntity(glass2);
                tr.AddNewlyCreatedDBObject(glass2, true);
            }
            catch { }
        }

        private static void DrawGridSystem(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, double ox, double oy, double L, double W, Database db)
        {
            try
            {
                double extend = 1200.0;
                double bubbleR = 300.0;

                // 1. Trục dọc
                List<double> xGrids = new List<double> { ox };
                if (L >= 5000.0) xGrids.Add(ox + L / 2.0);
                xGrids.Add(ox + L);

                for (int i = 0; i < xGrids.Count; i++)
                {
                    double gx = xGrids[i];
                    string label = (i + 1).ToString();

                    Line line = new Line(new Point3d(gx, oy - extend, 0), new Point3d(gx, oy + W + extend, 0)) { LayerId = layer };
                    CadUtils.SafeSetLinetype(line, db, "CENTER");
                    btr.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);

                    Circle cBot = new Circle(new Point3d(gx, oy - extend - bubbleR, 0), Vector3d.ZAxis, bubbleR) { LayerId = layer };
                    btr.AppendEntity(cBot);
                    tr.AddNewlyCreatedDBObject(cBot, true);
                    DrawMText(btr, tr, layer, textStyle, label, new Point3d(gx, oy - extend - bubbleR, 0), 220.0, AttachmentPoint.MiddleCenter);

                    Circle cTop = new Circle(new Point3d(gx, oy + W + extend + bubbleR, 0), Vector3d.ZAxis, bubbleR) { LayerId = layer };
                    btr.AppendEntity(cTop);
                    tr.AddNewlyCreatedDBObject(cTop, true);
                    DrawMText(btr, tr, layer, textStyle, label, new Point3d(gx, oy + W + extend + bubbleR, 0), 220.0, AttachmentPoint.MiddleCenter);
                }

                // 2. Trục ngang
                List<double> yGrids = new List<double> { oy };
                if (W >= 5000.0) yGrids.Add(oy + W / 2.0);
                yGrids.Add(oy + W);

                string[] yLabels = { "A", "B", "C", "D" };
                for (int i = 0; i < yGrids.Count; i++)
                {
                    double gy = yGrids[i];
                    string label = (i < yLabels.Length) ? yLabels[i] : $"Y{i + 1}";

                    Line line = new Line(new Point3d(ox - extend, gy, 0), new Point3d(ox + L + extend, gy, 0)) { LayerId = layer };
                    CadUtils.SafeSetLinetype(line, db, "CENTER");
                    btr.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);

                    Circle cLeft = new Circle(new Point3d(ox - extend - bubbleR, gy, 0), Vector3d.ZAxis, bubbleR) { LayerId = layer };
                    btr.AppendEntity(cLeft);
                    tr.AddNewlyCreatedDBObject(cLeft, true);
                    DrawMText(btr, tr, layer, textStyle, label, new Point3d(ox - extend - bubbleR, gy, 0), 220.0, AttachmentPoint.MiddleCenter);

                    Circle cRight = new Circle(new Point3d(ox + L + extend + bubbleR, gy, 0), Vector3d.ZAxis, bubbleR) { LayerId = layer };
                    btr.AppendEntity(cRight);
                    tr.AddNewlyCreatedDBObject(cRight, true);
                    DrawMText(btr, tr, layer, textStyle, label, new Point3d(ox + L + extend + bubbleR, gy, 0), 220.0, AttachmentPoint.MiddleCenter);
                }
            }
            catch { }
        }

        private static void Draw3LayerDimensions(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, double ox, double oy, double L, double W, Point2d doorLoc, double doorW, bool hasDoor)
        {
            try
            {
                double yL1 = oy - 2200.0;
                double yL2 = oy - 1600.0;
                double yL3 = oy - 900.0;

                // Lớp 1: Dim tổng thể L
                DrawAlignedDimLine(btr, tr, layer, textStyle, new Point2d(ox, oy), new Point2d(ox + L, oy), yL1, $"{L:0}");

                // Lớp 2: Dim khoảng cách trục
                if (L >= 5000.0)
                {
                    double midX = ox + L / 2.0;
                    DrawAlignedDimLine(btr, tr, layer, textStyle, new Point2d(ox, oy), new Point2d(midX, oy), yL2, $"{L / 2.0:0}");
                    DrawAlignedDimLine(btr, tr, layer, textStyle, new Point2d(midX, oy), new Point2d(ox + L, oy), yL2, $"{L / 2.0:0}");
                }
                else
                {
                    DrawAlignedDimLine(btr, tr, layer, textStyle, new Point2d(ox, oy), new Point2d(ox + L, oy), yL2, $"{L:0}");
                }

                // Lớp 3: Dim chi tiết tường & cửa
                if (hasDoor)
                {
                    double leftWall = doorLoc.X - ox;
                    double rightWall = (ox + L) - (doorLoc.X + doorW);
                    DrawAlignedDimLine(btr, tr, layer, textStyle, new Point2d(ox, oy), new Point2d(doorLoc.X, oy), yL3, $"{leftWall:0}");
                    DrawAlignedDimLine(btr, tr, layer, textStyle, new Point2d(doorLoc.X, oy), new Point2d(doorLoc.X + doorW, oy), yL3, $"{doorW:0}");
                    DrawAlignedDimLine(btr, tr, layer, textStyle, new Point2d(doorLoc.X + doorW, oy), new Point2d(ox + L, oy), yL3, $"{rightWall:0}");
                }

                // Phương Y
                double xL1 = ox - 2200.0;
                double xL2 = ox - 1600.0;
                DrawAlignedDimLineVertical(btr, tr, layer, textStyle, new Point2d(ox, oy), new Point2d(ox, oy + W), xL1, $"{W:0}");
                if (W >= 5000.0)
                {
                    double midY = oy + W / 2.0;
                    DrawAlignedDimLineVertical(btr, tr, layer, textStyle, new Point2d(ox, oy), new Point2d(ox, midY), xL2, $"{W / 2.0:0}");
                    DrawAlignedDimLineVertical(btr, tr, layer, textStyle, new Point2d(ox, midY), new Point2d(ox, oy + W), xL2, $"{W / 2.0:0}");
                }
                else
                {
                    DrawAlignedDimLineVertical(btr, tr, layer, textStyle, new Point2d(ox, oy), new Point2d(ox, oy + W), xL2, $"{W:0}");
                }
            }
            catch { }
        }

        private static void DrawAlignedDimLine(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d p1, Point2d p2, double dimY, string text)
        {
            try
            {
                Line ext1 = new Line(new Point3d(p1.X, p1.Y, 0), new Point3d(p1.X, dimY - 80, 0)) { LayerId = layer };
                Line ext2 = new Line(new Point3d(p2.X, p2.Y, 0), new Point3d(p2.X, dimY - 80, 0)) { LayerId = layer };
                btr.AppendEntity(ext1);
                tr.AddNewlyCreatedDBObject(ext1, true);
                btr.AppendEntity(ext2);
                tr.AddNewlyCreatedDBObject(ext2, true);

                Line dim = new Line(new Point3d(p1.X, dimY, 0), new Point3d(p2.X, dimY, 0)) { LayerId = layer };
                btr.AppendEntity(dim);
                tr.AddNewlyCreatedDBObject(dim, true);

                DrawTick(btr, tr, layer, p1.X, dimY);
                DrawTick(btr, tr, layer, p2.X, dimY);

                DrawMText(btr, tr, layer, textStyle, text, new Point3d((p1.X + p2.X) / 2.0, dimY + 40, 0), 125.0, AttachmentPoint.BottomCenter);
            }
            catch { }
        }

        private static void DrawAlignedDimLineVertical(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d p1, Point2d p2, double dimX, string text)
        {
            try
            {
                Line ext1 = new Line(new Point3d(p1.X, p1.Y, 0), new Point3d(dimX - 80, p1.Y, 0)) { LayerId = layer };
                Line ext2 = new Line(new Point3d(p2.X, p2.Y, 0), new Point3d(dimX - 80, p2.Y, 0)) { LayerId = layer };
                btr.AppendEntity(ext1);
                tr.AddNewlyCreatedDBObject(ext1, true);
                btr.AppendEntity(ext2);
                tr.AddNewlyCreatedDBObject(ext2, true);

                Line dim = new Line(new Point3d(dimX, p1.Y, 0), new Point3d(dimX, p2.Y, 0)) { LayerId = layer };
                btr.AppendEntity(dim);
                tr.AddNewlyCreatedDBObject(dim, true);

                DrawTick(btr, tr, layer, dimX, p1.Y);
                DrawTick(btr, tr, layer, dimX, p2.Y);

                MText mt = new MText
                {
                    Contents = text,
                    Location = new Point3d(dimX - 40, (p1.Y + p2.Y) / 2.0, 0),
                    TextHeight = 125.0,
                    Rotation = Math.PI / 2.0,
                    Attachment = AttachmentPoint.BottomCenter,
                    LayerId = layer,
                    TextStyleId = textStyle
                };
                btr.AppendEntity(mt);
                tr.AddNewlyCreatedDBObject(mt, true);
            }
            catch { }
        }

        private static void DrawTick(BlockTableRecord btr, Transaction tr, ObjectId layer, double x, double y)
        {
            try
            {
                double s = 60.0;
                Line tick = new Line(new Point3d(x - s, y - s, 0), new Point3d(x + s, y + s, 0)) { LayerId = layer };
                btr.AppendEntity(tick);
                tr.AddNewlyCreatedDBObject(tick, true);
            }
            catch { }
        }

        private static void DrawElectricalOverlay(BlockTableRecord btr, Transaction tr, ObjectId lightLayer, ObjectId wireLayer, ObjectId panelLayer, ObjectId textStyle, double ox, double oy, double L, double W, HousePlanConfig cfg, Database db)
        {
            try
            {
                if (cfg.PlaceDbPanel)
                {
                    DrawRect(btr, tr, panelLayer, ox + 150, oy + 200, 450, 140);
                    DrawMText(btr, tr, panelLayer, textStyle, "TỦ ĐIỆN DB", new Point3d(ox + 375, oy + 380, 0), 80.0, AttachmentPoint.BottomCenter);
                }

                int count = Math.Max(2, cfg.LightCount);
                int cols = Math.Max(2, (int)Math.Round(Math.Sqrt(count * (L / Math.Max(100.0, W)))));
                int rows = Math.Max(1, (int)Math.Ceiling((double)count / cols));

                List<Point2d> lights = new List<Point2d>();
                double dx = (L - 1000.0) / (cols + 1);
                double dy = (W - 1000.0) / (rows + 1);

                for (int r = 1; r <= rows; r++)
                {
                    for (int c = 1; c <= cols; c++)
                    {
                        if (lights.Count >= count) break;
                        Point2d lp = new Point2d(ox + 500.0 + c * dx, oy + 500.0 + r * dy);
                        lights.Add(lp);

                        Circle c1 = new Circle(new Point3d(lp.X, lp.Y, 0), Vector3d.ZAxis, 90.0) { LayerId = lightLayer };
                        Circle c2 = new Circle(new Point3d(lp.X, lp.Y, 0), Vector3d.ZAxis, 45.0) { LayerId = lightLayer };
                        btr.AppendEntity(c1);
                        tr.AddNewlyCreatedDBObject(c1, true);
                        btr.AppendEntity(c2);
                        tr.AddNewlyCreatedDBObject(c2, true);
                    }
                }

                Point2d swPt = new Point2d(ox + 1350.0, oy + 80.0);
                Circle swC = new Circle(new Point3d(swPt.X, swPt.Y, 0), Vector3d.ZAxis, 60.0) { LayerId = lightLayer };
                Line swTail = new Line(new Point3d(swPt.X + 60, swPt.Y, 0), new Point3d(swPt.X + 120, swPt.Y + 45, 0)) { LayerId = lightLayer };
                btr.AppendEntity(swC);
                tr.AddNewlyCreatedDBObject(swC, true);
                btr.AppendEntity(swTail);
                tr.AddNewlyCreatedDBObject(swTail, true);
                DrawMText(btr, tr, lightLayer, textStyle, "S2", new Point3d(swPt.X + 140, swPt.Y + 45, 0), 70.0, AttachmentPoint.MiddleLeft);

                if (lights.Count > 0)
                {
                    DrawArcWiring(btr, tr, wireLayer, swPt, lights[0], db);
                    for (int i = 0; i < lights.Count - 1; i++)
                    {
                        DrawArcWiring(btr, tr, wireLayer, lights[i], lights[i + 1], db);
                    }
                }
            }
            catch { }
        }

        private static void DrawArcWiring(BlockTableRecord btr, Transaction tr, ObjectId layer, Point2d p1, Point2d p2, Database db)
        {
            try
            {
                Polyline arc = new Polyline();
                arc.AddVertexAt(0, p1, 0.22, 0, 0);
                arc.AddVertexAt(1, p2, 0, 0, 0);
                arc.LayerId = layer;
                CadUtils.SafeSetLinetype(arc, db, "HIDDEN");
                btr.AppendEntity(arc);
                tr.AddNewlyCreatedDBObject(arc, true);
            }
            catch
            {
                try
                {
                    Line l = new Line(new Point3d(p1.X, p1.Y, 0), new Point3d(p2.X, p2.Y, 0)) { LayerId = layer };
                    btr.AppendEntity(l);
                    tr.AddNewlyCreatedDBObject(l, true);
                }
                catch { }
            }
        }

        private static void DrawRect(BlockTableRecord btr, Transaction tr, ObjectId layer, double x, double y, double w, double h)
        {
            try
            {
                Polyline p = new Polyline();
                p.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
                p.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
                p.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
                p.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
                p.Closed = true;
                p.LayerId = layer;
                btr.AppendEntity(p);
                tr.AddNewlyCreatedDBObject(p, true);
            }
            catch { }
        }

        private static void DrawMText(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, string text, Point3d pos, double height, AttachmentPoint ap)
        {
            try
            {
                MText mt = new MText
                {
                    Contents = text,
                    Location = pos,
                    TextHeight = height,
                    Attachment = ap,
                    LayerId = layer,
                    TextStyleId = textStyle
                };
                btr.AppendEntity(mt);
                tr.AddNewlyCreatedDBObject(mt, true);
            }
            catch { }
        }
    }
}
