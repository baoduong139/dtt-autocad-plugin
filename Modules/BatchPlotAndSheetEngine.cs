using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.Modules
{
    public class DetectedSheetFrame
    {
        public ObjectId EntityId { get; set; }
        public Extents3d Extents { get; set; }
        public double MinX => Extents.MinPoint.X;
        public double MaxX => Extents.MaxPoint.X;
        public double MinY => Extents.MinPoint.Y;
        public double MaxY => Extents.MaxPoint.Y;
        public double Width => MaxX - MinX;
        public double Height => MaxY - MinY;
        public int SheetIndex { get; set; }
        public string SheetCode { get; set; } = "";
        public string PaperSizeName { get; set; } = "A3";
    }

    public static class BatchPlotAndSheetEngine
    {
        /// <summary>
        /// Tự động quét Model Space để tìm tất cả các khung bản vẽ (Block hoặc Polyline hình chữ nhật chuẩn A1, A2, A3, A4)
        /// </summary>
        public static List<DetectedSheetFrame> DetectSheetFramesInModel(Database db)
        {
            List<DetectedSheetFrame> frames = new List<DetectedSheetFrame>();
            if (db == null) return frames;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in modelSpace)
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // 1. Nhận diện nếu là Block Khung Tên (BlockReference)
                    if (ent is BlockReference blkRef)
                    {
                        var ext = blkRef.GeometricExtents;
                        double w = ext.MaxPoint.X - ext.MinPoint.X;
                        double h = ext.MaxPoint.Y - ext.MinPoint.Y;

                        // Tỷ lệ khổ giấy A3/A4/A1 ~ 1.414 (ngang hoặc đứng) và kích thước tối thiểu
                        if (w > 200 && h > 200)
                        {
                            double ratio = Math.Max(w, h) / Math.Min(w, h);
                            if (ratio >= 1.25 && ratio <= 1.6)
                            {
                                frames.Add(new DetectedSheetFrame
                                {
                                    EntityId = id,
                                    Extents = ext,
                                    PaperSizeName = GuessPaperSize(w, h)
                                });
                            }
                        }
                    }
                    // 2. Nhận diện nếu là Polyline khép kín (Khung vẽ dạng hình chữ nhật)
                    else if (ent is Polyline poly && poly.Closed && (poly.NumberOfVertices == 4 || poly.NumberOfVertices == 5))
                    {
                        var ext = poly.GeometricExtents;
                        double w = ext.MaxPoint.X - ext.MinPoint.X;
                        double h = ext.MaxPoint.Y - ext.MinPoint.Y;

                        if (w > 250 && h > 180)
                        {
                            double ratio = Math.Max(w, h) / Math.Min(w, h);
                            if (ratio >= 1.25 && ratio <= 1.6)
                            {
                                frames.Add(new DetectedSheetFrame
                                {
                                    EntityId = id,
                                    Extents = ext,
                                    PaperSizeName = GuessPaperSize(w, h)
                                });
                            }
                        }
                    }
                }

                tr.Commit();
            }

            // Sắp xếp thứ tự bản vẽ: Từ Trên xuống Dưới (Y giảm dần), Từ Trái qua Phải (X tăng dần)
            frames = SortFramesReadingOrder(frames);

            // Gán số thứ tự tự động
            for (int i = 0; i < frames.Count; i++)
            {
                frames[i].SheetIndex = i + 1;
                frames[i].SheetCode = $"BẢN VẼ SỐ: {i + 1:D2}";
            }

            return frames;
        }

        private static string GuessPaperSize(double w, double h)
        {
            double maxDim = Math.Max(w, h);
            if (maxDim <= 350) return "A4";
            if (maxDim <= 500) return "A3";
            if (maxDim <= 700) return "A2";
            if (maxDim <= 1000) return "A1";
            return "A0 / Scale";
        }

        private static List<DetectedSheetFrame> SortFramesReadingOrder(List<DetectedSheetFrame> list)
        {
            if (list == null || list.Count <= 1) return list;

            // Nhóm theo hàng (Y tương đương với dung sai 15% chiều cao khung)
            double avgH = list.Average(f => f.Height);
            double rowTolerance = avgH * 0.4;

            return list
                .OrderByDescending(f => Math.Round(f.MaxY / rowTolerance) * rowTolerance)
                .ThenBy(f => f.MinX)
                .ToList();
        }

        /// <summary>
        /// Đánh số thứ tự bản vẽ tự động trực tiếp lên bản vẽ
        /// </summary>
        public static int AutoRenumberSheets(Database db, string prefix = "E-", int startNumber = 1)
        {
            var frames = DetectSheetFramesInModel(db);
            if (frames.Count == 0) return 0;

            ObjectId layerText = CadUtils.GetOrCreateLayer(db, "A-SHEET-NUM", 1, LineWeight.LineWeight035);
            ObjectId styleId = CadUtils.GetOrCreateUnicodeStyle(db);

            int count = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                for (int i = 0; i < frames.Count; i++)
                {
                    var f = frames[i];
                    int sheetNo = startNumber + i;
                    string code = $"{prefix}{sheetNo:D2}";

                    // Điểm đặt số bản vẽ ở góc dưới bên phải khung tên (khoảng offset 80mm từ góc phải dưới)
                    Point3d textPos = new Point3d(f.MaxX - (f.Width * 0.12), f.MinY + (f.Height * 0.05), 0);
                    double textHeight = Math.Max(25.0, f.Height * 0.015);

                    DBText numText = new DBText
                    {
                        Position = textPos,
                        Height = textHeight,
                        TextString = code,
                        TextStyleId = styleId,
                        LayerId = layerText
                    };
                    btr.AppendEntity(numText);
                    tr.AddNewlyCreatedDBObject(numText, true);
                    count++;
                }

                tr.Commit();
            }

            return count;
        }

        /// <summary>
        /// Tự động in hàng loạt danh sách các khung sang file PDF (dùng DWG To PDF.pc3)
        /// </summary>
        public static int ExecuteBatchPlotToPdf(Database db, string outputFolder, string filePrefix = "BanVe_")
        {
            if (db == null) return 0;
            var frames = DetectSheetFramesInModel(db);
            if (frames.Count == 0) return 0;

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            int successCount = 0;

            try
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    string pdfFileName = Path.Combine(outputFolder, $"{filePrefix}{i + 1:D2}.pdf");

                    // Thực thi Plot cho khung hiện tại
                    bool ok = PlotSingleWindowToPdf(db, frame.Extents, pdfFileName);
                    if (ok) successCount++;
                }
            }
            catch
            {
                // Fallback nếu máy người dùng không bật được Plot Engine
            }

            return successCount;
        }

        private static bool PlotSingleWindowToPdf(Database db, Extents3d extents, string targetPdfPath)
        {
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                    Layout layout = (Layout)tr.GetObject(btr.LayoutId, OpenMode.ForRead);

                    PlotInfo pi = new PlotInfo { Layout = btr.LayoutId };
                    PlotSettings ps = new PlotSettings(layout.ModelType);
                    ps.CopyFrom(layout);

                    PlotSettingsValidator psv = PlotSettingsValidator.Current;
                    psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                    psv.SetPlotWindowArea(ps, new Extents2d(extents.MinPoint.X, extents.MinPoint.Y, extents.MaxPoint.X, extents.MaxPoint.Y));
                    psv.SetPlotConfigurationName(ps, "DWG To PDF.pc3", "ISO_A3_(420.00_x_297.00_MM)");
                    psv.SetPlotCentered(ps, true);
                    psv.SetUseStandardScale(ps, true);
                    psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);

                    pi.OverrideSettings = ps;
                    PlotInfoValidator piv = new PlotInfoValidator { MediaMatchingPolicy = MatchingPolicy.MatchEnabled };
                    piv.Validate(pi);

                    if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                    {
                        using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                        {
                            pe.BeginPlot(null, null);
                            pe.BeginDocument(pi, db.Filename, null, 1, true, targetPdfPath);

                            PlotPageInfo ppi = new PlotPageInfo();
                            pe.BeginPage(ppi, pi, true, null);
                            pe.BeginGenerateGraphics(null);
                            pe.EndGenerateGraphics(null);
                            pe.EndPage(null);

                            pe.EndDocument(null);
                            pe.EndPlot(null);
                            return true;
                        }
                    }
                    tr.Commit();
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
