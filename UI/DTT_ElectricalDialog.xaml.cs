using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DTT_AutoCAD_Plugin.Modules;

namespace DTT_AutoCAD_Plugin.UI
{
    public partial class DTT_ElectricalDialog : Window
    {
        private class DeviceItem
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public double DefaultHeight { get; set; }
            public string DefaultSpec { get; set; }
            public string Description { get; set; }

            public override string ToString() => Name;
        }

        private readonly Dictionary<int, List<DeviceItem>> _catalog = new Dictionary<int, List<DeviceItem>>();

        public DTT_ElectricalDialog()
        {
            InitializeComponent();
            InitCatalog();
            ListCategories.SelectedIndex = 0;
        }

        private void InitCatalog()
        {
            // 0: Ổ CẮM
            _catalog[0] = new List<DeviceItem>
            {
                new DeviceItem { Name = "Ổ Cắm Đôi 16A", Category = "OUTLET", DefaultHeight = 300, DefaultSpec = "2x16A - 250V", Description = "Ổ cắm đôi có tiếp địa, lắp đặt tường phòng khách, phòng ngủ H=+300mm." },
                new DeviceItem { Name = "Ổ Cắm Đơn 16A", Category = "OUTLET", DefaultHeight = 300, DefaultSpec = "16A - 250V", Description = "Ổ cắm đơn 2 chấu + tiếp địa chuẩn TCVN." },
                new DeviceItem { Name = "Ổ Cắm Chống Nước (WC/Bếp)", Category = "OUTLET", DefaultHeight = 1200, DefaultSpec = "16A - IP55", Description = "Ổ cắm có nắp che chống nước tiêu chuẩn IP55, đặt khu vực bếp hoặc nhà vệ sinh H=+1200mm." },
                new DeviceItem { Name = "Ổ Cắm Âm Sàn (Floor Outlet)", Category = "OUTLET", DefaultHeight = 0, DefaultSpec = "Pop-up Metal 16A", Description = "Ổ cắm dạng bật nắp bằng đồng lắp âm sàn cho phòng họp, văn phòng." }
            };

            // 1: CHIẾU SÁNG
            _catalog[1] = new List<DeviceItem>
            {
                new DeviceItem { Name = "Đèn Downlight Âm Trần 9W", Category = "LIGHT", DefaultHeight = 2700, DefaultSpec = "LED 9W Phi 90", Description = "Đèn lon âm trần thạch cao, ánh sáng trung tính 4000K." },
                new DeviceItem { Name = "Đèn Downlight Âm Trần 12W", Category = "LIGHT", DefaultHeight = 2700, DefaultSpec = "LED 12W Phi 110", Description = "Đèn âm trần công suất lớn cho không gian phòng khách, sảnh." },
                new DeviceItem { Name = "Đèn Tuýp LED 1m2 (T8/T5)", Category = "LIGHT", DefaultHeight = 2600, DefaultSpec = "LED 18W - 1200mm", Description = "Bộ đèn tuýp chiếu sáng văn phòng, hầm xe, khu phụ trợ." },
                new DeviceItem { Name = "Đèn Panel Tấm 600x600", Category = "LIGHT", DefaultHeight = 2700, DefaultSpec = "LED 48W 600x600", Description = "Đèn Panel lắp trần thả văn phòng, độ rọi cao." },
                new DeviceItem { Name = "Đèn EXIT & Sự Cố Emergency", Category = "LIGHT", DefaultHeight = 2200, DefaultSpec = "LED Pin 2h", Description = "Đèn chỉ hướng thoát hiểm và chiếu sáng sự cố PCCC." }
            };

            // 2: CÔNG TẮC
            _catalog[2] = new List<DeviceItem>
            {
                new DeviceItem { Name = "Công Tắc Đơn 1 Hạt", Category = "SWITCH", DefaultHeight = 1250, DefaultSpec = "1 Way 16A", Description = "Công tắc bật tắt 1 lộ đèn, lắp đặt gần cửa ra vào H=+1250mm." },
                new DeviceItem { Name = "Công Tắc Đôi 2 Hạt", Category = "SWITCH", DefaultHeight = 1250, DefaultSpec = "2 Gang 16A", Description = "Công tắc 2 hạt điều khiển 2 nhóm đèn độc lập." },
                new DeviceItem { Name = "Công Tắc 3 Hạt", Category = "SWITCH", DefaultHeight = 1250, DefaultSpec = "3 Gang 16A", Description = "Bảng công tắc 3 hạt cho trung tâm phòng khách." },
                new DeviceItem { Name = "Công Tắc Đảo Chiều (Cầu Thang)", Category = "SWITCH", DefaultHeight = 1250, DefaultSpec = "2 Way 16A", Description = "Công tắc 3 cực đảo chiều bật tắt đèn cầu thang ở cả tầng trên và tầng dưới." }
            };

            // 3: TỦ ĐIỆN & BẢO VỆ
            _catalog[3] = new List<DeviceItem>
            {
                new DeviceItem { Name = "Tủ Điện Phân Phối Tầng (DB)", Category = "PANEL", DefaultHeight = 1500, DefaultSpec = "DB-01 (12 Mod)", Description = "Tủ điện âm tường chứa Aptomat MCB tổng và các nhánh phòng." },
                new DeviceItem { Name = "Tủ Điện Tổng Tòa Nhà (MSB)", Category = "PANEL", DefaultHeight = 0, DefaultSpec = "MSB 3P 250A", Description = "Tủ phân phối chính đặt tại phòng kỹ thuật điện." },
                new DeviceItem { Name = "Aptomat Khối MCCB 3P", Category = "PANEL", DefaultHeight = 1500, DefaultSpec = "MCCB 3P 100A 36kA", Description = "Thiết bị bảo vệ quá tải và ngắn mạch dòng lớn." },
                new DeviceItem { Name = "Aptomat Chống Giật RCBO", Category = "PANEL", DefaultHeight = 1500, DefaultSpec = "RCBO 2P 32A 30mA", Description = "Aptomat bảo vệ người dùng chống dòng rò điện giật 30mA." }
            };
        }

