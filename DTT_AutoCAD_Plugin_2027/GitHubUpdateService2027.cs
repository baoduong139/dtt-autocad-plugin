using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace DTT_AutoCAD_Plugin_2027
{
    /// <summary>
    /// DỊCH VỤ TỰ ĐỘNG CẬP NHẬT QUA GITHUB RELEASES API DÀNH CHO AUTOCAD ELECTRICAL 2027
    /// - Kiểm tra ngầm khi AutoCAD 2027 khởi động (Non-blocking async)
    /// - Tải bản zip/bản vá về %AppData%/DTT_Tools_2027/Versions/{version}/
    /// - Cập nhật an toàn không bị khóa file (nhờ cơ chế RAM Loader)
    /// - Cung cấp lệnh CAD: DTT2027_UPDATE để người dùng kiểm tra thủ công
    /// </summary>
    public static class GitHubUpdateService2027
    {
        public const string CurrentVersion = "2027.1.0";
        public const string DefaultGitHubRepo = "dtt-cad/dtt-autocad-electrical-2027";

        private static readonly HttpClient _httpClient = new HttpClient();
        private static bool _isChecking = false;

        static GitHubUpdateService2027()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("DTT_AutoCAD_2027_Updater", CurrentVersion));
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        public static string GetAppDataDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dttDir = Path.Combine(appData, "DTT_Tools_2027");
            if (!Directory.Exists(dttDir))
            {
                Directory.CreateDirectory(dttDir);
            }
            return dttDir;
        }

        public static string GetVersionsDirectory()
        {
            string vDir = Path.Combine(GetAppDataDirectory(), "Versions");
            if (!Directory.Exists(vDir))
            {
                Directory.CreateDirectory(vDir);
            }
            return vDir;
        }

        public static string GetActiveVersionMarkerPath()
        {
            return Path.Combine(GetAppDataDirectory(), "active_version.txt");
        }

        public static string GetCurrentActiveVersion()
        {
            try
            {
                string marker = GetActiveVersionMarkerPath();
                if (File.Exists(marker))
                {
                    string v = File.ReadAllText(marker).Trim();
                    if (!string.IsNullOrEmpty(v)) return v;
                }
            }
            catch { }
            return CurrentVersion;
        }

        public static void SetActiveVersion(string version)
        {
            try
            {
                string marker = GetActiveVersionMarkerPath();
                File.WriteAllText(marker, version.Trim());
            }
            catch { }
        }

        /// <summary>
        /// Khởi chạy kiểm tra cập nhật ngầm không làm đơ AutoCAD
        /// </summary>
        public static void StartBackgroundCheck()
        {
            Task.Run(async () =>
            {
                try
                {
                    // Chờ 5 giây sau khi AutoCAD khởi động hoàn tất
                    await Task.Delay(5000);
                    await CheckForUpdatesAsync(isManual: false);
                }
                catch { }
            });
        }

        /// <summary>
        /// Kiểm tra cập nhật qua GitHub Releases API
        /// </summary>
        public static async Task CheckForUpdatesAsync(bool isManual)
        {
            if (_isChecking) return;
            _isChecking = true;

            try
            {
                string repo = DefaultGitHubRepo;
                string apiUrl = $"https://api.github.com/repos/{repo}/releases/latest";

                PrintCadMessage(isManual ? "\n[DTT 2027 UPDATE] Đang kiểm tra phiên bản mới từ GitHub Releases..." : "");

                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    if (isManual)
                    {
                        PrintCadMessage($"\n[DTT 2027 UPDATE] Không thể kết nối tới GitHub ({response.StatusCode}). Bạn đang dùng bản v{GetCurrentActiveVersion()} ổn định nhất.");
                    }
                    return;
                }

                string json = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;
                    string tagName = root.GetProperty("tag_name").GetString() ?? "";
                    string cleanTag = tagName.TrimStart('v', 'V');

                    Version latestVer;
                    Version currentVer;
                    bool parsedLatest = Version.TryParse(cleanTag, out latestVer);
                    bool parsedCurrent = Version.TryParse(GetCurrentActiveVersion().TrimStart('v', 'V'), out currentVer);

                    bool hasNewUpdate = false;
                    if (parsedLatest && parsedCurrent)
                    {
                        hasNewUpdate = latestVer > currentVer;
                    }
                    else
                    {
                        hasNewUpdate = string.Compare(cleanTag, GetCurrentActiveVersion(), StringComparison.OrdinalIgnoreCase) > 0;
                    }

                    if (hasNewUpdate)
                    {
                        PrintCadMessage($"\n==================================================================");
                        PrintCadMessage($"\n[DTT 2027 UPDATE] Phát hiện bản cập nhật mới: v{cleanTag} (Hiện tại: v{GetCurrentActiveVersion()})");
                        PrintCadMessage($"\nĐang tự động tải về bản cập nhật ngầm...");
                        PrintCadMessage($"\n==================================================================");

                        // Tìm file zip asset
                        string downloadUrl = null;
                        if (root.TryGetProperty("assets", out JsonElement assets) && assets.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement asset in assets.EnumerateArray())
                            {
                                string name = asset.GetProperty("name").GetString() ?? "";
                                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                {
                                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(downloadUrl) && root.TryGetProperty("zipball_url", out JsonElement zipball))
                        {
                            downloadUrl = zipball.GetString();
                        }

                        if (!string.IsNullOrEmpty(downloadUrl))
                        {
                            bool dlOk = await DownloadAndStageUpdateAsync(downloadUrl, cleanTag);
                            if (dlOk)
                            {
                                SetActiveVersion(cleanTag);
                                PrintCadMessage($"\n[DTT 2027 UPDATE THÀNH CÔNG] Đã tải và cài đặt bản v{cleanTag} vào hệ thống!");
                                PrintCadMessage($"\n-> Bản cập nhật sẽ tự động có hiệu lực trong lần mở AutoCAD 2027 kế tiếp.");
                                PrintCadMessage($"\n-> Hoặc gõ lệnh DTT2027_RELOAD để nạp nóng ngay lập tức mà không cần đóng CAD!");
                            }
                        }
                    }
                    else
                    {
                        if (isManual)
                        {
                            PrintCadMessage($"\n[DTT 2027 UPDATE] Bạn đang sử dụng phiên bản mới nhất (v{GetCurrentActiveVersion()}). Không có bản cập nhật nào mới hơn.");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                if (isManual)
                {
                    PrintCadMessage($"\n[DTT 2027 UPDATE LỖI]: {ex.Message}");
                }
            }
            finally
            {
                _isChecking = false;
            }
        }

        private static async Task<bool> DownloadAndStageUpdateAsync(string downloadUrl, string newVersion)
        {
            try
            {
                string targetDir = Path.Combine(GetVersionsDirectory(), newVersion);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                string tempZip = Path.Combine(Path.GetTempPath(), $"dtt_2027_update_{Guid.NewGuid():N}.zip");
                byte[] data = await _httpClient.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(tempZip, data);

                // Giải nén đè vào thư mục target version
                ZipFile.ExtractToDirectory(tempZip, targetDir, overwriteFiles: true);

                try { File.Delete(tempZip); } catch { }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void PrintCadMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage(msg);
                }
            }
            catch { }
        }
    }
}
