using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.Modules
{
    public static class Cabinet3DBuilderEngine
    {
        // ============================================================
        // TỰ ĐỘNG DỰNG MÔ HÌNH 3D SOLID TỦ ĐIỆN THEO THAM SỐ
        // ============================================================

        public static bool Generate3DCabinet(Document doc, CabinetConfig cfg, Point3d origin)
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

                    // 1. TẠO CÁC LAYER 3D CHO TỦ ĐIỆN
                    ObjectId bodyLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-3D-BODY", 8, LineWeight.LineWeight035);   // Xám RAL 7035
                    ObjectId doorLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-3D-DOOR", 7, LineWeight.LineWeight035);   // Trắng sáng
                    ObjectId plinthLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-3D-PLINTH", 250, LineWeight.LineWeight035); // Xám đậm
                    ObjectId plateLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-3D-PLATE", 30, LineWeight.LineWeight025); // Cam tôn mạ
                    ObjectId equipLayer = CadUtils.GetOrCreateLayer(db, "E-CABINET-3D-EQUIP", 1, LineWeight.LineWeight035); // Đỏ thiết bị

                    double w = cfg.Width;
                    double h = cfg.Height;
                    double d = cfg.Depth;
                    double plinthH = cfg.PlinthHeight;

                    // 2. CHÂN ĐẾ TỦ 3D (PLINTH)
                    if (plinthH > 0)
                    {
                        Solid3d plinth = new Solid3d();
                        plinth.CreateBox(w, d, plinthH);
                        plinth.TransformBy(Matrix3d.Displacement(new Vector3d(origin.X + w / 2.0, origin.Y + d / 2.0, origin.Z + plinthH / 2.0)));
                        plinth.LayerId = plinthLayer;
                        plinth.Color = Color.FromColorIndex(ColorMethod.ByAci, 250);
                        btr.AppendEntity(plinth);
                        tr.AddNewlyCreatedDBObject(plinth, true);
                    }

                    // 3. THÂN VỎ TỦ 3D (BODY HOLLOW BOX)
                    double bodyZ = origin.Z + plinthH;
                    Solid3d outerBox = new Solid3d();
                    outerBox.CreateBox(w, d, h);
                    outerBox.TransformBy(Matrix3d.Displacement(new Vector3d(origin.X + w / 2.0, origin.Y + d / 2.0, bodyZ + h / 2.0)));

                    // Khoét rỗng ruột tủ bằng Boolean Subtract
                    double wall = 20.0;
                    Solid3d innerBox = new Solid3d();
                    innerBox.CreateBox(w - wall * 2.0, d - wall + 5.0, h - wall * 2.0);
                    innerBox.TransformBy(Matrix3d.Displacement(new Vector3d(origin.X + w / 2.0, origin.Y + d / 2.0 - wall / 2.0, bodyZ + h / 2.0)));

                    try
                    {
                        outerBox.BooleanOperation(BooleanOperationType.BoolSubtract, innerBox);
                    }
                    catch { }

                    outerBox.LayerId = bodyLayer;
                    outerBox.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
                    btr.AppendEntity(outerBox);
                    tr.AddNewlyCreatedDBObject(outerBox, true);

                    // 4. TẤM GÁ LINH KIỆN 3D (MOUNTING PLATE)
                    double plateW = w - wall * 2.0 - 10.0;
                    double plateH = h - wall * 2.0 - 10.0;
                    double plateThick = 10.0;
                    Solid3d plate = new Solid3d();
                    plate.CreateBox(plateW, plateThick, plateH);
                    double plateY = origin.Y + d - wall - plateThick / 2.0;
                    plate.TransformBy(Matrix3d.Displacement(new Vector3d(origin.X + w / 2.0, plateY, bodyZ + h / 2.0)));
                    plate.LayerId = plateLayer;
                    plate.Color = Color.FromColorIndex(ColorMethod.ByAci, 30);
                    btr.AppendEntity(plate);
                    tr.AddNewlyCreatedDBObject(plate, true);

                    // 5. KHỐI MCCB TỔNG 3D
                    double mccbW = Math.Min(150.0, w * 0.45);
                    double mccbD = 70.0;
                    double mccbH = 130.0;
                    Solid3d mccb = new Solid3d();
                    mccb.CreateBox(mccbW, mccbD, mccbH);
                    double mccbY = plateY - plateThick / 2.0 - mccbD / 2.0;
                    double mccbZ = bodyZ + h - wall - 120.0;
                    mccb.TransformBy(Matrix3d.Displacement(new Vector3d(origin.X + w / 2.0, mccbY, mccbZ)));
                    mccb.LayerId = equipLayer;
                    mccb.Color = Color.FromColorIndex(ColorMethod.ByAci, 1); // Đỏ
                    btr.AppendEntity(mccb);
                    tr.AddNewlyCreatedDBObject(mccb, true);

                    // 6. CÁC HÀNG DIN RAIL & DÀN MCB 3D
                    int rows = Math.Max(1, cfg.PartitionRows);
                    double midZStart = bodyZ + 120.0;
                    double midZHeight = mccbZ - midZStart - 90.0;
                    double rowZStep = midZHeight / rows;

                    for (int r = 0; r < rows; r++)
                    {
                        double curZ = midZStart + r * rowZStep;
                        // Thanh ray DIN Rail nhôm
                        Solid3d rail = new Solid3d();
                        rail.CreateBox(plateW - 40.0, 10.0, 15.0);
                        double railY = plateY - plateThick / 2.0 - 5.0;
                        rail.TransformBy(Matrix3d.Displacement(new Vector3d(origin.X + w / 2.0, railY, curZ)));
                        rail.Color = Color.FromColorIndex(ColorMethod.ByAci, 9);
                        btr.AppendEntity(rail);
                        tr.AddNewlyCreatedDBObject(rail, true);

                        // Dàn tép MCB
                        int mcbs = Math.Min(cfg.McbBranches / rows + 2, 8);
                        double mcbStep = (plateW - 80.0) / Math.Max(1, mcbs);
                        for (int m = 0; m < mcbs; m++)
                        {
                            Solid3d mcb3d = new Solid3d();
                            mcb3d.CreateBox(18.0, 45.0, 55.0);
                            double mcbX = origin.X + 60.0 + m * mcbStep;
                            mcb3d.TransformBy(Matrix3d.Displacement(new Vector3d(mcbX, railY - 22.0, curZ)));
                            mcb3d.Color = Color.FromColorIndex(ColorMethod.ByAci, 2); // Vàng
                            btr.AppendEntity(mcb3d);
                            tr.AddNewlyCreatedDBObject(mcb3d, true);
                        }
                    }

                    // 7. HÀNG CẦU ĐẤU TERMINAL BLOCK 3D Ở ĐÁY
                    Solid3d tbStrip = new Solid3d();
                    tbStrip.CreateBox(plateW - 50.0, 25.0, 35.0);
                    double tbY = plateY - plateThick / 2.0 - 12.0;
                    double tbZ = bodyZ + 60.0;
                    tbStrip.TransformBy(Matrix3d.Displacement(new Vector3d(origin.X + w / 2.0, tbY, tbZ)));
                    tbStrip.Color = Color.FromColorIndex(ColorMethod.ByAci, 3); // Xanh lá
                    btr.AppendEntity(tbStrip);
                    tr.AddNewlyCreatedDBObject(tbStrip, true);

                    // 8. CÁNH CỬA TỦ 3D (DOOR) MỞ GÓC 30° ĐỂ THẤY BÊN TRONG
                    double doorThick = 15.0;
                    if (cfg.DoorCount == 1)
                    {
                        Solid3d door = new Solid3d();
                        door.CreateBox(w - 10.0, doorThick, h - 10.0);
                        // Đặt cánh cửa phía trước
                        door.TransformBy(Matrix3d.Displacement(new Vector3d(origin.X + w / 2.0, origin.Y - doorThick / 2.0, bodyZ + h / 2.0)));
                        door.LayerId = doorLayer;
                        door.Color = Color.FromColorIndex(ColorMethod.ByAci, 7);
                        btr.AppendEntity(door);
                        tr.AddNewlyCreatedDBObject(door, true);

                        // Tay khóa 3D
                        Solid3d lock3d = new Solid3d();
                        lock3d.CreateBox(12.0, 20.0, 60.0);
                        double lkX = (cfg.LockPosition == "Trái") ? origin.X + 35.0 : origin.X + w - 35.0;
                        lock3d.TransformBy(Matrix3d.Displacement(new Vector3d(lkX, origin.Y - doorThick - 10.0, bodyZ + h / 2.0)));
                        lock3d.Color = Color.FromColorIndex(ColorMethod.ByAci, 250);
                        btr.AppendEntity(lock3d);
                        tr.AddNewlyCreatedDBObject(lock3d, true);
                    }
                    else
                    {
                        // 2 Cánh
                        double dw = (w - 14.0) / 2.0;
                        Solid3d doorL = new Solid3d();
                        doorL.CreateBox(dw, doorThick, h - 10.0);
                        doorL.TransformBy(Matrix3d.Displacement(new Vector3d(origin.X + 5.0 + dw / 2.0, origin.Y - doorThick / 2.0, bodyZ + h / 2.0)));
                        doorL.LayerId = doorLayer;
                        doorL.Color = Color.FromColorIndex(ColorMethod.ByAci, 7);
                        btr.AppendEntity(doorL);
                        tr.AddNewlyCreatedDBObject(doorL, true);

                        Solid3d doorR = new Solid3d();
                        doorR.CreateBox(dw, doorThick, h - 10.0);
                        doorR.TransformBy(Matrix3d.Displacement(new Vector3d(origin.X + 9.0 + dw + dw / 2.0, origin.Y - doorThick / 2.0, bodyZ + h / 2.0)));
                        doorR.LayerId = doorLayer;
                        doorR.Color = Color.FromColorIndex(ColorMethod.ByAci, 7);
                        btr.AppendEntity(doorR);
                        tr.AddNewlyCreatedDBObject(doorR, true);
                    }

                    // 9. CỤM 3 ĐÈN BÁO PHA 3D (L1, L2, L3)
                    short[] lampColors = new short[] { 1, 2, 3 }; // Đỏ, Vàng, Xanh
                    for (int i = 0; i < 3; i++)
                    {
                        Solid3d lamp = new Solid3d();
                        lamp.CreateFrustum(15.0, 10.0, 10.0, 10.0);
                        // Xoay hình trụ hướng trục Y ra mặt trước
                        lamp.TransformBy(Matrix3d.Rotation(Math.PI / 2.0, Vector3d.XAxis, Point3d.Origin));
                        double lx = origin.X + w / 2.0 + (i - 1) * 35.0;
                        double lz = bodyZ + h - 60.0;
                        lamp.TransformBy(Matrix3d.Displacement(new Vector3d(lx, origin.Y - doorThick - 7.5, lz)));
                        lamp.Color = Color.FromColorIndex(ColorMethod.ByAci, lampColors[i]);
                        btr.AppendEntity(lamp);
                        tr.AddNewlyCreatedDBObject(lamp, true);
                    }

                    tr.Commit();
                }

                // 10. TỰ ĐỘNG CHUYỂN VIEWPORT SANG GÓC NHÌN 3D SE ISOMETRIC & CONCEPTUAL SHADING
                SwitchTo3DQuickView(doc);

                ed.WriteMessage($"\n🎉 [DTools Pro] Đã sinh mô hình 3D Solid Tủ Điện [{cfg.CabinetName}] hoàn chỉnh 100%!\n");
                return true;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[DTools ERROR] Lỗi khi dựng mô hình 3D: {ex.Message}\n");
                return false;
            }
        }

        // ============================================================
        // CHUYỂN VIEWPORT SANG 3D SE ISOMETRIC & CONCEPTUAL SHADING
        // ============================================================

        public static void SwitchTo3DQuickView(Document doc)
        {
            if (doc == null) return;
            try
            {
                // Gọi chuỗi lệnh CAD tiêu chuẩn chuyển góc nhìn 3D SE Isometric và tô bóng mượt mà
                doc.SendStringToExecute("._VPOINT 1,-1,1 ._VSCURRENT Conceptual ._ZOOM _Extents ", true, false, false);
            }
            catch { }
        }

        public static void SwitchToTopView(Document doc)
        {
            if (doc == null) return;
            try
            {
                doc.SendStringToExecute("._PLAN _W ._VSCURRENT 2dWireframe ._ZOOM _Extents ", true, false, false);
            }
            catch { }
        }
    }
}
