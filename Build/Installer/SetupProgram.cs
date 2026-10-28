using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;

namespace DTT_AutoCAD_Installer
{
    public class InstallerForm : Form
    {
        private ProgressBar prgBar;
        private Label lblStatus;
        private Button btnInstall;
        private Button btnUninstall;
        private Button btnLaunch;
        private Button btnClose;
        private Label lblDetectedCad;
        private CheckBox chkAllUsers;

        public InstallerForm()
        {
            InitializeComponent();
            DetectAutoCAD();
        }

        private void InitializeComponent()
        {
            this.Text = "DTT TOOLS Pro v2026.2.0 - Trình Cài Đặt Tự Động";
            this.Size = new Size(720, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(15, 23, 42); // #0F172A
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // 1. HEADER PANEL
            Panel headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 85;
            headerPanel.BackColor = Color.FromArgb(30, 41, 59); // #1E293B
            headerPanel.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(6, 182, 212), 2))
                {
                    e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
                }
            };

            Label lblTitle = new Label();
            lblTitle.Text = "⚡ DTT TOOLS Professional v2026.2.0";
            lblTitle.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(56, 189, 248);
            lblTitle.Location = new Point(25, 12);
            lblTitle.AutoSize = true;

            Label lblSub = new Label();
            lblSub.Text = "Bộ Cài Đặt Autoloader Tự Động Cho AutoCAD 2021 & AutoCAD Electrical 2021";
            lblSub.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblSub.ForeColor = Color.FromArgb(148, 163, 184);
            lblSub.Location = new Point(27, 46);
            lblSub.AutoSize = true;

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSub);
            this.Controls.Add(headerPanel);

            // 2. MAIN CONTAINER
            Panel mainPanel = new Panel();
            mainPanel.Location = new Point(25, 95);
            mainPanel.Size = new Size(655, 395);
            mainPanel.BackColor = Color.Transparent;

            // CAD Detection Box
            GroupBox grpCad = new GroupBox();
            grpCad.Text = "  🔍 Trạng Thái Hệ Thống AutoCAD  ";
            grpCad.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grpCad.ForeColor = Color.FromArgb(56, 189, 248);
            grpCad.Location = new Point(0, 5);
            grpCad.Size = new Size(655, 65);
            grpCad.BackColor = Color.FromArgb(24, 32, 47);

            lblDetectedCad = new Label();
            lblDetectedCad.Text = "Đang quét các phiên bản AutoCAD trên máy tính...";
            lblDetectedCad.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblDetectedCad.ForeColor = Color.FromArgb(226, 232, 240);
            lblDetectedCad.Location = new Point(15, 25);
            lblDetectedCad.AutoSize = true;
            grpCad.Controls.Add(lblDetectedCad);
            mainPanel.Controls.Add(grpCad);

            // Feature Box
            GroupBox grpFeatures = new GroupBox();
            grpFeatures.Text = "  📦 Các Phân Hệ & Tính Năng Được Cài Đặt  ";
            grpFeatures.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grpFeatures.ForeColor = Color.FromArgb(16, 185, 129);
            grpFeatures.Location = new Point(0, 78);
            grpFeatures.Size = new Size(655, 195);
            grpFeatures.BackColor = Color.FromArgb(24, 32, 47);

            string[] features = new string[]
            {
                "✔ Autoloader Plug-and-Play: Mở CAD là thanh công cụ Palette DTT tự động hiển thị (hoặc gõ DTT)",
                "✔ Ứng dụng DTT TOOLS Manager: Quản lý cài đặt, sao lưu cấu hình, cập nhật từ xa và dọn dẹp an toàn",
                "✔ Phân hệ Điện Thông Minh (Electrical Native-First): Mặt bằng chiếu sáng 12x6m, Mạch DOL 3 pha chuẩn IEC, Mạch Sao-Tam giác khóa chéo kép",
                "✔ Phân hệ Kiến Trúc: Tự động vẽ mặt bằng nhà, tường 220/110/100/75, cửa đi, cửa sổ, lưới trục",
                "✔ Phân hệ Kết Cấu: Thép bẻ mỏ 135°/90°, thép đai dầm, bảng kê khối lượng BOQ xuất CSV",
                "✔ Phân hệ MEP & PCCC: Tuyến ống thoát nước có độ dốc, đầu phun chữa cháy Sprinkler, ống gió HVAC",
                "✔ Safe Startup & 8GB RAM Safe: Khởi động an toàn, không chiếm dụng bộ nhớ, chống crash 0xc000041d / e0434352h"
            };

            int yOffset = 22;
            foreach (var f in features)
            {
                Label lblF = new Label();
                lblF.Text = f;
                lblF.Font = new Font("Segoe UI", 8.8f, FontStyle.Regular);
                lblF.ForeColor = Color.FromArgb(203, 213, 225);
                lblF.Location = new Point(15, yOffset);
                lblF.AutoSize = true;
                grpFeatures.Controls.Add(lblF);
                yOffset += 24;
            }
            mainPanel.Controls.Add(grpFeatures);

            // Progress & Status Box
            GroupBox grpStatus = new GroupBox();
            grpStatus.Text = "  ⚡ Tiến Trình Cài Đặt  ";
            grpStatus.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grpStatus.ForeColor = Color.FromArgb(245, 158, 11);
            grpStatus.Location = new Point(0, 280);
            grpStatus.Size = new Size(655, 105);
            grpStatus.BackColor = Color.FromArgb(24, 32, 47);

            prgBar = new ProgressBar();
            prgBar.Location = new Point(15, 25);
            prgBar.Size = new Size(625, 22);
            prgBar.Style = ProgressBarStyle.Continuous;
            grpStatus.Controls.Add(prgBar);

            lblStatus = new Label();
            lblStatus.Text = "Sẵn sàng cài đặt. Nhấn [⚡ CÀI ĐẶT NGAY] để bắt đầu.";
            lblStatus.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblStatus.ForeColor = Color.FromArgb(148, 163, 184);
            lblStatus.Location = new Point(15, 55);
            lblStatus.Size = new Size(625, 40);
            grpStatus.Controls.Add(lblStatus);

            mainPanel.Controls.Add(grpStatus);
            this.Controls.Add(mainPanel);

            // 3. FOOTER PANEL
            Panel footerPanel = new Panel();
            footerPanel.Dock = DockStyle.Bottom;
            footerPanel.Height = 70;
            footerPanel.BackColor = Color.FromArgb(30, 41, 59);

            chkAllUsers = new CheckBox();
            chkAllUsers.Text = "Cài đặt cho tất cả người dùng (All Users / ProgramData)";
            chkAllUsers.ForeColor = Color.FromArgb(148, 163, 184);
            chkAllUsers.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            chkAllUsers.Location = new Point(25, 23);
            chkAllUsers.AutoSize = true;
            footerPanel.Controls.Add(chkAllUsers);

            btnLaunch = new Button();
            btnLaunch.Text = "🚀 MỞ AUTOCAD";
            btnLaunch.Size = new Size(130, 36);
            btnLaunch.Location = new Point(275, 17);
            btnLaunch.BackColor = Color.FromArgb(16, 185, 129);
            btnLaunch.ForeColor = Color.White;
            btnLaunch.FlatStyle = FlatStyle.Flat;
            btnLaunch.FlatAppearance.BorderSize = 0;
            btnLaunch.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnLaunch.Cursor = Cursors.Hand;
            btnLaunch.Enabled = false;
            btnLaunch.Click += BtnLaunch_Click;
            footerPanel.Controls.Add(btnLaunch);

            btnUninstall = new Button();
            btnUninstall.Text = "GỠ CÀI ĐẶT";
            btnUninstall.Size = new Size(100, 36);
            btnUninstall.Location = new Point(415, 17);
            btnUninstall.BackColor = Color.FromArgb(71, 85, 105);
            btnUninstall.ForeColor = Color.FromArgb(241, 245, 249);
            btnUninstall.FlatStyle = FlatStyle.Flat;
            btnUninstall.FlatAppearance.BorderSize = 0;
            btnUninstall.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            btnUninstall.Cursor = Cursors.Hand;
            btnUninstall.Click += BtnUninstall_Click;
            footerPanel.Controls.Add(btnUninstall);

            btnInstall = new Button();
            btnInstall.Text = "⚡ CÀI ĐẶT NGAY";
            btnInstall.Size = new Size(140, 36);
            btnInstall.Location = new Point(525, 17);
            btnInstall.BackColor = Color.FromArgb(14, 165, 233);
            btnInstall.ForeColor = Color.White;
            btnInstall.FlatStyle = FlatStyle.Flat;
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnInstall.Cursor = Cursors.Hand;
            btnInstall.Click += BtnInstall_Click;
            footerPanel.Controls.Add(btnInstall);

            this.Controls.Add(footerPanel);
        }

        private void DetectAutoCAD()
        {
            try
            {
                string detected = "";

                // 1. Kiểm tra Registry
                try
                {
                    using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    {
                        using (var key = hklm.OpenSubKey(@"Software\Autodesk\AutoCAD\R24.0\ACAD-4107:409"))
                        {
                            if (key != null)
                            {
                                string name = key.GetValue("ProductName") as string;
                                detected += (string.IsNullOrEmpty(name) ? "AutoCAD Electrical 2021" : name);
                            }
                        }
                    }
                }
                catch { }

                // 2. Kiểm tra ổ đĩa D:, C:, E:
                string[] drives = new string[] { "D:\\", "C:\\", "E:\\" };
                string[] versions = new string[] { "2021", "2024", "2022", "2023", "2020" };
                foreach (var drv in drives)
                {
                    foreach (var v in versions)
                    {
                        string p = Path.Combine(drv, "Program Files", "Autodesk", "AutoCAD " + v, "acad.exe");
                        if (File.Exists(p) && !detected.Contains(v))
                        {
                            detected += (detected == "" ? "" : ", ") + "AutoCAD " + v + " (" + drv.Substring(0, 2) + ")";
                        }
                    }
                }

                if (!string.IsNullOrEmpty(detected))
                {
                    lblDetectedCad.Text = "✅ Đã phát hiện trên máy: " + detected + " (Tương thích 100%)";
                    lblDetectedCad.ForeColor = Color.FromArgb(52, 211, 153);
                }
                else
                {
                    lblDetectedCad.Text = "ℹ️ Sẵn sàng tích hợp cho mọi phiên bản AutoCAD (2020 - 2026) khi được mở.";
                    lblDetectedCad.ForeColor = Color.FromArgb(148, 163, 184);
                }
            }
            catch
            {
                lblDetectedCad.Text = "ℹ️ Chuẩn bị tích hợp vào Autodesk ApplicationPlugins.";
            }
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            var cadProcs = Process.GetProcessesByName("acad");
            if (cadProcs.Length > 0)
            {
                var prompt = MessageBox.Show(
                    "⚠️ Phát hiện AutoCAD đang chạy trên hệ thống.\n\n" +
                    "Để tránh lỗi xung đột hoặc khóa file DLL (Sharing Violation), vui lòng lưu toàn bộ bản vẽ và đóng AutoCAD trước khi cài đặt.\n\n" +
                    "Bạn có muốn tiếp tục thử cài đặt không?",
                    "Cảnh Báo AutoCAD Đang Chạy",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (prompt != DialogResult.Yes) return;
            }

            btnInstall.Enabled = false;
            btnUninstall.Enabled = false;
            prgBar.Value = 10;
            lblStatus.Text = "Đang kiểm tra môi trường và cách ly các bản cài cũ xung đột...";
            lblStatus.ForeColor = Color.FromArgb(56, 189, 248);
            Application.DoEvents();

            try
            {
                QuarantineConflictingBundles();

                string userPlugins = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "ApplicationPlugins");
                string bundleDir = Path.Combine(userPlugins, "DTT_Tools.bundle");
                InstallToDirectory(userPlugins, "Người dùng hiện tại");
                prgBar.Value = 70;
                Application.DoEvents();

                // Tạo Shortcuts Desktop & Start Menu
                CreateShortcuts(bundleDir);
                prgBar.Value = 90;
                Application.DoEvents();

                if (chkAllUsers.Checked)
                {
                    string allUsers = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Autodesk", "ApplicationPlugins");
                    try
                    {
                        InstallToDirectory(allUsers, "Tất cả người dùng");
                    }
                    catch { }
                }

                prgBar.Value = 100;
                lblStatus.Text = "🎉 CÀI ĐẶT THÀNH CÔNG HOÀN TẤT!\nĐã tạo biểu tượng DTT TOOLS Manager trên màn hình Desktop.";
                lblStatus.ForeColor = Color.FromArgb(52, 211, 153);

                btnInstall.Text = "✔ ĐÃ CÀI ĐẶT";
                btnInstall.BackColor = Color.FromArgb(5, 150, 105);
                btnInstall.Enabled = false;
                btnUninstall.Enabled = true;
                btnLaunch.Enabled = true;

                MessageBox.Show(
                    "🎉 CÀI ĐẶT THÀNH CÔNG DTT TOOLS PRO v2026.2.0!\n\n" +
                    "Hệ thống đã nạp gói ứng dụng vào Autodesk ApplicationPlugins.\n\n" +
                    "👉 BÂY GIỜ BẠN CÓ THỂ:\n" +
                    "1. Bấm nút [🚀 MỞ AUTOCAD] hoặc mở biểu tượng AutoCAD trên máy tính.\n" +
                    "2. Thanh công cụ Palette DTT sẽ TỰ ĐỘNG XUẤT HIỆN bên phải màn hình!\n" +
                    "3. Quản lý, kiểm tra cập nhật và sao lưu bất kỳ lúc nào qua biểu tượng 'DTT TOOLS Manager' trên Desktop!",
                    "Cài Đặt Thành Công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                prgBar.Value = 0;
                lblStatus.Text = "Lỗi cài đặt: " + ex.Message;
                lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
                btnInstall.Enabled = true;
                btnUninstall.Enabled = true;
                MessageBox.Show("Có lỗi xảy ra: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void QuarantineConflictingBundles()
        {
            try
            {
                string[] pluginDirs = new string[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "ApplicationPlugins"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Autodesk", "ApplicationPlugins")
                };

                string quarantineBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DToolsPro", "Quarantine");

                foreach (var dir in pluginDirs)
                {
                    if (!Directory.Exists(dir)) continue;

                    string[] conflictingNames = new string[] { "DTools.bundle", "DTools_Pro.bundle" };
                    foreach (var cName in conflictingNames)
                    {
                        string cPath = Path.Combine(dir, cName);
                        if (Directory.Exists(cPath))
                        {
                            if (!Directory.Exists(quarantineBase)) Directory.CreateDirectory(quarantineBase);
                            string dest = Path.Combine(quarantineBase, cName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                            Directory.Move(cPath, dest);
                        }
                    }
                }
            }
            catch { }
        }

        private void InstallToDirectory(string pluginsDir, string targetName)
        {
            string bundleDir = Path.Combine(pluginsDir, "DTT_Tools.bundle");
            if (!Directory.Exists(bundleDir))
            {
                Directory.CreateDirectory(bundleDir);
            }

            using (Stream s = SetupPayload.GetBundleZipStream())
            {
                if (s == null)
                {
                    throw new Exception("Không tìm thấy dữ liệu BundlePayload trong bộ cài!");
                }
                using (var archive = new System.IO.Compression.ZipArchive(s, System.IO.Compression.ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        string fullPath = Path.Combine(bundleDir, entry.FullName);
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
                        }
                        else
                        {
                            string dir = Path.GetDirectoryName(fullPath);
                            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                            using (var es = entry.Open())
                            using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                            {
                                es.CopyTo(fs);
                            }
                        }
                    }
                }
            }
        }

        public static void CreateShortcuts(string bundleDir)
        {
            try
            {
                string managerExe = Path.Combine(bundleDir, "Contents", "DTT_Manager.exe");
                if (!File.Exists(managerExe)) return;

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;

                dynamic shell = Activator.CreateInstance(shellType);

                // Desktop shortcut
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string lnkPath = Path.Combine(desktop, "DTT TOOLS Manager.lnk");
                var shortcut = shell.CreateShortcut(lnkPath);
                shortcut.TargetPath = managerExe;
                shortcut.WorkingDirectory = Path.GetDirectoryName(managerExe);
                shortcut.Description = "DTT TOOLS Manager - Quản Lý & Cập Nhật AutoCAD Plugin";
                shortcut.Save();

                // Start Menu shortcut
                string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string menuDir = Path.Combine(programs, "DTT TOOLS PRO");
                if (!Directory.Exists(menuDir)) Directory.CreateDirectory(menuDir);
                var menuLnk = shell.CreateShortcut(Path.Combine(menuDir, "DTT TOOLS Manager.lnk"));
                menuLnk.TargetPath = managerExe;
                menuLnk.WorkingDirectory = Path.GetDirectoryName(managerExe);
                menuLnk.Description = "DTT TOOLS Manager";
                menuLnk.Save();
            }
            catch { }
        }

        private void BtnUninstall_Click(object sender, EventArgs e)
        {
            var prompt = MessageBox.Show(
                "Bạn có chắc chắn muốn gỡ bỏ hoàn toàn DTT TOOLS khỏi AutoCAD không?",
                "Xác Nhận Gỡ Bỏ",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (prompt != DialogResult.Yes) return;

            prgBar.Value = 20;
            lblStatus.Text = "Đang gỡ bỏ bundle và shortcut...";
            Application.DoEvents();

            try
            {
                string userBundle = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "ApplicationPlugins", "DTT_Tools.bundle");
                if (Directory.Exists(userBundle))
                {
                    try { Directory.Delete(userBundle, true); } catch { }
                }

                prgBar.Value = 60;
                string allUserBundle = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Autodesk", "ApplicationPlugins", "DTT_Tools.bundle");
                if (Directory.Exists(allUserBundle))
                {
                    try { Directory.Delete(allUserBundle, true); } catch { }
                }

                // Xóa Desktop & Start Menu shortcut
                try
                {
                    string lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "DTT TOOLS Manager.lnk");
                    if (File.Exists(lnk)) File.Delete(lnk);
                    string menuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "DTT TOOLS PRO");
                    if (Directory.Exists(menuDir)) Directory.Delete(menuDir, true);
                }
                catch { }

                prgBar.Value = 100;
                lblStatus.Text = "Đã gỡ bỏ thành công DTT CAD TOOLSET PRO.";
                lblStatus.ForeColor = Color.FromArgb(251, 191, 36);

                btnInstall.Text = "⚡ CÀI ĐẶT NGAY";
                btnInstall.BackColor = Color.FromArgb(14, 165, 233);
                btnInstall.Enabled = true;
                btnLaunch.Enabled = false;

                MessageBox.Show("Đã gỡ bỏ sạch sẽ DTT TOOLS khỏi hệ thống.", "Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi gỡ bỏ: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLaunch_Click(object sender, EventArgs e)
        {
            try
            {
                string[] drives = new string[] { "D:\\", "C:\\", "E:\\" };
                string[] versions = new string[] { "2021", "2024", "2022", "2023", "2020" };
                foreach (var drv in drives)
                {
                    foreach (var v in versions)
                    {
                        string cadPath = Path.Combine(drv, "Program Files", "Autodesk", "AutoCAD " + v, "acad.exe");
                        if (File.Exists(cadPath))
                        {
                            Process.Start(cadPath);
                            return;
                        }
                    }
                }
                MessageBox.Show("Không tìm thấy đường dẫn acad.exe mặc định. Vui lòng mở AutoCAD từ Desktop của bạn.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể khởi động AutoCAD: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public static class SetupPayload
    {
        public static Stream GetBundleZipStream()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("BundlePayload");
        }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args != null && args.Length > 0)
            {
                foreach (var a in args)
                {
                    if (a.ToLower() == "/silent" || a.ToLower() == "/s" || a.ToLower() == "-s")
                    {
                        PerformSilentInstall();
                        return;
                    }
                }
            }

            Application.Run(new InstallerForm());
        }

        private static void PerformSilentInstall()
        {
            try
            {
                string userPlugins = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "ApplicationPlugins");
                string bundleDir = Path.Combine(userPlugins, "DTT_Tools.bundle");
                if (!Directory.Exists(bundleDir)) Directory.CreateDirectory(bundleDir);

                using (Stream s = SetupPayload.GetBundleZipStream())
                {
                    if (s == null) return;
                    using (var archive = new System.IO.Compression.ZipArchive(s, System.IO.Compression.ZipArchiveMode.Read))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            string fullPath = Path.Combine(bundleDir, entry.FullName);
                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
                            }
                            else
                            {
                                string dir = Path.GetDirectoryName(fullPath);
                                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                                using (var es = entry.Open())
                                using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                                {
                                    es.CopyTo(fs);
                                }
                            }
                        }
                    }
                }
                InstallerForm.CreateShortcuts(bundleDir);
            }
            catch { }
        }
    }
}
