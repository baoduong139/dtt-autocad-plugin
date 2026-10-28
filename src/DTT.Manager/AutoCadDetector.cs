using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.Win32;

namespace DTT.Manager
{
    public class AutoCadInfo
    {
        public string ProductName { get; set; } = "AutoCAD";
        public string Version { get; set; } = "2021";
        public string InstallDirectory { get; set; } = "";
        public string AcadExePath { get; set; } = "";
        public bool IsRunning { get; set; }
        public bool IsElectrical { get; set; }
    }

    public class BundleInfo
    {
        public bool IsInstalled { get; set; }
        public string BundlePath { get; set; } = "";
        public string Version { get; set; } = "Chưa cài đặt";
        public string DllPath { get; set; } = "";
        public string DllSha256 { get; set; } = "";
        public long DllSizeBytes { get; set; } = 0;
        public List<string> ConflictingDlls { get; set; } = new List<string>();
    }

    public static class AutoCadDetector
    {
        public static AutoCadInfo DetectPrimaryAutoCad()
        {
            var info = new AutoCadInfo();

            // 1. Kiểm tra Registry HKLM
            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    // AutoCAD Electrical 2021
                    using (var key = hklm.OpenSubKey(@"Software\Autodesk\AutoCAD\R24.0\ACAD-4107:409"))
                    {
                        if (key != null)
                        {
                            string loc = key.GetValue("AcadLocation") as string;
                            string name = key.GetValue("ProductName") as string;
                            if (!string.IsNullOrEmpty(loc) && Directory.Exists(loc))
                            {
                                info.InstallDirectory = loc;
                                info.ProductName = !string.IsNullOrEmpty(name) ? name : "AutoCAD Electrical 2021";
                                info.Version = "2021 (R24.0)";
                                info.IsElectrical = true;
                                info.AcadExePath = Path.Combine(loc, "acad.exe");
                            }
                        }
                    }

                    // Standard AutoCAD 2021
                    if (string.IsNullOrEmpty(info.InstallDirectory))
                    {
                        using (var key = hklm.OpenSubKey(@"Software\Autodesk\AutoCAD\R24.0\ACAD-4101:409"))
                        {
                            if (key != null)
                            {
                                string loc = key.GetValue("AcadLocation") as string;
                                string name = key.GetValue("ProductName") as string;
                                if (!string.IsNullOrEmpty(loc) && Directory.Exists(loc))
                                {
                                    info.InstallDirectory = loc;
                                    info.ProductName = !string.IsNullOrEmpty(name) ? name : "AutoCAD 2021";
                                    info.Version = "2021 (R24.0)";
                                    info.IsElectrical = false;
                                    info.AcadExePath = Path.Combine(loc, "acad.exe");
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. Fallback quét ổ đĩa (D:, C:, E:)
            if (string.IsNullOrEmpty(info.InstallDirectory) || !File.Exists(info.AcadExePath))
            {
                string[] candidates = new string[]
                {
                    @"D:\Program Files\Autodesk\AutoCAD 2021",
                    @"C:\Program Files\Autodesk\AutoCAD 2021",
                    @"D:\Program Files\Autodesk\AutoCAD 2024",
                    @"C:\Program Files\Autodesk\AutoCAD 2024"
                };

                foreach (var dir in candidates)
                {
                    string exe = Path.Combine(dir, "acad.exe");
                    if (File.Exists(exe))
                    {
                        info.InstallDirectory = dir;
                        info.AcadExePath = exe;
                        info.Version = dir.Contains("2024") ? "2024" : "2021";
                        info.ProductName = "AutoCAD " + info.Version;
                        info.IsElectrical = File.Exists(Path.Combine(dir, "aceplotx.crx")) || Directory.Exists(Path.Combine(dir, "Acade"));
                        if (info.IsElectrical) info.ProductName = "AutoCAD Electrical " + info.Version;
                        break;
                    }
                }
            }

            // 3. Kiểm tra tiến trình acad đang chạy
            try
            {
                var procs = Process.GetProcessesByName("acad");
                info.IsRunning = procs != null && procs.Length > 0;
            }
            catch
            {
                info.IsRunning = false;
            }

            return info;
        }

        public static BundleInfo DetectInstalledBundle()
        {
            var info = new BundleInfo();
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string bundleDir = Path.Combine(appData, "Autodesk", "ApplicationPlugins", "DTT_Tools.bundle");
            info.BundlePath = bundleDir;

            if (Directory.Exists(bundleDir))
            {
                info.IsInstalled = true;
                string xmlPath = Path.Combine(bundleDir, "PackageContents.xml");
                if (File.Exists(xmlPath))
                {
                    try
                    {
                        var doc = XDocument.Load(xmlPath);
                        var root = doc.Root;
                        if (root != null)
                        {
                            var verAttr = root.Attribute("AppVersion");
                            if (verAttr != null) info.Version = verAttr.Value;
                        }
                    }
                    catch
                    {
                        info.Version = "Đã cài (Lỗi đọc XML)";
                    }
                }

                string dll = Path.Combine(bundleDir, "Contents", "DTT_AutoCAD_Plugin.dll");
                if (File.Exists(dll))
                {
                    info.DllPath = dll;
                    var fi = new FileInfo(dll);
                    info.DllSizeBytes = fi.Length;
                    info.DllSha256 = ComputeSha256(dll);
                }
            }

            // Phát hiện DLL phân tán / xung đột (Ví dụ file DLL nằm trong dist cũ hoặc thư mục temp)
            ScanConflictingDlls(info);

            return info;
        }

        private static void ScanConflictingDlls(BundleInfo info)
        {
            try
            {
                string temp = Path.GetTempPath();
                string[] searchDirs = new string[] { temp };
                foreach (var s in searchDirs)
                {
                    if (Directory.Exists(s))
                    {
                        foreach (var f in Directory.GetFiles(s, "*DTT_AutoCAD_Plugin*.dll", SearchOption.TopDirectoryOnly))
                        {
                            if (!string.Equals(f, info.DllPath, StringComparison.OrdinalIgnoreCase))
                            {
                                info.ConflictingDlls.Add(f);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public static string ComputeSha256(string filePath)
        {
            try
            {
                using (var sha = SHA256.Create())
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] hash = sha.ComputeHash(fs);
                    var sb = new StringBuilder();
                    foreach (byte b in hash) sb.Append(b.ToString("x2"));
                    return sb.ToString();
                }
            }
            catch
            {
                return "N/A";
            }
        }
    }
}

