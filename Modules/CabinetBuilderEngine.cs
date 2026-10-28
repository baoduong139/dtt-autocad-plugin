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
    public class CabinetConfig
    {
        // 1. KÍCH THƯỚC & GÓC NHÌN
        public double Width { get; set; } = 800.0;
        public double Height { get; set; } = 1200.0;
        public double Depth { get; set; } = 300.0;
        public double PlinthHeight { get; set; } = 100.0;

        public bool DrawFront { get; set; } = true;
        public bool DrawInnerDoor { get; set; } = true;
        public bool DrawSide { get; set; } = true;
        public bool DrawRear { get; set; } = false;
        public bool DrawTopBottom { get; set; } = false;
        public bool AutoDimension { get; set; } = true;
        public double DimTextHeight { get; set; } = 25.0; // Chiều cao chữ Dim (mm)
        public double DimScale { get; set; } = 1.0; // Tỉ lệ DimScale (1:1)

        // 2. CẤU TRÚC VỎ
        public int DoorCount { get; set; } = 1; // 1 hoặc 2 cánh
        public string LockPosition { get; set; } = "Phải"; // Trái, Phải, Giữa
        public bool HasLouver { get; set; } = true;
        public double LouverWidth { get; set; } = 200.0;
        public double LouverHeight { get; set; } = 120.0;
        public int PartitionRows { get; set; } = 3;
        public int PartitionCols { get; set; } = 1;
        public bool HasWiringDuct { get; set; } = true;

        // 3. BỐ TRÍ LINH KIỆN
        public int MccbCount { get; set; } = 1;
        public string MccbRating { get; set; } = "250A 3P";
        public int McbBranches { get; set; } = 18;
        public int RelayCount { get; set; } = 6;
        public int TerminalCount { get; set; } = 24;
        public bool HasPilotLamps { get; set; } = true;

        // 4. KHUNG TÊN & NHÃN
        public string CabinetName { get; set; } = "TỦ ĐIỆN ISS-1";
        public string PanelSize { get; set; } = "1500x1300";
        public string Voltage { get; set; } = "380V/220V - 50Hz";
        public string RatedCurrent { get; set; } = "250A";
        public string IpRating { get; set; } = "IP54";
        public bool DrawTitleBlock { get; set; } = true;
    }

    public static class CabinetBuilderEngine
    {
        public static bool GenerateCabinetDrawings(Document doc, CabinetConfig cfg, Point3d startPt)
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

                    // 1. TẠO CÁC LAYER CHUẨN KỸ THUẬT CHO TỦ ĐIỆN
                    ObjectId outlineLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-OUTLINE", 4, LineWeight.LineWeight050); // Cyan nét dày
                    ObjectId doorLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-DOOR", 3, LineWeight.LineWeight035);       // Green nét cánh
                    ObjectId plateLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-PLATE", 8, LineWeight.LineWeight025);     // Gray nét tấm gá
                    ObjectId equipLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-EQUIP", 2, LineWeight.LineWeight035);     // Yellow khí cụ điện
                    ObjectId dimLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-DIM", 1, LineWeight.LineWeight018);         // Red đường dim
                    ObjectId textLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-TEXT", 7, LineWeight.LineWeight025);       // White chữ nhãn mác
                    ObjectId hatchLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-HATCH", 9, LineWeight.LineWeight015);     // Xám nhạt nét chải

                    ObjectId textStyle = CadUtils.GetOrCreateUnicodeStyle(db);

                    double currentX = startPt.X;
                    double baseY = startPt.Y;
                    double gap = Math.Max(250.0, cfg.Width * 0.35);

                    // 2. VẼ MẶT TRƯỚC (FRONT VIEW)
                    if (cfg.DrawFront)
                    {
                        DrawFrontView(btr, tr, db, cfg, currentX, baseY, outlineLayer, doorLayer, equipLayer, textLayer, hatchLayer, dimLayer, textStyle);
                        currentX += cfg.Width + gap;
                    }

                    // 3. VẼ MẶT CỬA TRONG / TẤM GÁ LINH KIỆN (MOUNTING PLATE VIEW)
                    if (cfg.DrawInnerDoor)
                    {
                        DrawInnerPlateView(btr, tr, db, cfg, currentX, baseY, outlineLayer, plateLayer, equipLayer, textLayer, dimLayer, textStyle);
                        currentX += cfg.Width + gap;
                    }

                    // 4. VẼ MẶT HÔNG (SIDE VIEW)
                    if (cfg.DrawSide)
                    {
                        DrawSideView(btr, tr, db, cfg, currentX, baseY, outlineLayer, doorLayer, textLayer, dimLayer, textStyle);
                        currentX += cfg.Depth + gap;
                    }

                    // 5. VẼ MẶT SAU (REAR VIEW)
                    if (cfg.DrawRear)
                    {
                        DrawRearView(btr, tr, db, cfg, currentX, baseY, outlineLayer, doorLayer, textLayer, dimLayer, textStyle);
                        currentX += cfg.Width + gap;
                    }

                    // 6. VẼ MẶT TRÊN / MẶT ĐÁY (TOP/BOTTOM VIEW)
                    if (cfg.DrawTopBottom)
                    {
                        DrawTopBottomView(btr, tr, db, cfg, currentX, baseY, outlineLayer, equipLayer, textLayer, dimLayer, textStyle);
                        currentX += cfg.Width + gap;
                    }

                    // 7. VẼ BẢNG THÔNG SỐ KỸ THUẬT & KHUNG TÊN
                    if (cfg.DrawTitleBlock)
                    {
                        DrawSpecificationTable(btr, tr, db, cfg, currentX, baseY, outlineLayer, textLayer, textStyle);
                    }

                    tr.Commit();
                }

                ed.WriteMessage($"\n✅ [DTools Pro] Đã sinh tự động bản vẽ tủ điện [{cfg.CabinetName}] thành công 100%!\n");
                return true;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[DTools ERROR] Lỗi khi sinh bản vẽ tủ điện: {ex.Message}\n");
                return false;
            }
        }

        // ============================================================
        // VẼ MẶT TRƯỚC (FRONT VIEW)
        // ============================================================

        private static void DrawFrontView(BlockTableRecord btr, Transaction tr, Database db, CabinetConfig cfg,
            double x, double y, ObjectId outlineLayer, ObjectId doorLayer, ObjectId equipLayer, ObjectId textLayer, ObjectId hatchLayer, ObjectId dimLayer, ObjectId textStyle)
        {
            double w = cfg.Width;
            double h = cfg.Height;
            double plinthH = cfg.PlinthHeight;

            // 1. Chân đế tủ (Plinth)
            if (plinthH > 0)
            {
                DrawRect(btr, tr, outlineLayer, x, y, w, plinthH);
                // Gân tăng cứng chân đế
                DrawLine(btr, tr, outlineLayer, x + w * 0.15, y, x + w * 0.15, y + plinthH);
                DrawLine(btr, tr, outlineLayer, x + w * 0.85, y, x + w * 0.85, y + plinthH);
                AddMText(btr, tr, textLayer, textStyle, "CHÂN ĐẾ", x + w / 2.0, y + plinthH / 2.0, 15.0, AttachmentPoint.MiddleCenter);
            }

            // 2. Thân vỏ tủ ngoài
            double bodyY = y + plinthH;
            DrawRect(btr, tr, outlineLayer, x, bodyY, w, h);

            // 3. Cánh cửa tủ ngoài
            double doorInset = 15.0;
            double doorW = w - (doorInset * 2.0);
            double doorH = h - (doorInset * 2.0);
            double doorX = x + doorInset;
            double doorY = bodyY + doorInset;

            if (cfg.DoorCount == 1)
            {
                // Cửa 1 cánh
                DrawRect(btr, tr, doorLayer, doorX, doorY, doorW, doorH);

                // Khóa tủ / Tay nắm gạt
                double lockX = (cfg.LockPosition == "Trái") ? doorX + 30.0 : doorX + doorW - 30.0;
                double lockY = doorY + doorH * 0.5;
                DrawLockHandle(btr, tr, equipLayer, textLayer, lockX, lockY);

                // Bản lề tủ
                double hingeX = (cfg.LockPosition == "Trái") ? doorX + doorW : doorX;
                DrawHinge(btr, tr, equipLayer, hingeX, doorY + doorH * 0.85);
                DrawHinge(btr, tr, equipLayer, hingeX, doorY + doorH * 0.15);
            }
            else
            {
                // Cửa 2 cánh mở giữa
                double singleW = (doorW - 6.0) / 2.0;
                DrawRect(btr, tr, doorLayer, doorX, doorY, singleW, doorH);
                DrawRect(btr, tr, doorLayer, doorX + singleW + 6.0, doorY, singleW, doorH);

                // Khóa tủ 2 cánh ở giữa
                DrawLockHandle(btr, tr, equipLayer, textLayer, doorX + singleW - 15.0, doorY + doorH * 0.5);
                DrawLockHandle(btr, tr, equipLayer, textLayer, doorX + singleW + 21.0, doorY + doorH * 0.5);

                // Bản lề 2 bên
                DrawHinge(btr, tr, equipLayer, doorX, doorY + doorH * 0.85);
                DrawHinge(btr, tr, equipLayer, doorX, doorY + doorH * 0.15);
                DrawHinge(btr, tr, equipLayer, doorX + doorW, doorY + doorH * 0.85);
                DrawHinge(btr, tr, equipLayer, doorX + doorW, doorY + doorH * 0.15);
            }

            // 4. Biển tên tủ mica (Title Plate)
            double plateW = Math.Min(w * 0.6, 320.0);
            double plateH = 45.0;
            double plateX = x + (w - plateW) / 2.0;
            double plateY = bodyY + h - plateH - 25.0;
            DrawRect(btr, tr, equipLayer, plateX, plateY, plateW, plateH);
            AddMText(btr, tr, textLayer, textStyle, cfg.CabinetName, plateX + plateW / 2.0, plateY + plateH / 2.0, 16.0, AttachmentPoint.MiddleCenter);

            // 5. Cụm Đèn báo pha L1-L2-L3 & Đồng hồ đo V/A
            if (cfg.HasPilotLamps)
            {
                double lampY = plateY - 45.0;
                double lampR = 9.0;
                double centerLampX = x + w / 2.0;

                // 3 Đèn báo pha (Đỏ - Vàng - Xanh)
                DrawPilotLamp(btr, tr, db, centerLampX - 45.0, lampY, lampR, 1, "R");
                DrawPilotLamp(btr, tr, db, centerLampX, lampY, lampR, 2, "S");
                DrawPilotLamp(btr, tr, db, centerLampX + 45.0, lampY, lampR, 3, "T");

                // 2 Đồng hồ V và A
                double meterY = lampY - 45.0;
                DrawMeterBox(btr, tr, db, equipLayer, textLayer, textStyle, centerLampX - 40.0, meterY, 32.0, "V");
                DrawMeterBox(btr, tr, db, equipLayer, textLayer, textStyle, centerLampX + 40.0, meterY, 32.0, "A");
            }

            // 6. Chớp tản nhiệt (Louver)
            if (cfg.HasLouver)
            {
                double louverW = Math.Min(cfg.LouverWidth, w * 0.4);
                double louverH = Math.Min(cfg.LouverHeight, 90.0);
                double louverX = x + (w - louverW) / 2.0;
                double louverY = doorY + 30.0;
                DrawLouverVent(btr, tr, doorLayer, louverX, louverY, louverW, louverH);
            }

            // 7. Nhãn tiêu đề hình chiếu
            AddMText(btr, tr, textLayer, textStyle, "MẶT TRƯỚC (FRONT VIEW)", x + w / 2.0, y - 40.0, 22.0, AttachmentPoint.TopCenter);
            AddMText(btr, tr, textLayer, textStyle, $"KÍCH THƯỚC: {w:F0} x {h + plinthH:F0} x {cfg.Depth:F0} mm", x + w / 2.0, y - 75.0, 14.0, AttachmentPoint.TopCenter);

            // 8. Kích thước Dimension
            if (cfg.AutoDimension)
            {
                double dOffset = Math.Max(35.0, cfg.DimTextHeight * 1.5);
                AddDimension(btr, tr, db, dimLayer, new Point3d(x, y, 0), new Point3d(x + w, y, 0), new Point3d(x + w / 2.0, y - dOffset, 0), false, cfg);
                AddDimension(btr, tr, db, dimLayer, new Point3d(x, y, 0), new Point3d(x, y + plinthH + h, 0), new Point3d(x - dOffset, y + (h + plinthH) / 2.0, 0), true, cfg);
                if (plinthH > 0)
                {
                    AddDimension(btr, tr, db, dimLayer, new Point3d(x + w, y, 0), new Point3d(x + w, y + plinthH, 0), new Point3d(x + w + dOffset, y + plinthH / 2.0, 0), true, cfg);
                    AddDimension(btr, tr, db, dimLayer, new Point3d(x + w, y + plinthH, 0), new Point3d(x + w, y + plinthH + h, 0), new Point3d(x + w + dOffset, y + plinthH + h / 2.0, 0), true, cfg);
                }
            }
        }

        // ============================================================
        // VẼ MẶT CỬA TRONG / TẤM GÁ LINH KIỆN (MOUNTING PLATE VIEW)
        // ============================================================

        private static void DrawInnerPlateView(BlockTableRecord btr, Transaction tr, Database db, CabinetConfig cfg,
            double x, double y, ObjectId outlineLayer, ObjectId plateLayer, ObjectId equipLayer, ObjectId textLayer, ObjectId dimLayer, ObjectId textStyle)
        {
            double w = cfg.Width;
            double h = cfg.Height;
            double plinthH = cfg.PlinthHeight;

            // Khung ngoài tủ
            if (plinthH > 0) DrawRect(btr, tr, outlineLayer, x, y, w, plinthH);
            double bodyY = y + plinthH;
            DrawRect(btr, tr, outlineLayer, x, bodyY, w, h);

            // Tấm gá thiết bị (Mounting Plate)
            double plateInset = 35.0;
            double pW = w - (plateInset * 2.0);
            double pH = h - (plateInset * 2.0);
            double pX = x + plateInset;
            double pY = bodyY + plateInset;
            DrawRect(btr, tr, plateLayer, pX, pY, pW, pH);

            // 4 Bulong bắt tấm gá ở 4 góc
            double bOffset = 12.0;
            DrawCircle(btr, tr, plateLayer, pX + bOffset, pY + bOffset, 4.0);
            DrawCircle(btr, tr, plateLayer, pX + pW - bOffset, pY + bOffset, 4.0);
            DrawCircle(btr, tr, plateLayer, pX + bOffset, pY + pH - bOffset, 4.0);
            DrawCircle(btr, tr, plateLayer, pX + pW - bOffset, pY + pH - bOffset, 4.0);

            // Máng cáp đi dây nhựa lược (Wiring Duct) viền trong
            double ductW = 35.0;
            if (cfg.HasWiringDuct)
            {
                // Máng dọc trái & phải
                DrawWiringDuct(btr, tr, plateLayer, pX + 5.0, pY + 5.0, ductW, pH - 10.0, true);
                DrawWiringDuct(btr, tr, plateLayer, pX + pW - ductW - 5.0, pY + 5.0, ductW, pH - 10.0, true);
                // Máng ngang đỉnh & đáy
                DrawWiringDuct(btr, tr, plateLayer, pX + ductW + 5.0, pY + pH - ductW - 5.0, pW - (ductW * 2.0) - 10.0, ductW, false);
                DrawWiringDuct(btr, tr, plateLayer, pX + ductW + 5.0, pY + 5.0, pW - (ductW * 2.0) - 10.0, ductW, false);
            }

            // KHOANG 1: MCCB TỔNG & THANH CÁI BUSBAR (Phía trên)
            double topAreaY = pY + pH - ductW - 160.0;
            double mccbW = Math.Min(150.0, pW * 0.45);
            double mccbH = 120.0;
            double mccbX = pX + (pW - mccbW) / 2.0;

            DrawMccbBlock(btr, tr, db, equipLayer, textLayer, textStyle, mccbX, topAreaY, mccbW, mccbH, cfg.MccbRating);

            // Thanh cái đồng Busbar R-S-T-N phía trên MCCB
            double busY = topAreaY + mccbH + 15.0;
            DrawBusbars(btr, tr, db, equipLayer, textLayer, textStyle, pX + ductW + 15.0, busY, pW - (ductW * 2.0) - 30.0);

            // KHOANG 2: CÁC DÀN MCB NHÁNH & RƠ-LE TRUNG GIAN (Chia theo PartitionRows)
            int rows = Math.Max(1, cfg.PartitionRows);
            double midAreaY = pY + 110.0;
            double midAreaH = topAreaY - midAreaY - 25.0;
            double rowH = midAreaH / rows;

            int remainingBranches = cfg.McbBranches;
            int branchIndex = 1;

            for (int r = 0; r < rows; r++)
            {
                double curRowY = midAreaY + (rows - 1 - r) * rowH;

                // Máng cáp ngang chia tầng
                if (cfg.HasWiringDuct && r > 0)
                {
                    DrawWiringDuct(btr, tr, plateLayer, pX + ductW + 5.0, curRowY + rowH - 18.0, pW - (ductW * 2.0) - 10.0, 25.0, false);
                }

                // Thanh ray DIN Rail nhôm
                double railY = curRowY + rowH * 0.4;
                double railX = pX + ductW + 15.0;
                double railW = pW - (ductW * 2.0) - 30.0;
                DrawDinRail(btr, tr, plateLayer, railX, railY, railW, 12.0);

                if (r == 0 && cfg.RelayCount > 0)
                {
                    // Tầng trên cùng của khoang giữa: Xếp cụm Rơ-le kiếng
                    double rlyW = 22.0;
                    double rlyH = 45.0;
                    double startRlyX = railX + 20.0;
                    for (int k = 0; k < Math.Min(cfg.RelayCount, 8); k++)
                    {
                        DrawRelaySocket(btr, tr, db, equipLayer, textLayer, textStyle, startRlyX + k * (rlyW + 8.0), railY - 16.0, rlyW, rlyH, $"R{k + 1}");
                    }
                }
                else
                {
                    // Xếp dàn MCB nhánh
                    double mcbW = 18.0; // Chiều rộng 1 tép MCB 1P chuẩn 18mm
                    double mcbH = 65.0;
                    int rowBranches = Math.Min(remainingBranches, (int)(railW / (mcbW + 4.0)));
                    double startMcbX = railX + (railW - (rowBranches * (mcbW + 4.0))) / 2.0;

                    for (int b = 0; b < rowBranches; b++)
                    {
                        DrawMcbModule(btr, tr, db, equipLayer, textLayer, textStyle, startMcbX + b * (mcbW + 4.0), railY - 26.0, mcbW, mcbH, $"CB{branchIndex}");
                        branchIndex++;
                        remainingBranches--;
                    }
                }
            }

            // KHOANG 3: DÀN CẦU ĐẤU DÂY (TERMINAL BLOCKS - ĐÁY TỦ)
            double tbAreaY = pY + ductW + 15.0;
            double tbRailW = pW - (ductW * 2.0) - 30.0;
            DrawDinRail(btr, tr, plateLayer, pX + ductW + 15.0, tbAreaY + 18.0, tbRailW, 12.0);

            int tbCount = Math.Min(cfg.TerminalCount, (int)(tbRailW / 7.0));
            double tbStartX = pX + ductW + 20.0;
            for (int t = 0; t < tbCount; t++)
            {
                DrawTerminalBlockUnit(btr, tr, db, equipLayer, textLayer, textStyle, tbStartX + t * 6.5, tbAreaY, 6.0, 38.0, (t + 1).ToString());
            }

            // Nhãn tiêu đề hình chiếu
            AddMText(btr, tr, textLayer, textStyle, "MẶT CỬA TRONG / TẤM GÁ LINH KIỆN", x + w / 2.0, y - 40.0, 22.0, AttachmentPoint.TopCenter);
            AddMText(btr, tr, textLayer, textStyle, $"TẤM PANEL GÁ: {cfg.PanelSize}", x + w / 2.0, y - 75.0, 14.0, AttachmentPoint.TopCenter);

            if (cfg.AutoDimension)
            {
                double dOffset = Math.Max(30.0, cfg.DimTextHeight * 1.3);
                AddDimension(btr, tr, db, dimLayer, new Point3d(pX, pY, 0), new Point3d(pX + pW, pY, 0), new Point3d(pX + pW / 2.0, pY - dOffset, 0), false, cfg);
                AddDimension(btr, tr, db, dimLayer, new Point3d(pX, pY, 0), new Point3d(pX, pY + pH, 0), new Point3d(pX - dOffset, pY + pH / 2.0, 0), true, cfg);
            }
        }

        // ============================================================
        // VẼ MẶT HÔNG (SIDE VIEW)
        // ============================================================

        private static void DrawSideView(BlockTableRecord btr, Transaction tr, Database db, CabinetConfig cfg,
            double x, double y, ObjectId outlineLayer, ObjectId doorLayer, ObjectId textLayer, ObjectId dimLayer, ObjectId textStyle)
        {
            double d = cfg.Depth;
            double h = cfg.Height;
            double plinthH = cfg.PlinthHeight;

            // Chân đế hông
            if (plinthH > 0)
            {
                DrawRect(btr, tr, outlineLayer, x, y, d, plinthH);
                AddMText(btr, tr, textLayer, textStyle, "ĐẾ", x + d / 2.0, y + plinthH / 2.0, 14.0, AttachmentPoint.MiddleCenter);
            }

            // Thân tủ hông
            double bodyY = y + plinthH;
            DrawRect(btr, tr, outlineLayer, x, bodyY, d, h);

            // Gờ cánh cửa phía trước (bên trái mặt hông)
            DrawRect(btr, tr, doorLayer, x - 18.0, bodyY + 15.0, 18.0, h - 30.0);

            // Chớp tản nhiệt bên hông
            if (cfg.HasLouver)
            {
                double louverW = d * 0.6;
                double louverH = 80.0;
                double louverX = x + (d - louverW) / 2.0;
                DrawLouverVent(btr, tr, doorLayer, louverX, bodyY + h - louverH - 50.0, louverW, louverH);
                DrawLouverVent(btr, tr, doorLayer, louverX, bodyY + 40.0, louverW, louverH);
            }

            // Tai treo bắt nắp hông
            DrawCircle(btr, tr, outlineLayer, x + d * 0.25, bodyY + h * 0.9, 4.0);
            DrawCircle(btr, tr, outlineLayer, x + d * 0.75, bodyY + h * 0.9, 4.0);
            DrawCircle(btr, tr, outlineLayer, x + d * 0.25, bodyY + h * 0.1, 4.0);
            DrawCircle(btr, tr, outlineLayer, x + d * 0.75, bodyY + h * 0.1, 4.0);

            // Tiêu đề
            AddMText(btr, tr, textLayer, textStyle, "MẶT HÔNG (SIDE VIEW)", x + d / 2.0, y - 40.0, 22.0, AttachmentPoint.TopCenter);
            AddMText(btr, tr, textLayer, textStyle, $"CHIỀU SÂU: {d:F0} mm", x + d / 2.0, y - 75.0, 14.0, AttachmentPoint.TopCenter);

            if (cfg.AutoDimension)
            {
                double dOffset = Math.Max(35.0, cfg.DimTextHeight * 1.5);
                AddDimension(btr, tr, db, dimLayer, new Point3d(x, y, 0), new Point3d(x + d, y, 0), new Point3d(x + d / 2.0, y - dOffset, 0), false, cfg);
            }
        }

        // ============================================================
        // VẼ MẶT SAU (REAR VIEW)
        // ============================================================

        private static void DrawRearView(BlockTableRecord btr, Transaction tr, Database db, CabinetConfig cfg,
            double x, double y, ObjectId outlineLayer, ObjectId doorLayer, ObjectId textLayer, ObjectId dimLayer, ObjectId textStyle)
        {
            double w = cfg.Width;
            double h = cfg.Height;
            double plinthH = cfg.PlinthHeight;

            if (plinthH > 0) DrawRect(btr, tr, outlineLayer, x, y, w, plinthH);
            double bodyY = y + plinthH;
            DrawRect(btr, tr, outlineLayer, x, bodyY, w, h);

            // Nắp lưng tháo rời kèm vít bắt
            double backInset = 20.0;
            DrawRect(btr, tr, doorLayer, x + backInset, bodyY + backInset, w - backInset * 2.0, h - backInset * 2.0);

            // 4 Tai bắt treo tường
            double earW = 40.0;
            double earH = 30.0;
            DrawRect(btr, tr, outlineLayer, x - earW, bodyY + h - earH - 40.0, earW, earH);
            DrawCircle(btr, tr, outlineLayer, x - earW / 2.0, bodyY + h - earH / 2.0 - 40.0, 5.0);

            DrawRect(btr, tr, outlineLayer, x + w, bodyY + h - earH - 40.0, earW, earH);
            DrawCircle(btr, tr, outlineLayer, x + w + earW / 2.0, bodyY + h - earH / 2.0 - 40.0, 5.0);

            DrawRect(btr, tr, outlineLayer, x - earW, bodyY + 40.0, earW, earH);
            DrawCircle(btr, tr, outlineLayer, x - earW / 2.0, bodyY + 40.0 + earH / 2.0, 5.0);

            DrawRect(btr, tr, outlineLayer, x + w, bodyY + 40.0, earW, earH);
            DrawCircle(btr, tr, outlineLayer, x + w + earW / 2.0, bodyY + 40.0 + earH / 2.0, 5.0);

            AddMText(btr, tr, textLayer, textStyle, "MẶT SAU (REAR VIEW)", x + w / 2.0, y - 40.0, 22.0, AttachmentPoint.TopCenter);
            AddMText(btr, tr, textLayer, textStyle, "NẮP LƯNG THÁO RỜI", x + w / 2.0, y - 75.0, 14.0, AttachmentPoint.TopCenter);
        }

        // ============================================================
        // VẼ MẶT TRÊN / MẶT ĐÁY (TOP/BOTTOM VIEW)
        // ============================================================

        private static void DrawTopBottomView(BlockTableRecord btr, Transaction tr, Database db, CabinetConfig cfg,
            double x, double y, ObjectId outlineLayer, ObjectId equipLayer, ObjectId textLayer, ObjectId dimLayer, ObjectId textStyle)
        {
            double w = cfg.Width;
            double d = cfg.Depth;

            // Khung bao mặt đáy / nóc
            DrawRect(btr, tr, outlineLayer, x, y, w, d);

            // Tấm nẹp luồn cáp (Cable Gland Plate)
            double glandW = w * 0.7;
            double glandH = d * 0.55;
            double glandX = x + (w - glandW) / 2.0;
            double glandY = y + (d - glandH) / 2.0;
            DrawRect(btr, tr, equipLayer, glandX, glandY, glandW, glandH);

            // Các lỗ khoét knockout cao su luồn cáp
            int holeCols = Math.Max(2, (int)(glandW / 60.0));
            int holeRows = Math.Max(1, (int)(glandH / 45.0));
            double stepX = glandW / (holeCols + 1);
            double stepY = glandH / (holeRows + 1);

            for (int r = 1; r <= holeRows; r++)
            {
                for (int c = 1; c <= holeCols; c++)
                {
                    DrawCircle(btr, tr, equipLayer, glandX + c * stepX, glandY + r * stepY, 14.0);
                }
            }

            AddMText(btr, tr, textLayer, textStyle, "MẶT ĐÁY / LỖ LUỒN CÁP (GLAND PLATE)", x + w / 2.0, y - 40.0, 22.0, AttachmentPoint.TopCenter);
            AddMText(btr, tr, textLayer, textStyle, $"ĐỘT LỖ KNOCKOUT {holeCols * holeRows} VỊ TRÍ", x + w / 2.0, y - 75.0, 14.0, AttachmentPoint.TopCenter);

            if (cfg.AutoDimension)
            {
                double dOffset = Math.Max(30.0, cfg.DimTextHeight * 1.3);
                AddDimension(btr, tr, db, dimLayer, new Point3d(x, y, 0), new Point3d(x + w, y, 0), new Point3d(x + w / 2.0, y - dOffset, 0), false, cfg);
                AddDimension(btr, tr, db, dimLayer, new Point3d(x, y, 0), new Point3d(x, y + d, 0), new Point3d(x - dOffset, y + d / 2.0, 0), true, cfg);
            }
        }

        // ============================================================
        // VẼ BẢNG THÔNG SỐ KỸ THUẬT & KHUNG TÊN BẢN VẼ
        // ============================================================

        private static void DrawSpecificationTable(BlockTableRecord btr, Transaction tr, Database db, CabinetConfig cfg,
            double x, double y, ObjectId outlineLayer, ObjectId textLayer, ObjectId textStyle)
        {
            double tableW = 420.0;
            double rowH = 34.0;
            string[,] tableData = new string[,]
            {
                { "TÊN THIẾT BỊ:", cfg.CabinetName },
                { "KÍCH THƯỚC VỎ (WxHxD):", $"{cfg.Width:F0} x {cfg.Height:F0} x {cfg.Depth:F0} mm" },
                { "CHIỀU CAO CHÂN ĐẾ:", $"{cfg.PlinthHeight:F0} mm" },
                { "KÍCH THƯỚC TẤM PANEL:", cfg.PanelSize },
                { "ĐIỆN ÁP / TẦN SỐ ĐỊNH MỨC:", cfg.Voltage },
                { "DÒNG ĐIỆN ĐỊNH MỨC (In):", cfg.RatedCurrent },
                { "MCCB CHÍNH / APTOMAT TỔNG:", $"{cfg.MccbRating} ({cfg.MccbCount} bộ)" },
                { "SỐ NHÁNH MCB TÉP RA TẢI:", $"{cfg.McbBranches} nhánh tép" },
                { "RƠ-LE PHỤ TRỢ / TIMER:", $"{cfg.RelayCount} bộ" },
                { "DÀN CẦU ĐẤU DÂY (TB):", $"{cfg.TerminalCount} cực" },
                { "CẤP BẢO VỆ CHỐNG NƯỚC/BỤI:", cfg.IpRating },
                { "TIÊU CHUẨN THIẾT KẾ:", "IEC 61439-1 / TCVN" }
            };

            int totalRows = tableData.GetLength(0);
            double totalH = (totalRows + 1) * rowH;

            // Khung bao bảng
            DrawRect(btr, tr, outlineLayer, x, y, tableW, totalH);

            // Tiêu đề bảng
            double headerY = y + totalH - rowH;
            DrawLine(btr, tr, outlineLayer, x, headerY, x + tableW, headerY);
            AddMText(btr, tr, textLayer, textStyle, "BẢNG THÔNG SỐ KỸ THUẬT TỦ ĐIỆN", x + tableW / 2.0, headerY + rowH / 2.0, 16.0, AttachmentPoint.MiddleCenter);

            // Đường phân cột
            double col1W = 200.0;
            DrawLine(btr, tr, outlineLayer, x + col1W, y, x + col1W, headerY);

            // Từng dòng dữ liệu
            for (int i = 0; i < totalRows; i++)
            {
                double curY = y + (totalRows - 1 - i) * rowH;
                if (i > 0) DrawLine(btr, tr, outlineLayer, x, curY + rowH, x + tableW, curY + rowH);

                AddMText(btr, tr, textLayer, textStyle, tableData[i, 0], x + 10.0, curY + rowH / 2.0, 12.0, AttachmentPoint.MiddleLeft);
                AddMText(btr, tr, textLayer, textStyle, tableData[i, 1], x + col1W + 10.0, curY + rowH / 2.0, 12.0, AttachmentPoint.MiddleLeft);
            }

            // Nhãn tiêu đề dưới bảng
            AddMText(btr, tr, textLayer, textStyle, "THIẾT KẾ BỞI DTools Pro v3.5 - DYNAMIC CABINET BUILDER", x + tableW / 2.0, y - 40.0, 14.0, AttachmentPoint.TopCenter);
        }

        // ============================================================
        // HÀM HỖ TRỢ VẼ HÌNH HỌC CHI TIẾT
        // ============================================================

        private static void DrawMccbBlock(BlockTableRecord btr, Transaction tr, Database db, ObjectId equipLayer, ObjectId textLayer, ObjectId textStyle,
            double x, double y, double w, double h, string rating)
        {
            // Vỏ MCCB
            DrawRect(btr, tr, equipLayer, x, y, w, h);
            // Gờ tay gạt toggle ở giữa
            double toggleW = w * 0.35;
            double toggleH = 26.0;
            DrawRect(btr, tr, equipLayer, x + (w - toggleW) / 2.0, y + (h - toggleH) / 2.0, toggleW, toggleH);

            // Cực đấu 3 pha trên
            for (int i = 0; i < 3; i++)
            {
                double px = x + (i + 1) * (w / 4.0);
                DrawCircle(btr, tr, equipLayer, px, y + h - 12.0, 6.0);
                DrawCircle(btr, tr, equipLayer, px, y + 12.0, 6.0);
            }

            AddMText(btr, tr, textLayer, textStyle, $"MCCB {rating}", x + w / 2.0, y + h * 0.72, 12.0, AttachmentPoint.MiddleCenter);
            AddMText(btr, tr, textLayer, textStyle, "ON / OFF", x + w / 2.0, y + h * 0.28, 9.0, AttachmentPoint.MiddleCenter);
        }

        private static void DrawMcbModule(BlockTableRecord btr, Transaction tr, Database db, ObjectId equipLayer, ObjectId textLayer, ObjectId textStyle,
            double x, double y, double w, double h, string label)
        {
            DrawRect(btr, tr, equipLayer, x, y, w, h);
            // Nút gạt
            DrawRect(btr, tr, equipLayer, x + 3.0, y + h * 0.45, w - 6.0, 14.0);
            DrawCircle(btr, tr, equipLayer, x + w / 2.0, y + h - 8.0, 2.5);
            DrawCircle(btr, tr, equipLayer, x + w / 2.0, y + 8.0, 2.5);
            AddMText(btr, tr, textLayer, textStyle, label, x + w / 2.0, y + h * 0.72, 7.5, AttachmentPoint.MiddleCenter);
        }

        private static void DrawRelaySocket(BlockTableRecord btr, Transaction tr, Database db, ObjectId equipLayer, ObjectId textLayer, ObjectId textStyle,
            double x, double y, double w, double h, string label)
        {
            DrawRect(btr, tr, equipLayer, x, y, w, h);
            DrawRect(btr, tr, equipLayer, x + 3.0, y + 6.0, w - 6.0, h - 12.0);
            AddMText(btr, tr, textLayer, textStyle, label, x + w / 2.0, y + h / 2.0, 8.5, AttachmentPoint.MiddleCenter);
        }

        private static void DrawTerminalBlockUnit(BlockTableRecord btr, Transaction tr, Database db, ObjectId equipLayer, ObjectId textLayer, ObjectId textStyle,
            double x, double y, double w, double h, string label)
        {
            DrawRect(btr, tr, equipLayer, x, y, w, h);
            DrawCircle(btr, tr, equipLayer, x + w / 2.0, y + h - 6.0, 1.8);
            DrawCircle(btr, tr, equipLayer, x + w / 2.0, y + 6.0, 1.8);
        }

        private static void DrawBusbars(BlockTableRecord btr, Transaction tr, Database db, ObjectId equipLayer, ObjectId textLayer, ObjectId textStyle,
            double x, double y, double w)
        {
            string[] phases = new string[] { "R (L1)", "S (L2)", "T (L3)", "N (Neutral)" };
            for (int i = 0; i < 4; i++)
            {
                double curY = y + i * 14.0;
                DrawLine(btr, tr, equipLayer, x, curY, x + w, curY);
                AddMText(btr, tr, textLayer, textStyle, phases[i], x - 8.0, curY, 8.0, AttachmentPoint.MiddleRight);
            }
        }

        private static void DrawDinRail(BlockTableRecord btr, Transaction tr, ObjectId layerId, double x, double y, double w, double h)
        {
            DrawRect(btr, tr, layerId, x, y, w, h);
            // Lỗ đục rãnh DIN rail
            int slots = Math.Max(2, (int)(w / 35.0));
            double step = w / slots;
            for (int i = 0; i < slots; i++)
            {
                DrawRect(btr, tr, layerId, x + i * step + 6.0, y + 3.0, step - 12.0, h - 6.0);
            }
        }

        private static void DrawWiringDuct(BlockTableRecord btr, Transaction tr, ObjectId layerId, double x, double y, double w, double h, bool isVertical)
        {
            DrawRect(btr, tr, layerId, x, y, w, h);
            if (isVertical)
            {
                for (double cy = y + 15.0; cy < y + h - 15.0; cy += 12.0)
                {
                    DrawLine(btr, tr, layerId, x, cy, x + w * 0.35, cy);
                    DrawLine(btr, tr, layerId, x + w * 0.65, cy, x + w, cy);
                }
            }
            else
            {
                for (double cx = x + 15.0; cx < x + w - 15.0; cx += 12.0)
                {
                    DrawLine(btr, tr, layerId, cx, y, cx, y + h * 0.35);
                    DrawLine(btr, tr, layerId, cx, y + h * 0.65, cx, y + h);
                }
            }
        }

        private static void DrawLockHandle(BlockTableRecord btr, Transaction tr, ObjectId equipLayer, ObjectId textLayer, double x, double y)
        {
            DrawCircle(btr, tr, equipLayer, x, y, 16.0);
            DrawRect(btr, tr, equipLayer, x - 5.0, y - 35.0, 10.0, 70.0);
            DrawCircle(btr, tr, equipLayer, x, y, 6.0);
        }

        private static void DrawHinge(BlockTableRecord btr, Transaction tr, ObjectId equipLayer, double x, double y)
        {
            DrawRect(btr, tr, equipLayer, x - 6.0, y - 20.0, 12.0, 40.0);
            DrawCircle(btr, tr, equipLayer, x, y, 3.5);
        }

        private static void DrawPilotLamp(BlockTableRecord btr, Transaction tr, Database db, double cx, double cy, double r, short colorIndex, string phase)
        {
            ObjectId lampLayer = CadUtils.GetOrCreateLayer(db, $"E-LAMP-{phase}", colorIndex, LineWeight.LineWeight035);
            Circle c = new Circle(new Point3d(cx, cy, 0), Vector3d.ZAxis, r) { LayerId = lampLayer };
            btr.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);

            double d = r * Math.Cos(Math.PI / 4.0);
            DrawLine(btr, tr, lampLayer, cx - d, cy - d, cx + d, cy + d);
            DrawLine(btr, tr, lampLayer, cx - d, cy + d, cx + d, cy - d);
        }

        private static void DrawMeterBox(BlockTableRecord btr, Transaction tr, Database db, ObjectId equipLayer, ObjectId textLayer, ObjectId textStyle,
            double cx, double cy, double size, string label)
        {
            DrawRect(btr, tr, equipLayer, cx - size / 2.0, cy - size / 2.0, size, size);
            DrawCircle(btr, tr, equipLayer, cx, cy, size * 0.38);
            AddMText(btr, tr, textLayer, textStyle, label, cx, cy, 14.0, AttachmentPoint.MiddleCenter);
        }

        private static void DrawLouverVent(BlockTableRecord btr, Transaction tr, ObjectId layerId, double x, double y, double w, double h)
        {
            DrawRect(btr, tr, layerId, x, y, w, h);
            int blades = Math.Max(3, (int)(h / 12.0));
            double step = h / (blades + 1);
            for (int i = 1; i <= blades; i++)
            {
                double by = y + i * step;
                DrawLine(btr, tr, layerId, x + 6.0, by, x + w - 6.0, by);
            }
        }

        private static void DrawRect(BlockTableRecord btr, Transaction tr, ObjectId layerId, double x, double y, double w, double h)
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

        private static void DrawLine(BlockTableRecord btr, Transaction tr, ObjectId layerId, double x1, double y1, double x2, double y2)
        {
            Line l = new Line(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0)) { LayerId = layerId };
            btr.AppendEntity(l);
            tr.AddNewlyCreatedDBObject(l, true);
        }

        private static void DrawCircle(BlockTableRecord btr, Transaction tr, ObjectId layerId, double cx, double cy, double r)
        {
            Circle c = new Circle(new Point3d(cx, cy, 0), Vector3d.ZAxis, r) { LayerId = layerId };
            btr.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);
        }

        private static void AddMText(BlockTableRecord btr, Transaction tr, ObjectId layerId, ObjectId textStyle, string text, double x, double y, double height, AttachmentPoint ap)
        {
            MText mt = new MText
            {
                Contents = text,
                Location = new Point3d(x, y, 0),
                TextHeight = height,
                Attachment = ap,
                TextStyleId = textStyle,
                LayerId = layerId
            };
            btr.AppendEntity(mt);
            tr.AddNewlyCreatedDBObject(mt, true);
        }

        private static void AddDimension(BlockTableRecord btr, Transaction tr, Database db, ObjectId dimLayer, Point3d p1, Point3d p2, Point3d dimLinePt, bool isVertical, CabinetConfig cfg = null)
        {
            try
            {
                double th = (cfg != null && cfg.DimTextHeight > 0) ? cfg.DimTextHeight : 25.0;
                double sc = (cfg != null && cfg.DimScale > 0) ? cfg.DimScale : 1.0;

                RotatedDimension dim = new RotatedDimension
                {
                    Rotation = isVertical ? Math.PI / 2.0 : 0.0,
                    XLine1Point = p1,
                    XLine2Point = p2,
                    DimLinePoint = dimLinePt,
                    DimensionStyle = db.Dimstyle,
                    LayerId = dimLayer
                };

                // Override các thuộc tính kích thước để chữ số tỉ lệ 1:1, nhỏ gọn không bị đè lên hình vẽ
                dim.Dimscale = sc;
                dim.Dimtxt = th;
                dim.Dimasz = Math.Max(2.5, th * 0.7);
                dim.Dimexe = Math.Max(2.0, th * 0.4);
                dim.Dimexo = Math.Max(2.0, th * 0.4);
                dim.Dimgap = Math.Max(1.5, th * 0.25);
                dim.Dimtad = 1; // Đặt chữ số nằm phía trên đường kích thước

                btr.AppendEntity(dim);
                tr.AddNewlyCreatedDBObject(dim, true);
            }
            catch { }
        }
    }
}
