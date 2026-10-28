using System;
using System.Collections.Generic;
using System.IO;
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

        private static readonly Dictionary<string, System.Windows.Media.ImageSource> _thumbnailCache =
            new Dictionary<string, System.Windows.Media.ImageSource>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Đọc trực tiếp block thumbnail từ file .dwg trong thư mục Symbols (Database.ReadDwgFile + Database.ThumbnailBitmap)
        /// Cung cấp bộ nhớ đệm cache tự động để tối ưu hiệu năng và RAM
        /// </summary>
        public static System.Windows.Media.ImageSource GetDwgThumbnail(string dwgPath)
        {
            if (string.IsNullOrEmpty(dwgPath) || !File.Exists(dwgPath))
                return null;

            lock (_thumbnailCache)
            {
                if (_thumbnailCache.TryGetValue(dwgPath, out var cached))
                    return cached;
            }

            try
            {
                using (Database extDb = new Database(false, true))
                {
                    extDb.ReadDwgFile(dwgPath, FileOpenMode.OpenForReadAndAllShare, true, "");
                    using (System.Drawing.Bitmap bmp = extDb.ThumbnailBitmap)
                    {
                        if (bmp != null)
                        {
                            using (var ms = new MemoryStream())
                            {
                                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                ms.Position = 0;
                                var bi = new System.Windows.Media.Imaging.BitmapImage();
                                bi.BeginInit();
                                bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                bi.StreamSource = ms;
                                bi.EndInit();
                                bi.Freeze();

                                lock (_thumbnailCache)
                                {
                                    _thumbnailCache[dwgPath] = bi;
                                }
                                return bi;
                            }
                        }
                    }
                }
            }
            catch { }

            lock (_thumbnailCache)
            {
                _thumbnailCache[dwgPath] = null;
            }
            return null;
        }

        /// <summary>
        /// Tự động tính toán Scale Factor tối ưu cho ký hiệu dựa trên:
        /// 1. Đơn vị bản vẽ (INSUNITS: Millimeters, Meters, Inches, v.v.)
        /// 2. Tỷ lệ kích thước DIMSCALE
        /// 3. Tỷ lệ ghi chú CANNOSCALE (Annotation Scale)
        /// 4. Kích thước khung nhìn hiện tại (VIEWSIZE)
        /// </summary>
        public static double CalculateAutoSymbolScale(Database db, double userScale = 1.0, double baseSymbolSize = 50.0)
        {
            if (db == null) return userScale > 0 ? userScale : 1.0;

            double scaleFactor = userScale > 0 ? userScale : 1.0;

            try
            {
                // Áp dụng cấu hình mặc định nếu có
                if (DTools.Core.DToolsConfiguration.DefaultSymbolScale > 0 && Math.Abs(scaleFactor - 1.0) < 0.001)
                {
                    scaleFactor = DTools.Core.DToolsConfiguration.DefaultSymbolScale;
                }

                // 1. Kiểm tra đơn vị bản vẽ INSUNITS
                UnitsValue units = db.Insunits;

                // 2. Lấy tỷ lệ DIMSCALE
                double dimScale = db.Dimscale;

                // 3. Lấy Annotation Scale (CANNOSCALE)
                try
                {
                    var annoScale = db.Cannoscale;
                    if (annoScale != null && annoScale.Scale > 0 && annoScale.Scale < 1.0)
                    {
                        double annoFactor = 1.0 / annoScale.Scale;
                        if (annoFactor > dimScale)
                        {
                            dimScale = annoFactor;
                        }
                    }
                }
                catch { }

                // 4. Nếu bản vẽ đặt đơn vị là METERS (UnitsValue.Meters = 6)
                if (units == UnitsValue.Meters)
                {
                    if (dimScale > 1.0)
                    {
                        scaleFactor = scaleFactor * (dimScale * 0.001);
                    }
                    else
                    {
                        scaleFactor = scaleFactor * 0.001;
                    }
                    if (scaleFactor < 0.005) scaleFactor = 0.005;
                    return scaleFactor;
                }

                // 5. Nếu bản vẽ là MILLIMETERS (4) hoặc UNITLESS (0)
                if (userScale != 1.0 && userScale > 0)
                {
                    scaleFactor = userScale;
                }
                else if (dimScale > 1.0 && dimScale <= 1000.0)
                {
                    scaleFactor = dimScale;
                }
                else
                {
                    try
                    {
                        object vsObj = Application.GetSystemVariable("VIEWSIZE");
                        if (vsObj != null)
                        {
                            double viewSize = Convert.ToDouble(vsObj);
                            if (viewSize > 2500.0)
                            {
                                double sensibleScale = Math.Round(viewSize * 0.035 / (baseSymbolSize > 0 ? baseSymbolSize : 50.0));
                                if (sensibleScale >= 2.0 && sensibleScale <= 500.0)
                                {
                                    scaleFactor = sensibleScale;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return Math.Max(0.001, scaleFactor);
        }

        /// <summary>
        /// Cập nhật đồ họa hiển thị bản vẽ tức thì bằng FlushGraphics(), Regen() và UpdateScreen()
        /// </summary>
        public static void RefreshScreenAndGraphics(Editor ed)
        {
            try
            {
                Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
            }
            catch { }

            try
            {
                ed?.Regen();
            }
            catch { }

            try
            {
                ed?.UpdateScreen();
            }
            catch { }
        }
    }
}
