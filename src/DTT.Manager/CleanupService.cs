using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DTools.Core;

namespace DTT.Manager
{
    public enum CleanupRiskLevel
    {
        LowRisk,        // Có thể dọn dẹp với rủi ro thấp (File tạm %TEMP% cũ, Staging dở dang)
        ReviewRequired, // Cần người dùng xem xét (Bản sao lưu cũ, thư mục dist tạm)
        Protected       // Nghiêm cấm tự động can thiệp (Mã nguồn, DWG, Plugin đang dùng)
    }

    public class CleanupItem
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public long SizeBytes { get; set; }
        public string SizeFormatted { get; set; }
        public DateTime LastModified { get; set; }
        public CleanupRiskLevel RiskLevel { get; set; }
        public string Reason { get; set; }
        public bool IsSelected { get; set; }
        public bool IsDirectory { get; set; }
    }

    public class CleanupReport
    {
        public DateTime ScanTime { get; set; } = DateTime.Now;
        public long TotalScanSizeBytes { get; set; }
        public long SelectedCleanSizeBytes { get; set; }
        public List<CleanupItem> Items { get; set; } = new List<CleanupItem>();
        public string QuarantinePath { get; set; }
    }

    public static class CleanupService
    {
        public static string GetQuarantineRoot()
        {
            string root = Path.Combine(ResourceLocator.GetUserDataDirectory(), "Quarantine");
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);
            return root;
        }

        public static CleanupReport Scan(string workspaceRoot = null)
        {
            var report = new CleanupReport();
            var list = new List<CleanupItem>();

            // 1. Quét %TEMP% của User (Chỉ các file tạm > 24 giờ của DTT hoặc temp build)
            try
            {
                string tempDir = Path.GetTempPath();
                if (Directory.Exists(tempDir))
                {
                    var di = new DirectoryInfo(tempDir);
                    // Chỉ quét tệp tạm có tiền tố dtt_, autocad_ hoặc .tmp cũ
                    foreach (var fi in di.GetFiles("*.*", SearchOption.TopDirectoryOnly))
                    {
                        if ((fi.Name.StartsWith("dtt", StringComparison.OrdinalIgnoreCase) ||
                             fi.Name.StartsWith("tmp", StringComparison.OrdinalIgnoreCase) ||
                             fi.Extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase) ||
                             fi.Name.EndsWith(".log", StringComparison.OrdinalIgnoreCase)) &&
                            (DateTime.Now - fi.LastWriteTime).TotalHours > 24)
                        {
                            list.Add(new CleanupItem
                            {
                                Path = fi.FullName,
                                Name = fi.Name,
                                SizeBytes = fi.Length,
                                SizeFormatted = FormatBytes(fi.Length),
                                LastModified = fi.LastWriteTime,
                                RiskLevel = CleanupRiskLevel.LowRisk,
                                Reason = "File tạm %TEMP% đã cũ hơn 24 giờ, an toàn dọn dẹp.",
                                IsSelected = true,
                                IsDirectory = false
                            });
                        }
                    }
                }
            }
            catch { }

            // 2. Quét Staging của DTT Updater (%LocalAppData%\DToolsPro\Staging)
            try
            {
                string staging = Path.Combine(ResourceLocator.GetUserDataDirectory(), "Staging");
                if (Directory.Exists(staging))
                {
                    var di = new DirectoryInfo(staging);
                    foreach (var fi in di.GetFiles("*.*", SearchOption.AllDirectories))
                    {
                        list.Add(new CleanupItem
                        {
                            Path = fi.FullName,
                            Name = fi.Name,
                            SizeBytes = fi.Length,
                            SizeFormatted = FormatBytes(fi.Length),
                            LastModified = fi.LastWriteTime,
                            RiskLevel = CleanupRiskLevel.LowRisk,
                            Reason = "Tệp tạm dở dang của bộ cập nhật DTT Staging.",
                            IsSelected = true,
                            IsDirectory = false
                        });
                    }
                }
            }
            catch { }

            // 3. Quét các thư mục Build Trung Gian (obj, bin tạm) nếu có workspace
            if (!string.IsNullOrEmpty(workspaceRoot) && Directory.Exists(workspaceRoot))
            {
                try
                {
                    string[] targetSubdirs = new[] { "obj", "bin", ".dotnet" };
                    foreach (var sub in targetSubdirs)
                    {
                        string p = Path.Combine(workspaceRoot, sub);
                        if (Directory.Exists(p))
                        {
                            long sz = GetDirectorySize(p);
                            list.Add(new CleanupItem
                            {
                                Path = p,
                                Name = sub + " (Intermediate Build Cache)",
                                SizeBytes = sz,
                                SizeFormatted = FormatBytes(sz),
                                LastModified = Directory.GetLastWriteTime(p),
                                RiskLevel = CleanupRiskLevel.ReviewRequired,
                                Reason = "Bộ nhớ đệm biên dịch MSBuild/C#. Có thể tái tạo hoàn toàn bằng lệnh build.",
                                IsSelected = false, // Không chọn mặc định để an toàn
                                IsDirectory = true
                            });
                        }
                    }
                }
                catch { }
            }

            // Tính tổng dung lượng
            long total = 0;
            foreach (var item in list) total += item.SizeBytes;
            report.TotalScanSizeBytes = total;
            report.Items = list;
            return report;
        }

        public static bool CleanApprovedItems(List<CleanupItem> approvedItems, out string quarantineDir, out string message)
        {
            quarantineDir = null;
            message = "";

            if (approvedItems == null || approvedItems.Count == 0)
            {
                message = "Không có mục nào được chọn để dọn dẹp.";
                return false;
            }

            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            quarantineDir = Path.Combine(GetQuarantineRoot(), $"Quarantine_{ts}");
            Directory.CreateDirectory(quarantineDir);

            var manifestSb = new StringBuilder();
            manifestSb.AppendLine($"# DTT TOOLS CLEANUP MANIFEST - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            manifestSb.AppendLine("# Danh sách các mục được cách ly an toàn (Có thể khôi phục):");

            int movedCount = 0;
            long reclaimedBytes = 0;

            foreach (var item in approvedItems)
            {
                if (!item.IsSelected) continue;

                // BẢO VỆ TUYỆT ĐỐI: Không bao giờ xóa file nguồn .cs, .sln, .csproj, .dwg
                if (item.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                    item.Path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                    item.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                    item.Path.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    if (item.IsDirectory)
                    {
                        if (Directory.Exists(item.Path))
                        {
                            string destDir = Path.Combine(quarantineDir, Path.GetFileName(item.Path));
                            Directory.Move(item.Path, destDir);
                            manifestSb.AppendLine($"DIR|{item.Path}|{destDir}|{item.SizeBytes}");
                            movedCount++;
                            reclaimedBytes += item.SizeBytes;
                        }
                    }
                    else
                    {
                        if (File.Exists(item.Path))
                        {
                            string destFile = Path.Combine(quarantineDir, Path.GetFileName(item.Path));
                            if (File.Exists(destFile)) destFile += "_" + Guid.NewGuid().ToString("N").Substring(0, 4);
                            File.Move(item.Path, destFile);
                            manifestSb.AppendLine($"FILE|{item.Path}|{destFile}|{item.SizeBytes}");
                            movedCount++;
                            reclaimedBytes += item.SizeBytes;
                        }
                    }
                }
                catch (Exception ex)
                {
                    manifestSb.AppendLine($"ERROR|{item.Path}|{ex.Message}");
                }
            }

            string manifestPath = Path.Combine(quarantineDir, "manifest.txt");
            File.WriteAllText(manifestPath, manifestSb.ToString(), Encoding.UTF8);

            message = $"Đã cách ly thành công {movedCount} mục ({FormatBytes(reclaimedBytes)}) vào thư mục Quarantine.";
            DToolsLogger.LogInfo("Cleanup", message);
            return true;
        }

        public static bool RestoreLatestQuarantine(out string message)
        {
            message = "";
            string root = GetQuarantineRoot();
            var dirs = Directory.GetDirectories(root, "Quarantine_*");
            if (dirs.Length == 0)
            {
                message = "Không tìm thấy phiên bản cách ly nào để khôi phục.";
                return false;
            }

            Array.Sort(dirs);
            string latest = dirs[dirs.Length - 1];
            string manifestFile = Path.Combine(latest, "manifest.txt");
            if (!File.Exists(manifestFile))
            {
                message = "Không tìm thấy tệp manifest.txt trong phiên bản cách ly.";
                return false;
            }

            int restoredCount = 0;
            var lines = File.ReadAllLines(manifestFile, Encoding.UTF8);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                var parts = line.Split('|');
                if (parts.Length >= 3)
                {
                    string type = parts[0];
                    string originalPath = parts[1];
                    string quarantinePath = parts[2];

                    try
                    {
                        if (type == "FILE" && File.Exists(quarantinePath))
                        {
                            string parent = Path.GetDirectoryName(originalPath);
                            if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);
                            File.Move(quarantinePath, originalPath);
                            restoredCount++;
                        }
                        else if (type == "DIR" && Directory.Exists(quarantinePath))
                        {
                            Directory.Move(quarantinePath, originalPath);
                            restoredCount++;
                        }
                    }
                    catch { }
                }
            }

            message = $"Đã khôi phục thành công {restoredCount} mục về vị trí ban đầu.";
            return true;
        }

        private static long GetDirectorySize(string dir)
        {
            long size = 0;
            try
            {
                foreach (var fi in new DirectoryInfo(dir).GetFiles("*.*", SearchOption.AllDirectories))
                {
                    size += fi.Length;
                }
            }
            catch { }
            return size;
        }

        public static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double d = bytes;
            while (d >= 1024 && i < suffixes.Length - 1)
            {
                d /= 1024;
                i++;
            }
            return $"{d:0.##} {suffixes[i]}";
        }
    }
}

