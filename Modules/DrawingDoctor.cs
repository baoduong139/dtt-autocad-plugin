using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace DTT_AutoCAD_Plugin.Modules
{
    public static class DrawingDoctor
    {
        public static string FixFontVietnamese(Document doc)
        {
            if (doc == null) return "Không tìm thấy bản vẽ!";
            Database db = doc.Database;
            Editor ed = doc.Editor;
            int count = 0;

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    TextStyleTable tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForWrite);
                    foreach (ObjectId tsId in tst)
                    {
                        try
                        {
                            TextStyleTableRecord tstr = (TextStyleTableRecord)tr.GetObject(tsId, OpenMode.ForWrite);
                            string fn = tstr.FileName?.ToLower() ?? "";
                            if (string.IsNullOrEmpty(fn) || fn.Contains("vn") || fn.Contains("vni") || fn.Contains(".vn") || fn.Contains("txt.shx"))
                            {
                                tstr.FileName = "arial.ttf";
                                tstr.BigFontFileName = "";
                                count++;
                            }
                        }
                        catch { }
                    }
                    tr.Commit();
                    ed.Regen();
                }

                string msg = $"Chuẩn hóa Font hoàn tất: Đã cập nhật {count} kiểu chữ sang Arial Unicode!";
                ed.WriteMessage($"\n[DTT BÁC SĨ] {msg}\n");
                return msg;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[DTT BÁC SĨ LỖI]: {ex.Message}\n");
                return $"Lỗi Fix Font: {ex.Message}";
            }
        }

        public static string FlattenZ(Document doc)
        {
            if (doc == null) return "Không tìm thấy bản vẽ!";
            Database db = doc.Database;
            Editor ed = doc.Editor;
            int flattened = 0;
            Vector3d norm = new Vector3d(0, 0, 1);

            try
            {
                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    foreach (ObjectId btrId in bt)
                    {
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                        if (!btr.IsLayout) continue;

                        btr.UpgradeOpen();
                        foreach (ObjectId entId in btr)
                        {
                            if (entId.IsNull || entId.IsErased) continue;
                            try
                            {
                                Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                                if (ent == null) continue;

                                // Bỏ qua layer bị khóa (locked layer) để không gây crash
                                LayerTableRecord ltr = tr.GetObject(ent.LayerId, OpenMode.ForRead) as LayerTableRecord;
                                if (ltr != null && ltr.IsLocked) continue;

                                ent.UpgradeOpen();

                                if (ent is Line line)
                                {
                                    line.StartPoint = new Point3d(line.StartPoint.X, line.StartPoint.Y, 0);
                                    line.EndPoint = new Point3d(line.EndPoint.X, line.EndPoint.Y, 0);
                                    flattened++;
                                }
                                else if (ent is Polyline pl)
                                {
                                    pl.Elevation = 0;
                                    pl.Normal = norm;
                                    flattened++;
                                }
                                else if (ent is Circle c)
                                {
                                    c.Center = new Point3d(c.Center.X, c.Center.Y, 0);
                                    c.Normal = norm;
                                    flattened++;
                                }
                                else if (ent is DBText txt)
                                {
                                    txt.Position = new Point3d(txt.Position.X, txt.Position.Y, 0);
                                    txt.Normal = norm;
                                    flattened++;
                                }
                            }
                            catch { }
                        }
                    }
                    tr.Commit();
                    ed.Regen();
                }

                string msg = $"Đưa về Z=0 thành công! Đã làm phẳng {flattened} đối tượng.";
                ed.WriteMessage($"\n[DTT BÁC SĨ] {msg}\n");
                return msg;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[DTT BÁC SĨ LỖI]: {ex.Message}\n");
                return $"Lỗi Flatten Z: {ex.Message}";
            }
        }
    }
}
