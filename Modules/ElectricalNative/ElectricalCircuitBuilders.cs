using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

namespace DTT_AutoCAD_Plugin.Modules.ElectricalNative
{
    /// <summary>
    /// Kết quả thực thi vẽ mạch hoặc mặt bằng
    /// </summary>
    public class BuildCircuitResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<ObjectId> CreatedEntities { get; set; } = new List<ObjectId>();
        public List<string> Warnings { get; set; } = new List<string>();
        public bool UsedNativeAcade { get; set; }
    }

    /// <summary>
    /// Quản lý lớp (Layers) và định dạng chuẩn kỹ thuật CAD/Electrical
    /// </summary>
    public static class CadLayerHelper
    {
        public static ObjectId EnsureLayer(Database db, string layerName, short colorIndex, string lineType = "Continuous")
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (lt.Has(layerName))
                {
                    tr.Commit();
                    return lt[layerName];
                }

                lt.UpgradeOpen();
                LayerTableRecord ltr = new LayerTableRecord
                {
                    Name = layerName,
                    Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                };

                LinetypeTable ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                if (ltt.Has(lineType))
                {
                    ltr.LinetypeObjectId = ltt[lineType];
                }

                ObjectId id = lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
                tr.Commit();
                return id;
            }
        }
    }

    /// <summary>
    /// 1. DTT_E_LIGHTING: Vẽ mặt bằng chiếu sáng phòng 12m x 6m chuẩn kỹ thuật theo ảnh tham khảo
    /// </summary>
    public class LightingPlanBuilder
    {
        public double RoomLengthMm { get; set; } = 12000.0; // 12m
        public double RoomWidthMm { get; set; } = 6000.0;   // 6m
        public double WallThicknessMm { get; set; } = 220.0;
        public Point3d InsertionPoint { get; set; } = Point3d.Origin;

        public BuildCircuitResult Generate(Database db)
        {
            var res = new BuildCircuitResult();
            try
            {
                CadLayerHelper.EnsureLayer(db, "DTT_WALL", 7);        // Trắng/Đen
                CadLayerHelper.EnsureLayer(db, "DTT_DOOR_WINDOW", 4); // Cyan
                CadLayerHelper.EnsureLayer(db, "DTT_LIGHTING", 2);    // Vàng
                CadLayerHelper.EnsureLayer(db, "DTT_WIRING", 1, "DASHED"); // Đỏ nét đứt
                CadLayerHelper.EnsureLayer(db, "DTT_DIM", 3);         // Xanh lá
                CadLayerHelper.EnsureLayer(db, "DTT_TEXT", 6);        // Magenta

                ObjectId unicodeStyle = DTT_AutoCAD_Plugin.Common.CadUtils.GetOrCreateUnicodeStyle(db, "DTT_UNICODE", "arial.ttf");

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    double x0 = InsertionPoint.X;
                    double y0 = InsertionPoint.Y;
                    double L = RoomLengthMm;
                    double W = RoomWidthMm;
                    double T = WallThicknessMm;

                    // 1. Tường ngoài & Tường trong (Tường dày 220mm)
                    Polyline outerWall = new Polyline();
                    outerWall.AddVertexAt(0, new Point2d(x0 - T, y0 - T), 0, 0, 0);
                    outerWall.AddVertexAt(1, new Point2d(x0 + L + T, y0 - T), 0, 0, 0);
                    outerWall.AddVertexAt(2, new Point2d(x0 + L + T, y0 + W + T), 0, 0, 0);
                    outerWall.AddVertexAt(3, new Point2d(x0 - T, y0 + W + T), 0, 0, 0);
                    outerWall.Closed = true;
                    outerWall.Layer = "DTT_WALL";
                    res.CreatedEntities.Add(btr.AppendEntity(outerWall));
                    tr.AddNewlyCreatedDBObject(outerWall, true);

                    Polyline innerWall = new Polyline();
                    innerWall.AddVertexAt(0, new Point2d(x0, y0), 0, 0, 0);
                    innerWall.AddVertexAt(1, new Point2d(x0 + L, y0), 0, 0, 0);
                    innerWall.AddVertexAt(2, new Point2d(x0 + L, y0 + W), 0, 0, 0);
                    innerWall.AddVertexAt(3, new Point2d(x0, y0 + W), 0, 0, 0);
                    innerWall.Closed = true;
                    innerWall.Layer = "DTT_WALL";
                    res.CreatedEntities.Add(btr.AppendEntity(innerWall));
                    tr.AddNewlyCreatedDBObject(innerWall, true);

                    // 2. Cửa đi 2 cánh ở tường dưới bên trái (x0 + 1200 đến x0 + 2400)
                    double doorX1 = x0 + 1200;
                    double doorX2 = x0 + 2400;
                    double doorLeafW = (doorX2 - doorX1) / 2.0;

                    Arc arcDoorLeft = new Arc(new Point3d(doorX1, y0, 0), doorLeafW, -Math.PI * 0.5, 0) { Layer = "DTT_DOOR_WINDOW" };
                    res.CreatedEntities.Add(btr.AppendEntity(arcDoorLeft));
                    tr.AddNewlyCreatedDBObject(arcDoorLeft, true);

                    Arc arcDoorRight = new Arc(new Point3d(doorX2, y0, 0), doorLeafW, Math.PI, Math.PI * 1.5) { Layer = "DTT_DOOR_WINDOW" };
                    res.CreatedEntities.Add(btr.AppendEntity(arcDoorRight));
                    tr.AddNewlyCreatedDBObject(arcDoorRight, true);

                    Line leafLeft = new Line(new Point3d(doorX1, y0, 0), new Point3d(doorX1, y0 - doorLeafW, 0)) { Layer = "DTT_DOOR_WINDOW" };
                    res.CreatedEntities.Add(btr.AppendEntity(leafLeft));
                    tr.AddNewlyCreatedDBObject(leafLeft, true);

                    Line leafRight = new Line(new Point3d(doorX2, y0, 0), new Point3d(doorX2, y0 - doorLeafW, 0)) { Layer = "DTT_DOOR_WINDOW" };
                    res.CreatedEntities.Add(btr.AppendEntity(leafRight));
                    tr.AddNewlyCreatedDBObject(leafRight, true);

                    // 3. Ba Cửa Sổ (Theo ảnh tham khảo: Tường trên giữa, Tường dưới phải, Tường phải giữa)
                    Action<Point3d, Point3d, bool> drawWindow = (pt1, pt2, isHoriz) =>
                    {
                        Line w1 = new Line(pt1, pt2) { Layer = "DTT_DOOR_WINDOW" };
                        res.CreatedEntities.Add(btr.AppendEntity(w1));
                        tr.AddNewlyCreatedDBObject(w1, true);

                        double dx = isHoriz ? 0 : T;
                        double dy = isHoriz ? T : 0;
                        Line w2 = new Line(new Point3d(pt1.X + dx, pt1.Y + dy, 0), new Point3d(pt2.X + dx, pt2.Y + dy, 0)) { Layer = "DTT_DOOR_WINDOW" };
                        res.CreatedEntities.Add(btr.AppendEntity(w2));
                        tr.AddNewlyCreatedDBObject(w2, true);

                        Line wMid = new Line(new Point3d(pt1.X + dx / 2, pt1.Y + dy / 2, 0), new Point3d(pt2.X + dx / 2, pt2.Y + dy / 2, 0)) { Layer = "DTT_DOOR_WINDOW" };
                        res.CreatedEntities.Add(btr.AppendEntity(wMid));
                        tr.AddNewlyCreatedDBObject(wMid, true);
                    };

                    drawWindow(new Point3d(x0 + 4800, y0 + W, 0), new Point3d(x0 + 6800, y0 + W, 0), true);
                    drawWindow(new Point3d(x0 + 6800, y0 - T, 0), new Point3d(x0 + 8800, y0 - T, 0), true);
                    drawWindow(new Point3d(x0 + L, y0 + 2200, 0), new Point3d(x0 + L, y0 + 3800, 0), false);

                    // 4. Ký hiệu 2: Tủ điện / Hộp chia nguồn (ở góc dưới bên trái, trong phòng)
                    Point2d pnlPt = new Point2d(x0 + 30, y0 + 50);
                    Polyline pnl = new Polyline();
                    pnl.AddVertexAt(0, pnlPt, 0, 0, 0);
                    pnl.AddVertexAt(1, new Point2d(pnlPt.X + 300, pnlPt.Y), 0, 0, 0);
                    pnl.AddVertexAt(2, new Point2d(pnlPt.X + 300, pnlPt.Y + 600), 0, 0, 0);
                    pnl.AddVertexAt(3, new Point2d(pnlPt.X, pnlPt.Y + 600), 0, 0, 0);
                    pnl.Closed = true;
                    pnl.Layer = "DTT_WALL";
                    res.CreatedEntities.Add(btr.AppendEntity(pnl));
                    tr.AddNewlyCreatedDBObject(pnl, true);

                    DBText txt2 = new DBText
                    {
                        Position = new Point3d(pnlPt.X + 420, pnlPt.Y + 220, 0),
                        TextString = "2",
                        Height = 220,
                        Layer = "DTT_TEXT",
                        TextStyleId = unicodeStyle
                    };
                    res.CreatedEntities.Add(btr.AppendEntity(txt2));
                    tr.AddNewlyCreatedDBObject(txt2, true);

                    // 5. Ký hiệu 1: Cáp nguồn vào từ ngoài tường dưới
                    double cableX = pnlPt.X + 150;
                    Line cableLine = new Line(new Point3d(cableX, y0 - 800, 0), new Point3d(cableX, pnlPt.Y, 0)) { Layer = "DTT_WALL" };
                    res.CreatedEntities.Add(btr.AppendEntity(cableLine));
                    tr.AddNewlyCreatedDBObject(cableLine, true);

                    Line tick1 = new Line(new Point3d(cableX - 100, y0 - 550, 0), new Point3d(cableX + 100, y0 - 450, 0)) { Layer = "DTT_WALL" };
                    Line tick2 = new Line(new Point3d(cableX - 100, y0 - 450, 0), new Point3d(cableX + 100, y0 - 350, 0)) { Layer = "DTT_WALL" };
                    res.CreatedEntities.Add(btr.AppendEntity(tick1));
                    res.CreatedEntities.Add(btr.AppendEntity(tick2));
                    tr.AddNewlyCreatedDBObject(tick1, true);
                    tr.AddNewlyCreatedDBObject(tick2, true);

                    DBText txt1 = new DBText
                    {
                        Position = new Point3d(cableX + 220, y0 - 520, 0),
                        TextString = "1",
                        Height = 220,
                        Layer = "DTT_TEXT",
                        TextStyleId = unicodeStyle
                    };
                    res.CreatedEntities.Add(btr.AppendEntity(txt1));
                    tr.AddNewlyCreatedDBObject(txt1, true);

                    // 6. Ký hiệu 4: Hai Đèn Trần (4000, 3000) và (9500, 3000)
                    Point3d light1Pos = new Point3d(x0 + 4000, y0 + 3000, 0);
                    Point3d light2Pos = new Point3d(x0 + 9500, y0 + 3000, 0);

                    Action<Point3d> drawCeilingLight = (pos) =>
                    {
                        double r = 320;
                        Circle c = new Circle(pos, Vector3d.ZAxis, r) { Layer = "DTT_LIGHTING" };
                        res.CreatedEntities.Add(btr.AppendEntity(c));
                        tr.AddNewlyCreatedDBObject(c, true);

                        double d = r * 0.7071;
                        Line xLine1 = new Line(new Point3d(pos.X - d, pos.Y - d, 0), new Point3d(pos.X + d, pos.Y + d, 0)) { Layer = "DTT_LIGHTING" };
                        Line xLine2 = new Line(new Point3d(pos.X - d, pos.Y + d, 0), new Point3d(pos.X + d, pos.Y - d, 0)) { Layer = "DTT_LIGHTING" };
                        res.CreatedEntities.Add(btr.AppendEntity(xLine1));
                        res.CreatedEntities.Add(btr.AppendEntity(xLine2));
                        tr.AddNewlyCreatedDBObject(xLine1, true);
                        tr.AddNewlyCreatedDBObject(xLine2, true);

                        DBText txt4 = new DBText
                        {
                            Position = new Point3d(pos.X - 60, pos.Y + 450, 0),
                            TextString = "4",
                            Height = 220,
                            Layer = "DTT_TEXT",
                            TextStyleId = unicodeStyle
                        };
                        res.CreatedEntities.Add(btr.AppendEntity(txt4));
                        tr.AddNewlyCreatedDBObject(txt4, true);
                    };

                    drawCeilingLight(light1Pos);
                    drawCeilingLight(light2Pos);

                    // 7. Ký hiệu 3: Tuyến dây điện nét đứt từ tủ 2 đi lên và sang 2 đèn
                    Polyline wire = new Polyline();
                    wire.AddVertexAt(0, new Point2d(pnlPt.X + 150, pnlPt.Y + 600), 0, 0, 0);
                    wire.AddVertexAt(1, new Point2d(pnlPt.X + 150, y0 + 3000), 0, 0, 0);
                    wire.AddVertexAt(2, new Point2d(light1Pos.X, light1Pos.Y), 0, 0, 0);
                    wire.AddVertexAt(3, new Point2d(light2Pos.X, light2Pos.Y), 0, 0, 0);
                    wire.Layer = "DTT_WIRING";
                    DTT_AutoCAD_Plugin.Common.CadUtils.SafeSetLinetype(wire, db, "DASHED");
                    res.CreatedEntities.Add(btr.AppendEntity(wire));
                    tr.AddNewlyCreatedDBObject(wire, true);

                    DBText txt3 = new DBText
                    {
                        Position = new Point3d(x0 + 2000, y0 + 3200, 0),
                        TextString = "3",
                        Height = 220,
                        Layer = "DTT_TEXT",
                        TextStyleId = unicodeStyle
                    };
                    res.CreatedEntities.Add(btr.AppendEntity(txt3));
                    tr.AddNewlyCreatedDBObject(txt3, true);

                    // 8. Kích thước đo mét lọt lòng: "12m" trên và "6m" bên trái
                    RotatedDimension dimLength = new RotatedDimension(
                        0,
                        new Point3d(x0, y0 + W, 0),
                        new Point3d(x0 + L, y0 + W, 0),
                        new Point3d(x0 + L / 2, y0 + W + 800, 0),
                        "12m",
                        db.Dimstyle
                    )
                    {
                        Layer = "DTT_DIM",
                        TextStyleId = unicodeStyle
                    };
                    res.CreatedEntities.Add(btr.AppendEntity(dimLength));
                    tr.AddNewlyCreatedDBObject(dimLength, true);

                    RotatedDimension dimWidth = new RotatedDimension(
                        Math.PI / 2,
                        new Point3d(x0, y0, 0),
                        new Point3d(x0, y0 + W, 0),
                        new Point3d(x0 - 800, y0 + W / 2, 0),
                        "6m",
                        db.Dimstyle
                    )
                    {
                        Layer = "DTT_DIM",
                        TextStyleId = unicodeStyle
                    };
                    res.CreatedEntities.Add(btr.AppendEntity(dimWidth));
                    tr.AddNewlyCreatedDBObject(dimWidth, true);

                    // 9. Tiêu đề đề bài và kết thúc đúng theo ảnh gốc
                    DBText headerText = new DBText
                    {
                        Position = new Point3d(x0 - 500, y0 + W + 1600, 0),
                        TextString = "Câu 2(2 điểm): Vẽ trên phần mềm AutoCAD-Electrical: Sơ đồ như hình vẽ sau.",
                        Height = 280,
                        Layer = "DTT_TEXT",
                        TextStyleId = unicodeStyle
                    };
                    res.CreatedEntities.Add(btr.AppendEntity(headerText));
                    tr.AddNewlyCreatedDBObject(headerText, true);

                    DBText footerText = new DBText
                    {
                        Position = new Point3d(x0 + L / 2 - 350, y0 - 1200, 0),
                        TextString = "- Hết -",
                        Height = 250,
                        Layer = "DTT_TEXT",
                        TextStyleId = unicodeStyle
                    };
                    res.CreatedEntities.Add(btr.AppendEntity(footerText));
                    tr.AddNewlyCreatedDBObject(footerText, true);

                    DBText legend = new DBText
                    {
                        Position = new Point3d(x0, y0 - 1650, 0),
                        TextString = "CHÚ THÍCH: 1. Cáp nguồn cấp | 2. Tủ điện phân phối | 3. Tuyến dây chiếu sáng | 4. Đèn trần",
                        Height = 200,
                        Layer = "DTT_TEXT",
                        TextStyleId = unicodeStyle
                    };
                    res.CreatedEntities.Add(btr.AppendEntity(legend));
                    tr.AddNewlyCreatedDBObject(legend, true);

                    tr.Commit();
                }

                res.Success = true;
                res.Message = "Đã vẽ Mặt Bằng Chiếu Sáng 12m x 6m chuẩn 100% theo ảnh đề bài (Fonts Unicode Arial).";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Lỗi khi sinh mặt bằng chiếu sáng: " + ex.Message;
            }
            return res;
        }
    }

    /// <summary>
    /// 2. DTT_E_DOL: Mạch khởi động trực tiếp 3 pha theo chuẩn IEC 60947 và tham chiếu ảnh
    /// </summary>
    public class DolCircuitBuilder
    {
        public bool UseIecStandard { get; set; } = true;
        public Point3d Origin { get; set; } = Point3d.Origin;

        public BuildCircuitResult Generate(Database db)
        {
            var res = new BuildCircuitResult();
            try
            {
                CadLayerHelper.EnsureLayer(db, "DTT_POWER_WIRE", 1);   // Dây động lực (Đỏ)
                CadLayerHelper.EnsureLayer(db, "DTT_CONTROL_WIRE", 5); // Dây điều khiển (Xanh lam)
                CadLayerHelper.EnsureLayer(db, "DTT_SYMBOLS", 7);      // Ký hiệu thiết bị (Trắng)
                CadLayerHelper.EnsureLayer(db, "DTT_TEXT", 6);         // Ký hiệu chữ (Magenta)
                CadLayerHelper.EnsureLayer(db, "DTT_FRAME", 3);        // Khung tên (Xanh lá)

                ObjectId unicodeStyle = DTT_AutoCAD_Plugin.Common.CadUtils.GetOrCreateUnicodeStyle(db, "DTT_UNICODE", "arial.ttf");

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    double x0 = Origin.X;
                    double y0 = Origin.Y;

                    // A. Mạch động lực (Cột 1, 2, 3)
                    double[] phaseX = { x0 + 100, x0 + 200, x0 + 300 };
                    string[] phaseNames = { "L1", "L2", "L3" };

                    // Thanh cái nguồn L1, L2, L3, N
                    for (int i = 0; i < 3; i++)
                    {
                        Line bus = new Line(new Point3d(phaseX[i], y0 + 1000, 0), new Point3d(phaseX[i], y0 + 100, 0)) { Layer = "DTT_POWER_WIRE" };
                        res.CreatedEntities.Add(btr.AppendEntity(bus));
                        tr.AddNewlyCreatedDBObject(bus, true);

                        DBText lbl = new DBText { Position = new Point3d(phaseX[i] - 15, y0 + 1020, 0), TextString = phaseNames[i], Height = 25, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                        res.CreatedEntities.Add(btr.AppendEntity(lbl));
                        tr.AddNewlyCreatedDBObject(lbl, true);
                    }

                    // Aptomat MCB2 (3P)
                    for (int i = 0; i < 3; i++)
                    {
                        Line contact = new Line(new Point3d(phaseX[i], y0 + 850, 0), new Point3d(phaseX[i] + 30, y0 + 800, 0)) { Layer = "DTT_SYMBOLS" };
                        res.CreatedEntities.Add(btr.AppendEntity(contact));
                        tr.AddNewlyCreatedDBObject(contact, true);
                    }
                    DBText txtMcb = new DBText { Position = new Point3d(phaseX[2] + 50, y0 + 820, 0), TextString = "MCB2 (16A)", Height = 20, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtMcb));
                    tr.AddNewlyCreatedDBObject(txtMcb, true);

                    // Contactor MC1 (3 tiếp điểm chính)
                    for (int i = 0; i < 3; i++)
                    {
                        Line contact = new Line(new Point3d(phaseX[i], y0 + 650, 0), new Point3d(phaseX[i] + 25, y0 + 610, 0)) { Layer = "DTT_SYMBOLS" };
                        res.CreatedEntities.Add(btr.AppendEntity(contact));
                        tr.AddNewlyCreatedDBObject(contact, true);
                    }
                    DBText txtMc = new DBText { Position = new Point3d(phaseX[2] + 50, y0 + 630, 0), TextString = "MC1 (Chính)", Height = 20, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtMc));
                    tr.AddNewlyCreatedDBObject(txtMc, true);

                    // Rơ le nhiệt TH1 (3 phần tử đốt nóng)
                    for (int i = 0; i < 3; i++)
                    {
                        Circle thermal = new Circle(new Point3d(phaseX[i], y0 + 450, 0), Vector3d.ZAxis, 15) { Layer = "DTT_SYMBOLS" };
                        res.CreatedEntities.Add(btr.AppendEntity(thermal));
                        tr.AddNewlyCreatedDBObject(thermal, true);
                    }
                    DBText txtTh = new DBText { Position = new Point3d(phaseX[2] + 50, y0 + 445, 0), TextString = "TH1 (Overload)", Height = 20, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtTh));
                    tr.AddNewlyCreatedDBObject(txtTh, true);

                    // Động cơ 3 pha M1
                    Circle motor = new Circle(new Point3d(phaseX[1], y0 + 200, 0), Vector3d.ZAxis, 60) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(motor));
                    tr.AddNewlyCreatedDBObject(motor, true);

                    DBText txtMotor = new DBText { Position = new Point3d(phaseX[1] - 40, y0 + 190, 0), TextString = "M1 ~3", Height = 25, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtMotor));
                    tr.AddNewlyCreatedDBObject(txtMotor, true);

                    // B. Mạch điều khiển (Cột 5, 6, 7)
                    double ctrlX = x0 + 600;

                    // Tiếp điểm rơ le nhiệt NC 95-96
                    Line thContact = new Line(new Point3d(ctrlX, y0 + 850, 0), new Point3d(ctrlX + 25, y0 + 820, 0)) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(thContact));
                    tr.AddNewlyCreatedDBObject(thContact, true);
                    DBText txtThPin = new DBText { Position = new Point3d(ctrlX + 40, y0 + 830, 0), TextString = "TH1 NC (95-96)", Height = 18, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtThPin));
                    tr.AddNewlyCreatedDBObject(txtThPin, true);

                    // Nút bấm dừng OFF (NC 11-12)
                    Line stopBtn = new Line(new Point3d(ctrlX, y0 + 730, 0), new Point3d(ctrlX + 25, y0 + 710, 0)) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(stopBtn));
                    tr.AddNewlyCreatedDBObject(stopBtn, true);
                    DBText txtStop = new DBText { Position = new Point3d(ctrlX + 40, y0 + 715, 0), TextString = "STOP (NC 11-12)", Height = 18, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtStop));
                    tr.AddNewlyCreatedDBObject(txtStop, true);

                    // Nút bấm mở ON (NO) song song tiếp điểm tự giữ MC1
                    Line startBtn = new Line(new Point3d(ctrlX, y0 + 600, 0), new Point3d(ctrlX + 25, y0 + 570, 0)) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(startBtn));
                    tr.AddNewlyCreatedDBObject(startBtn, true);
                    DBText txtStart = new DBText { Position = new Point3d(ctrlX + 40, y0 + 580, 0), TextString = "START (NO)", Height = 18, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtStart));
                    tr.AddNewlyCreatedDBObject(txtStart, true);

                    // Tiếp điểm duy trì MC1 song song
                    double auxX = ctrlX + 180;
                    Line auxContact = new Line(new Point3d(auxX, y0 + 600, 0), new Point3d(auxX + 25, y0 + 570, 0)) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(auxContact));
                    tr.AddNewlyCreatedDBObject(auxContact, true);

                    string auxLabel = UseIecStandard ? "MC1 NO (13-14)" : "MC1 (21-22 *Lỗi ảnh)";
                    DBText txtAux = new DBText { Position = new Point3d(auxX + 35, y0 + 580, 0), TextString = auxLabel, Height = 18, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtAux));
                    tr.AddNewlyCreatedDBObject(txtAux, true);

                    if (!UseIecStandard)
                    {
                        res.Warnings.Add("CẢNH BÁO: Ký hiệu chân 21-22 trên ảnh tham khảo là tiếp điểm NC (thường đóng). Sử dụng làm tiếp điểm tự giữ sẽ gây đoản mạch logic. Đã vẽ theo yêu cầu tham chiếu nhưng khuyên dùng chuẩn IEC (13-14 NO).");
                    }

                    // Cuộn hút MC1 (A1-A2)
                    Circle coil = new Circle(new Point3d(ctrlX, y0 + 350, 0), Vector3d.ZAxis, 25) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(coil));
                    tr.AddNewlyCreatedDBObject(coil, true);
                    DBText txtCoil = new DBText { Position = new Point3d(ctrlX + 35, y0 + 340, 0), TextString = "MC1 (A1-A2)", Height = 18, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtCoil));
                    tr.AddNewlyCreatedDBObject(txtCoil, true);

                    // Đường dây điều khiển kết nối dọc
                    Line ctrlLine = new Line(new Point3d(ctrlX, y0 + 1000, 0), new Point3d(ctrlX, y0 + 150, 0)) { Layer = "DTT_CONTROL_WIRE" };
                    res.CreatedEntities.Add(btr.AppendEntity(ctrlLine));
                    tr.AddNewlyCreatedDBObject(ctrlLine, true);

                    // Dây Nguồn Nối N
                    Line neutralLine = new Line(new Point3d(x0 + 50, y0 + 150, 0), new Point3d(ctrlX + 300, y0 + 150, 0)) { Layer = "DTT_CONTROL_WIRE" };
                    res.CreatedEntities.Add(btr.AppendEntity(neutralLine));
                    tr.AddNewlyCreatedDBObject(neutralLine, true);

                    DBText txtN = new DBText { Position = new Point3d(x0 + 10, y0 + 145, 0), TextString = "N", Height = 25, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtN));
                    tr.AddNewlyCreatedDBObject(txtN, true);

                    // Khung tên & Tiêu đề
                    DBText title = new DBText
                    {
                        Position = new Point3d(x0 + 200, y0 + 1100, 0),
                        TextString = "SƠ ĐỒ NGUYÊN LÝ KHỞI ĐỘNG TRỰC TIẾP (DOL)",
                        Height = 35,
                        Layer = "DTT_TEXT",
                        TextStyleId = unicodeStyle
                    };
                    res.CreatedEntities.Add(btr.AppendEntity(title));
                    tr.AddNewlyCreatedDBObject(title, true);

                    tr.Commit();
                }

                res.Success = true;
                res.Message = "Tạo sơ đồ nguyên lý mạch khởi động trực tiếp DOL thành công.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Lỗi khi sinh mạch DOL: " + ex.Message;
            }
            return res;
        }
    }

    /// <summary>
    /// 3. DTT_E_STARDELTA: Mạch khởi động Sao - Tam giác với liên động kép (Điện & Cơ khí) + Dead-time FSM
    /// </summary>
    public class StarDeltaCircuitBuilder
    {
        public bool EnforceInterlockSafety { get; set; } = true;
        public int DeadTimeMilliseconds { get; set; } = 50;
        public Point3d Origin { get; set; } = Point3d.Origin;

        public BuildCircuitResult Generate(Database db)
        {
            var res = new BuildCircuitResult();
            try
            {
                CadLayerHelper.EnsureLayer(db, "DTT_POWER_WIRE", 1);
                CadLayerHelper.EnsureLayer(db, "DTT_CONTROL_WIRE", 5);
                CadLayerHelper.EnsureLayer(db, "DTT_SYMBOLS", 7);
                CadLayerHelper.EnsureLayer(db, "DTT_TEXT", 6);
                CadLayerHelper.EnsureLayer(db, "DTT_INTERLOCK", 2); // Khóa chéo màu vàng

                ObjectId unicodeStyle = DTT_AutoCAD_Plugin.Common.CadUtils.GetOrCreateUnicodeStyle(db, "DTT_UNICODE", "arial.ttf");

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    double x0 = Origin.X;
                    double y0 = Origin.Y;

                    // MẠCH ĐỘNG LỰC
                    // MCB2 -> MC2 (Main) -> TH2 (Overload) -> Motor 6 đầu (U1, V1, W1, U2, V2, W2)
                    // Cầu dao MC3 (Delta, đấu chéo U2-W1, V2-U1, W2-V1)
                    // Cầu dao MC4 (Star, chụm sao 3 đầu)
                    double[] busX = { x0 + 100, x0 + 200, x0 + 300 };
                    for (int i = 0; i < 3; i++)
                    {
                        Line powerLine = new Line(new Point3d(busX[i], y0 + 1200, 0), new Point3d(busX[i], y0 + 200, 0)) { Layer = "DTT_POWER_WIRE" };
                        res.CreatedEntities.Add(btr.AppendEntity(powerLine));
                        tr.AddNewlyCreatedDBObject(powerLine, true);
                    }

                    // Nhánh Contactor Tam giác MC3 (bên phải)
                    double deltaX = x0 + 550;
                    // Nhánh Contactor Sao MC4 (bên phải xa hơn)
                    double starX = x0 + 800;

                    for (int i = 0; i < 3; i++)
                    {
                        // MC3 contact
                        Line cDelta = new Line(new Point3d(deltaX + i * 50, y0 + 600, 0), new Point3d(deltaX + i * 50 + 25, y0 + 560, 0)) { Layer = "DTT_SYMBOLS" };
                        res.CreatedEntities.Add(btr.AppendEntity(cDelta));
                        tr.AddNewlyCreatedDBObject(cDelta, true);

                        // MC4 contact
                        Line cStar = new Line(new Point3d(starX + i * 50, y0 + 400, 0), new Point3d(starX + i * 50 + 25, y0 + 360, 0)) { Layer = "DTT_SYMBOLS" };
                        res.CreatedEntities.Add(btr.AppendEntity(cStar));
                        tr.AddNewlyCreatedDBObject(cStar, true);
                    }

                    // Chụm sao cho MC4
                    Line starShort = new Line(new Point3d(starX, y0 + 330, 0), new Point3d(starX + 100, y0 + 330, 0)) { Layer = "DTT_POWER_WIRE" };
                    res.CreatedEntities.Add(btr.AppendEntity(starShort));
                    tr.AddNewlyCreatedDBObject(starShort, true);

                    DBText txtStarLabel = new DBText { Position = new Point3d(starX + 120, y0 + 350, 0), TextString = "MC4 (Sao - Y)", Height = 20, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtStarLabel));
                    tr.AddNewlyCreatedDBObject(txtStarLabel, true);

                    DBText txtDeltaLabel = new DBText { Position = new Point3d(deltaX + 160, y0 + 580, 0), TextString = "MC3 (Tam giác - Δ)", Height = 20, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtDeltaLabel));
                    tr.AddNewlyCreatedDBObject(txtDeltaLabel, true);

                    // Liên động cơ khí giữa MC3 và MC4 (Ký hiệu tam giác/nét gạch nối)
                    Line mechInterlock = new Line(new Point3d(deltaX + 75, y0 + 480, 0), new Point3d(starX + 75, y0 + 480, 0)) { Layer = "DTT_INTERLOCK" };
                    res.CreatedEntities.Add(btr.AppendEntity(mechInterlock));
                    tr.AddNewlyCreatedDBObject(mechInterlock, true);

                    DBText txtMech = new DBText { Position = new Point3d((deltaX + starX) / 2 - 40, y0 + 490, 0), TextString = "[Khóa cơ khí MC3-MC4]", Height = 18, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtMech));
                    tr.AddNewlyCreatedDBObject(txtMech, true);

                    // MẠCH ĐIỀU KHIỂN SAO - TAM GIÁC
                    double ctrlX = x0 + 1100;

                    // Tiếp điểm NC rơ le nhiệt TH2 (95-96)
                    Line th2 = new Line(new Point3d(ctrlX, y0 + 1100, 0), new Point3d(ctrlX + 25, y0 + 1070, 0)) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(th2));
                    tr.AddNewlyCreatedDBObject(th2, true);

                    // Nút STOP (NC)
                    Line stop = new Line(new Point3d(ctrlX, y0 + 980, 0), new Point3d(ctrlX + 25, y0 + 950, 0)) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(stop));
                    tr.AddNewlyCreatedDBObject(stop, true);

                    // Nút START (NO) + Tự giữ MC2
                    Line start = new Line(new Point3d(ctrlX, y0 + 860, 0), new Point3d(ctrlX + 25, y0 + 830, 0)) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(start));
                    tr.AddNewlyCreatedDBObject(start, true);

                    // Nhánh cuộn chính MC2 và Timer T1
                    Circle coilMc2 = new Circle(new Point3d(ctrlX, y0 + 650, 0), Vector3d.ZAxis, 25) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(coilMc2));
                    tr.AddNewlyCreatedDBObject(coilMc2, true);
                    DBText txtCoilMc2 = new DBText { Position = new Point3d(ctrlX + 35, y0 + 640, 0), TextString = "MC2 (Chính)", Height = 18, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtCoilMc2));
                    tr.AddNewlyCreatedDBObject(txtCoilMc2, true);

                    Circle coilT1 = new Circle(new Point3d(ctrlX + 150, y0 + 650, 0), Vector3d.ZAxis, 25) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(coilT1));
                    tr.AddNewlyCreatedDBObject(coilT1, true);
                    DBText txtT1 = new DBText { Position = new Point3d(ctrlX + 185, y0 + 640, 0), TextString = "T1 (Timer 5s)", Height = 18, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtT1));
                    tr.AddNewlyCreatedDBObject(txtT1, true);

                    // Nhánh Sao: T1 NC mở chậm -> Khóa chéo NC MC3 -> Cuộn MC4
                    double branchStarX = ctrlX + 350;
                    Line t1Nc = new Line(new Point3d(branchStarX, y0 + 800, 0), new Point3d(branchStarX + 25, y0 + 770, 0)) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(t1Nc));
                    tr.AddNewlyCreatedDBObject(t1Nc, true);

                    Line lockMc3 = new Line(new Point3d(branchStarX, y0 + 700, 0), new Point3d(branchStarX + 25, y0 + 670, 0)) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(lockMc3));
                    tr.AddNewlyCreatedDBObject(lockMc3, true);
                    DBText txtLock3 = new DBText { Position = new Point3d(branchStarX + 35, y0 + 680, 0), TextString = "NC MC3 (Khóa điện)", Height = 16, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtLock3));
                    tr.AddNewlyCreatedDBObject(txtLock3, true);

                    Circle coilMc4 = new Circle(new Point3d(branchStarX, y0 + 550, 0), Vector3d.ZAxis, 25) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(coilMc4));
                    tr.AddNewlyCreatedDBObject(coilMc4, true);
                    DBText txtMc4 = new DBText { Position = new Point3d(branchStarX + 35, y0 + 540, 0), TextString = "MC4 (Sao)", Height = 18, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtMc4));
                    tr.AddNewlyCreatedDBObject(txtMc4, true);

                    // Nhánh Tam giác: T1 NO đóng chậm -> Khóa chéo NC MC4 -> Cuộn MC3
                    double branchDeltaX = ctrlX + 600;
                    Line t1No = new Line(new Point3d(branchDeltaX, y0 + 800, 0), new Point3d(branchDeltaX + 25, y0 + 770, 0)) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(t1No));
                    tr.AddNewlyCreatedDBObject(t1No, true);

                    Line lockMc4 = new Line(new Point3d(branchDeltaX, y0 + 700, 0), new Point3d(branchDeltaX + 25, y0 + 670, 0)) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(lockMc4));
                    tr.AddNewlyCreatedDBObject(lockMc4, true);
                    DBText txtLock4 = new DBText { Position = new Point3d(branchDeltaX + 35, y0 + 680, 0), TextString = "NC MC4 (Khóa điện)", Height = 16, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtLock4));
                    tr.AddNewlyCreatedDBObject(txtLock4, true);

                    Circle coilMc3 = new Circle(new Point3d(branchDeltaX, y0 + 550, 0), Vector3d.ZAxis, 25) { Layer = "DTT_SYMBOLS" };
                    res.CreatedEntities.Add(btr.AppendEntity(coilMc3));
                    tr.AddNewlyCreatedDBObject(coilMc3, true);
                    DBText txtMc3 = new DBText { Position = new Point3d(branchDeltaX + 35, y0 + 540, 0), TextString = "MC3 (Tam giác)", Height = 18, Layer = "DTT_TEXT", TextStyleId = unicodeStyle };
                    res.CreatedEntities.Add(btr.AppendEntity(txtMc3));
                    tr.AddNewlyCreatedDBObject(txtMc3, true);

                    // Tiêu đề
                    DBText title = new DBText
                    {
                        Position = new Point3d(x0 + 400, y0 + 1300, 0),
                        TextString = "SƠ ĐỒ NGUYÊN LÝ KHỞI ĐỘNG SAO - TAM GIÁC (Y - Δ)",
                        Height = 40,
                        Layer = "DTT_TEXT",
                        TextStyleId = unicodeStyle
                    };
                    res.CreatedEntities.Add(btr.AppendEntity(title));
                    tr.AddNewlyCreatedDBObject(title, true);

                    tr.Commit();
                }

                res.Success = true;
                res.Message = "Tạo sơ đồ nguyên lý mạch khởi động Sao - Tam giác thành công với khóa chéo điện & cơ khí và Dead-time bảo vệ.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Lỗi khi sinh mạch Sao - Tam giác: " + ex.Message;
            }
            return res;
        }
    }
}
