using System;
using System.IO;
using System.Collections.Generic;
using DTools.Core;

namespace DTT_AutoCAD_Plugin.Modules.ElectricalNative
{
    public class AcadeCapabilityDetector : IElectricalCapabilityProvider, IAssetCatalogService
    {
        private static AcadeCapabilityDetector _instance;
        public static AcadeCapabilityDetector Instance => _instance ?? (_instance = new AcadeCapabilityDetector());

        public bool IsAcadeInstalled { get; private set; }
        public string HostProduct { get; private set; } = "AutoCAD";
        public string HostVersion { get; private set; } = "2021";
        public string SymbolLibraryPath { get; private set; } = string.Empty;
        public string LibraryStandard { get; private set; } = "IEC-60617";
        public bool HasCircuitBuilder { get; private set; }
        public bool HasWireNumbering { get; private set; }
        public bool HasCrossReference { get; private set; }
        public bool HasElectricalAudit { get; private set; }
        public string CircuitBuilderConfigPath { get; private set; } = string.Empty;

        private AcadeCapabilityDetector()
        {
            Refresh();
        }

        public void Refresh()
        {
            try
            {
                // 1. Quét thư viện ký hiệu chuẩn IEC / NFPA của AutoCAD Electrical
                string publicDoc = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                string acadeBasePath = Path.Combine(publicDoc, "Autodesk", "Acade 2021");

                string[] libCandidates = new[]
                {
                    Path.Combine(acadeBasePath, "Libs", "iec-60617"),
                    Path.Combine(acadeBasePath, "Libs", "iec2"),
                    Path.Combine(acadeBasePath, "Libs", "iec4"),
                    Path.Combine(acadeBasePath, "Libs", "NFPA"),
                    Path.Combine(acadeBasePath, "Libs", "jic125")
                };

                foreach (var path in libCandidates)
                {
                    if (Directory.Exists(path))
                    {
                        IsAcadeInstalled = true;
                        SymbolLibraryPath = path;
                        LibraryStandard = Path.GetFileName(path).ToUpperInvariant();
                        break;
                    }
                }

                // 2. Quét file cấu hình Circuit Builder
                string cbXls = Path.Combine(acadeBasePath, "Support", "en-US", "ace_circuit_builder.xls");
                if (File.Exists(cbXls))
                {
                    CircuitBuilderConfigPath = cbXls;
                    HasCircuitBuilder = true;
                }

                // 3. Quét tệp nhị phân ACADE trên các ổ đĩa và Registry
                var cadDirs = new List<string>();
                try
                {
                    using (var hklm = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64))
                    {
                        using (var key = hklm.OpenSubKey(@"Software\Autodesk\AutoCAD\R24.0\ACAD-4107:409"))
                        {
                            if (key != null)
                            {
                                string loc = key.GetValue("AcadLocation") as string;
                                if (!string.IsNullOrEmpty(loc) && Directory.Exists(loc))
                                    cadDirs.Add(loc);
                            }
                        }
                    }
                }
                catch { }

                // Bổ sung các đường dẫn chuẩn trên nhiều ổ đĩa (C:, D:, E:)
                string[] drives = new[] { "D:\\", "C:\\", "E:\\" };
                foreach (var drv in drives)
                {
                    cadDirs.Add(Path.Combine(drv, "Program Files", "Autodesk", "AutoCAD 2021"));
                    cadDirs.Add(Path.Combine(drv, "Program Files", "Autodesk", "AutoCAD 2024"));
                }

                foreach (var dir in cadDirs)
                {
                    if (Directory.Exists(dir))
                    {
                        string acePlot = Path.Combine(dir, "aceplotx.crx");
                        string aceTrans = Path.Combine(dir, "aceTransmitui.arx");
                        if (File.Exists(acePlot) || File.Exists(aceTrans) || IsAcadeInstalled)
                        {
                            IsAcadeInstalled = true;
                            HostProduct = "AutoCAD Electrical";
                            HostVersion = dir.Contains("2024") ? "2024" : "2021";
                            HasCircuitBuilder = true;
                            HasWireNumbering = true;
                            HasCrossReference = true;
                            HasElectricalAudit = true;
                            break;
                        }
                    }
                }

                DToolsLogger.LogInfo("ACADE", $"Phát hiện Host: {HostProduct} {HostVersion} | Chế độ: {DetectHostMode()} | Tiêu chuẩn: {LibraryStandard}");
            }
            catch (Exception ex)
            {
                DToolsLogger.LogWarning("ACADE", "Lỗi quét năng lực ACADE: " + ex.Message);
            }
        }

