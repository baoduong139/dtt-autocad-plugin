using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DTT.Updater
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (args == null || args.Length == 0)
            {
                // Run in interactive mode
                return RunInteractiveGui();
            }

            string mode = args[0].ToLowerInvariant();

            if (mode == "--check")
            {
                string feedUrl = args.Length > 1 ? args[1] : null;
                var task = UpdateEngine.CheckForUpdatesAsync(feedUrl);
                task.Wait();
                var res = task.Result;
                Console.WriteLine($"[DTOOLS_UPDATER] Check: Available={res.IsUpdateAvailable}, Current={res.CurrentVersion}, Latest={res.LatestVersion}");
                Console.WriteLine($"[MESSAGE] {res.Message}");
                return res.IsUpdateAvailable ? 1 : 0;
            }

            if (mode == "--update")
            {
                string feedUrl = args.Length > 1 ? args[1] : null;
                var checkTask = UpdateEngine.CheckForUpdatesAsync(feedUrl);
                checkTask.Wait();
                var check = checkTask.Result;

                if (!check.IsUpdateAvailable || check.Manifest == null)
                {
                    Console.WriteLine("[DTOOLS_UPDATER] Không có bản cập nhật mới hoặc manifest không hợp lệ.");
                    return 0;
                }

                if (UpdateEngine.IsAutoCadRunning())
                {
                    Console.WriteLine("[DTOOLS_UPDATER] AutoCAD đang chạy! Vui lòng đóng AutoCAD trước khi cập nhật.");
                    return 2;
                }

                var updateTask = UpdateEngine.DownloadAndApplyUpdateAsync(check.Manifest, (status, pct) =>
                {
                    Console.WriteLine($"[{pct}%] {status}");
                });
                updateTask.Wait();

                bool ok = updateTask.Result;
                Console.WriteLine(ok ? "[DTOOLS_UPDATER] CẬP NHẬT HOÀN TẤT THÀNH CÔNG!" : "[DTOOLS_UPDATER] CẬP NHẬT THẤT BẠI!");
                return ok ? 0 : 3;
            }

            if (mode == "--rollback")
            {
                if (UpdateEngine.IsAutoCadRunning())
                {
                    Console.WriteLine("[DTOOLS_UPDATER] AutoCAD đang chạy! Vui lòng đóng AutoCAD trước khi khôi phục.");
                    return 2;
                }

                bool rolledBack = UpdateEngine.RollbackLatestBackup();
                Console.WriteLine(rolledBack ? "[DTOOLS_UPDATER] Đã khôi phục bản sao lưu thành công!" : "[DTOOLS_UPDATER] Không tìm thấy bản sao lưu để khôi phục.");
                return rolledBack ? 0 : 1;
            }

            Console.WriteLine("DTT Tools Updater v2026.2.0");
            Console.WriteLine("Cách sử dụng:");
            Console.WriteLine("  DTT_Updater.exe --check [feed-url]");
            Console.WriteLine("  DTT_Updater.exe --update [feed-url]");
            Console.WriteLine("  DTT_Updater.exe --rollback");
            return 0;
        }

        private static int RunInteractiveGui()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (UpdateEngine.IsAutoCadRunning())
            {
                var prompt = MessageBox.Show(
                    "AutoCAD đang chạy trên hệ thống.\n\nĐể kiểm tra và cài đặt bản cập nhật an toàn, vui lòng lưu công việc và đóng AutoCAD.\n\nBạn có muốn tiếp tục kiểm tra không?",
                    "DTT TOOLS Updater",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (prompt != DialogResult.Yes) return 0;
            }

            var checkTask = UpdateEngine.CheckForUpdatesAsync();
            checkTask.Wait();
            var res = checkTask.Result;

            if (res.IsUpdateAvailable && res.Manifest != null)
            {
                var dr = MessageBox.Show(
                    $"Tìm thấy bản cập nhật mới: v{res.LatestVersion} (Phiên bản hiện tại: v{res.CurrentVersion})\n\n" +
                    $"Ghi chú: {res.Manifest.ReleaseNotes}\n\n" +
                    "Bạn có muốn tải và áp dụng bản cập nhật ngay bây giờ không?",
                    "DTT TOOLS - Bản Cập Nhật Mới",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (dr == DialogResult.Yes)
                {
                    if (UpdateEngine.IsAutoCadRunning())
                    {
                        MessageBox.Show("AutoCAD vẫn đang mở! Vui lòng đóng AutoCAD và thử lại.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return 2;
                    }

                    var applyTask = UpdateEngine.DownloadAndApplyUpdateAsync(res.Manifest);
                    applyTask.Wait();
                    if (applyTask.Result)
                    {
                        MessageBox.Show($"Đã cập nhật thành công DTT TOOLS lên phiên bản v{res.LatestVersion}!\nHãy khởi động lại AutoCAD để sử dụng tính năng mới.", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return 0;
                    }
                    else
                    {
                        MessageBox.Show("Cập nhật thất bại. Hệ thống đã tự động hoàn tác về phiên bản trước.", "Lỗi Cập Nhật", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return 1;
                    }
                }
            }
            else
            {
                MessageBox.Show(res.Message, "DTT TOOLS Updater", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return 0;
        }
    }
}

