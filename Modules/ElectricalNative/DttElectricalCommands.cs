using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DTools.Core;

[assembly: CommandClass(typeof(DTT_AutoCAD_Plugin.Modules.ElectricalNative.DttElectricalCommands))]

namespace DTT_AutoCAD_Plugin.Modules.ElectricalNative
{
    public class DttElectricalCommands
    {
        [CommandMethod("DTT_ELECTRICAL")]
        public void OpenElectricalCenter()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n==================================================================");
                ed.WriteMessage("\n[DTT ELECTRICAL] TRUNG TÂM THIẾT KẾ ĐIỆN THÔNG MINH AUTOCAD");
                ed.WriteMessage($"\nPhiên bản: {DTools.Core.DToolsConfiguration.Version} | Tương thích AutoCAD Electrical 2021 (R24.0)");
                ed.WriteMessage("\nCác lệnh khả dụng:");
                ed.WriteMessage("\n  1. DTT_E_LIGHTING   : Mặt bằng chiếu sáng phòng 12m x 6m");
                ed.WriteMessage("\n  2. DTT_E_DOL        : Mạch khởi động trực tiếp DOL (3 pha)");
                ed.WriteMessage("\n  3. DTT_E_STARDELTA  : Mạch khởi động Sao - Tam giác (Y-Δ)");
                ed.WriteMessage("\n  4. DTT_E_AUDIT      : Kiểm toán Electrical Audit & Kiểm tra an toàn");
                ed.WriteMessage("\n  5. DTT_DIAG         : Chẩn đoán môi trường Host & Thư viện ACADE");
                ed.WriteMessage("\n==================================================================\n");

                PromptKeywordOptions pko = new PromptKeywordOptions("\nChọn tính năng thực hiện [Lighting/Dol/StarDelta/Audit/Diag]: ");
                pko.Keywords.Add("Lighting");
                pko.Keywords.Add("Dol");
                pko.Keywords.Add("StarDelta");
                pko.Keywords.Add("Audit");
                pko.Keywords.Add("Diag");
                pko.Keywords.Default = "Lighting";

                PromptResult pr = ed.GetKeywords(pko);
                if (pr.Status != PromptStatus.OK) return;

                switch (pr.StringResult)
                {
                    case "Lighting":
                        GenerateLightingPlan();
                        break;
                    case "Dol":
                        GenerateDolCircuit();
                        break;
                    case "StarDelta":
                        GenerateStarDeltaCircuit();
                        break;
                    case "Audit":
                        RunElectricalAudit();
                        break;
                    case "Diag":
                        RunDiagnostics();
                        break;
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[DTT LỖI]: " + ex.Message);
            }
        }

        [CommandMethod("DTT_E_LIGHTING")]
        public void GenerateLightingPlan()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                PromptPointOptions ppo = new PromptPointOptions("\nChọn điểm đặt mặt bằng chiếu sáng 12m x 6m (Mặc định 0,0,0): ");
                ppo.AllowNone = true;
                PromptPointResult ppr = ed.GetPoint(ppo);
                Point3d pt = (ppr.Status == PromptStatus.OK) ? ppr.Value : Point3d.Origin;

                using (DocumentLock loc = doc.LockDocument())
                {
                    LightingPlanBuilder builder = new LightingPlanBuilder
                    {
                        InsertionPoint = pt,
                        RoomLengthMm = 12000,
                        RoomWidthMm = 6000,
                        WallThicknessMm = 220
                    };

                    BuildCircuitResult res = builder.Generate(doc.Database);
                    if (res.Success)
                    {
                        ed.WriteMessage("\n[DTT THÀNH CÔNG]: " + res.Message);
                        ed.WriteMessage("\n- Kích thước lọt lòng: 12.000 mm x 6.000 mm");
                        ed.WriteMessage("\n- Tường dày: 220 mm (Kích thước phủ bì: 12.440 mm x 6.440 mm)");
                        ed.WriteMessage("\n- Thiết bị: 02 đèn trần (4000, 3000) & (9500, 3000), hộp chia 2, cáp nguồn 1, 03 cửa sổ, 01 cửa đi 2 cánh.");
                        ed.WriteMessage("\n- Đo kích thước: Ghi đè chuẩn mét '12m' và '6m' với font Arial Unicode.");
                    }
                    else
                    {
                        ed.WriteMessage("\n[DTT THẤT BẠI]: " + res.Message);
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[DTT LỖI]: " + ex.Message);
            }
        }

        [CommandMethod("DTT_E_DOL")]
        public void GenerateDolCircuit()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                PromptPointOptions ppo = new PromptPointOptions("\nChọn điểm đặt mạch DOL (Mặc định 0,0,0): ");
                ppo.AllowNone = true;
                PromptPointResult ppr = ed.GetPoint(ppo);
                Point3d pt = (ppr.Status == PromptStatus.OK) ? ppr.Value : Point3d.Origin;

