using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows;
using DTools.Core;
using DTT.Updater;

namespace DTT.Manager
{
    public partial class MainWindow : Window
    {
        private AutoCadInfo _cadInfo;
        private BundleInfo _bundleInfo;
        private CleanupReport _lastCleanupReport;
        private UpdateManifest _latestManifest;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshAllStatus();
        }

        private void RefreshAllStatus()
        {
            try
            {
                // 1. Quét thông tin AutoCAD
                _cadInfo = AutoCadDetector.DetectPrimaryAutoCad();
                TxtCadProduct.Text = $"Phiên bản phát hiện: {_cadInfo.ProductName} ({_cadInfo.Version})";
                TxtCadPath.Text = string.IsNullOrEmpty(_cadInfo.InstallDirectory) ? "Đường dẫn cài đặt: Không tìm thấy" : $"Đường dẫn cài đặt: {_cadInfo.InstallDirectory}";
                TxtCadStatus.Text = _cadInfo.IsRunning ? "⚡ ĐANG CHẠY (acad.exe)" : "ĐÃ ĐÓNG (Sẵn sàng thao tác tệp)";
                TxtCadStatus.Foreground = _cadInfo.IsRunning ? (System.Windows.Media.Brush)FindResource("AccentYellow") : (System.Windows.Media.Brush)FindResource("AccentGreen");

                // 2. Quét thông tin Bundle
                _bundleInfo = AutoCadDetector.DetectInstalledBundle();
                TxtBundleInstalled.Text = _bundleInfo.IsInstalled ? "Trạng thái: ĐÃ CÀI ĐẶT TRÊN MÁY" : "Trạng thái: CHƯA CÀI ĐẶT";
                TxtBundleVersion.Text = $"Phiên bản cài đặt: {_bundleInfo.Version}";
                TxtBundlePath.Text = $"Vị trí Bundle: {_bundleInfo.BundlePath}";
                TxtDllHash.Text = string.IsNullOrEmpty(_bundleInfo.DllSha256) ? "SHA-256: N/A" : $"SHA-256 Core DLL: {_bundleInfo.DllSha256}";

                if (_bundleInfo.ConflictingDlls.Count > 0)
                {
                    TxtConflictWarning.Visibility = Visibility.Visible;
                    TxtConflictWarning.Text = $"⚠️ Phát hiện {_bundleInfo.ConflictingDlls.Count} tệp DLL phân tán / trùng lặp có thể gây xung đột nạp trong thư mục Temp.";
                }
                else
                {
                    TxtConflictWarning.Visibility = Visibility.Collapsed;
                }

                TxtFooterStatus.Text = $"Cập nhật lúc: {DateTime.Now:HH:mm:ss} | Môi trường: Windows 64-bit | RAM tối ưu: 8GB";
            }
            catch (Exception ex)
            {
                TxtFooterStatus.Text = "Lỗi khi làm mới trạng thái: " + ex.Message;
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshAllStatus();
        }

        private void BtnLaunchCad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_cadInfo?.AcadExePath) && File.Exists(_cadInfo.AcadExePath))
                {
                    Process.Start(_cadInfo.AcadExePath);
                    TxtFooterStatus.Text = "Đang khởi động AutoCAD...";
                }
                else
                {
                    MessageBox.Show("Không tìm thấy tệp acad.exe. Vui lòng mở AutoCAD thủ công từ màn hình Desktop.", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi động AutoCAD: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnInstallBundle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cadInfo != null && _cadInfo.IsRunning)
                {
                    MessageBox.Show("AutoCAD đang chạy! Vui lòng đóng AutoCAD trước khi cài đặt hoặc cập nhật gói plugin.", "Cảnh Báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string sourceBundle = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DTT_Tools.bundle");
                if (!Directory.Exists(sourceBundle))
                {
                    sourceBundle = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.FullName ?? "", "DTT_Tools.bundle");
                }
                if (!Directory.Exists(sourceBundle))
                {
                    sourceBundle = @"C:\Users\Duong\.gemini\antigravity-ide\scratch\DTT_AutoCAD_Plugin\DTT_Tools.bundle";
                }

                if (!Directory.Exists(sourceBundle))
                {
                    MessageBox.Show("Không tìm thấy thư mục nguồn DTT_Tools.bundle để cài đặt.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string targetDir = _bundleInfo.BundlePath;
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                CopyDirectory(sourceBundle, targetDir);
                RefreshAllStatus();
                MessageBox.Show("Đã cài đặt thành công DTT TOOLS vào Autodesk ApplicationPlugins!\nHãy mở AutoCAD để trải nghiệm.", "Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cài đặt: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRepairBundle_Click(object sender, RoutedEventArgs e)
        {
            BtnInstallBundle_Click(sender, e);
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string bundleDir = _bundleInfo.BundlePath;
                if (!Directory.Exists(bundleDir))
                {
                    MessageBox.Show("Chưa cài đặt DTT TOOLS nên không có dữ liệu để sao lưu.", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string backupDir = Path.Combine(ResourceLocator.GetUserDataDirectory(), "Backups");
                if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

                string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string zipPath = Path.Combine(backupDir, $"backup_bundle_{ts}.zip");
                ZipFile.CreateFromDirectory(bundleDir, zipPath, CompressionLevel.Optimal, false);

                MessageBox.Show($"Sao lưu thành công!\nTệp lưu tại:\n{zipPath}", "Sao Lưu Hoàn Tất", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi sao lưu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cadInfo != null && _cadInfo.IsRunning)
                {
                    MessageBox.Show("AutoCAD đang mở! Vui lòng đóng AutoCAD trước khi khôi phục.", "Cảnh Báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool ok = UpdateEngine.RollbackLatestBackup();
                if (ok)
                {
                    RefreshAllStatus();
                    MessageBox.Show("Đã khôi phục thành công bản sao lưu gần nhất!", "Khôi Phục Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Không tìm thấy bản sao lưu nào trong thư mục Backups.", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khôi phục: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUninstallBundle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cadInfo != null && _cadInfo.IsRunning)
                {
                    MessageBox.Show("AutoCAD đang mở! Vui lòng đóng AutoCAD trước khi gỡ cài đặt.", "Cảnh Báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dr = MessageBox.Show("Bạn có chắc chắn muốn gỡ bỏ hoàn toàn DTT TOOLS khỏi AutoCAD không?", "Xác Nhận Gỡ Cài Đặt", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (dr == MessageBoxResult.Yes)
                {
                    if (Directory.Exists(_bundleInfo.BundlePath))
                    {
                        Directory.Delete(_bundleInfo.BundlePath, true);
                    }
                    RefreshAllStatus();
                    MessageBox.Show("Đã gỡ cài đặt thành công DTT TOOLS khỏi hệ thống.", "Hoàn Tất", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi gỡ cài đặt: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logDir = ResourceLocator.GetLogsDirectory();
                Process.Start("explorer.exe", logDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi mở thư mục Logs: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Remote Update Handlers

        private async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            TxtUpdateStatus.Text = "Đang kết nối kiểm tra cập nhật...";
            TxtFooterStatus.Text = "Đang kiểm tra máy chủ cập nhật...";

            string url = TxtFeedUrl.Text.Trim();
            var check = await UpdateEngine.CheckForUpdatesAsync(url);

            TxtUpdateStatus.Text = check.Message;
            TxtUpdateVersion.Text = $"Phiên bản mới nhất: v{check.LatestVersion ?? "Không rõ"}";

            if (check.IsUpdateAvailable && check.Manifest != null)
            {
                _latestManifest = check.Manifest;
                TxtReleaseNotes.Text = check.Manifest.ReleaseNotes ?? "Bản cập nhật tối ưu hóa hiệu năng và sửa lỗi.";
                BtnApplyUpdate.IsEnabled = true;
                TxtFooterStatus.Text = "Có bản cập nhật mới!";
            }
            else
            {
                _latestManifest = null;
                BtnApplyUpdate.IsEnabled = false;
                TxtFooterStatus.Text = "Hệ thống đang ở phiên bản mới nhất.";
            }
        }

        private async void BtnApplyUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_latestManifest == null) return;

            if (_cadInfo != null && _cadInfo.IsRunning)
            {
                MessageBox.Show("AutoCAD đang chạy! Vui lòng lưu bản vẽ và đóng AutoCAD để tiến hành cập nhật an toàn.", "Yêu Cầu Đóng AutoCAD", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnApplyUpdate.IsEnabled = false;
            TxtUpdateStatus.Text = "Đang tiến hành tải và cài đặt...";

            bool success = await UpdateEngine.DownloadAndApplyUpdateAsync(_latestManifest, (status, pct) =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtUpdateStatus.Text = $"[{pct}%] {status}";
                    TxtFooterStatus.Text = status;
                });
            });

            if (success)
            {
                RefreshAllStatus();
                MessageBox.Show($"Cập nhật thành công lên phiên bản v{_latestManifest.Version}!\nHãy khởi động AutoCAD để sử dụng tính năng mới.", "Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Cập nhật thất bại. Hệ thống đã khôi phục về trạng thái trước đó.", "Lỗi Cập Nhật", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            BtnApplyUpdate.IsEnabled = true;
        }

        #endregion

        #region Safe Cleanup Handlers

        private void BtnScanTemp_Click(object sender, RoutedEventArgs e)
        {
            TxtFooterStatus.Text = "Đang quét các tệp tạm an toàn trong %TEMP%...";
            _lastCleanupReport = CleanupService.Scan(null);
            DgCleanupItems.ItemsSource = _lastCleanupReport.Items;
            UpdateCleanupSummary();
            TxtFooterStatus.Text = $"Quét hoàn tất: Tìm thấy {_lastCleanupReport.Items.Count} mục ({CleanupService.FormatBytes(_lastCleanupReport.TotalScanSizeBytes)}).";
        }

        private void BtnScanBuild_Click(object sender, RoutedEventArgs e)
        {
            TxtFooterStatus.Text = "Đang quét thư mục bộ nhớ đệm build DTT...";
            string ws = AppDomain.CurrentDomain.BaseDirectory;
            _lastCleanupReport = CleanupService.Scan(ws);
            DgCleanupItems.ItemsSource = _lastCleanupReport.Items;
            UpdateCleanupSummary();
            TxtFooterStatus.Text = $"Quét hoàn tất: Tìm thấy {_lastCleanupReport.Items.Count} mục ({CleanupService.FormatBytes(_lastCleanupReport.TotalScanSizeBytes)}).";
        }

        private void UpdateCleanupSummary()
        {
            if (_lastCleanupReport == null) return;
            long selectedBytes = _lastCleanupReport.Items.Where(i => i.IsSelected).Sum(i => i.SizeBytes);
            int selectedCount = _lastCleanupReport.Items.Count(i => i.IsSelected);
            TxtCleanupSummary.Text = $"Tổng số: {_lastCleanupReport.Items.Count} mục ({CleanupService.FormatBytes(_lastCleanupReport.TotalScanSizeBytes)}) | Đã chọn: {selectedCount} mục ({CleanupService.FormatBytes(selectedBytes)}) có thể thu hồi.";
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_lastCleanupReport?.Items != null)
            {
                foreach (var it in _lastCleanupReport.Items)
                {
                    // Chỉ chọn LowRisk khi bấm Chọn Tất Cả để bảo vệ
                    if (it.RiskLevel == CleanupRiskLevel.LowRisk) it.IsSelected = true;
                }
                DgCleanupItems.Items.Refresh();
                UpdateCleanupSummary();
            }
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_lastCleanupReport?.Items != null)
            {
                foreach (var it in _lastCleanupReport.Items) it.IsSelected = false;
                DgCleanupItems.Items.Refresh();
                UpdateCleanupSummary();
            }
        }

        private void BtnExecuteClean_Click(object sender, RoutedEventArgs e)
        {
            if (_lastCleanupReport?.Items == null) return;
            var selected = _lastCleanupReport.Items.Where(i => i.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Chưa chọn mục nào để dọn dẹp.", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            long totalBytes = selected.Sum(i => i.SizeBytes);
            var confirm = MessageBox.Show(
                $"Xác nhận dọn dẹp {selected.Count} mục ({CleanupService.FormatBytes(totalBytes)})?\n\n" +
                "Mọi tệp sẽ được chuyển vào thư mục Cách Ly (Quarantine) và có thể hoàn tác 100%.",
                "Xác Nhận Dọn Dẹp An Toàn",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            bool ok = CleanupService.CleanApprovedItems(selected, out string quarantineDir, out string msg);
            TxtCleanResult.Text = msg;
            MessageBox.Show(msg + $"\n\nVị trí lưu trữ cách ly:\n{quarantineDir}", "Hoàn Tất Dọn Dẹp", MessageBoxButton.OK, MessageBoxImage.Information);

            // Quét lại danh sách sau khi dọn
            BtnScanTemp_Click(sender, e);
        }

        private void BtnRestoreQuarantine_Click(object sender, RoutedEventArgs e)
        {
            bool ok = CleanupService.RestoreLatestQuarantine(out string msg);
            MessageBox.Show(msg, "Khôi Phục Cách Ly", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
            BtnScanTemp_Click(sender, e);
        }

        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (_lastCleanupReport?.Items == null)
            {
                MessageBox.Show("Chưa có dữ liệu quét để xuất báo cáo.", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string reportPath = Path.Combine(ResourceLocator.GetUserDataDirectory(), $"CleanupReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                var sb = new StringBuilder();
                sb.AppendLine("==========================================================================");
                sb.AppendLine("           BÁO CÁO DỌN DẸP AN TOÀN HỆ THỐNG - DTT TOOLS MANAGER           ");
                sb.AppendLine("==========================================================================");
                sb.AppendLine($"Thời gian quét: {_lastCleanupReport.ScanTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Tổng số mục: {_lastCleanupReport.Items.Count}");
                sb.AppendLine($"Tổng dung lượng đo được: {CleanupService.FormatBytes(_lastCleanupReport.TotalScanSizeBytes)}");
                sb.AppendLine("\nDANH SÁCH CHI TIẾT:");
                foreach (var it in _lastCleanupReport.Items)
                {
                    sb.AppendLine($"[{it.RiskLevel}] {it.Name} | {it.SizeFormatted} | {it.Path} | Lý do: {it.Reason}");
                }

                File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
                Process.Start("notepad.exe", reportPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất báo cáo: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private static void CopyDirectory(string sourceDir, string targetDir)
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

