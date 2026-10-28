using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.Modules
{
    public enum FurnitureRoomType
    {
        LivingRoom,    // Phòng khách & Sofa
        KitchenDining, // Bếp & Bàn ăn
        MasterBedroom, // Phòng ngủ Master
        RestroomWc,    // Phòng vệ sinh & Tắm
        HomeOffice     // Phòng làm việc
    }

    public enum StaircaseType
    {
        UTurn2Flights, // Cầu thang 2 vế chữ U có chiếu nghỉ
        Straight1Flight, // Cầu thang thẳng 1 vế chữ I
        LTurn2Flights  // Cầu thang chữ L đổi hướng 90 độ
    }

    public class StaircaseConfig
    {
        public StaircaseType Type { get; set; } = StaircaseType.UTurn2Flights;
        public int TotalSteps { get; set; } = 21; // 21 bậc (Cung Sinh theo phong thủy)
        public double FlightWidth { get; set; } = 1000.0; // Rộng vế thang 1000mm
        public double TreadDepth { get; set; } = 260.0;   // Chiều rộng mặt bậc 260mm
        public double LandingDepth { get; set; } = 1000.0;// Chiều sâu chiếu nghỉ 1000mm
        public double WellWidth { get; set; } = 150.0;    // Khe hở giữa 2 vế (giếng thang)
        public double FloorHeight { get; set; } = 3600.0; // Chiều cao tầng 3.6m
        public bool ShowStepNumbers { get; set; } = true; // Đánh số thứ tự 1..21
    }

    public static class InteriorAndStaircaseEngine
    {
        // ============================================================
        // 1. THUẬT TOÁN VẼ CẦU THANG PHONG THỦY KIẾN TRÚC
        // ============================================================

        public static void GenerateStaircase(Database db, Point3d insertPt, StaircaseConfig cfg)
        {
            if (db == null || cfg == null) return;

            ObjectId layerStep = CadUtils.GetOrCreateLayer(db, "A-STAIR-STEP", 7);     // Trắng: Nét bậc thang thấy
            ObjectId layerHidden = CadUtils.GetOrCreateLayer(db, "A-STAIR-HIDD", 8);   // Xám: Nét bậc thang khuất
            ObjectId layerRail = CadUtils.GetOrCreateLayer(db, "A-STAIR-RAIL", 2);     // Vàng: Tay vịn lan can
            ObjectId layerPath = CadUtils.GetOrCreateLayer(db, "A-STAIR-PATH", 4);     // Cyan: Đường tim đi & mũi tên
            ObjectId layerText = CadUtils.GetOrCreateLayer(db, "A-STAIR-TEXT", 3);     // Xanh lá: Số bậc
            ObjectId linetypeDashed = CadUtils.EnsureLinetype(db, "DASHED");
            ObjectId styleId = CadUtils.GetOrCreateUnicodeStyle(db);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                double bx = insertPt.X;
                double by = insertPt.Y;

                if (cfg.Type == StaircaseType.UTurn2Flights)
                {
                    DrawUTurnStaircase(btr, tr, bx, by, cfg, layerStep, layerHidden, layerRail, layerPath, layerText, linetypeDashed, styleId);
                }
                else
                {
                    DrawStraightStaircase(btr, tr, bx, by, cfg, layerStep, layerHidden, layerRail, layerPath, layerText, linetypeDashed, styleId);
                }

                tr.Commit();
            }
        }

        private static void DrawUTurnStaircase(BlockTableRecord btr, Transaction tr, double bx, double by, StaircaseConfig cfg,
            ObjectId layerStep, ObjectId layerHidden, ObjectId layerRail, ObjectId layerPath, ObjectId layerText, ObjectId linetypeDashed, ObjectId styleId)
        {
            // Vế 1 đi lên (từ bậc 1 đến bậc chiếu nghỉ), Chiếu nghỉ là 1 bậc, Vế 2 đi tiếp lên tầng
            int flight1Steps = (cfg.TotalSteps - 1) / 2; // Ví dụ 21 bậc -> Vế 1: 10 bậc, Chiếu nghỉ: bậc 11, Vế 2: 10 bậc
            int flight2Steps = cfg.TotalSteps - 1 - flight1Steps;

            double w = cfg.FlightWidth;
            double tread = cfg.TreadDepth;
            double well = cfg.WellWidth;
            double landing = cfg.LandingDepth;
            double flight1Length = flight1Steps * tread;
            double totalWidth = w * 2 + well;

            // 1. Khung bao toàn bộ lồng cầu thang
            Polyline frame = new Polyline();
            frame.AddVertexAt(0, new Point2d(bx, by), 0, 0, 0);
            frame.AddVertexAt(1, new Point2d(bx + flight1Length + landing, by), 0, 0, 0);
            frame.AddVertexAt(2, new Point2d(bx + flight1Length + landing, by + totalWidth), 0, 0, 0);
            frame.AddVertexAt(3, new Point2d(bx, by + totalWidth), 0, 0, 0);
            frame.Closed = true;
            frame.LayerId = layerRail;
            btr.AppendEntity(frame); tr.AddNewlyCreatedDBObject(frame, true);

            // 2. Chiếu nghỉ (Landing)
            double landingX = bx + flight1Length;
            Line landingLine = new Line(new Point3d(landingX, by, 0), new Point3d(landingX, by + totalWidth, 0)) { LayerId = layerStep };
            btr.AppendEntity(landingLine); tr.AddNewlyCreatedDBObject(landingLine, true);

            // Khe giếng thang giữa 2 vế
            Line wellLine1 = new Line(new Point3d(bx, by + w, 0), new Point3d(landingX, by + w, 0)) { LayerId = layerRail };
            Line wellLine2 = new Line(new Point3d(bx, by + w + well, 0), new Point3d(landingX, by + w + well, 0)) { LayerId = layerRail };
            Line wellEnd = new Line(new Point3d(landingX, by + w, 0), new Point3d(landingX, by + w + well, 0)) { LayerId = layerRail };
            btr.AppendEntity(wellLine1); tr.AddNewlyCreatedDBObject(wellLine1, true);
            btr.AppendEntity(wellLine2); tr.AddNewlyCreatedDBObject(wellLine2, true);
            btr.AppendEntity(wellEnd); tr.AddNewlyCreatedDBObject(wellEnd, true);

            // 3. Vẽ các bậc Vế 1 (Đi từ trái sang phải, vế dưới)
            for (int i = 1; i <= flight1Steps; i++)
            {
                double sx = bx + i * tread;
                Line stepLine = new Line(new Point3d(sx, by, 0), new Point3d(sx, by + w, 0)) { LayerId = layerStep };
                btr.AppendEntity(stepLine); tr.AddNewlyCreatedDBObject(stepLine, true);

                if (cfg.ShowStepNumbers)
                {
                    DBText num = new DBText
                    {
                        Position = new Point3d(bx + (i - 0.5) * tread - 20, by + 120, 0),
                        Height = 65,
                        TextString = i.ToString(),
                        TextStyleId = styleId,
                        LayerId = layerText
                    };
                    btr.AppendEntity(num); tr.AddNewlyCreatedDBObject(num, true);
                }
            }

            // Số bậc tại Chiếu nghỉ
            int landingStepNum = flight1Steps + 1;
            if (cfg.ShowStepNumbers)
            {
                DBText txtLand = new DBText
                {
                    Position = new Point3d(landingX + landing / 2 - 40, by + totalWidth / 2 - 30, 0),
                    Height = 70,
                    TextString = $"{landingStepNum}",
                    TextStyleId = styleId,
                    LayerId = layerText
                };
                btr.AppendEntity(txtLand); tr.AddNewlyCreatedDBObject(txtLand, true);
            }

            // 4. Vẽ các bậc Vế 2 (Đi từ phải sang trái, vế trên). Thường sau nét cắt bậc 4-5 là nét khuất
            double cutStepIndex = 4; // Vị trí nét cắt tượng trưng
            for (int j = 1; j <= flight2Steps; j++)
            {
                double sx = landingX - j * tread;
                bool isAboveCut = j > cutStepIndex;

                Line stepLine = new Line(new Point3d(sx, by + w + well, 0), new Point3d(sx, by + totalWidth, 0))
                {
                    LayerId = isAboveCut ? layerHidden : layerStep,
                    LinetypeId = isAboveCut ? linetypeDashed : ObjectId.Null
                };
                btr.AppendEntity(stepLine); tr.AddNewlyCreatedDBObject(stepLine, true);

                int stepNum = landingStepNum + j;
                if (cfg.ShowStepNumbers)
                {
                    DBText num = new DBText
                    {
                        Position = new Point3d(landingX - (j - 0.5) * tread - 20, by + totalWidth - 180, 0),
                        Height = 65,
                        TextString = stepNum.ToString(),
                        TextStyleId = styleId,
                        LayerId = layerText
                    };
                    btr.AppendEntity(num); tr.AddNewlyCreatedDBObject(num, true);
                }
            }

            // 5. Đường tim đi (Walking Path) kèm ký hiệu mũi tên và vòng tròn bắt đầu
            double pathY1 = by + w / 2;
            double pathY2 = by + w + well + w / 2;
            Point3d pStart = new Point3d(bx + 150, pathY1, 0);
            Point3d pTurn1 = new Point3d(landingX + landing / 2, pathY1, 0);
            Point3d pTurn2 = new Point3d(landingX + landing / 2, pathY2, 0);
            Point3d pEnd = new Point3d(bx + 200, pathY2, 0);

            // Vòng tròn bắt đầu
            Circle startCircle = new Circle(pStart, Vector3d.ZAxis, 25) { LayerId = layerPath };
            btr.AppendEntity(startCircle); tr.AddNewlyCreatedDBObject(startCircle, true);

            // Tuyến tim đi
            Line path1 = new Line(new Point3d(pStart.X + 25, pathY1, 0), pTurn1) { LayerId = layerPath };
            Line pathTurn = new Line(pTurn1, pTurn2) { LayerId = layerPath };
            Line path2 = new Line(pTurn2, new Point3d(pEnd.X + 50, pathY2, 0)) { LayerId = layerPath };
            btr.AppendEntity(path1); tr.AddNewlyCreatedDBObject(path1, true);
            btr.AppendEntity(pathTurn); tr.AddNewlyCreatedDBObject(pathTurn, true);
            btr.AppendEntity(path2); tr.AddNewlyCreatedDBObject(path2, true);

            // Mũi tên kết thúc
            Polyline arrow = new Polyline();
            arrow.AddVertexAt(0, new Point2d(pEnd.X + 50, pathY2), 0, 40, 0);
            arrow.AddVertexAt(1, new Point2d(pEnd.X, pathY2), 0, 0, 0);
            arrow.LayerId = layerPath;
            btr.AppendEntity(arrow); tr.AddNewlyCreatedDBObject(arrow, true);

            // Chữ "LÊN" / "UP"
            DBText txtUp = new DBText
            {
                Position = new Point3d(pStart.X + 60, pathY1 + 35, 0),
                Height = 85,
                TextString = "LÊN (UP)",
                TextStyleId = styleId,
                LayerId = layerPath
            };
            btr.AppendEntity(txtUp); tr.AddNewlyCreatedDBObject(txtUp, true);

            // Ký hiệu nét cắt thang (Zigzag breakline) ở giữa
            double cutX = bx + 4 * tread;
            Polyline breakLine = new Polyline();
            breakLine.AddVertexAt(0, new Point2d(cutX - 50, by - 50), 0, 0, 0);
            breakLine.AddVertexAt(1, new Point2d(cutX + 15, by + w * 0.4), 0, 0, 0);
            breakLine.AddVertexAt(2, new Point2d(cutX - 25, by + w * 0.7), 0, 0, 0);
            breakLine.AddVertexAt(3, new Point2d(cutX + 35, by + totalWidth + 50), 0, 0, 0);
            breakLine.LayerId = layerStep;
            btr.AppendEntity(breakLine); tr.AddNewlyCreatedDBObject(breakLine, true);
        }

        private static void DrawStraightStaircase(BlockTableRecord btr, Transaction tr, double bx, double by, StaircaseConfig cfg,
            ObjectId layerStep, ObjectId layerHidden, ObjectId layerRail, ObjectId layerPath, ObjectId layerText, ObjectId linetypeDashed, ObjectId styleId)
        {
            double w = cfg.FlightWidth;
            double tread = cfg.TreadDepth;
            double totalLen = cfg.TotalSteps * tread;

            // Khung thang
            Polyline frame = new Polyline();
            frame.AddVertexAt(0, new Point2d(bx, by), 0, 0, 0);
            frame.AddVertexAt(1, new Point2d(bx + totalLen, by), 0, 0, 0);
            frame.AddVertexAt(2, new Point2d(bx + totalLen, by + w), 0, 0, 0);
            frame.AddVertexAt(3, new Point2d(bx, by + w), 0, 0, 0);
            frame.Closed = true;
            frame.LayerId = layerRail;
            btr.AppendEntity(frame); tr.AddNewlyCreatedDBObject(frame, true);

            for (int i = 1; i < cfg.TotalSteps; i++)
            {
                double sx = bx + i * tread;
                Line step = new Line(new Point3d(sx, by, 0), new Point3d(sx, by + w, 0)) { LayerId = layerStep };
                btr.AppendEntity(step); tr.AddNewlyCreatedDBObject(step, true);

                if (cfg.ShowStepNumbers)
                {
                    DBText num = new DBText
                    {
                        Position = new Point3d(bx + (i - 0.5) * tread - 15, by + 120, 0),
                        Height = 65,
                        TextString = i.ToString(),
                        TextStyleId = styleId,
                        LayerId = layerText
                    };
                    btr.AppendEntity(num); tr.AddNewlyCreatedDBObject(num, true);
                }
            }
        }

        // ============================================================
        // 2. THUẬT TOÁN TỰ ĐỘNG BỐ TRÍ NỘI THẤT HOÀN THIỆN
        // ============================================================

        public static void GenerateRoomFurniture(Database db, Point3d centerPt, FurnitureRoomType roomType, double roomW, double roomH)
        {
            if (db == null) return;

            ObjectId layerFurn = CadUtils.GetOrCreateLayer(db, "A-FURN", 30);      // Màu cam nâu ấm: Nội thất
            ObjectId layerSan = CadUtils.GetOrCreateLayer(db, "A-SANR", 4);        // Cyan: Thiết bị vệ sinh
            ObjectId layerText = CadUtils.GetOrCreateLayer(db, "A-FURN-TEXT", 7);  // Trắng: Nhãn nội thất
            ObjectId styleId = CadUtils.GetOrCreateUnicodeStyle(db);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                double cx = centerPt.X;
                double cy = centerPt.Y;

                switch (roomType)
                {
                    case FurnitureRoomType.LivingRoom:
                        DrawLivingRoomLayout(btr, tr, cx, cy, layerFurn, layerText, styleId);
                        break;
                    case FurnitureRoomType.KitchenDining:
                        DrawKitchenDiningLayout(btr, tr, cx, cy, layerFurn, layerText, styleId);
                        break;
                    case FurnitureRoomType.MasterBedroom:
                        DrawMasterBedroomLayout(btr, tr, cx, cy, layerFurn, layerText, styleId);
                        break;
                    case FurnitureRoomType.RestroomWc:
                        DrawRestroomSanitaryLayout(btr, tr, cx, cy, layerSan, layerText, styleId);
                        break;
                    case FurnitureRoomType.HomeOffice:
                        DrawHomeOfficeLayout(btr, tr, cx, cy, layerFurn, layerText, styleId);
                        break;
                }

                tr.Commit();
            }
        }

        private static void DrawLivingRoomLayout(BlockTableRecord btr, Transaction tr, double cx, double cy, ObjectId layerFurn, ObjectId layerText, ObjectId styleId)
        {
            // 1. Sofa Góc L (2800 x 1800 x sâu 900)
            double sofaX = cx - 1200;
            double sofaY = cy - 800;

            Polyline sofaL = new Polyline();
            sofaL.AddVertexAt(0, new Point2d(sofaX, sofaY), 0, 0, 0);
            sofaL.AddVertexAt(1, new Point2d(sofaX + 2600, sofaY), 0, 0, 0);
            sofaL.AddVertexAt(2, new Point2d(sofaX + 2600, sofaY + 900), 0, 0, 0);
            sofaL.AddVertexAt(3, new Point2d(sofaX + 900, sofaY + 900), 0, 0, 0);
            sofaL.AddVertexAt(4, new Point2d(sofaX + 900, sofaY + 1800), 0, 0, 0);
            sofaL.AddVertexAt(5, new Point2d(sofaX, sofaY + 1800), 0, 0, 0);
            sofaL.Closed = true;
            sofaL.LayerId = layerFurn;
            btr.AppendEntity(sofaL); tr.AddNewlyCreatedDBObject(sofaL, true);

            // Đệm lưng ghế tựa
            Line back1 = new Line(new Point3d(sofaX + 180, sofaY + 180, 0), new Point3d(sofaX + 2450, sofaY + 180, 0)) { LayerId = layerFurn };
            Line back2 = new Line(new Point3d(sofaX + 180, sofaY + 180, 0), new Point3d(sofaX + 180, sofaY + 1650, 0)) { LayerId = layerFurn };
            btr.AppendEntity(back1); tr.AddNewlyCreatedDBObject(back1, true);
            btr.AppendEntity(back2); tr.AddNewlyCreatedDBObject(back2, true);

            // 2. Bàn trà đôi (Coffee Table tròn hoặc chữ nhật bo góc 1200x600)
            Polyline coffeeTable = new Polyline();
            double tx = cx - 100;
            double ty = cy - 100;
            coffeeTable.AddVertexAt(0, new Point2d(tx - 600, ty - 300), 0.2, 0, 0);
            coffeeTable.AddVertexAt(1, new Point2d(tx + 600, ty - 300), 0.2, 0, 0);
            coffeeTable.AddVertexAt(2, new Point2d(tx + 600, ty + 300), 0.2, 0, 0);
            coffeeTable.AddVertexAt(3, new Point2d(tx - 600, ty + 300), 0.2, 0, 0);
            coffeeTable.Closed = true;
            coffeeTable.LayerId = layerFurn;
            btr.AppendEntity(coffeeTable); tr.AddNewlyCreatedDBObject(coffeeTable, true);

            // 3. Kệ TV & Màn hình TV 65 inch
            double tvY = cy + 1100;
            Polyline tvStand = new Polyline();
            tvStand.AddVertexAt(0, new Point2d(cx - 1000, tvY), 0, 0, 0);
            tvStand.AddVertexAt(1, new Point2d(cx + 1000, tvY), 0, 0, 0);
            tvStand.AddVertexAt(2, new Point2d(cx + 1000, tvY + 400), 0, 0, 0);
            tvStand.AddVertexAt(3, new Point2d(cx - 1000, tvY + 400), 0, 0, 0);
            tvStand.Closed = true;
            tvStand.LayerId = layerFurn;
            btr.AppendEntity(tvStand); tr.AddNewlyCreatedDBObject(tvStand, true);

            // Màn hình TV (Nét dày)
            Polyline tvScreen = new Polyline();
            tvScreen.AddVertexAt(0, new Point2d(cx - 700, tvY + 200), 0, 16, 16);
            tvScreen.AddVertexAt(1, new Point2d(cx + 700, tvY + 200), 0, 16, 16);
            tvScreen.LayerId = layerFurn;
            btr.AppendEntity(tvScreen); tr.AddNewlyCreatedDBObject(tvScreen, true);

            // Nhãn chữ
            DBText txtTv = new DBText
            {
                Position = new Point3d(cx - 200, tvY + 230, 0),
                Height = 70,
                TextString = "KỆ TV 65\"",
                TextStyleId = styleId,
                LayerId = layerText
            };
            btr.AppendEntity(txtTv); tr.AddNewlyCreatedDBObject(txtTv, true);
        }

        private static void DrawMasterBedroomLayout(BlockTableRecord btr, Transaction tr, double cx, double cy, ObjectId layerFurn, ObjectId layerText, ObjectId styleId)
        {
            // 1. Giường Master King Size (1800 x 2000)
            double bx = cx - 900;
            double by = cy - 1000;

            Polyline bed = new Polyline();
            bed.AddVertexAt(0, new Point2d(bx, by), 0, 0, 0);
            bed.AddVertexAt(1, new Point2d(bx + 1800, by), 0, 0, 0);
            bed.AddVertexAt(2, new Point2d(bx + 1800, by + 2000), 0, 0, 0);
            bed.AddVertexAt(3, new Point2d(bx, by + 2000), 0, 0, 0);
            bed.Closed = true;
            bed.LayerId = layerFurn;
            btr.AppendEntity(bed); tr.AddNewlyCreatedDBObject(bed, true);

            // Đầu giường đệm nệm (Headboard)
            Polyline headboard = new Polyline();
            headboard.AddVertexAt(0, new Point2d(bx - 100, by + 2000), 0, 0, 0);
            headboard.AddVertexAt(1, new Point2d(bx + 1900, by + 2000), 0, 0, 0);
            headboard.AddVertexAt(2, new Point2d(bx + 1900, by + 2120), 0, 0, 0);
            headboard.AddVertexAt(3, new Point2d(bx - 100, by + 2120), 0, 0, 0);
            headboard.Closed = true;
            headboard.LayerId = layerFurn;
            btr.AppendEntity(headboard); tr.AddNewlyCreatedDBObject(headboard, true);

            // 2 Gối nằm (Pillows: 600x400)
            DrawBox(btr, tr, bx + 150, by + 1450, 600, 400, layerFurn);
            DrawBox(btr, tr, bx + 1050, by + 1450, 600, 400, layerFurn);

            // Nếp chăn ga gối
            Line blanket = new Line(new Point3d(bx, by + 1200, 0), new Point3d(bx + 1800, by + 1200, 0)) { LayerId = layerFurn };
            btr.AppendEntity(blanket); tr.AddNewlyCreatedDBObject(blanket, true);

            // 2. Hai Tab đầu giường (Nightstands: 500x450)
            DrawBox(btr, tr, bx - 650, by + 1600, 500, 450, layerFurn);
            DrawBox(btr, tr, bx + 1950, by + 1600, 500, 450, layerFurn);

            // 3. Tủ quần áo cánh trượt (Wardrobe: 2000x600)
            DrawBox(btr, tr, cx - 1000, cy - 1450, 2000, 600, layerFurn);

            DBText txtBed = new DBText
            {
                Position = new Point3d(cx - 300, cy - 200, 0),
                Height = 85,
                TextString = "GIƯỜNG 1.8x2.0m",
                TextStyleId = styleId,
                LayerId = layerText
            };
            btr.AppendEntity(txtBed); tr.AddNewlyCreatedDBObject(txtBed, true);
        }

        private static void DrawKitchenDiningLayout(BlockTableRecord btr, Transaction tr, double cx, double cy, ObjectId layerFurn, ObjectId layerText, ObjectId styleId)
        {
            // Bàn ăn 6 ghế (1600 x 850)
            double tx = cx - 800;
            double ty = cy - 425;
            DrawBox(btr, tr, tx, ty, 1600, 850, layerFurn);

            // 6 Ghế ăn quanh bàn
            double chairW = 420;
            double chairD = 420;
            // 3 ghế trên
            DrawBox(btr, tr, tx + 100, ty + 870, chairW, chairD, layerFurn);
            DrawBox(btr, tr, tx + 590, ty + 870, chairW, chairD, layerFurn);
            DrawBox(btr, tr, tx + 1080, ty + 870, chairW, chairD, layerFurn);
            // 3 ghế dưới
            DrawBox(btr, tr, tx + 100, ty - 440, chairW, chairD, layerFurn);
            DrawBox(btr, tr, tx + 590, ty - 440, chairW, chairD, layerFurn);
            DrawBox(btr, tr, tx + 1080, ty - 440, chairW, chairD, layerFurn);

            // Tủ bếp chữ L (L-shape Kitchen Counter)
            double kx = cx + 1100;
            double ky = cy - 1200;
            Polyline kCounter = new Polyline();
            kCounter.AddVertexAt(0, new Point2d(kx, ky), 0, 0, 0);
            kCounter.AddVertexAt(1, new Point2d(kx + 1600, ky), 0, 0, 0);
            kCounter.AddVertexAt(2, new Point2d(kx + 1600, ky + 2400), 0, 0, 0);
            kCounter.AddVertexAt(3, new Point2d(kx + 1000, ky + 2400), 0, 0, 0);
            kCounter.AddVertexAt(4, new Point2d(kx + 1000, ky + 600), 0, 0, 0);
            kCounter.AddVertexAt(5, new Point2d(kx, ky + 600), 0, 0, 0);
            kCounter.Closed = true;
            kCounter.LayerId = layerFurn;
            btr.AppendEntity(kCounter); tr.AddNewlyCreatedDBObject(kCounter, true);

            // Bếp từ 3 vùng nấu
            DrawBox(btr, tr, kx + 200, ky + 100, 750, 450, layerFurn);
            Circle b1 = new Circle(new Point3d(kx + 350, ky + 325, 0), Vector3d.ZAxis, 90) { LayerId = layerFurn };
            Circle b2 = new Circle(new Point3d(kx + 650, ky + 325, 0), Vector3d.ZAxis, 90) { LayerId = layerFurn };
            btr.AppendEntity(b1); tr.AddNewlyCreatedDBObject(b1, true);
            btr.AppendEntity(b2); tr.AddNewlyCreatedDBObject(b2, true);

            // Bồn rửa chén chậu đôi (Kitchen Double Sink)
            DrawBox(btr, tr, kx + 1080, ky + 1100, 480, 800, layerFurn);

            DBText txtDining = new DBText
            {
                Position = new Point3d(cx - 300, cy - 20, 0),
                Height = 80,
                TextString = "BÀN ĂN 6 CHỖ",
                TextStyleId = styleId,
                LayerId = layerText
            };
            btr.AppendEntity(txtDining); tr.AddNewlyCreatedDBObject(txtDining, true);
        }

        private static void DrawRestroomSanitaryLayout(BlockTableRecord btr, Transaction tr, double cx, double cy, ObjectId layerSan, ObjectId layerText, ObjectId styleId)
        {
            // 1. Lavabo chậu rửa mặt (600x480)
            double lx = cx - 700;
            double ly = cy + 400;
            DrawBox(btr, tr, lx, ly, 600, 480, layerSan);
            Circle basin = new Circle(new Point3d(lx + 300, ly + 240, 0), Vector3d.ZAxis, 180) { LayerId = layerSan };
            btr.AppendEntity(basin); tr.AddNewlyCreatedDBObject(basin, true);

            // 2. Bồn cầu két nước (Water Closet: 400x700)
            double wx = cx + 200;
            double wy = cy + 400;
            // Két nước
            DrawBox(btr, tr, wx, wy + 520, 400, 180, layerSan);
            // Thân cầu
            Polyline bowl = new Polyline();
            bowl.AddVertexAt(0, new Point2d(wx + 30, wy + 520), 0, 0, 0);
            bowl.AddVertexAt(1, new Point2d(wx + 370, wy + 520), 0.5, 0, 0);
            bowl.AddVertexAt(2, new Point2d(wx + 370, wy + 100), 0.7, 0, 0);
            bowl.AddVertexAt(3, new Point2d(wx + 200, wy), 0.7, 0, 0);
            bowl.AddVertexAt(4, new Point2d(wx + 30, wy + 100), 0.5, 0, 0);
            bowl.Closed = true;
            bowl.LayerId = layerSan;
            btr.AppendEntity(bowl); tr.AddNewlyCreatedDBObject(bowl, true);

            // 3. Vách kính tắm đứng (Glass Shower: 1000x1000)
            double sx = cx - 700;
            double sy = cy - 800;
            Polyline shower = new Polyline();
            shower.AddVertexAt(0, new Point2d(sx, sy), 0, 0, 0);
            shower.AddVertexAt(1, new Point2d(sx + 1000, sy), 0, 0, 0);
            shower.AddVertexAt(2, new Point2d(sx + 1000, sy + 1000), 0, 0, 0);
            shower.AddVertexAt(3, new Point2d(sx, sy + 1000), 0, 0, 0);
            shower.Closed = true;
            shower.LayerId = layerSan;
            btr.AppendEntity(shower); tr.AddNewlyCreatedDBObject(shower, true);

            // Phễu thu sàn vòi sen
            Circle drain = new Circle(new Point3d(sx + 500, sy + 500, 0), Vector3d.ZAxis, 60) { LayerId = layerSan };
            btr.AppendEntity(drain); tr.AddNewlyCreatedDBObject(drain, true);

            DBText txtWc = new DBText
            {
                Position = new Point3d(cx - 200, cy - 20, 0),
                Height = 80,
                TextString = "WC & TẮM",
                TextStyleId = styleId,
                LayerId = layerText
            };
            btr.AppendEntity(txtWc); tr.AddNewlyCreatedDBObject(txtWc, true);
        }

        private static void DrawHomeOfficeLayout(BlockTableRecord btr, Transaction tr, double cx, double cy, ObjectId layerFurn, ObjectId layerText, ObjectId styleId)
        {
            // Bàn làm việc (1400 x 700)
            DrawBox(btr, tr, cx - 700, cy - 350, 1400, 700, layerFurn);
            // Ghế xoay
            Circle chair = new Circle(new Point3d(cx, cy - 650, 0), Vector3d.ZAxis, 260) { LayerId = layerFurn };
            btr.AppendEntity(chair); tr.AddNewlyCreatedDBObject(chair, true);

            // Kệ sách (1800 x 350)
            DrawBox(btr, tr, cx - 900, cy + 700, 1800, 350, layerFurn);

            DBText txtOffice = new DBText
            {
                Position = new Point3d(cx - 250, cy - 50, 0),
                Height = 75,
                TextString = "BÀN LÀM VIỆC",
                TextStyleId = styleId,
                LayerId = layerText
            };
            btr.AppendEntity(txtOffice); tr.AddNewlyCreatedDBObject(txtOffice, true);
        }

        private static void DrawBox(BlockTableRecord btr, Transaction tr, double x, double y, double w, double h, ObjectId layerId)
        {
            Polyline box = new Polyline();
            box.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
            box.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
            box.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
            box.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
            box.Closed = true;
            box.LayerId = layerId;
            btr.AppendEntity(box);
            tr.AddNewlyCreatedDBObject(box, true);
        }
    }
}