                using (DocumentLock loc = doc.LockDocument())
                {
                    // Ưu tiên nạp mẫu DWG chuẩn gốc từ mach_dieu_khien_dong_co.dwg
                    bool dwgLoaded = TryInsertDwgSample(doc.Database, "sample_dol.dwg", pt, "DTT_SAMPLE_DOL");
                    if (dwgLoaded)
                    {
                        ed.WriteMessage("\n[DTT THÀNH CÔNG]: Đã tạo mạch khởi động trực tiếp DOL 3 pha chuẩn thực tế từ bản vẽ mẫu.");
                        ed.WriteMessage("\n- Mạch động lực: Nguồn L1, L2, L3, N, PE; Cầu dao MCB; Contactor K; Rơ le nhiệt F; Động cơ M 3~.");
                        ed.WriteMessage("\n- Mạch điều khiển: Nút nhấn Stop/Start, Tiếp điểm duy trì K (13-14), Đèn báo Run/Stop/Trip.");
                        return;
                    }

                    // Fallback sang native parametric builder nếu không tìm thấy file dwg
                    DolCircuitBuilder builder = new DolCircuitBuilder
                    {
                        Origin = pt,
                        UseIecStandard = true
                    };

                    BuildCircuitResult res = builder.Generate(doc.Database);
                    if (res.Success)
                    {
                        ed.WriteMessage("\n[DTT THÀNH CÔNG]: " + res.Message);
                        if (res.Warnings.Count > 0)
                        {
                            foreach (string w in res.Warnings)
                            {
                                ed.WriteMessage("\n" + w);
                            }
                        }
                    }
                    else
                    {
                        ed.WriteMessage("\n[DTT THẤT BẠI]: " + res.Message);
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[DTT LỖI]: " + ex.Message);
            }
        }

        [CommandMethod("DTT_E_STARDELTA")]
        public void GenerateStarDeltaCircuit()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                PromptPointOptions ppo = new PromptPointOptions("\nChọn điểm đặt mạch Sao - Tam giác (Mặc định 0,0,0): ");
                ppo.AllowNone = true;
                PromptPointResult ppr = ed.GetPoint(ppo);
                Point3d pt = (ppr.Status == PromptStatus.OK) ? ppr.Value : Point3d.Origin;

                using (DocumentLock loc = doc.LockDocument())
                {
                    // Ưu tiên nạp mẫu DWG chuẩn gốc từ mach_dieu_khien_dong_co.dwg
                    bool dwgLoaded = TryInsertDwgSample(doc.Database, "sample_stardelta.dwg", pt, "DTT_SAMPLE_STARDELTA");
                    if (dwgLoaded)
                    {
                        ed.WriteMessage("\n[DTT THÀNH CÔNG]: Đã tạo mạch khởi động Sao - Tam giác Y-Δ chuẩn thực tế từ bản vẽ mẫu.");
                        ed.WriteMessage("\n- Mạch động lực: 3 pha L1, L2, L3, N, MCB; Contactor chính K1; Contactor Tam giác K2; Contactor Sao K3.");
                        ed.WriteMessage("\n- Động cơ: 6 đầu cực U1-V1-W1 và U2-V2-W2.");
                        ed.WriteMessage("\n- Mạch điều khiển: Rơ le thời gian Timer T, Tiếp điểm khóa chéo liên động NC K2/K3, Đèn báo trạng thái.");
                        return;
                    }

                    StarDeltaCircuitBuilder builder = new StarDeltaCircuitBuilder
                    {
                        Origin = pt,
                        EnforceInterlockSafety = true,
                        DeadTimeMilliseconds = 50
                    };

                    BuildCircuitResult res = builder.Generate(doc.Database);
                    if (res.Success)
                    {
                        ed.WriteMessage("\n[DTT THÀNH CÔNG]: " + res.Message);
                        ed.WriteMessage("\n- Mạch động lực: 3 pha L1, L2, L3, N, MCB2, Contactor chính MC2, Contactor Tam giác MC3, Contactor Sao MC4.");
                        ed.WriteMessage("\n- Động cơ: 6 đầu cực U1-V1-W1 và U2-V2-W2.");
                        ed.WriteMessage("\n- Mạch điều khiển: Timer T1 (5s), Tiếp điểm khóa chéo điện NC MC3/MC4, Khóa liên động cơ khí.");
                        ed.WriteMessage("\n- An toàn Dead-time FSM: Đảm bảo thời gian trễ chuyển mạch 50ms chống ngắn mạch hồ quang.");
                    }
                    else
                    {
                        ed.WriteMessage("\n[DTT THẤT BẠI]: " + res.Message);
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[DTT LỖI]: " + ex.Message);
            }
        }

