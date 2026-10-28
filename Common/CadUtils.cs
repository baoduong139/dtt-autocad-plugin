using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace DTT_AutoCAD_Plugin.Common
{
    public static class CadUtils
    {
        public static ObjectId GetOrCreateLayer(Database db, string layerName, short colorIndex = 7, LineWeight lineWeight = LineWeight.ByLineWeightDefault)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (lt.Has(layerName))
                {
                    ObjectId layerId = lt[layerName];
                    tr.Commit();
                    return layerId;
                }

                lt.UpgradeOpen();
                LayerTableRecord ltr = new LayerTableRecord
                {
                    Name = layerName,
                    Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex),
                    LineWeight = lineWeight
                };

                ObjectId newLayerId = lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
                tr.Commit();
                return newLayerId;
            }
        }

        public static ObjectId GetOrCreateUnicodeStyle(Database db, string styleName = "DTT_UNICODE", string fontFace = "arial.ttf")
        {
            if (db == null) return ObjectId.Null;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    TextStyleTable tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                    if (tst.Has(styleName))
                    {
                        ObjectId styleId = tst[styleName];
                        tr.Commit();
                        return styleId;
                    }

                    tst.UpgradeOpen();
                    TextStyleTableRecord tstr = new TextStyleTableRecord
                    {
                        Name = styleName,
                        FileName = fontFace,
                        Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor("Arial", false, false, 0, 34)
                    };

                    ObjectId newStyleId = tst.Add(tstr);
                    tr.AddNewlyCreatedDBObject(tstr, true);
                    tr.Commit();
                    return newStyleId;
                }
            }
            catch
            {
                return db.Textstyle; // Luôn đảm bảo có Text Style hợp lệ, không bao giờ gây eKeyNotFound
            }
        }

        public static ObjectId EnsureLinetype(Database db, string linetypeName = "HIDDEN")
        {
            if (db == null) return ObjectId.Null;
            try
            {
                // 1. Kiểm tra linetype đã có trong LinetypeTable chưa
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LinetypeTable lt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                    if (lt.Has(linetypeName))
                    {
                        ObjectId ltId = lt[linetypeName];
                        tr.Commit();
                        return ltId;
                    }
                    tr.Commit();
                }

                // 2. Nạp file ngoài transaction để tránh xung đột khóa LinetypeTable trong AutoCAD
                try
                {
                    db.LoadLineTypeFile(linetypeName, "acad.lin");
                }
                catch
                {
                    try
                    {
                        db.LoadLineTypeFile(linetypeName, "acadiso.lin");
                    }
                    catch { }
                }

                // 3. Lấy lại ObjectId sau khi nạp
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LinetypeTable lt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                    if (lt.Has(linetypeName))
                    {
                        ObjectId ltId = lt[linetypeName];
                        tr.Commit();
                        return ltId;
                    }
                    tr.Commit();
                }
            }
            catch { }
            return db.ByLayerLinetype;
        }

        public static void SafeSetLinetype(Entity ent, Database db, string linetypeName = "HIDDEN")
        {
            if (ent == null || db == null) return;
            try
            {
                ObjectId ltId = EnsureLinetype(db, linetypeName);
                if (ltId != ObjectId.Null && ltId != db.ByLayerLinetype && ltId.IsValid)
                {
                    ent.LinetypeId = ltId;
                }
            }
            catch
            {
                // Tuyệt đối không để crash hoặc ném exception eKeyNotFound
            }
        }

        public static void FocusDwgView()
        {
            try
            {
                Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
            }
            catch
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null && doc.Window != null)
                {
                    doc.Window.Focus();
                }
            }
        }
    }
}
