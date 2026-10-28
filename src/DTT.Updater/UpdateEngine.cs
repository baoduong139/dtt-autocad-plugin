using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DTools.Core;

namespace DTT.Updater
{
    public class UpdateEngine
    {
        public static string DefaultFeedUrl => "https://raw.githubusercontent.com/baoduong139/dtt-autocad-plugin/main/dist/version.json";
        public static string DefaultDownloadUrl => "https://raw.githubusercontent.com/baoduong139/dtt-autocad-plugin/main/dist/update.zip";

        public static string GetBundleDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Autodesk", "ApplicationPlugins", "DTT_Tools.bundle");
        }

        public static string GetStagingDirectory()
        {
            string dir = Path.Combine(ResourceLocator.GetUserDataDirectory(), "Staging");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        public static string GetBackupDirectory()
        {
            string dir = Path.Combine(ResourceLocator.GetUserDataDirectory(), "Backups");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        public static bool IsAutoCadRunning()
        {
            Process[] procs = Process.GetProcessesByName("acad");
            return procs != null && procs.Length > 0;
        }

        public static async Task<UpdateCheckResult> CheckForUpdatesAsync(string feedUrl = null)
        {
            var res = new UpdateCheckResult
            {
                CurrentVersion = DToolsConfiguration.Version
            };

            string url = (string.IsNullOrEmpty(feedUrl) || feedUrl.Contains("dtt-cad")) ? DefaultFeedUrl : feedUrl;

            // Security: Enforce HTTPS for remote URLs
            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("file://", StringComparison.OrdinalIgnoreCase) &&
                !File.Exists(url))
            {
                res.Message = "Lỗi an ninh: Nguồn cập nhật phải bắt đầu bằng HTTPS hoặc tệp tin cục bộ hợp lệ.";
                return res;
            }

            try
            {
                string json = null;
                if (File.Exists(url))
                {
                    json = File.ReadAllText(url, Encoding.UTF8);
                }
                else
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(10);
                        client.DefaultRequestHeaders.Add("User-Agent", "DTT-Updater/2026.2.0");
                        json = await client.GetStringAsync(url);
                    }
                }

                if (string.IsNullOrEmpty(json))
                {
                    res.Message = "Không nhận được dữ liệu manifest cập nhật.";
                    return res;
                }

                var manifest = ParseManifest(json);
                res.Manifest = manifest;
                res.LatestVersion = manifest.Version;

                // SemVer / Version comparison
                if (IsNewerVersion(manifest.Version, res.CurrentVersion))
                {
                    res.IsUpdateAvailable = true;
                    res.Message = $"Có phiên bản mới: v{manifest.Version} (Hiện tại: v{res.CurrentVersion})";
                }
                else
                {
                    res.IsUpdateAvailable = false;
                    res.Message = $"DTT TOOLS đang ở phiên bản mới nhất (v{res.CurrentVersion}).";
                }
            }
            catch (Exception ex)
            {
                res.Message = "Không thể kết nối máy chủ cập nhật: " + ex.Message;
                DToolsLogger.LogWarning("Updater", "Kiểm tra cập nhật thất bại: " + ex.Message);
            }