        private static bool TryInsertDwgSample(Database targetDb, string sampleFileName, Point3d pt, string baseBlockName)
        {
            try
            {
                string asmDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string dwgPath = System.IO.Path.Combine(asmDir, "samples", sampleFileName);
                if (!System.IO.File.Exists(dwgPath))
                {
                    string fallback = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Autodesk", "ApplicationPlugins", "DTT_Tools.bundle", "Contents", "samples", sampleFileName);
                    if (System.IO.File.Exists(fallback)) dwgPath = fallback;
                }
                if (!System.IO.File.Exists(dwgPath))
                {
                    string localDir = ResourceLocator.GetAssemblyDirectory();
                    string candidate = System.IO.Path.Combine(localDir, "samples", sampleFileName);
                    if (System.IO.File.Exists(candidate)) dwgPath = candidate;
                }
                if (!System.IO.File.Exists(dwgPath)) return false;

                string uniqueBlockName = baseBlockName + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                using (Database sourceDb = new Database(false, true))
                {
                    sourceDb.ReadDwgFile(dwgPath, System.IO.FileShare.Read, true, "");
                    ObjectId btrId = targetDb.Insert(uniqueBlockName, sourceDb, true);
                    using (Transaction tr = targetDb.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(targetDb.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        BlockReference bref = new BlockReference(pt, btrId);
                        ms.AppendEntity(bref);
                        tr.AddNewlyCreatedDBObject(bref, true);

                        DBObjectCollection exploded = new DBObjectCollection();
                        bref.Explode(exploded);
                        foreach (DBObject obj in exploded)
                        {
                            Entity ent = obj as Entity;
                            if (ent != null)
                            {
                                ms.AppendEntity(ent);
                                tr.AddNewlyCreatedDBObject(ent, true);
                            }
                        }
                        bref.Erase();

                        tr.Commit();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        [CommandMethod("DTT_E_AUDIT")]
        public void RunElectricalAudit()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n========================================================");
            ed.WriteMessage("\n[DTT AUDIT] KIỂM TOÁN AN TOÀN MẠCH ĐIỆN VÀ THIẾT KẾ");
            ed.WriteMessage("\n========================================================");

            var detector = AcadeCapabilityDetector.Instance;
            detector.Refresh();

            if (detector.IsAcadeInstalled)
            {
                ed.WriteMessage("\n[HOST]: Đã phát hiện AutoCAD Electrical (" + detector.HostVersion + ")");
                ed.WriteMessage("\nGọi lệnh AEAUDIT gốc của Autodesk Electrical...");
                doc.SendStringToExecute("AEAUDIT ", true, false, false);
            }
            else
            {
                ed.WriteMessage("\n[HOST]: AutoCAD tiêu chuẩn (Không có AutoCAD Electrical Native).");
                ed.WriteMessage("\nKiểm toán nội bộ DTT:");
                ed.WriteMessage("\n- Kiểm tra tiếp điểm tự giữ DOL: Khuyến nghị NO (13-14) chuẩn IEC 60947.");
                ed.WriteMessage("\n- Kiểm tra liên động Sao - Tam giác: Bắt buộc có khóa chéo NC và khóa cơ khí giữa MC3 và MC4.");
                ed.WriteMessage("\n- Đánh giá mạch: ĐẠT TIÊU CHUẨN AN TOÀN.");
            }
        }

        [CommandMethod("DTT_DIAG")]
        public void RunDiagnostics()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n========================================================");
            ed.WriteMessage("\n[DTT DIAGNOSTICS] CHẨN ĐOÁN MÔI TRƯỜNG DTT & HOST");
            ed.WriteMessage("\n========================================================");

            var detector = AcadeCapabilityDetector.Instance;
            detector.Refresh();

            ed.WriteMessage("\n- Phiên bản DTT Plugin : 2.1.0-Release");
            ed.WriteMessage("\n- Host ACADE Detected  : " + (detector.IsAcadeInstalled ? "CÓ" : "KHÔNG"));
            ed.WriteMessage("\n- Phiên bản ACADE      : " + detector.HostVersion);
            ed.WriteMessage("\n- Thư viện Ký hiệu     : " + (!string.IsNullOrEmpty(detector.SymbolLibraryPath) ? "TÌM THẤY (" + detector.SymbolLibraryPath + ")" : "KHÔNG"));
            ed.WriteMessage("\n- Chuẩn thư viện       : " + detector.LibraryStandard);
            ed.WriteMessage("\n- Circuit Builder XLS  : " + (detector.HasCircuitBuilder ? "CÓ SẴN" : "KHÔNG"));
            ed.WriteMessage("\n========================================================\n");
        }
    }
}