        private void ListCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = ListCategories.SelectedIndex;
            if (_catalog.ContainsKey(idx))
            {
                CmbDevices.ItemsSource = _catalog[idx];
                if (_catalog[idx].Count > 0)
                {
                    CmbDevices.SelectedIndex = 0;
                }
            }
        }

        private void CmbDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbDevices.SelectedItem is DeviceItem dev)
            {
                TxtHeight.Text = dev.DefaultHeight.ToString("0");
                TxtSpec.Text = dev.DefaultSpec;
                TxtPreviewName.Text = dev.Name;
                TxtPreviewDesc.Text = dev.Description;
            }
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Insert_Click(object sender, RoutedEventArgs e)
        {
            if (CmbDevices.SelectedItem is DeviceItem dev)
            {
                double h = 300.0;
                double.TryParse(TxtHeight.Text, out h);
                string spec = TxtSpec.Text;

                double scale = 1.0;
                if (CmbScale.SelectedIndex == 1) scale = 0.5;
                else if (CmbScale.SelectedIndex == 2) scale = 0.25;

                this.Close();

                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    var all = ElectricalSymbolsManager.GetAllSymbols();
                    var sym = all.Find(s => s.NameVi.IndexOf(dev.Name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            s.Id.IndexOf(dev.Category, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (sym == null)
                    {
                        sym = new ElectricalSymbolItem
                        {
                            Id = $"SYM_{dev.Category}_{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                            NameVi = dev.Name,
                            NameEn = dev.Description,
                            Category = dev.Category,
                            SymbolType = dev.Category,
                            DefaultSpec = spec,
                            DefaultHeight = h,
                            Icon = "⚡"
                        };
                    }

                    ElectricalPlacementContext.TargetSymbol = sym;
                    ElectricalPlacementContext.Scale = scale;
                    ElectricalPlacementContext.Rotation = 0.0;

                    Common.CadUtils.FocusDwgView();
                    doc.SendStringToExecute("DTT_PLACE_SYMBOL\n", true, false, false);
                }
            }
        }
    }
}