            return res;
        }

        public static async Task<bool> DownloadAndApplyUpdateAsync(UpdateManifest manifest, Action<string, int> progressCallback = null)
        {
            if (manifest == null)
            {
                DToolsLogger.LogError("Updater", "Manifest rỗng");
                return false;
            }

            if (string.IsNullOrEmpty(manifest.DownloadUrl) || manifest.DownloadUrl.Contains("dtt-cad"))
            {
                manifest.DownloadUrl = DefaultDownloadUrl;
            }

            if (IsAutoCadRunning())
            {
                progressCallback?.Invoke("AutoCAD đang mở! Vui lòng đóng AutoCAD trước khi tiếp tục.", 0);
                DToolsLogger.LogWarning("Updater", "AutoCAD đang chạy, hủy thao tác ghi đè tệp.");
                return false;
            }

            string stagingDir = GetStagingDirectory();
            string downloadZip = Path.Combine(stagingDir, $"update_{manifest.Version}.zip");

            try
            {
                // 1. Tải gói về thư mục tạm Staging
                progressCallback?.Invoke($"Đang tải bản cập nhật v{manifest.Version}...", 20);

                if (File.Exists(manifest.DownloadUrl))
                {
                    File.Copy(manifest.DownloadUrl, downloadZip, true);
                }
                else
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromMinutes(3);
                        using (var response = await client.GetAsync(manifest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();
                            using (var fs = new FileStream(downloadZip, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                await response.Content.CopyToAsync(fs);
                            }
                        }
                    }
                }

                progressCallback?.Invoke("Đang kiểm tra tính toàn vẹn SHA-256...", 50);

                // 2. Xác thực SHA-256
                if (!string.IsNullOrEmpty(manifest.Sha256))
                {
                    string computedHash = ComputeSha256(downloadZip);
                    if (!string.Equals(computedHash, manifest.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        DToolsLogger.LogError("Updater", $"SHA256 sai lệch! Kỳ vọng: {manifest.Sha256}, Thực tế: {computedHash}");
                        progressCallback?.Invoke("Lỗi kiểm tra tính toàn vẹn: SHA-256 không trùng khớp!", 50);
                        return false;
                    }
                }

                // 3. Sao lưu bundle hiện tại (Rollback safety)
                string bundleDir = GetBundleDirectory();
                string backupDir = GetBackupDirectory();
                string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupZip = Path.Combine(backupDir, $"bundle_backup_{ts}.zip");

                progressCallback?.Invoke("Đang sao lưu phiên bản hiện tại...", 70);
                if (Directory.Exists(bundleDir))
                {
                    ZipFile.CreateFromDirectory(bundleDir, backupZip, CompressionLevel.Fastest, false);
                }

                // 4. Giải nén gói mới vào Staging trước
                string extractedStaging = Path.Combine(stagingDir, "extracted");
                if (Directory.Exists(extractedStaging)) Directory.Delete(extractedStaging, true);
                ZipFile.ExtractToDirectory(downloadZip, extractedStaging);

                // 5. Triển khai nguyên tử vào thư mục Bundle
                progressCallback?.Invoke("Đang cập nhật tệp tin DTT TOOLS...", 90);
                if (!Directory.Exists(bundleDir)) Directory.CreateDirectory(bundleDir);

                // Copy đè các tệp từ staging vào bundle
                CopyDirectoryRecursive(extractedStaging, bundleDir);

                // 6. Kiểm tra lại sự tồn tại của DTT_AutoCAD_Plugin.dll
                string targetDll = Path.Combine(bundleDir, "Contents", "DTT_AutoCAD_Plugin.dll");
                if (!File.Exists(targetDll))
                {
                    throw new FileNotFoundException("Không tìm thấy DTT_AutoCAD_Plugin.dll sau khi cập nhật!", targetDll);
                }

                progressCallback?.Invoke("Cập nhật thành công 100%!", 100);
                DToolsLogger.LogInfo("Updater", $"Cập nhật thành công lên phiên bản v{manifest.Version}.");
                return true;
            }
            catch (Exception ex)
            {
                DToolsLogger.LogFatal("Updater", "Lỗi trong quá trình cập nhật: " + ex.Message, ex);
                progressCallback?.Invoke("Lỗi cập nhật: " + ex.Message, 0);

                // Tự động Rollback nếu có bản sao lưu
                try
                {
                    RollbackLatestBackup();
                }
                catch { }

                return false;
            }
        }

        public static bool RollbackLatestBackup()
        {
            try
            {
                string backupDir = GetBackupDirectory();
                string bundleDir = GetBundleDirectory();
                var files = Directory.GetFiles(backupDir, "bundle_backup_*.zip");
                if (files.Length == 0) return false;

                Array.Sort(files);
                string latestBackup = files[files.Length - 1];

                if (Directory.Exists(bundleDir))
                {
                    Directory.Delete(bundleDir, true);
                }

                ZipFile.ExtractToDirectory(latestBackup, bundleDir);
                DToolsLogger.LogInfo("Updater", "Đã khôi phục thành công từ bản sao lưu: " + Path.GetFileName(latestBackup));
                return true;
            }
            catch (Exception ex)
            {
                DToolsLogger.LogError("Updater", "Lỗi khôi phục sao lưu: " + ex.Message, ex);
                return false;
            }
        }

        public static string ComputeSha256(string filePath)
        {
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(filePath))
            {
                byte[] hash = sha.ComputeHash(fs);
                var sb = new StringBuilder();
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static bool IsNewerVersion(string newVer, string currentVer)
        {
            try
            {
                var vNew = new Version(NormalizeVersionString(newVer));
                var vCurr = new Version(NormalizeVersionString(currentVer));
                return vNew > vCurr;
            }
            catch
            {
                return string.Compare(newVer, currentVer, StringComparison.OrdinalIgnoreCase) > 0;
            }
        }

        private static string NormalizeVersionString(string v)
        {
            if (string.IsNullOrEmpty(v)) return "0.0.0.0";
            var m = Regex.Match(v, @"^(\d+)(\.\d+)?(\.\d+)?(\.\d+)?");
            if (!m.Success) return "0.0.0.0";
            string[] parts = m.Value.Split('.');
            while (parts.Length < 2) parts = new string[] { parts[0], "0" };
            return string.Join(".", parts);
        }

        private static UpdateManifest ParseManifest(string json)
        {
            var m = new UpdateManifest();
            m.Version = ExtractJsonString(json, "version") ?? "2026.2.0";
            m.Channel = ExtractJsonString(json, "channel") ?? "stable";
            m.DownloadUrl = ExtractJsonString(json, "downloadUrl") ?? "";
            m.Sha256 = ExtractJsonString(json, "sha256") ?? "";
            m.ReleaseNotes = ExtractJsonString(json, "releaseNotes") ?? "";
            m.Publisher = ExtractJsonString(json, "publisher") ?? "DTT CAD Software Corporation";
            m.MinAutoCadVersion = ExtractJsonString(json, "minAutoCadVersion") ?? "2020";
            m.MaxAutoCadVersion = ExtractJsonString(json, "maxAutoCadVersion") ?? "2026";
            return m;
        }

        private static string ExtractJsonString(string json, string key)
        {
            var match = Regex.Match(json, $@"""{key}""\s*:\s*""([^""]*)""", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
        {
            foreach (string dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                string rel = dir.Substring(sourceDir.Length).TrimStart('\\', '/');
                string dest = Path.Combine(targetDir, rel);
                if (!Directory.Exists(dest)) Directory.CreateDirectory(dest);
            }

            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string rel = file.Substring(sourceDir.Length).TrimStart('\\', '/');
                string dest = Path.Combine(targetDir, rel);
                File.Copy(file, dest, true);
            }
        }
    }
}

