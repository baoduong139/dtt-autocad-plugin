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
    public enum RoomShapeKind
    {
        Rectangle,
        LShape,
        TShape,
        PickBoundary
    }

    public enum LightingFixtureType
    {
        DownlightLed,   // 12W D110
        TubeLedDouble,  // 2x18W 1.2m
        PanelLed600,    // 600x600 48W
        ExitEmergency   // Đèn thoát hiểm
    }

    public enum DoorStyleType
    {
        Single900,
        Double1200,
        Sliding1000
    }

    public enum WiringRouteStyle
    {
        CurvedArc,     // Nét vòng cung mềm mại MEP
        Orthogonal     // Nét vuông góc
    }

    public class FloorPlanConfig
    {
        // 1. HÌNH DÁNG & KÍCH THƯỚC PHÒNG
        public RoomShapeKind Shape { get; set; } = RoomShapeKind.Rectangle;
        public string RoomName { get; set; } = "PHÒNG LÀM VIỆC CHÍNH";
        public double RoomLength { get; set; } = 6000.0; // mm
        public double RoomWidth { get; set; } = 4000.0;  // mm
        public double CeilingHeight { get; set; } = 2800.0; // mm

        // Kích thước phòng chữ L / T
        public double BranchLength { get; set; } = 3000.0; // mm
        public double BranchWidth { get; set; } = 2500.0;  // mm

        // Tường & Cửa
        public double WallThickness { get; set; } = 220.0; // 220mm hoặc 110mm
        public bool HasDoor { get; set; } = true;
        public DoorStyleType DoorStyle { get; set; } = DoorStyleType.Single900;
        public bool HasWindow { get; set; } = true;
        public double WindowWidth { get; set; } = 1500.0;

        // 2. HỆ THỐNG CHIẾU SÁNG (LIGHTING)
        public LightingFixtureType LightType { get; set; } = LightingFixtureType.DownlightLed;
        public bool UseTargetLux { get; set; } = true;
        public double TargetLux { get; set; } = 400.0; // Lux (300-500)
        public int ManualLightCount { get; set; } = 6;
        public bool HasExitLight { get; set; } = true;

        // 3. HỆ THỐNG Ổ CẮM & ĐỘNG LỰC (POWER)
        public bool AutoSocketPerimeter { get; set; } = true;
        public double SocketSpacing { get; set; } = 2500.0; // mm
        public bool HasAcUnit { get; set; } = true;
        public bool HasCeilingFan { get; set; } = false;
        public bool HasWaterHeater { get; set; } = false;

        // 4. HỆ THỐNG ĐIỆN NHẸ (ELV)
        public bool HasDataLan { get; set; } = true;
        public bool HasTvOutlet { get; set; } = true;
        public bool HasSmokeDetector { get; set; } = true;
        public bool HasSecurityCamera { get; set; } = true;

        // 5. CÔNG TẮC & ĐI DÂY (WIRING & CIRCUITING)
        public int SwitchGangs { get; set; } = 2; // Công tắc 2 hạt
        public WiringRouteStyle WiringStyle { get; set; } = WiringRouteStyle.CurvedArc;
        public bool DrawHomeRun { get; set; } = true;
        public bool PlaceDbPanel { get; set; } = true;
        public string DbPanelTag { get; set; } = "DB-FLOOR-01";
    }

    public static class FloorPlanElectricalEngine
    {
        // ============================================================
        // HÀM CHÍNH SINH TOÀN BỘ MẶT BẰNG ĐIỆN PRO
        // ============================================================

        public static bool GenerateFloorPlan(Document doc, FloorPlanConfig cfg, Point3d startPt, Polyline boundaryPoly = null)
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

                    // 1. TẠO LAYER CHUẨN KỸ THUẬT MEP & KIẾN TRÚC
                    ObjectId wallLayer = CadUtils.GetOrCreateLayer(db, "A-WALL", 7, LineWeight.LineWeight035);
                    ObjectId doorLayer = CadUtils.GetOrCreateLayer(db, "A-DOOR", 3, LineWeight.LineWeight025);
                    ObjectId windowLayer = CadUtils.GetOrCreateLayer(db, "A-GLAZ", 4, LineWeight.LineWeight025);
                    ObjectId roomTextLayer = CadUtils.GetOrCreateLayer(db, "A-ROOM-TEXT", 7, LineWeight.LineWeight020);

                    ObjectId lightFixtureLayer = CadUtils.GetOrCreateLayer(db, "E-LIGHT-FIXTURE", 2, LineWeight.LineWeight035);
                    ObjectId lightWireLayer = CadUtils.GetOrCreateLayer(db, "E-LIGHT-WIRE", 30, LineWeight.LineWeight025);
                    ObjectId lightSwitchLayer = CadUtils.GetOrCreateLayer(db, "E-LIGHT-SWITCH", 5, LineWeight.LineWeight030);

                    ObjectId powerOutletLayer = CadUtils.GetOrCreateLayer(db, "E-POWER-OUTLET", 1, LineWeight.LineWeight035);
                    ObjectId powerWireLayer = CadUtils.GetOrCreateLayer(db, "E-POWER-WIRE", 1, LineWeight.LineWeight025);
                    ObjectId panelLayer = CadUtils.GetOrCreateLayer(db, "E-PANEL-DB", 4, LineWeight.LineWeight050);

                    ObjectId elvLayer = CadUtils.GetOrCreateLayer(db, "E-ELV-SYS", 6, LineWeight.LineWeight030);
                    ObjectId fireLayer = CadUtils.GetOrCreateLayer(db, "E-ELV-FIRE", 10, LineWeight.LineWeight030);
                    ObjectId cctvLayer = CadUtils.GetOrCreateLayer(db, "E-ELV-CCTV", 210, LineWeight.LineWeight030);
                    ObjectId dimLayer = CadUtils.GetOrCreateLayer(db, "A-DIM", 8, LineWeight.LineWeight018);

                    ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                    // 2. XÁC ĐỊNH RANH GIỚI VÀ VẼ TƯỜNG (WALLS & ROOM ENVELOPE)
                    List<Point2d> boundaryPts = new List<Point2d>();
                    double origX = startPt.X;
                    double origY = startPt.Y;
                    double w = cfg.RoomWidth;
                    double l = cfg.RoomLength;
                    double wallT = cfg.WallThickness;

                    List<Point2d> innerBoundaryPts = new List<Point2d>();

                    if (cfg.Shape == RoomShapeKind.LShape)
                    {
                        // Phòng chữ L chuẩn kiến trúc
                        double bL = cfg.BranchLength > 0 ? cfg.BranchLength : l * 0.5;
                        double bW = cfg.BranchWidth > 0 ? cfg.BranchWidth : w * 0.5;
                        boundaryPts.Add(new Point2d(origX, origY));
                        boundaryPts.Add(new Point2d(origX + l, origY));
                        boundaryPts.Add(new Point2d(origX + l, origY + bW));
                        boundaryPts.Add(new Point2d(origX + bL, origY + bW));
                        boundaryPts.Add(new Point2d(origX + bL, origY + w));
                        boundaryPts.Add(new Point2d(origX, origY + w));

                        innerBoundaryPts.Add(new Point2d(origX + wallT, origY + wallT));
                        innerBoundaryPts.Add(new Point2d(origX + l - wallT, origY + wallT));
                        innerBoundaryPts.Add(new Point2d(origX + l - wallT, origY + bW - wallT));
                        innerBoundaryPts.Add(new Point2d(origX + bL + wallT, origY + bW - wallT));
                        innerBoundaryPts.Add(new Point2d(origX + bL + wallT, origY + w - wallT));
                        innerBoundaryPts.Add(new Point2d(origX + wallT, origY + w - wallT));
                    }
                    else if (cfg.Shape == RoomShapeKind.TShape)
                    {
                        // Phòng chữ T chuẩn kiến trúc
                        double bL = cfg.BranchLength > 0 ? cfg.BranchLength : l * 0.5;
                        double bW = cfg.BranchWidth > 0 ? cfg.BranchWidth : w * 0.5;
                        double stepX = (l - bL) / 2.0;
                        boundaryPts.Add(new Point2d(origX + stepX, origY));
                        boundaryPts.Add(new Point2d(origX + stepX + bL, origY));
                        boundaryPts.Add(new Point2d(origX + stepX + bL, origY + bW));
                        boundaryPts.Add(new Point2d(origX + l, origY + bW));
                        boundaryPts.Add(new Point2d(origX + l, origY + w));
                        boundaryPts.Add(new Point2d(origX, origY + w));
                        boundaryPts.Add(new Point2d(origX, origY + bW));
                        boundaryPts.Add(new Point2d(origX + stepX, origY + bW));

                        innerBoundaryPts.Add(new Point2d(origX + stepX + wallT, origY + wallT));
                        innerBoundaryPts.Add(new Point2d(origX + stepX + bL - wallT, origY + wallT));
                        innerBoundaryPts.Add(new Point2d(origX + stepX + bL - wallT, origY + bW + wallT));
                        innerBoundaryPts.Add(new Point2d(origX + l - wallT, origY + bW + wallT));
                        innerBoundaryPts.Add(new Point2d(origX + l - wallT, origY + w - wallT));
                        innerBoundaryPts.Add(new Point2d(origX + wallT, origY + w - wallT));
                        innerBoundaryPts.Add(new Point2d(origX + wallT, origY + bW + wallT));
                        innerBoundaryPts.Add(new Point2d(origX + stepX + wallT, origY + bW + wallT));
                    }
                    else if (cfg.Shape == RoomShapeKind.PickBoundary && boundaryPoly != null)
                    {
                        // Đọc đỉnh từ Polyline người dùng chọn
                        for (int i = 0; i < boundaryPoly.NumberOfVertices; i++)
                        {
                            boundaryPts.Add(boundaryPoly.GetPoint2dAt(i));
                        }
                    }
                    else // Mặc định: Hình Chữ Nhật
                    {
                        boundaryPts.Add(new Point2d(origX, origY));
                        boundaryPts.Add(new Point2d(origX + l, origY));
                        boundaryPts.Add(new Point2d(origX + l, origY + w));
                        boundaryPts.Add(new Point2d(origX, origY + w));

                        innerBoundaryPts.Add(new Point2d(origX + wallT, origY + wallT));
                        innerBoundaryPts.Add(new Point2d(origX + l - wallT, origY + wallT));
                        innerBoundaryPts.Add(new Point2d(origX + l - wallT, origY + w - wallT));
                        innerBoundaryPts.Add(new Point2d(origX + wallT, origY + w - wallT));
                    }

                    // Vẽ đường bao tường ngoài & tường trong
                    DrawWallBoundary(btr, tr, wallLayer, boundaryPts, innerBoundaryPts, wallT);

                    // 3. TRỔ CỬA ĐI & CỬA SỔ
                    Point2d doorPt = boundaryPts[0];
                    double doorWidth = (cfg.DoorStyle == DoorStyleType.Double1200) ? 1200.0 : 900.0;
                    if (cfg.HasDoor)
                    {
                        doorPt = new Point2d(boundaryPts[0].X + 400.0, boundaryPts[0].Y); // Cửa ở chân tường trước
                        DrawDoorSymbol(btr, tr, doorLayer, doorPt, doorWidth, cfg.DoorStyle, 0.0);
                    }

                    // Cửa sổ
                    if (cfg.HasWindow)
                    {
                        Point2d winPt = new Point2d(boundaryPts[0].X + l / 2.0 - cfg.WindowWidth / 2.0, boundaryPts[0].Y + w);
                        DrawWindowSymbol(btr, tr, windowLayer, winPt, cfg.WindowWidth, wallT);
                    }

                    // Tên phòng & diện tích
                    double roomAreaM2 = (l * w) / 1000000.0;
                    AddText(btr, tr, roomTextLayer, textStyle, cfg.RoomName.ToUpper(), origX + l / 2.0, origY + w / 2.0 + 80.0, 180.0, AttachmentPoint.MiddleCenter);
                    AddText(btr, tr, roomTextLayer, textStyle, $"S = {roomAreaM2:F1} m² | H = {cfg.CeilingHeight / 1000.0:F2}m", origX + l / 2.0, origY + w / 2.0 - 120.0, 120.0, AttachmentPoint.MiddleCenter);

                    // 4. BỐ TRÍ TỦ ĐIỆN TỔNG DB (DISTRIBUTION BOARD)
                    Point2d dbPos = new Point2d(boundaryPts[0].X + 100.0, boundaryPts[0].Y + 150.0);
                    if (cfg.PlaceDbPanel)
                    {
                        DrawDbPanelBlock(btr, tr, panelLayer, textStyle, dbPos, cfg.DbPanelTag);
                    }
                    else
                    {
                        // Nếu không đặt tủ DB trong phòng, hướng dây nguồn ra cửa / hành lang ngoài
                        dbPos = new Point2d(origX - 300.0, origY + w * 0.3);
                    }

                    // 5. TÍNH TOÁN & RẢI LƯỚI ĐÈN CHIẾU SÁNG
                    int lightCount = cfg.ManualLightCount;
                    if (cfg.UseTargetLux)
                    {
                        // Công thức kỹ thuật: N = (E * S) / (F * U * K)
                        double lumenPerFix = (cfg.LightType == LightingFixtureType.DownlightLed) ? 1100.0 :
                                             (cfg.LightType == LightingFixtureType.PanelLed600) ? 4200.0 : 3600.0;
                        double u = 0.65; // Hệ số lợi dụng
                        double k = 0.80; // Hệ số duy trì
                        double totalLumenReq = (cfg.TargetLux * roomAreaM2) / (u * k);
                        lightCount = Math.Max(2, (int)Math.Ceiling(totalLumenReq / lumenPerFix));
                    }

                    // Tính số cột và hàng đèn cân đối với tỷ lệ L / W
                    int cols = Math.Max(2, (int)Math.Round(Math.Sqrt(lightCount * (l / Math.Max(1.0, w)))));
                    int rows = Math.Max(1, (int)Math.Ceiling((double)lightCount / cols));

                    List<Point2d> lightPositions = new List<Point2d>();
                    double dx = (l - 800.0) / (cols + 1);
                    double dy = (w - 800.0) / (rows + 1);

                    for (int r = 1; r <= rows; r++)
                    {
                        for (int c = 1; c <= cols; c++)
                        {
                            if (lightPositions.Count >= lightCount) break;
                            double lx = origX + 400.0 + c * dx;
                            double ly = origY + 400.0 + r * dy;
                            Point2d lp = new Point2d(lx, ly);
                            lightPositions.Add(lp);
                            DrawLightFixture(btr, tr, lightFixtureLayer, lp, cfg.LightType);
                        }
                    }

                    // Đèn Exit thoát hiểm
                    if (cfg.HasExitLight)
                    {
                        Point2d exitPt = cfg.HasDoor ? new Point2d(doorPt.X + doorWidth / 2.0, doorPt.Y + 20.0) : new Point2d(origX + l / 2.0, origY + 20.0);
                        DrawExitLight(btr, tr, lightFixtureLayer, textStyle, exitPt);
                    }

                    // 6. ĐẶT CÔNG TẮC & ĐI DÂY CHIẾU SÁNG (WIRING)
                    Point2d switchPt = cfg.HasDoor ? new Point2d(doorPt.X + doorWidth + 150.0, doorPt.Y + 60.0) : new Point2d(origX + 350.0, origY + 60.0);
                    DrawSwitchSymbol(btr, tr, lightSwitchLayer, textStyle, switchPt, cfg.SwitchGangs);

                    // Vẽ tuyến dây nối từ công tắc đến bóng đầu tiên, rồi loop qua các bóng
                    if (lightPositions.Count > 0)
                    {
                        bool isOrtho = (cfg.WiringStyle == WiringRouteStyle.Orthogonal);
                        if (isOrtho)
                        {
                            DrawWiringOrthogonal(btr, tr, lightWireLayer, switchPt, lightPositions[0], db);
                            for (int i = 0; i < lightPositions.Count - 1; i++)
                            {
                                DrawWiringOrthogonal(btr, tr, lightWireLayer, lightPositions[i], lightPositions[i + 1], db);
                            }
                        }
                        else
                        {
                            DrawWiringArc(btr, tr, lightWireLayer, switchPt, lightPositions[0], db);
                            for (int i = 0; i < lightPositions.Count - 1; i++)
                            {
                                DrawWiringArc(btr, tr, lightWireLayer, lightPositions[i], lightPositions[i + 1], db);
                            }
                        }

                        // Mũi tên Home-run lộ N1 về tủ DB
                        if (cfg.DrawHomeRun)
                        {
                            string dest = cfg.PlaceDbPanel ? "-> TỦ DB" : "-> NGUỒN NGOÀI";
                            DrawHomeRunArrow(btr, tr, lightWireLayer, textStyle, lightPositions[0], dbPos, $"N1: 2x1.5+E1.5/D20 {dest}");
                        }
                    }

                    // 7. BỐ TRÍ Ổ CẮM & ĐỘNG LỰC (SOCKETS & POWER)
                    List<Point2d> socketPositions = new List<Point2d>();
                    if (cfg.AutoSocketPerimeter)
                    {
                        double sStep = cfg.SocketSpacing;
                        // Tường dưới
                        for (double sx = origX + 1200.0; sx < origX + l - 600.0; sx += sStep)
                        {
                            Point2d sp = new Point2d(sx, origY + 50.0);
                            socketPositions.Add(sp);
                            DrawSocketSymbol(btr, tr, powerOutletLayer, sp, 0.0);
                        }
                        // Tường trên
                        for (double sx = origX + 800.0; sx < origX + l - 800.0; sx += sStep)
                        {
                            Point2d sp = new Point2d(sx, origY + w - 50.0);
                            socketPositions.Add(sp);
                            DrawSocketSymbol(btr, tr, powerOutletLayer, sp, Math.PI);
                        }
                        // Tường trái & phải
                        for (double sy = origY + 1200.0; sy < origY + w - 800.0; sy += sStep)
                        {
                            Point2d spL = new Point2d(origX + 50.0, sy);
                            socketPositions.Add(spL);
                            DrawSocketSymbol(btr, tr, powerOutletLayer, spL, Math.PI / 2.0);

                            Point2d spR = new Point2d(origX + l - 50.0, sy);
                            socketPositions.Add(spR);
                            DrawSocketSymbol(btr, tr, powerOutletLayer, spR, -Math.PI / 2.0);
                        }

                        // Loop dây ổ cắm lộ N2
                        if (socketPositions.Count > 1)
                        {
                            for (int i = 0; i < socketPositions.Count - 1; i++)
                            {
                                DrawWiringLine(btr, tr, powerWireLayer, socketPositions[i], socketPositions[i + 1], db);
                            }
                            if (cfg.DrawHomeRun)
                            {
                                string dest = cfg.PlaceDbPanel ? "-> TỦ DB" : "-> NGUỒN NGOÀI";
                                DrawHomeRunArrow(btr, tr, powerWireLayer, textStyle, socketPositions[0], dbPos, $"N2: 3x2.5/D20 {dest}");
                            }
                        }
                    }

                    // Thiết bị động lực riêng: Điều hòa (AC), Quạt trần, Bình nóng lạnh
                    if (cfg.HasAcUnit)
                    {
                        Point2d acPt = new Point2d(origX + l - 400.0, origY + w - 150.0);
                        DrawAcUnitSymbol(btr, tr, powerOutletLayer, textStyle, acPt);
                        if (cfg.DrawHomeRun)
                        {
                            string dest = cfg.PlaceDbPanel ? "-> TỦ DB" : "-> NGUỒN NGOÀI";
                            DrawHomeRunArrow(btr, tr, powerWireLayer, textStyle, acPt, dbPos, $"N3 (AC): 3x4.0/D25 {dest}");
                        }
                    }

                    if (cfg.HasCeilingFan)
                    {
                        Point2d fanPt = new Point2d(origX + l / 2.0, origY + w / 2.0);
                        DrawCeilingFanSymbol(btr, tr, lightFixtureLayer, textStyle, fanPt);
                    }

                    if (cfg.HasWaterHeater)
                    {
                        Point2d whPt = new Point2d(origX + 250.0, origY + w - 250.0);
                        DrawWaterHeaterSymbol(btr, tr, powerOutletLayer, textStyle, whPt);
                        if (cfg.DrawHomeRun)
                        {
                            string dest = cfg.PlaceDbPanel ? "-> TỦ DB" : "-> NGUỒN NGOÀI";
                            DrawHomeRunArrow(btr, tr, powerWireLayer, textStyle, whPt, dbPos, $"N4 (BÌNH NÓNG): 3x2.5/D20 {dest}");
                        }
                    }

                    // 8. HỆ THỐNG ĐIỆN NHẸ (ELV: LAN, TV, BÁO KHÓI, CAMERA)
                    if (cfg.HasDataLan)
                    {
                        Point2d lanPt = new Point2d(origX + 1600.0, origY + 50.0);
                        DrawLanOutletSymbol(btr, tr, elvLayer, textStyle, lanPt);
                    }

                    if (cfg.HasTvOutlet)
                    {
                        Point2d tvPt = new Point2d(origX + 2000.0, origY + 50.0);
                        DrawTvOutletSymbol(btr, tr, elvLayer, textStyle, tvPt);
                    }

                    if (cfg.HasSmokeDetector)
                    {
                        Point2d smokePt = new Point2d(origX + l * 0.75, origY + w * 0.75);
                        DrawSmokeDetectorSymbol(btr, tr, fireLayer, textStyle, smokePt);
                    }

                    if (cfg.HasSecurityCamera)
                    {
                        Point2d camPt = new Point2d(origX + l - 100.0, origY + 100.0);
                        DrawCctvSymbol(btr, tr, cctvLayer, textStyle, camPt);
                    }

                    // 9. GHI CHÚ BẢNG LỘ CẤP NGUỒN (CHỈ VẼ KHI NGƯỜI DÙNG BẬT TỦ DB)
                    if (cfg.PlaceDbPanel)
                    {
                        DrawCircuitScheduleSummary(btr, tr, panelLayer, textStyle, new Point2d(origX + l + 500.0, origY + w), cfg, lightCount, socketPositions.Count);
                    }

                    tr.Commit();
                }

                ed.WriteMessage($"\n✅ [DTools PRO] Đã hoàn thành vẽ mặt bằng điện 2D [{cfg.RoomName}] với đầy đủ thiết bị chiếu sáng, ổ cắm, điện nhẹ, đi dây loop và tủ DB!\n");
                return true;
            }
            catch (System.Exception ex)
            {
                ed?.WriteMessage($"\n[DTools LỖI] Sinh mặt bằng điện thất bại: {ex.Message}\n");
                return false;
            }
        }

        // ============================================================
        // CÁC HÀM HÌNH HỌC & ĐỒ HỌA BLOCK VECTOR (GRAPHICS ENGINES)
        // ============================================================

        private static void DrawWallBoundary(BlockTableRecord btr, Transaction tr, ObjectId wallLayer, List<Point2d> outerPts, List<Point2d> innerPts, double wallT)
        {
            if (outerPts.Count < 3) return;

            // Polyline ngoài
            Polyline pOut = new Polyline();
            for (int i = 0; i < outerPts.Count; i++)
            {
                pOut.AddVertexAt(i, outerPts[i], 0, 0, 0);
            }
            pOut.Closed = true;
            pOut.LayerId = wallLayer;
            btr.AppendEntity(pOut);
            tr.AddNewlyCreatedDBObject(pOut, true);

            // Polyline trong
            if (innerPts != null && innerPts.Count >= 3)
            {
                Polyline pIn = new Polyline();
                for (int i = 0; i < innerPts.Count; i++)
                {
                    pIn.AddVertexAt(i, innerPts[i], 0, 0, 0);
                }
                pIn.Closed = true;
                pIn.LayerId = wallLayer;
                btr.AppendEntity(pIn);
                tr.AddNewlyCreatedDBObject(pIn, true);
            }
            else
            {
                // Fallback offset cho PickBoundary
                try
                {
                    var offCurves = pOut.GetOffsetCurves(-wallT);
                    if (offCurves == null || offCurves.Count == 0)
                        offCurves = pOut.GetOffsetCurves(wallT);

                    if (offCurves != null && offCurves.Count > 0)
                    {
                        foreach (Entity ent in offCurves)
                        {
                            ent.LayerId = wallLayer;
                            btr.AppendEntity(ent);
                            tr.AddNewlyCreatedDBObject(ent, true);
                        }
                    }
                }
                catch { }
            }
        }

        private static void DrawDoorSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, Point2d pt, double width, DoorStyleType style, double rot)
        {
            // Cánh cửa và cung mở cửa
            Line doorLeaf = new Line(new Point3d(pt.X, pt.Y, 0), new Point3d(pt.X, pt.Y + width, 0)) { LayerId = layer };
            btr.AppendEntity(doorLeaf);
            tr.AddNewlyCreatedDBObject(doorLeaf, true);

            Arc swing = new Arc(new Point3d(pt.X, pt.Y, 0), width, 0, Math.PI / 2.0) { LayerId = layer };
            btr.AppendEntity(swing);
            tr.AddNewlyCreatedDBObject(swing, true);
        }

        private static void DrawWindowSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, Point2d pt, double width, double wallT)
        {
            // 4 nét kính cửa sổ
            DrawRect(btr, tr, layer, pt.X, pt.Y - wallT, width, wallT);
            Line glass1 = new Line(new Point3d(pt.X, pt.Y - wallT * 0.35, 0), new Point3d(pt.X + width, pt.Y - wallT * 0.35, 0)) { LayerId = layer };
            Line glass2 = new Line(new Point3d(pt.X, pt.Y - wallT * 0.65, 0), new Point3d(pt.X + width, pt.Y - wallT * 0.65, 0)) { LayerId = layer };
            btr.AppendEntity(glass1);
            tr.AddNewlyCreatedDBObject(glass1, true);
            btr.AppendEntity(glass2);
            tr.AddNewlyCreatedDBObject(glass2, true);
        }

        private static void DrawLightFixture(BlockTableRecord btr, Transaction tr, ObjectId layer, Point2d pt, LightingFixtureType type)
        {
            double r = 100.0;
            if (type == LightingFixtureType.DownlightLed)
            {
                // Hình tròn Downlight âm trần
                Circle cOut = new Circle(new Point3d(pt.X, pt.Y, 0), Vector3d.ZAxis, r) { LayerId = layer };
                Circle cIn = new Circle(new Point3d(pt.X, pt.Y, 0), Vector3d.ZAxis, r * 0.5) { LayerId = layer };
                btr.AppendEntity(cOut);
                tr.AddNewlyCreatedDBObject(cOut, true);
                btr.AppendEntity(cIn);
                tr.AddNewlyCreatedDBObject(cIn, true);

                // Dấu gạch chéo
                Line l1 = new Line(new Point3d(pt.X - r * 0.7, pt.Y - r * 0.7, 0), new Point3d(pt.X + r * 0.7, pt.Y + r * 0.7, 0)) { LayerId = layer };
                Line l2 = new Line(new Point3d(pt.X - r * 0.7, pt.Y + r * 0.7, 0), new Point3d(pt.X + r * 0.7, pt.Y - r * 0.7, 0)) { LayerId = layer };
                btr.AppendEntity(l1);
                tr.AddNewlyCreatedDBObject(l1, true);
                btr.AppendEntity(l2);
                tr.AddNewlyCreatedDBObject(l2, true);
            }
            else if (type == LightingFixtureType.PanelLed600)
            {
                // Hình vuông Panel 600x600
                DrawRect(btr, tr, layer, pt.X - 250.0, pt.Y - 250.0, 500.0, 500.0);
                Line l1 = new Line(new Point3d(pt.X - 250.0, pt.Y - 250.0, 0), new Point3d(pt.X + 250.0, pt.Y + 250.0, 0)) { LayerId = layer };
                Line l2 = new Line(new Point3d(pt.X - 250.0, pt.Y + 250.0, 0), new Point3d(pt.X + 250.0, pt.Y - 250.0, 0)) { LayerId = layer };
                btr.AppendEntity(l1);
                tr.AddNewlyCreatedDBObject(l1, true);
                btr.AppendEntity(l2);
                tr.AddNewlyCreatedDBObject(l2, true);
            }
            else
            {
                // Đèn tuýp LED đôi 1.2m
                DrawRect(btr, tr, layer, pt.X - 600.0, pt.Y - 60.0, 1200.0, 120.0);
            }
        }

        private static void DrawExitLight(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d pt)
        {
            DrawRect(btr, tr, layer, pt.X - 180.0, pt.Y - 50.0, 360.0, 100.0);
            AddText(btr, tr, layer, textStyle, "EXIT", pt.X, pt.Y, 50.0, AttachmentPoint.MiddleCenter);
        }

        private static void DrawSwitchSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d pt, int gangs)
        {
            Circle c = new Circle(new Point3d(pt.X, pt.Y, 0), Vector3d.ZAxis, 60.0) { LayerId = layer };
            btr.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);

            Line tail = new Line(new Point3d(pt.X + 60.0, pt.Y, 0), new Point3d(pt.X + 130.0, pt.Y + 50.0, 0)) { LayerId = layer };
            btr.AppendEntity(tail);
            tr.AddNewlyCreatedDBObject(tail, true);

            AddText(btr, tr, layer, textStyle, $"S{gangs}", pt.X + 160.0, pt.Y + 50.0, 70.0, AttachmentPoint.MiddleLeft);
        }

        private static void DrawSocketSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, Point2d pt, double rot)
        {
            // Bán nguyệt ổ cắm chuẩn TCVN
            Arc arc = new Arc(new Point3d(pt.X, pt.Y, 0), 80.0, rot, rot + Math.PI) { LayerId = layer };
            btr.AppendEntity(arc);
            tr.AddNewlyCreatedDBObject(arc, true);

            Line chord = new Line(
                new Point3d(pt.X - 80.0 * Math.Sin(rot), pt.Y + 80.0 * Math.Cos(rot), 0),
                new Point3d(pt.X + 80.0 * Math.Sin(rot), pt.Y - 80.0 * Math.Cos(rot), 0))
            { LayerId = layer };
            btr.AppendEntity(chord);
            tr.AddNewlyCreatedDBObject(chord, true);
        }

        private static void DrawAcUnitSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d pt)
        {
            DrawRect(btr, tr, layer, pt.X - 400.0, pt.Y - 100.0, 800.0, 200.0);
            AddText(btr, tr, layer, textStyle, "ĐIỀU HÒA (AC)", pt.X, pt.Y, 70.0, AttachmentPoint.MiddleCenter);
        }

        private static void DrawCeilingFanSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d pt)
        {
            Circle c = new Circle(new Point3d(pt.X, pt.Y, 0), Vector3d.ZAxis, 80.0) { LayerId = layer };
            btr.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);

            // 3 Cánh quạt
            for (int i = 0; i < 3; i++)
            {
                double a = i * (2.0 * Math.PI / 3.0);
                Line blade = new Line(
                    new Point3d(pt.X, pt.Y, 0),
                    new Point3d(pt.X + 350.0 * Math.Cos(a), pt.Y + 350.0 * Math.Sin(a), 0))
                { LayerId = layer };
                btr.AppendEntity(blade);
                tr.AddNewlyCreatedDBObject(blade, true);
            }
            AddText(btr, tr, layer, textStyle, "QUẠT TRẦN", pt.X, pt.Y - 140.0, 60.0, AttachmentPoint.TopCenter);
        }

        private static void DrawWaterHeaterSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d pt)
        {
            DrawRect(btr, tr, layer, pt.X - 150.0, pt.Y - 150.0, 300.0, 300.0);
            Circle c = new Circle(new Point3d(pt.X, pt.Y, 0), Vector3d.ZAxis, 100.0) { LayerId = layer };
            btr.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);
            AddText(btr, tr, layer, textStyle, "WH", pt.X, pt.Y, 60.0, AttachmentPoint.MiddleCenter);
        }

        private static void DrawLanOutletSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d pt)
        {
            DrawRect(btr, tr, layer, pt.X - 60.0, pt.Y - 60.0, 120.0, 120.0);
            AddText(btr, tr, layer, textStyle, "LAN", pt.X, pt.Y, 45.0, AttachmentPoint.MiddleCenter);
        }

        private static void DrawTvOutletSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d pt)
        {
            DrawRect(btr, tr, layer, pt.X - 60.0, pt.Y - 60.0, 120.0, 120.0);
            AddText(btr, tr, layer, textStyle, "TV", pt.X, pt.Y, 45.0, AttachmentPoint.MiddleCenter);
        }

        private static void DrawSmokeDetectorSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d pt)
        {
            Circle c = new Circle(new Point3d(pt.X, pt.Y, 0), Vector3d.ZAxis, 90.0) { LayerId = layer };
            btr.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);
            AddText(btr, tr, layer, textStyle, "SD", pt.X, pt.Y, 65.0, AttachmentPoint.MiddleCenter);
        }

        private static void DrawCctvSymbol(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d pt)
        {
            Circle c = new Circle(new Point3d(pt.X, pt.Y, 0), Vector3d.ZAxis, 75.0) { LayerId = layer };
            btr.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);

            Line l1 = new Line(new Point3d(pt.X, pt.Y, 0), new Point3d(pt.X - 120.0, pt.Y - 120.0, 0)) { LayerId = layer };
            Line l2 = new Line(new Point3d(pt.X, pt.Y, 0), new Point3d(pt.X - 60.0, pt.Y - 140.0, 0)) { LayerId = layer };
            btr.AppendEntity(l1);
            tr.AddNewlyCreatedDBObject(l1, true);
            btr.AppendEntity(l2);
            tr.AddNewlyCreatedDBObject(l2, true);

            AddText(btr, tr, layer, textStyle, "CAM", pt.X, pt.Y + 110.0, 55.0, AttachmentPoint.BottomCenter);
        }

        private static void DrawDbPanelBlock(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d pt, string tag)
        {
            // Hộp tủ điện tổng âm tường
            double pw = 500.0;
            double ph = 150.0;
            DrawRect(btr, tr, layer, pt.X, pt.Y, pw, ph);

            // Nửa đen nửa trắng chuẩn ký hiệu Tủ DB TCVN
            Polyline hatchBox = new Polyline();
            hatchBox.AddVertexAt(0, new Point2d(pt.X, pt.Y), 0, 0, 0);
            hatchBox.AddVertexAt(1, new Point2d(pt.X + pw / 2.0, pt.Y), 0, 0, 0);
            hatchBox.AddVertexAt(2, new Point2d(pt.X + pw / 2.0, pt.Y + ph), 0, 0, 0);
            hatchBox.AddVertexAt(3, new Point2d(pt.X, pt.Y + ph), 0, 0, 0);
            hatchBox.Closed = true;
            hatchBox.LayerId = layer;
            btr.AppendEntity(hatchBox);
            tr.AddNewlyCreatedDBObject(hatchBox, true);

            AddText(btr, tr, layer, textStyle, tag, pt.X + pw / 2.0, pt.Y + ph + 60.0, 90.0, AttachmentPoint.BottomCenter);
        }

        private static void DrawWiringArc(BlockTableRecord btr, Transaction tr, ObjectId layer, Point2d p1, Point2d p2, Database db)
        {
            try
            {
                // Tạo cung cong mềm mại với độ vồng thẩm mỹ
                double dist = p1.GetDistanceTo(p2);
                double bulge = 0.25; // Độ cong nhẹ đẹp mắt

                Polyline arcPoly = new Polyline();
                arcPoly.AddVertexAt(0, p1, bulge, 0, 0);
                arcPoly.AddVertexAt(1, p2, 0, 0, 0);
                if (layer != ObjectId.Null && layer.IsValid) arcPoly.LayerId = layer;
                CadUtils.SafeSetLinetype(arcPoly, db, "HIDDEN");

                btr.AppendEntity(arcPoly);
                tr.AddNewlyCreatedDBObject(arcPoly, true);
            }
            catch
            {
                DrawWiringLine(btr, tr, layer, p1, p2, db);
            }
        }

        private static void DrawWiringLine(BlockTableRecord btr, Transaction tr, ObjectId layer, Point2d p1, Point2d p2, Database db)
        {
            try
            {
                Line l = new Line(new Point3d(p1.X, p1.Y, 0), new Point3d(p2.X, p2.Y, 0));
                if (layer != ObjectId.Null && layer.IsValid) l.LayerId = layer;
                CadUtils.SafeSetLinetype(l, db, "HIDDEN");
                btr.AppendEntity(l);
                tr.AddNewlyCreatedDBObject(l, true);
            }
            catch { }
        }

        private static void DrawWiringOrthogonal(BlockTableRecord btr, Transaction tr, ObjectId layer, Point2d p1, Point2d p2, Database db)
        {
            try
            {
                Polyline poly = new Polyline();
                poly.AddVertexAt(0, p1, 0, 0, 0);
                poly.AddVertexAt(1, new Point2d(p2.X, p1.Y), 0, 0, 0);
                poly.AddVertexAt(2, p2, 0, 0, 0);
                if (layer != ObjectId.Null && layer.IsValid) poly.LayerId = layer;
                CadUtils.SafeSetLinetype(poly, db, "HIDDEN");
                btr.AppendEntity(poly);
                tr.AddNewlyCreatedDBObject(poly, true);
            }
            catch
            {
                DrawWiringLine(btr, tr, layer, p1, p2, db);
            }
        }

        private static void DrawHomeRunArrow(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d fromPt, Point2d toPt, string label)
        {
            Vector2d delta = toPt - fromPt;
            if (delta.Length < 1e-4) return;
            Vector2d dir = delta.GetNormal();
            double arrowLen = 400.0;
            Point2d tip = fromPt + dir * arrowLen;

            // Nét dây chính
            Line stem = new Line(new Point3d(fromPt.X, fromPt.Y, 0), new Point3d(tip.X, tip.Y, 0));
            if (layer != ObjectId.Null && layer.IsValid) stem.LayerId = layer;
            btr.AppendEntity(stem);
            tr.AddNewlyCreatedDBObject(stem, true);

            // Mũi tên
            Vector2d perp = new Vector2d(-dir.Y, dir.X);
            Point2d b1 = tip - dir * 100.0 + perp * 40.0;
            Point2d b2 = tip - dir * 100.0 - perp * 40.0;

            Solid arrowSolid = new Solid(new Point3d(tip.X, tip.Y, 0), new Point3d(b1.X, b1.Y, 0), new Point3d(b2.X, b2.Y, 0), new Point3d(tip.X, tip.Y, 0));
            if (layer != ObjectId.Null && layer.IsValid) arrowSolid.LayerId = layer;
            btr.AppendEntity(arrowSolid);
            tr.AddNewlyCreatedDBObject(arrowSolid, true);

            // Nhãn lộ dây
            AddText(btr, tr, layer, textStyle, label, tip.X + 80.0, tip.Y + 40.0, 75.0, AttachmentPoint.MiddleLeft);
        }

        private static void DrawCircuitScheduleSummary(BlockTableRecord btr, Transaction tr, ObjectId layer, ObjectId textStyle, Point2d origin, FloorPlanConfig cfg, int lightCount, int socketCount)
        {
            double ox = origin.X;
            double oy = origin.Y;
            double tableW = 2800.0;
            double rowH = 140.0;

            // Tiêu đề bảng
            DrawRect(btr, tr, layer, ox, oy - rowH, tableW, rowH);
            AddText(btr, tr, layer, textStyle, $"BẢNG TÍNH LỘ ĐIỆN & CÔNG SUẤT: {cfg.RoomName.ToUpper()}", ox + tableW / 2.0, oy - rowH / 2.0, 70.0, AttachmentPoint.MiddleCenter);

            string[] rows = new string[]
            {
                $"Lộ N1 (Chiếu sáng): {lightCount} bộ đèn LED | Dây: Cu/PVC 2x1.5+E1.5 | MCB 1P 10A | P ~ {lightCount * 18}W",
                $"Lộ N2 (Ổ cắm sinh hoạt): {socketCount} ổ cắm đôi | Dây: Cu/PVC 3x2.5 | MCB 1P 20A / RCBO 30mA | P ~ {socketCount * 250}W",
                cfg.HasAcUnit ? "Lộ N3 (Điều hòa Inverter): 1 bộ 12000BTU | Dây: Cu/PVC 3x4.0 | MCB 1P 25A | P ~ 1200W" : "",
                cfg.HasWaterHeater ? "Lộ N4 (Bình nước nóng): 1 bộ 30L | Dây: Cu/PVC 3x2.5 | RCBO 16A 30mA | P ~ 2500W" : "",
                $"Tủ Điện Cấp Nguồn: {cfg.DbPanelTag} (Khối MCCB tổng 40A 2P / SPD chống sét lan truyền)"
            };

            int validRowIdx = 1;
            foreach (var r in rows)
            {
                if (string.IsNullOrEmpty(r)) continue;
                double ry = oy - (validRowIdx + 1) * rowH;
                DrawRect(btr, tr, layer, ox, ry, tableW, rowH);
                AddText(btr, tr, layer, textStyle, r, ox + 60.0, ry + rowH / 2.0, 55.0, AttachmentPoint.MiddleLeft);
                validRowIdx++;
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
            if (layer != ObjectId.Null && layer.IsValid) poly.LayerId = layer;
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