        public ElectricalHostMode DetectHostMode()
        {
            return IsAcadeInstalled ? ElectricalHostMode.AutoCadElectricalNative : ElectricalHostMode.CadGeometryFallback;
        }

        public string GetCapabilityReport()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== BÁO CÁO NĂNG LỰC HỆ THỐNG AUTOCAD ELECTRICAL (CAPABILITY MATRIX) ===");
            sb.AppendLine($"• Host hiện hành: {HostProduct} {HostVersion}");
            sb.AppendLine($"• Chế độ vận hành: {(IsAcadeInstalled ? "⚡ AutoCAD Electrical-native (IEC-60617)" : "📐 CAD Geometry Fallback")}");
            sb.AppendLine($"• Tiêu chuẩn thư viện ký hiệu: {LibraryStandard}");
            sb.AppendLine($"• Đường dẫn Symbol Library: {(string.IsNullOrEmpty(SymbolLibraryPath) ? "(Dùng thư viện nội bộ DTT)" : SymbolLibraryPath)}");
            sb.AppendLine($"• Trình dựng mạch 3 pha (AECIRCBUILDER): {(HasCircuitBuilder ? "SẴN SÀNG" : "KHÔNG KHẢ DỤNG")}");
            sb.AppendLine($"• Đánh số dây tự động (AEWIRENO): {(HasWireNumbering ? "SẴN SÀNG" : "KHÔNG KHẢ DỤNG")}");
            sb.AppendLine($"• Cập nhật tham chiếu chéo (AEXREF): {(HasCrossReference ? "SẴN SÀNG" : "KHÔNG KHẢ DỤNG")}");
            sb.AppendLine($"• Kiểm định bản vẽ điện (AEAUDIT): {(HasElectricalAudit ? "SẴN SÀNG" : "KHÔNG KHẢ DỤNG")}");
            return sb.ToString();
        }

        public string GetSymbolBlockPath(string symbolFamily)
        {
            if (!IsAcadeInstalled || string.IsNullOrEmpty(SymbolLibraryPath))
                return string.Empty;

            // Mapping các family tiêu chuẩn IEC-60617
            string fileName = "";
            switch (symbolFamily.ToUpperInvariant())
            {
                case "COIL": fileName = "VCR1.dwg"; break;
                case "NO": fileName = "VNO1.dwg"; break;
                case "NC": fileName = "VNC1.dwg"; break;
                case "MOTOR": fileName = "VMS1.dwg"; break;
                case "DISCONNECT": fileName = "VDS1.dwg"; break;
                case "OVERLOAD": fileName = "VFL1.dwg"; break;
                case "TIMER": fileName = "VTR1.dwg"; break;
                case "DRAWING_PROPERTIES": fileName = "wd_m.dwg"; break;
            }

            if (!string.IsNullOrEmpty(fileName))
            {
                string fullPath = Path.Combine(SymbolLibraryPath, fileName);
                if (File.Exists(fullPath)) return fullPath;
            }

            return string.Empty;
        }

        public bool VerifyCatalogDatabase()
        {
            string userDoc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string commonDoc = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);

            string[] candidates = new[]
            {
                Path.Combine(userDoc, @"Acade 2021\AeData\en-US\Catalogs\default_cat.mdb"),
                Path.Combine(commonDoc, @"Autodesk\Acade 2021\AeData\en-US\Catalogs\default_cat.mdb"),
                Path.Combine(userDoc, @"Acade 2024\AeData\en-US\Catalogs\default_cat.mdb"),
                Path.Combine(commonDoc, @"Autodesk\Acade 2024\AeData\en-US\Catalogs\default_cat.mdb")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path)) return true;
            }

            return false;
        }
    }
}
