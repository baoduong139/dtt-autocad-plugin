using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace DTT_AutoCAD_Plugin.Common
{
    /// <summary>
    /// SERVICE QUẢN LÝ & CHÈN DYNAMIC BLOCKS NỘI BỘ DTOOLS
    /// Tự động xác định đường dẫn động tới Dtools/Blocks/
    /// Hỗ trợ nạp DWG, chèn BlockReference và tự động cấu hình Dynamic Properties (Kích thước, Flip, Visibility)
    /// </summary>
    public static class DtoolsBlockService
    {
        private const string BLOCKS_DIR_NAME = "Blocks";

        #region 1. Xác Định Đường Dẫn Thư Mục Blocks Nội Bộ

        /// <summary>
        /// Xác định đường dẫn thư mục Blocks tương đối từ vị trí Dtools đang chạy
        /// </summary>
        public static string GetBlocksDirectory()
        {
            return DTools.Core.ResourceLocator.GetBlocksDirectory();
        }

        /// <summary>
        /// Lấy đường dẫn đầy đủ tới 1 file block DWG trong thư viện (tự động dò tìm trong Blocks/ và Symbols/)
        /// </summary>
        public static string GetBlockFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "";

            string cleanName = Path.GetFileNameWithoutExtension(fileName);
            string withExt = cleanName + ".dwg";
            string cleanRel = fileName.Replace('/', '\\');

            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string[] candidateDirs = new string[]
            {
                Path.Combine(assemblyPath, "Blocks"),
                Path.Combine(assemblyPath, "Symbols"),
                assemblyPath,
                Path.Combine(assemblyPath, "Contents", "Blocks"),
                Path.Combine(assemblyPath, "Contents", "Symbols"),
                GetBlocksDirectory()
            };

            foreach (var dir in candidateDirs)
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;

                // 1. Khớp đường dẫn tương đối trực tiếp
                string p1 = Path.Combine(dir, cleanRel);
                if (File.Exists(p1)) return p1;

                // 2. Khớp theo tên tệp trong thư mục
                string p2 = Path.Combine(dir, withExt);
                if (File.Exists(p2)) return p2;

                // 3. Quét đệ quy tìm tệp trong các thư mục con
                try
                {
                    string[] matches = Directory.GetFiles(dir, withExt, SearchOption.AllDirectories);
                    if (matches != null && matches.Length > 0)
                        return matches[0];
                }
                catch { }
            }

            // Fallback thư mục Blocks mặc định
            string blocksDir = GetBlocksDirectory();
            return Path.Combine(blocksDir, withExt);
        }

        #endregion

        #region 2. Tự Động Khởi Tạo 3 File Block Chuẩn Mẫu (Nếu Chưa Có)

        /// <summary>
        /// Tự sinh 3 file Block DWG chuẩn nếu thư viện chưa có:
        /// Architectural_Doors.dwg, Architectural_Columns.dwg, Annotations.dwg
        /// </summary>
        public static void EnsureDefaultBlocksExist()
        {
            try
            {
                string blocksDir = GetBlocksDirectory();
                if (!Directory.Exists(blocksDir))
                    Directory.CreateDirectory(blocksDir);

                string doorDwg = Path.Combine(blocksDir, "Architectural_Doors.dwg");
                string colDwg = Path.Combine(blocksDir, "Architectural_Columns.dwg");
                string annoDwg = Path.Combine(blocksDir, "Annotations.dwg");

                if (!File.Exists(doorDwg)) CreateDoorTemplateDwg(doorDwg);
                if (!File.Exists(colDwg)) CreateColumnTemplateDwg(colDwg);
                if (!File.Exists(annoDwg)) CreateAnnotationTemplateDwg(annoDwg);
            }
            catch (System.Exception ex)
            {
                DtoolsResourceManager.PrintMessage($"\n⚠️ [Dtools Blocks]: Khởi tạo mẫu block: {ex.Message}");
            }
        }

        private static void CreateDoorTemplateDwg(string filePath)
        {
            using (Database db = new Database(true, true))
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // 1. Khuôn cửa trái (50x110)
                    Polyline jambLeft = new Polyline(4);
                    jambLeft.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
                    jambLeft.AddVertexAt(1, new Point2d(50, 0), 0, 0, 0);
                    jambLeft.AddVertexAt(2, new Point2d(50, 110), 0, 0, 0);
                    jambLeft.AddVertexAt(3, new Point2d(0, 110), 0, 0, 0);
                    jambLeft.Closed = true;
                    jambLeft.Color = Color.FromColorIndex(ColorMethod.ByAci, 1); // Đỏ
                    ms.AppendEntity(jambLeft);
                    tr.AddNewlyCreatedDBObject(jambLeft, true);

                    // 2. Khuôn cửa phải (cách 900mm)
                    Polyline jambRight = new Polyline(4);
                    jambRight.AddVertexAt(0, new Point2d(900, 0), 0, 0, 0);
                    jambRight.AddVertexAt(1, new Point2d(950, 0), 0, 0, 0);
                    jambRight.AddVertexAt(2, new Point2d(950, 110), 0, 0, 0);
                    jambRight.AddVertexAt(3, new Point2d(900, 110), 0, 0, 0);
                    jambRight.Closed = true;
                    jambRight.Color = Color.FromColorIndex(ColorMethod.ByAci, 1);
                    ms.AppendEntity(jambRight);
                    tr.AddNewlyCreatedDBObject(jambRight, true);

                    // 3. Cánh cửa mở 90 độ (dày 40mm, rộng 850mm)
                    Polyline doorLeaf = new Polyline(4);
                    doorLeaf.AddVertexAt(0, new Point2d(50, 0), 0, 0, 0);
                    doorLeaf.AddVertexAt(1, new Point2d(50, 850), 0, 0, 0);
                    doorLeaf.AddVertexAt(2, new Point2d(90, 850), 0, 0, 0);
                    doorLeaf.AddVertexAt(3, new Point2d(90, 0), 0, 0, 0);
                    doorLeaf.Closed = true;
                    doorLeaf.Color = Color.FromColorIndex(ColorMethod.ByAci, 4); // Cyan
                    ms.AppendEntity(doorLeaf);
                    tr.AddNewlyCreatedDBObject(doorLeaf, true);

                    // 4. Cung tròn quét góc mở cửa (bán kính 850mm)
                    Arc swingArc = new Arc(new Point3d(50, 0, 0), 850, 0, Math.PI / 2.0);
                    swingArc.Color = Color.FromColorIndex(ColorMethod.ByAci, 8); // Xám mảnh
                    ms.AppendEntity(swingArc);
                    tr.AddNewlyCreatedDBObject(swingArc, true);

                    // 5. Thuộc tính TAG cửa (D1 900x2200)
                    AttributeDefinition attTag = new AttributeDefinition
                    {
                        Position = new Point3d(475, 450, 0),
                        Tag = "DOOR_TAG",
                        Prompt = "Ký hiệu cửa:",
                        TextString = "D1 (900x2200)",
                        Height = 65,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, 2) // Vàng
                    };
                    ms.AppendEntity(attTag);
                    tr.AddNewlyCreatedDBObject(attTag, true);

                    tr.Commit();
                }
                db.SaveAs(filePath, DwgVersion.Current);
            }
        }

        private static void CreateColumnTemplateDwg(string filePath)
        {
            using (Database db = new Database(true, true))
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // 1. Thân cột bê tông 220x220 (tâm tại 0,0)
                    Polyline colOutline = new Polyline(4);
                    colOutline.AddVertexAt(0, new Point2d(-110, -110), 0, 0, 0);
                    colOutline.AddVertexAt(1, new Point2d(110, -110), 0, 0, 0);
                    colOutline.AddVertexAt(2, new Point2d(110, 110), 0, 0, 0);
                    colOutline.AddVertexAt(3, new Point2d(-110, 110), 0, 0, 0);
                    colOutline.Closed = true;
                    colOutline.Color = Color.FromColorIndex(ColorMethod.ByAci, 3); // Green
                    ms.AppendEntity(colOutline);
                    tr.AddNewlyCreatedDBObject(colOutline, true);

                    // 2. Tim trục chéo (Centerlines)
                    Line lineH = new Line(new Point3d(-160, 0, 0), new Point3d(160, 0, 0))
                    {
                        Color = Color.FromColorIndex(ColorMethod.ByAci, 1)
                    };
                    Line lineV = new Line(new Point3d(0, -160, 0), new Point3d(0, 160, 0))
                    {
                        Color = Color.FromColorIndex(ColorMethod.ByAci, 1)
                    };
                    ms.AppendEntity(lineH);
                    tr.AddNewlyCreatedDBObject(lineH, true);
                    ms.AppendEntity(lineV);
                    tr.AddNewlyCreatedDBObject(lineV, true);

                    // 3. Ký hiệu cột
                    AttributeDefinition attCol = new AttributeDefinition
                    {
                        Position = new Point3d(0, 0, 0),
                        Tag = "COL_TAG",
                        Prompt = "Ký hiệu cột:",
                        TextString = "C1",
                        Height = 60,
                        HorizontalMode = TextHorizontalMode.TextCenter,
                        VerticalMode = TextVerticalMode.TextVerticalMid,
                        AlignmentPoint = new Point3d(0, 0, 0),
                        Color = Color.FromColorIndex(ColorMethod.ByAci, 2)
                    };
                    ms.AppendEntity(attCol);
                    tr.AddNewlyCreatedDBObject(attCol, true);

                    tr.Commit();
                }
                db.SaveAs(filePath, DwgVersion.Current);
            }
        }

        private static void CreateAnnotationTemplateDwg(string filePath)
        {
            using (Database db = new Database(true, true))
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // 1. Mũi tên cao độ tam giác ngược (chóp tại 0,0)
                    Polyline arrow = new Polyline(3);
                    arrow.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
                    arrow.AddVertexAt(1, new Point2d(-60, 60), 0, 0, 0);
                    arrow.AddVertexAt(2, new Point2d(60, 60), 0, 0, 0);
                    arrow.Closed = true;
                    arrow.Color = Color.FromColorIndex(ColorMethod.ByAci, 4); // Cyan
                    ms.AppendEntity(arrow);
                    tr.AddNewlyCreatedDBObject(arrow, true);

                    // 2. Đường gióng ngang
                    Line leadLine = new Line(new Point3d(-80, 60, 0), new Point3d(240, 60, 0))
                    {
                        Color = Color.FromColorIndex(ColorMethod.ByAci, 4)
                    };
                    ms.AppendEntity(leadLine);
                    tr.AddNewlyCreatedDBObject(leadLine, true);

                    // 3. Thuộc tính cao độ
                    AttributeDefinition attElev = new AttributeDefinition
                    {
                        Position = new Point3d(70, 75, 0),
                        Tag = "ELEVATION",
                        Prompt = "Nhập cao độ:",
                        TextString = "+0.000",
                        Height = 55,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, 2)
                    };
                    ms.AppendEntity(attElev);
                    tr.AddNewlyCreatedDBObject(attElev, true);

                    tr.Commit();
                }
                db.SaveAs(filePath, DwgVersion.Current);
            }
        }

        #endregion

        #region 3. Nạp & Chèn Block DWG Vào Database Hiện Hành

        /// <summary>
        /// Lấy danh sách tên các file Block DWG có sẵn trong thư mục Dtools/Blocks/ (không kèm đuôi .dwg)
        /// </summary>
        public static List<string> GetAvailableBlockNames()
        {
            List<string> list = new List<string>();
            try
            {
                string blocksDir = GetBlocksDirectory();
                if (Directory.Exists(blocksDir))
                {
                    string[] files = Directory.GetFiles(blocksDir, "*.dwg", SearchOption.TopDirectoryOnly);
                    foreach (string file in files)
                    {
                        list.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
            }
            catch { }

            // Đảm bảo luôn có 3 block chuẩn
            if (!list.Contains("Architectural_Doors")) list.Add("Architectural_Doors");
            if (!list.Contains("Architectural_Columns")) list.Add("Architectural_Columns");
            if (!list.Contains("Annotations")) list.Add("Annotations");

            return list;
        }

        /// <summary>
        /// Nạp định nghĩa Block từ file DWG bên ngoài vào Database đích (sử dụng WblockCloneObjects hoặc Insert)
        /// Bảo toàn 100% Dynamic Block Authoring Data và tham số động
        /// </summary>
        public static ObjectId ImportBlockFromDwg(Database targetDb, string dwgPath, string blockName)
        {
            using (Transaction tr = targetDb.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(targetDb.BlockTableId, OpenMode.ForRead);

                // 1. Nếu đã tồn tại block definition, trả về ID ngay
                if (bt.Has(blockName))
                {
                    ObjectId existingId = bt[blockName];
                    tr.Commit();
                    return existingId;
                }
                tr.Commit();
            }

            // 2. Nếu chưa có file DWG hợp lệ, thử tìm lại qua GetBlockFilePath
            if (!File.Exists(dwgPath))
            {
                dwgPath = GetBlockFilePath(blockName);
            }

            if (!File.Exists(dwgPath))
            {
                // Thử tự động tạo mẫu block nếu thuộc 3 mẫu mặc định
                EnsureDefaultBlocksExist();
                dwgPath = GetBlockFilePath(blockName);
            }

            if (!File.Exists(dwgPath))
                throw new FileNotFoundException($"Không tìm thấy file Block DWG: {dwgPath} (Tên block: {blockName})");

            // Nạp file DWG ngoài vào Database
            using (Database sourceDb = new Database(false, true))
            {
                sourceDb.ReadDwgFile(dwgPath, FileOpenMode.OpenForReadAndAllShare, true, "");

                ObjectId btrIdToClone = ObjectId.Null;
                using (Transaction srcTr = sourceDb.TransactionManager.StartTransaction())
                {
                    BlockTable srcBt = (BlockTable)srcTr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);

                    // 1. Tìm block có tên trùng khớp trước
                    if (srcBt.Has(blockName))
                    {
                        btrIdToClone = srcBt[blockName];
                    }
                    else
                    {
                        // 2. Tìm block động đầu tiên trong file DWG
                        foreach (ObjectId id in srcBt)
                        {
                            BlockTableRecord btr = (BlockTableRecord)srcTr.GetObject(id, OpenMode.ForRead);
                            if (!btr.IsLayout && !btr.IsAnonymous && !btr.Name.StartsWith("*"))
                            {
                                if (btr.IsDynamicBlock)
                                {
                                    btrIdToClone = id;
                                    break;
                                }
                                if (btrIdToClone.IsNull)
                                {
                                    btrIdToClone = id;
                                }
                            }
                        }
                    }
                    srcTr.Commit();
                }

                // Nếu tìm thấy BlockTableRecord (Dynamic Block), dùng WblockCloneObjects để giữ nguyên Extension Dictionary
                if (!btrIdToClone.IsNull)
                {
                    ObjectIdCollection ids = new ObjectIdCollection { btrIdToClone };
                    IdMapping mapping = new IdMapping();
                    sourceDb.WblockCloneObjects(ids, targetDb.BlockTableId, mapping, DuplicateRecordCloning.Replace, false);

                    ObjectId resId = mapping[btrIdToClone].Value;

                    // Đổi tên Block vừa clone thành blockName nếu tên gốc khác nhau
                    using (Transaction tr = targetDb.TransactionManager.StartTransaction())
                    {
                        BlockTableRecord clonedBtr = (BlockTableRecord)tr.GetObject(resId, OpenMode.ForWrite);
                        if (!clonedBtr.Name.Equals(blockName, StringComparison.OrdinalIgnoreCase))
                        {
                            BlockTable bt = (BlockTable)tr.GetObject(targetDb.BlockTableId, OpenMode.ForRead);
                            if (!bt.Has(blockName))
                            {
                                clonedBtr.Name = blockName;
                            }
                        }
                        tr.Commit();
                    }

                    return resId;
                }

                // 3. Nếu là file DWG nguyên khối (ModelSpace của DWG chính là Block)
                ObjectId insertedId = targetDb.Insert(blockName, sourceDb, true);
                return insertedId;
            }
        }

        /// <summary>
        /// Tạo BlockReference từ ObjectId của BlockTableRecord tại tọa độ chỉ định
        /// </summary>
        public static ObjectId InsertBlockReference(Database db, ObjectId btrId, Point3d insertPt, Scale3d scale, double rotationRad)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                BlockReference blkRef = new BlockReference(insertPt, btrId)
                {
                    ScaleFactors = scale,
                    Rotation = rotationRad
                };

                ObjectId blkRefId = ms.AppendEntity(blkRef);
                tr.AddNewlyCreatedDBObject(blkRef, true);

                // Đồng bộ thuộc tính AttributeDefinitions sang AttributeReferences
                if (btr.HasAttributeDefinitions)
                {
                    foreach (ObjectId id in btr)
                    {
                        DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                        if (obj is AttributeDefinition attDef && !attDef.Constant)
                        {
                            AttributeReference attRef = new AttributeReference();
                            attRef.SetAttributeFromBlock(attDef, blkRef.BlockTransform);
                            attRef.Position = attDef.Position.TransformBy(blkRef.BlockTransform);
                            attRef.TextString = attDef.TextString;

                            blkRef.AttributeCollection.AppendAttribute(attRef);
                            tr.AddNewlyCreatedDBObject(attRef, true);
                        }
                    }
                }

                tr.Commit();
                return blkRefId;
            }
        }

        /// <summary>
        /// Tạo BlockReference từ Tên Block tại tọa độ chỉ định trong ModelSpace
        /// </summary>
        public static ObjectId InsertBlockReference(Database db, string blockName, Point3d insertPt, Scale3d scale, double rotationRad)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(blockName))
                    throw new ArgumentException($"Block '{blockName}' chưa được định nghĩa trong bản vẽ!");

                ObjectId btrId = bt[blockName];
                tr.Commit();
                return InsertBlockReference(db, btrId, insertPt, scale, rotationRad);
            }
        }

        /// <summary>
        /// Chèn Block hoàn chỉnh từ Thư viện Dtools/Blocks/ vào bản vẽ hiện hành (Auto-Import + Auto-Insert)
        /// </summary>
        public static ObjectId InsertBlockFromLibrary(Database db, string blockNameOrFileName, Point3d insertPt, Scale3d scale, double rotationRad)
        {
            EnsureDefaultBlocksExist();

            string cleanName = Path.GetFileNameWithoutExtension(blockNameOrFileName);
            string dwgPath = GetBlockFilePath(cleanName);

            // 1. Nạp định nghĩa Block vào database bản vẽ (bảo toàn Dynamic Block)
            ObjectId btrId = ImportBlockFromDwg(db, dwgPath, cleanName);

            // 2. Chèn BlockReference tại tọa độ chỉ định bằng btrId
            return InsertBlockReference(db, btrId, insertPt, scale, rotationRad);
        }

        /// <summary>
        /// Hoàn thiện hàm chèn Block tương tác từ giao diện WPF / Lệnh CAD:
        /// 1. Kiểm tra khóa tài liệu (DocumentLock)
        /// 2. Lấy BlockTableRecord nếu đã tồn tại, hoặc nạp từ Symbols/Blocks DWG nếu chưa có
        /// 3. Yêu cầu người dùng chọn điểm đặt (Editor.GetPoint) và tạo BlockReference
        /// </summary>
        public static bool InsertBlockInteractive(Document doc, string blockNameOrFileName, double width = 0, double height = 0, bool flip = false)
        {
            if (doc == null)
            {
                doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return false;
            }

            Editor ed = doc.Editor;
            Database db = doc.Database;
            string cleanName = Path.GetFileNameWithoutExtension(blockNameOrFileName);

            try
            {
                using (DocumentLock docLock = doc.LockDocument())
                {
                    ObjectId btrId = ObjectId.Null;

                    // 1. Kiểm tra block đã có trong database chưa
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        if (bt.Has(cleanName))
                        {
                            btrId = bt[cleanName];
                        }
                        tr.Commit();
                    }

                    // 2. Nếu chưa có: nạp từ file DWG
                    if (btrId.IsNull || !btrId.IsValid)
                    {
                        string dwgPath = GetBlockFilePath(cleanName);
                        if (!File.Exists(dwgPath))
                        {
                            EnsureDefaultBlocksExist();
                            dwgPath = GetBlockFilePath(cleanName);
                        }

                        if (!File.Exists(dwgPath))
                        {
                            ed.WriteMessage($"\n❌ [DTools Lỗi]: Không tìm thấy file DWG cho block '{cleanName}' tại: {dwgPath}\n");
                            return false;
                        }

                        try
                        {
                            btrId = ImportBlockFromDwg(db, dwgPath, cleanName);
                            ed.WriteMessage($"\n[DTools] Đã nạp block '{cleanName}' từ tệp: {dwgPath}");
                        }
                        catch (System.Exception exImp)
                        {
                            ed.WriteMessage($"\n❌ [DTools Lỗi nạp Block '{cleanName}']: {exImp.Message}\n");
                            return false;
                        }
                    }

                    if (btrId.IsNull || !btrId.IsValid)
                    {
                        ed.WriteMessage($"\n❌ [DTools Lỗi]: Không thể tạo định nghĩa Block cho '{cleanName}'!\n");
                        return false;
                    }

                    // 3. Yêu cầu người dùng chỉ định điểm chèn (Editor.GetPoint)
                    PromptPointOptions ppo = new PromptPointOptions($"\n📍 [DTools] Click chọn điểm đặt Block '{cleanName}' [Enter/Esc để kết thúc]: ");
                    ppo.AllowNone = true;

                    int count = 0;
                    while (true)
                    {
                        PromptPointResult ppr = ed.GetPoint(ppo);
                        if (ppr.Status != PromptStatus.OK) break;

                        ObjectId blkRefId = InsertBlockReference(db, btrId, ppr.Value, new Scale3d(1, 1, 1), 0.0);
                        if (!blkRefId.IsNull)
                        {
                            // Gán thuộc tính dynamic block nếu có
                            if (width > 0)
                            {
                                SetDynamicBlockProperty(blkRefId, "Door Size", width);
                                SetDynamicBlockProperty(blkRefId, "W", width);
                                SetDynamicBlockProperty(blkRefId, "Distance", width);
                                SetDynamicBlockProperty(blkRefId, "Width", width);
                            }
                            if (height > 0)
                            {
                                SetDynamicBlockProperty(blkRefId, "H", height);
                            }
                            if (flip)
                            {
                                FlipDynamicBlock(blkRefId);
                            }

                            count++;
                            ppo.Message = $"\n📍 [DTools] Đã đặt {count} block '{cleanName}'. Click điểm tiếp theo [Enter/Esc để kết thúc]: ";
                        }
                        else
                        {
                            ed.WriteMessage($"\n❌ [DTools]: Không thể tạo thực thể BlockReference cho '{cleanName}'.");
                            break;
                        }
                    }

                    if (count > 0)
                    {
                        ed.WriteMessage($"\n✅ [DTools Pro] Đã hoàn thành chèn {count} block '{cleanName}'.\n");
                    }

                    return count > 0;
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n❌ [DTools Lỗi chèn Block]: {ex.Message}\n");
                return false;
            }
        }

        #endregion

        #region 4. Quản Lý & Tự Động Gán Thuộc Tính Dynamic Block

        /// <summary>
        /// Gán giá trị cho một tham số của Dynamic Block (Width, Length, Flip, Visibility...)
        /// Tự động chuyển đổi kiểu dữ liệu phù hợp (string, double, short)
        /// </summary>
        public static bool SetDynamicBlockProperty(ObjectId blkRefId, string propertyName, object value)
        {
            if (blkRefId.IsNull || blkRefId.IsErased) return false;

            using (Transaction tr = blkRefId.Database.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = tr.GetObject(blkRefId, OpenMode.ForWrite) as BlockReference;
                if (blkRef == null) return false;

                bool updated = false;

                if (blkRef.IsDynamicBlock)
                {
                    DynamicBlockReferencePropertyCollection props = blkRef.DynamicBlockReferencePropertyCollection;

                    // Pass 1: Tìm khớp chính xác tên thuộc tính (Exact Match)
                    DynamicBlockReferenceProperty matchedProp = null;
                    foreach (DynamicBlockReferenceProperty prop in props)
                    {
                        if (!prop.ReadOnly && prop.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedProp = prop;
                            break;
                        }
                    }

                    // Pass 2: Nếu chưa tìm thấy, tìm khớp gần đúng (Fuzzy/Smart Match)
                    if (matchedProp == null)
                    {
                        foreach (DynamicBlockReferenceProperty prop in props)
                        {
                            if (prop.ReadOnly) continue;

                            if (prop.PropertyName.IndexOf(propertyName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                propertyName.IndexOf(prop.PropertyName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                (IsSizeProperty(propertyName) && IsSizeProperty(prop.PropertyName)) ||
                                (IsFlipProperty(propertyName) && IsFlipProperty(prop.PropertyName)) ||
                                (IsVisibilityProperty(propertyName) && IsVisibilityProperty(prop.PropertyName)))
                            {
                                matchedProp = prop;
                                break;
                            }
                        }
                    }

                    if (matchedProp != null)
                    {
                        try
                        {
                            Type targetType = matchedProp.Value != null ? matchedProp.Value.GetType() : null;

                            if (targetType == typeof(double) || matchedProp.UnitsType == DynamicBlockReferencePropertyUnitsType.Distance || matchedProp.UnitsType == DynamicBlockReferencePropertyUnitsType.Angular || matchedProp.UnitsType == DynamicBlockReferencePropertyUnitsType.Area)
                            {
                                matchedProp.Value = Convert.ToDouble(value);
                                updated = true;
                            }
                            else if (targetType == typeof(short))
                            {
                                matchedProp.Value = Convert.ToInt16(value);
                                updated = true;
                            }
                            else if (targetType == typeof(int))
                            {
                                matchedProp.Value = Convert.ToInt32(value);
                                updated = true;
                            }
                            else if (targetType == typeof(string))
                            {
                                matchedProp.Value = value.ToString();
                                updated = true;
                            }
                            else
                            {
                                try
                                {
                                    matchedProp.Value = Convert.ToDouble(value);
                                    updated = true;
                                }
                                catch
                                {
                                    try
                                    {
                                        matchedProp.Value = Convert.ToInt16(value);
                                        updated = true;
                                    }
                                    catch
                                    {
                                        matchedProp.Value = value.ToString();
                                        updated = true;
                                    }
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            DtoolsResourceManager.PrintMessage($"\n[Prop Error {matchedProp.PropertyName}]: {ex.Message} (TypeCode={matchedProp.PropertyTypeCode}, Value={value})");
                        }
                    }
                }

                // Fallback: nếu không có dynamic property hoặc block thông thường, kiểm tra Tag Attributes
                if (!updated && blkRef.AttributeCollection.Count > 0)
                {
                    foreach (ObjectId attId in blkRef.AttributeCollection)
                    {
                        AttributeReference attRef = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                        if (attRef != null && (attRef.Tag.Equals(propertyName, StringComparison.OrdinalIgnoreCase) ||
                                               attRef.Tag.IndexOf(propertyName, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            attRef.TextString = value.ToString();
                            updated = true;
                            break;
                        }
                    }
                }

                if (updated)
                {
                    tr.Commit();
                }
                return updated;
            }
        }

        private static bool IsSizeProperty(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant().Trim();
            return n == "w" || n == "h" || n == "width" || n == "height" ||
                   n.Contains("size") || n.Contains("distance") ||
                   n.Contains("length") || n.Contains("rong") || n.Contains("dai");
        }

        private static bool IsFlipProperty(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant();
            return n.Contains("flip") || n.Contains("hinge") || n.Contains("swing") ||
                   n.Contains("lat") || n.Contains("huong") || n.Contains("direction");
        }

        private static bool IsVisibilityProperty(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant();
            return n.Contains("visibility") || n.Contains("angle") || n.Contains("opening") ||
                   n.Contains("trangthai") || n.Contains("kieu");
        }

        /// <summary>
        /// Lật hướng Dynamic Block (Flip Property) hoặc lật Scale X nếu là block thông thường
        /// </summary>
        public static bool FlipDynamicBlock(ObjectId blkRefId, string flipParamName = null)
        {
            if (blkRefId.IsNull || blkRefId.IsErased) return false;

            using (Transaction tr = blkRefId.Database.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = tr.GetObject(blkRefId, OpenMode.ForWrite) as BlockReference;
                if (blkRef == null) return false;

                bool flipped = false;

                if (blkRef.IsDynamicBlock)
                {
                    DynamicBlockReferencePropertyCollection props = blkRef.DynamicBlockReferencePropertyCollection;
                    foreach (DynamicBlockReferenceProperty prop in props)
                    {
                        bool isFlipProp = false;
                        if (!string.IsNullOrEmpty(flipParamName))
                        {
                            isFlipProp = prop.PropertyName.IndexOf(flipParamName, StringComparison.OrdinalIgnoreCase) >= 0;
                        }
                        else
                        {
                            isFlipProp = prop.PropertyName.IndexOf("Flip", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         prop.PropertyName.IndexOf("Hinge", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         prop.PropertyName.IndexOf("Swing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         prop.PropertyName.IndexOf("Arrow Direction", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         prop.PropertyName.IndexOf("Lat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         prop.PropertyName.IndexOf("Huong", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         prop.PropertyName.IndexOf("Direction", StringComparison.OrdinalIgnoreCase) >= 0;
                        }

                        if (isFlipProp && !prop.ReadOnly)
                        {
                            try
                            {
                                short curVal = Convert.ToInt16(prop.Value);
                                prop.Value = (short)(curVal == 0 ? 1 : 0);
                                flipped = true;
                                break;
                            }
                            catch { }
                        }
                    }
                }

                if (!flipped)
                {
                    // Fallback: Lật theo trục X
                    Scale3d curScale = blkRef.ScaleFactors;
                    blkRef.ScaleFactors = new Scale3d(-curScale.X, curScale.Y, curScale.Z);
                    flipped = true;
                }

                tr.Commit();
                return flipped;
            }
        }

        /// <summary>
        /// Đổi Visibility State của Dynamic Block theo tên trạng thái hiển thị
        /// </summary>
        public static bool SetDynamicBlockVisibility(ObjectId blkRefId, string visibilityStateName)
        {
            if (blkRefId.IsNull || blkRefId.IsErased) return false;

            using (Transaction tr = blkRefId.Database.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = tr.GetObject(blkRefId, OpenMode.ForWrite) as BlockReference;
                if (blkRef == null || !blkRef.IsDynamicBlock) return false;

                DynamicBlockReferencePropertyCollection props = blkRef.DynamicBlockReferencePropertyCollection;
                foreach (DynamicBlockReferenceProperty prop in props)
                {
                    if (prop.PropertyName.IndexOf("Visibility", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        prop.PropertyName.IndexOf("TrangThai", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        prop.PropertyName.IndexOf("Kieu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        prop.PropertyTypeCode == 1)
                    {
                        if (!prop.ReadOnly)
                        {
                            try
                            {
                                prop.Value = visibilityStateName;
                                tr.Commit();
                                return true;
                            }
                            catch { }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Lấy danh sách chi tiết các thuộc tính Dynamic Block của đối tượng
        /// </summary>
        public static List<DynamicPropertyInfo> GetDynamicPropertiesInfo(ObjectId blkRefId)
        {
            List<DynamicPropertyInfo> list = new List<DynamicPropertyInfo>();
            if (blkRefId.IsNull || blkRefId.IsErased) return list;

            using (Transaction tr = blkRefId.Database.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = tr.GetObject(blkRefId, OpenMode.ForRead) as BlockReference;
                if (blkRef != null && blkRef.IsDynamicBlock)
                {
                    foreach (DynamicBlockReferenceProperty prop in blkRef.DynamicBlockReferencePropertyCollection)
                    {
                        DynamicPropertyInfo info = new DynamicPropertyInfo
                        {
                            Name = prop.PropertyName,
                            Value = prop.Value,
                            UnitsType = prop.UnitsType.ToString(),
                            ReadOnly = prop.ReadOnly
                        };

                        try
                        {
                            object[] allowed = prop.GetAllowedValues();
                            if (allowed != null)
                            {
                                foreach (object a in allowed)
                                {
                                    if (a != null) info.AllowedValues.Add(a.ToString());
                                }
                            }
                        }
                        catch { }

                        list.Add(info);
                    }
                }
                tr.Commit();
            }
            return list;
        }

        #endregion
    }

    /// <summary>
    /// Thông tin chi tiết một thuộc tính Dynamic Block
    /// </summary>
    public class DynamicPropertyInfo
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public string UnitsType { get; set; }
        public bool ReadOnly { get; set; }
        public List<string> AllowedValues { get; set; } = new List<string>();

        public override string ToString()
        {
            string allowed = AllowedValues.Count > 0 ? $" [Tùy chọn: {string.Join(", ", AllowedValues)}]" : "";
            return $"{Name} = {Value} ({UnitsType}){(ReadOnly ? " [Read-Only]" : "")}{allowed}";
        }
    }
}

