using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using DTT_AutoCAD_Plugin.Modules;

namespace DTT_AutoCAD_Plugin.UI
{
    /// <summary>
    /// Quick Floating Parameter Menu - xuất hiện khi bấm các nút có lựa chọn thông số
    /// Hỗ trợ: Tường, Cửa, Cửa sổ, Thép, Ống, Tủ điện, Sơ đồ điện, v.v.
    /// </summary>
    public partial class DTT_QuickMenuDialog : Window
    {
        public string SelectedCommand { get; private set; }

        public DTT_QuickMenuDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Hiển thị Menu Tổng Hợp DTT TOOLS khi người dùng gõ lệnh DTT / DTOOLS / DTT_MENU
        /// </summary>
        public static void ShowMainMenu()
        {
            var dlg = new DTT_QuickMenuDialog();
            dlg.TxtDialogIcon.Text = "⚡";
            dlg.TxtDialogTitle.Text = "TRUNG TÂM CÔNG CỤ DTT TOOLS";
            dlg.TxtDialogSubtitle.Text = "Bộ Tiện Ích Thiết Kế Điện, Kiến Trúc, Kết Cấu & MEP Thông Minh";

            var panel = dlg.ContentPanel;

            AddSectionLabel(panel, "⚡ THIẾT KẾ ĐIỆN THÔNG MINH (AUTOCAD ELECTRICAL):");
            AddVipButton(panel, "🏠 Vẽ Mặt Bằng Chiếu Sáng 12m x 6m (DTT_E_LIGHTING)", () => RunCmd("DTT_E_LIGHTING"), dlg);
            AddButton(panel, "⚡ Khởi Động Trực Tiếp DOL 3 Pha (DTT_E_DOL)",
                      "#FFB700", "#2a1500", () => RunCmd("DTT_E_DOL"), dlg);
            AddButton(panel, "🔄 Khởi Động Sao - Tam Giác Y-Δ (DTT_E_STARDELTA)",
                      "#00f2fe", "#001f2a", () => RunCmd("DTT_E_STARDELTA"), dlg);
            AddButton(panel, "🎛️ Chèn Ký Hiệu Điện Chuẩn IEC (DTT_PLACE_SYMBOL)",
                      "#10b981", "#001a12", () => RunCmd("DTT_PLACE_SYMBOL"), dlg);
            AddButton(panel, "🔍 Kiểm Toán An Toàn Mạch Điện (DTT_E_AUDIT)",
                      "#9d4edd", "#1a0030", () => RunCmd("DTT_E_AUDIT"), dlg);

            AddSectionLabel(panel, "🏛️ CÔNG CỤ KIẾN TRÚC & KẾT CẤU & MEP:");
            AddButton(panel, "🧱 Menu Tường & Cửa (Kiến Trúc)",
                      "#0284c7", "#001824", () => ShowWallMenu(), dlg);
            AddButton(panel, "🪝 Menu Cốt Thép & Mặt Cắt (Kết Cấu)",
                      "#f59e0b", "#2a1800", () => ShowRebarMenu(), dlg);
            AddButton(panel, "💧 Menu Cấp Thoát Nước & PCCC (MEP)",
                      "#06b6d4", "#001d24", () => ShowMepMenu(), dlg);

            AddSectionLabel(panel, "🔧 TIỆN ÍCH BẢN VẼ & HỆ THỐNG:");
            AddButton(panel, "📦 Menu Popup 2D/3D Dynamic Blocks (DT_MODAL)",
                      "#8b5cf6", "#1c0b3b", () => RunCmd("DT_MODAL"), dlg);
            AddButton(panel, "🔤 Chuẩn Hóa Font Tiếng Việt Hết Lỗi '?' (DTT_FIXFONT)",
                      "#10b981", "#001a12", () => RunCmd("DTT_FIXFONT"), dlg);
            AddButton(panel, "📐 Đưa Toàn Bộ Đối Tượng Về Z=0 (DTT_FLATTEN)",
                      "#64748b", "#0f172a", () => RunCmd("DTT_FLATTEN"), dlg);
            AddButton(panel, "📌 Bật / Tắt Thanh Palette Bên Phải (DTT_PALETTE)",
                      "#ec4899", "#2e0018", () => DttPaletteManager.TogglePalette(), dlg);

            dlg.ShowDialog();
        }

        /// <summary>
        /// Hiển thị menu chọn loại TƯỜNG
        /// </summary>
        public static void ShowWallMenu()
        {
            var dlg = new DTT_QuickMenuDialog();
            dlg.TxtDialogIcon.Text = "🧱";
            dlg.TxtDialogTitle.Text = "CHỌN LOẠI TƯỜNG";
            dlg.TxtDialogSubtitle.Text = "Kiến Trúc - Tường Xây Gạch";

            var panel = dlg.ContentPanel;

            AddVipButton(panel, "⚡🤖 Quét Bản Vẽ (Ảnh/PDF) Ra CAD (AI Vision)", () => ArchitectureTools.CmdScanToCad(), dlg);

            AddSectionLabel(panel, "📐 LOẠI TƯỜNG VÀ ĐỘ DÀY:");

            AddButton(panel, "🧱 Tường Bao Ngoài 220mm  (Một Gạch Thẻ)",
                      "#FFB700", "#2a1500", () => RunCmd("DTT_WALL220"), dlg);
            AddButton(panel, "🧱 Tường Ngăn Phòng 110mm  (Nửa Gạch)",
                      "#00f2fe", "#001f2a", () => RunCmd("DTT_WALL110"), dlg);
            AddButton(panel, "🧱 Tường Nhẹ CLC 100mm  (Block Nhẹ)",
                      "#10b981", "#001a12", () => RunCmd("DTT_WALL100"), dlg);
            AddButton(panel, "🧱 Tường Nhẹ CLC 75mm  (Ngăn WC/Bếp)",
                      "#9d4edd", "#1a0030", () => RunCmd("DTT_WALL75"), dlg);
            AddButton(panel, "⭕ Vẽ Liên Tục Tường 220mm (Chuỗi Đa Điểm)",
                      "#ff6b35", "#2a1200", () => RunCmd("DTT_WALL_CONT"), dlg);

            dlg.ShowDialog();
        }

        /// <summary>
        /// Hiển thị menu AI Vision & Quét Bản Vẽ
        /// </summary>
        public static void ShowAiVisionMenu()
        {
            var dlg = new DTT_QuickMenuDialog();
            dlg.TxtDialogIcon.Text = "🤖";
            dlg.TxtDialogTitle.Text = "AI VISION & QUÉT BẢN VẼ SCAN/PDF";
            dlg.TxtDialogSubtitle.Text = "Chuyển ảnh raster / PDF thành nét vẽ CAD tự động";

            var panel = dlg.ContentPanel;

            AddSectionLabel(panel, "🤖 CÔNG CỤ AI VISION & VECTORIZER:");
            AddVipButton(panel, "⚡🤖 Quét Bản Vẽ (Ảnh/PDF) Ra CAD (AI Vision)", () => ArchitectureTools.CmdScanToCad(), dlg);
            AddButton(panel, "🏛️ Vẽ Mặt Bằng Kiến Trúc & Preview 2D/3D", "#0284C7", "#F0F9FF", () => ArchitectureTools.CmdHousePlanInteractive(), dlg);
            AddButton(panel, "🏠 Vẽ Mặt Bằng Tầng Điển Hình (4 Phòng)", "#10B981", "#ECFDF5", () => ArchitectureTools.CmdStandardFloorPlan(), dlg);

            dlg.ShowDialog();
        }

        /// <summary>
        /// Hiển thị menu chọn loại CỬA ĐI
        /// </summary>
        public static void ShowDoorMenu()
        {
            var dlg = new DTT_QuickMenuDialog();
            dlg.TxtDialogIcon.Text = "🚪";
            dlg.TxtDialogTitle.Text = "CHỌN LOẠI CỬA ĐI";
            dlg.TxtDialogSubtitle.Text = "Kiến Trúc - Cửa Đi Chuẩn TCVN";

            var panel = dlg.ContentPanel;

            AddSectionLabel(panel, "🚪 CỬA ĐI THEO KÍCH THƯỚC:");
            AddButton(panel, "🚪 Cửa 1 Cánh D800x2100  (Phòng Ngủ)",
                      "#10b981", "#001a12", () => RunCmd("DTT_DOOR800"), dlg);
            AddButton(panel, "🚪 Cửa 1 Cánh D900x2200  (Cửa Chính/Ban Công)",
                      "#FFB700", "#2a1500", () => RunCmd("DTT_DOOR900"), dlg);
            AddButton(panel, "🚪 Cửa 2 Cánh D1200x2200  (Phòng Khách Chính)",
                      "#00f2fe", "#001f2a", () => RunCmd("DTT_DOOR1200"), dlg);
            AddButton(panel, "🚪 Cửa Gấp 4 Cánh D2400x2400  (Hội Trường/Sảnh)",
                      "#9d4edd", "#1a0030", () => RunCmd("DTT_DOOR_FOLDING"), dlg);
            AddButton(panel, "🚿 Cửa WC Trượt D700x2100  (Buồng Tắm/WC)",
                      "#38bdf8", "#00121a", () => RunCmd("DTT_DOOR_SLIDE"), dlg);

            dlg.ShowDialog();
        }

        /// <summary>
        /// Hiển thị menu chọn loại CỬA SỔ
        /// </summary>
        public static void ShowWindowMenu()
        {
            var dlg = new DTT_QuickMenuDialog();
            dlg.TxtDialogIcon.Text = "🪟";
            dlg.TxtDialogTitle.Text = "CHỌN LOẠI CỬA SỔ";
            dlg.TxtDialogSubtitle.Text = "Kiến Trúc - Cửa Sổ & Lam Gió";

            var panel = dlg.ContentPanel;

            AddSectionLabel(panel, "🪟 CỬA SỔ THEO KÍCH THƯỚC:");
            AddButton(panel, "🪟 Cửa Sổ 1 Cánh S600x1200  (Nhỏ - WC/Bếp)",
                      "#10b981", "#001a12", () => RunCmd("DTT_WIN600"), dlg);
            AddButton(panel, "🪟 Cửa Sổ 2 Cánh S1200x1400  (Tiêu Chuẩn)",
                      "#00f2fe", "#001f2a", () => RunCmd("DTT_WIN1200"), dlg);
            AddButton(panel, "🪟 Cửa Sổ 2 Cánh S1500x1600  (Phòng Khách)",
                      "#FFB700", "#2a1500", () => RunCmd("DTT_WIN1500"), dlg);
            AddButton(panel, "🪟 Cửa Sổ 3 Cánh S1800x1600  (Lớn - Mặt Tiền)",
                      "#9d4edd", "#1a0030", () => RunCmd("DTT_WIN1800"), dlg);
            AddButton(panel, "♨ Lam Gió Thông Gió 500x300  (Cửa Gió KT/WC)",
                      "#38bdf8", "#00121a", () => RunCmd("DTT_LOUVER"), dlg);

            dlg.ShowDialog();
        }

        /// <summary>
        /// Hiển thị menu chọn loại THÉP KẾT CẤU
        /// </summary>
        public static void ShowRebarMenu()
        {
            var dlg = new DTT_QuickMenuDialog();
            dlg.TxtDialogIcon.Text = "🪝";
            dlg.TxtDialogTitle.Text = "CHỌN LOẠI THÉP KẾT CẤU";
            dlg.TxtDialogSubtitle.Text = "Cốt Thép - Vẽ Và Ký Hiệu Chuẩn";

            var panel = dlg.ContentPanel;

            AddSectionLabel(panel, "🪝 THÉP CHỦ & THÉP ĐAI:");
            AddButton(panel, "🪝 Thép Chủ Móc 135° Kháng Chấn  (D16-D22)",
                      "#9d4edd", "#1a0030", () => RunCmd("DTT_REBAR135"), dlg);
            AddButton(panel, "📐 Thép Chủ Móc 90° Thẳng  (D12-D18)",
                      "#FFB700", "#2a1500", () => RunCmd("DTT_REBAR90"), dlg);
            AddButton(panel, "🔲 Thép Đai Kín Móc 135° Kháng Chấn  (D8-D10)",
                      "#00f2fe", "#001f2a", () => RunCmd("DTT_STIRRUP"), dlg);
            AddSectionLabel(panel, "🏛️ MẶT CẮT & THỐNG KÊ:");
            AddButton(panel, "🏛️ Mặt Cắt Dầm/Cột (BxH) Kèm Thép Góc",
                      "#10b981", "#001a12", () => RunCmd("DTT_BEAMSEC"), dlg);
            AddButton(panel, "📊 Xuất Bảng Thống Kê Thép (Tự Tính Kg)",
                      "#ff6b35", "#2a1200", () => RunCmd("DTT_REBARTABLE"), dlg);

            dlg.ShowDialog();
        }

        /// <summary>
        /// Hiển thị menu chọn loại ỐNG MEP
        /// </summary>
        public static void ShowMepMenu()
        {
            var dlg = new DTT_QuickMenuDialog();
            dlg.TxtDialogIcon.Text = "💧";
            dlg.TxtDialogTitle.Text = "CHỌN LOẠI ĐƯỜNG ỐNG MEP";
            dlg.TxtDialogSubtitle.Text = "Cơ Điện - Nước Cấp / Thoát / HVAC";

            var panel = dlg.ContentPanel;

            AddSectionLabel(panel, "🚰 ỐNG NƯỚC & HVAC:");
            AddButton(panel, "🚰 Ống Thoát Dốc Z  (i% + Đường Kính D)",
                      "#00f2fe", "#001f2a", () => RunCmd("DTT_DRAINPIPE"), dlg);
            AddButton(panel, "💧 Ống Cấp Nước Ngang  (PPPE/Inox D20-D50)",
                      "#10b981", "#001a12", () => RunCmd("DTT_SUPERPIPE"), dlg);
            AddButton(panel, "💨 Ống Gió HVAC Chữ Nhật  (WxH + Tim Ống)",
                      "#38bdf8", "#00121a", () => RunCmd("DTT_DUCT"), dlg);
            AddSectionLabel(panel, "🔥 PCCC - PHÒNG CHÁY CHỮA CHÁY:");
            AddButton(panel, "🔥 Bố Trí Sprinkler PCCC  (R=2100mm Phủ Sóng)",
                      "#ef4444", "#2a0000", () => RunCmd("DTT_SPRINKLER"), dlg);
            AddButton(panel, "🧯 Tủ Chữa Cháy FCU  (Mặt Bằng Vị Trí)",
                      "#ff6b35", "#2a1200", () => RunCmd("DTT_FCU_POS"), dlg);

            dlg.ShowDialog();
        }

        /// <summary>
        /// Hiển thị menu chọn loại THIẾT BỊ ĐIỆN - THÊM NHIỀU KÝ HIỆU HƠN
        /// </summary>
        public static void ShowElecDeviceMenu()
        {
            var dlg = new DTT_QuickMenuDialog();
            dlg.TxtDialogIcon.Text = "⚡";
            dlg.TxtDialogTitle.Text = "THƯ VIỆN KÝ HIỆU & THIẾT BỊ ĐIỆN PRO";
            dlg.TxtDialogSubtitle.Text = "Chuẩn IEC / TCVN • Tải từ symbols_database.json • Click chèn ngay";

            var panel = dlg.ContentPanel;

            // NÚT VIP: BỘ THIẾT KẾ TỦ ĐIỆN VẠN NĂNG
            AddVipButton(panel, "⚡ BỘ THIẾT KẾ TỦ ĐIỆN VẠN NĂNG (DYNAMIC BUILDER)",
                         () => ElectricalTools.CmdCabinetBuilder(), dlg);

            var allSymbols = ElectricalSymbolsManager.GetAllSymbols();

            // 1. 🔌 Ổ CẮM ĐIỆN
            var outlets = allSymbols.FindAll(s => string.Equals(s.Category, "OUTLET", StringComparison.OrdinalIgnoreCase));
            AddSectionLabel(panel, "🔌 Ổ CẮM ĐIỆN (SOCKETS / OUTLETS):");
            if (outlets.Count > 0)
            {
                foreach (var s in outlets)
                {
                    AddSymbolButton(panel, s, dlg);
                }
            }
            else
            {
                AddButton(panel, "🔌 Ổ Cắm Đôi 16A Có Tiếp Địa (H=300mm)", "#0284C7", "#F0F9FF",
                          () => RunElecDevice("OUTLET", "Ổ Cắm Đôi 16A", "2x16A+PE", 300), dlg);
                AddButton(panel, "🔌 Ổ Cắm Đơn 16A Chuẩn TCVN (H=300mm)", "#0284C7", "#F0F9FF",
                          () => RunElecDevice("OUTLET", "Ổ Cắm Đơn 16A", "1P-16A", 300), dlg);
                AddButton(panel, "💧 Ổ Cắm Chống Thấm IP55 (H=1000mm)", "#059669", "#ECFDF5",
                          () => RunElecDevice("OUTLET_WP", "Ổ Cắm IP55", "WP-16A", 1000), dlg);
            }

            // 2. 💡 ĐÈN CHIẾU SÁNG
            var lights = allSymbols.FindAll(s => string.Equals(s.Category, "LIGHT", StringComparison.OrdinalIgnoreCase));
            AddSectionLabel(panel, "💡 ĐÈN CHIẾU SÁNG (LIGHTING FIXTURES):");
            if (lights.Count > 0)
            {
                foreach (var s in lights)
                {
                    AddSymbolButton(panel, s, dlg);
                }
            }
            else
            {
                AddButton(panel, "💡 Đèn Downlight LED Âm Trần D110 (12W)", "#F59E0B", "#FFFBEB",
                          () => RunElecDevice("LIGHT", "Đèn Downlight LED", "D110-12W", 2700), dlg);
                AddButton(panel, "💡 Đèn Panel LED Tấm 600x600 (48W)", "#7C3AED", "#FAF5FF",
                          () => RunElecDevice("LIGHT_PANEL", "Đèn Panel 600x600", "600x600-48W", 2700), dlg);
                AddButton(panel, "🚨 Đèn Sự Cố Thoát Hiểm EXIT PCCC", "#DC2626", "#FEF2F2",
                          () => RunElecDevice("LIGHT_EXIT", "Đèn Exit", "EXIT-LED", 2200), dlg);
            }

            // 3. 🔲 CÔNG TẮC & BẢO VỆ
            var switches = allSymbols.FindAll(s => string.Equals(s.Category, "SWITCH", StringComparison.OrdinalIgnoreCase));
            AddSectionLabel(panel, "🔲 CÔNG TẮC & BẢO VỆ (SWITCHES & PROTECTION):");
            if (switches.Count > 0)
            {
                foreach (var s in switches)
                {
                    AddSymbolButton(panel, s, dlg);
                }
            }
            else
            {
                AddButton(panel, "🔘 Công Tắc Đơn 1 Hạt 16A (H=1250mm)", "#0284C7", "#F0F9FF",
                          () => RunElecDevice("SWITCH", "Công Tắc 1 Hạt", "S1-16A", 1250), dlg);
                AddButton(panel, "🔘 Công Tắc Đôi 2 Hạt 16A (H=1250mm)", "#0284C7", "#F0F9FF",
                          () => RunElecDevice("SWITCH", "Công Tắc 2 Hạt", "S2-16A", 1250), dlg);
                AddButton(panel, "🛡️ Aptomat MCB 1P 16A Gắn Ray", "#EA580C", "#FFF7ED",
                          () => RunElecDevice("CB", "MCB 1P 16A", "MCB-1P-16A", 1400), dlg);
                AddButton(panel, "🛡️ Aptomat Chống Rò RCBO / ELCB 30mA", "#7C3AED", "#FAF5FF",
                          () => RunElecDevice("CB", "RCBO 30mA", "RCBO-30mA", 1400), dlg);
            }

            // 4. 📦 TỦ ĐIỆN & THIẾT BỊ ĐIỀU KHIỂN
            var panels = allSymbols.FindAll(s => string.Equals(s.Category, "PANEL", StringComparison.OrdinalIgnoreCase));
            AddSectionLabel(panel, "📦 TỦ ĐIỆN & THIẾT BỊ ĐIỀU KHIỂN (PANELS & CONTROL):");
            if (panels.Count > 0)
            {
                foreach (var s in panels)
                {
                    AddSymbolButton(panel, s, dlg);
                }
            }
            else
            {
                AddButton(panel, "📦 Tủ Điện Phân Phối Tầng DB (Nhập W/H)", "#F59E0B", "#FFFBEB",
                          () => RunCmd("DTT_ELEC_DB"), dlg);
                AddButton(panel, "⚡ Tủ Điện Tổng Tòa Nhà MDB 3 Pha", "#DC2626", "#FEF2F2",
                          () => RunCmd("DTT_ELEC_MDB"), dlg);
                AddButton(panel, "🔀 Sơ Đồ Cấp Điện Đơn Tuyến Nhanh", "#059669", "#ECFDF5",
                          () => RunCmd("DTT_ELEC_SINGLELINE"), dlg);
            }

            dlg.ShowDialog();
        }

        // ============================================================
        // HELPER METHODS TẠO NÚT BẤM HIỂN THỊ RÕ RÀNG 100%
        // ============================================================

        private static void AddSectionLabel(StackPanel panel, string text)
        {
            var border = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 10, 0, 4)
            };
            border.Child = new TextBlock
            {
                Text = text,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42))
            };
            panel.Children.Add(border);
        }

        private static void AddSymbolButton(StackPanel panel, ElectricalSymbolItem sym, DTT_QuickMenuDialog dlg)
        {
            var btn = new Button
            {
                Height = 36,
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                ToolTip = $"{sym.NameVi}\n{sym.NameEn}\n👉 Click để chọn điểm chèn vào CAD"
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1.2),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225)),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255))
            };

            var grid = new Grid { Margin = new Thickness(8, 0, 8, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconTxt = new TextBlock
            {
                Text = sym.Icon ?? "⚡",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(iconTxt, 0);

            var spText = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var nameTxt = new TextBlock
            {
                Text = sym.NameVi,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var specTxt = new TextBlock
            {
                Text = string.IsNullOrEmpty(sym.DefaultSpec) ? sym.NameEn : $"{sym.DefaultSpec} • {sym.NameEn}",
                FontSize = 8.5,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            spText.Children.Add(nameTxt);
            spText.Children.Add(specTxt);
            Grid.SetColumn(spText, 1);

            grid.Children.Add(iconTxt);
            grid.Children.Add(spText);
            border.Child = grid;

            btn.Content = border;

            btn.Click += (s, e) =>
            {
                dlg.Close();
                InsertSymbolDirectly(sym);
            };

            btn.MouseEnter += (s, e) =>
            {
                border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 249, 255));
                border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(2, 132, 199));
            };
            btn.MouseLeave += (s, e) =>
            {
                border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225));
            };

            panel.Children.Add(btn);
        }

        private static void InsertSymbolDirectly(ElectricalSymbolItem sym)
        {
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc == null || sym == null) return;

                ElectricalPlacementContext.TargetSymbol = sym;
                ElectricalPlacementContext.Scale = 1.0;
                ElectricalPlacementContext.Rotation = 0.0;

                Common.CadUtils.FocusDwgView();
                doc.SendStringToExecute("DTT_PLACE_SYMBOL\n", true, false, false);
            }
            catch { }
        }

        private static void AddVipButton(StackPanel panel, string label, Action action, DTT_QuickMenuDialog dlg)
        {
            var btn = new Button
            {
                Height = 38,
                Margin = new Thickness(0, 4, 0, 6),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(7),
                BorderThickness = new Thickness(1.5),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(2, 132, 199)),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 249, 255)),
                Child = new TextBlock
                {
                    Text = label,
                    FontSize = 11.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(2, 132, 199)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            btn.Content = border;
            btn.Click += (s, e) =>
            {
                dlg.Close();
                action?.Invoke();
            };

            panel.Children.Add(btn);
        }

        private static void AddButton(StackPanel panel, string label, string accentHex, string bgHex,
                                       Action action, DTT_QuickMenuDialog dlg)
        {
            var btn = new Button
            {
                Height = 34,
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                ToolTip = label
            };

            System.Windows.Media.Color accent = (System.Windows.Media.Color)
                System.Windows.Media.ColorConverter.ConvertFromString(accentHex);
            System.Windows.Media.Color bg = (System.Windows.Media.Color)
                System.Windows.Media.ColorConverter.ConvertFromString(bgHex);

            var border = new Border
            {
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1.2),
                BorderBrush = new System.Windows.Media.SolidColorBrush(accent),
                Background = new System.Windows.Media.SolidColorBrush(bg),
                Child = new TextBlock
                {
                    Text = label,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new System.Windows.Media.SolidColorBrush(accent),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            btn.Content = border;

            btn.Click += (s, e) =>
            {
                dlg.Close();
                action?.Invoke();
            };

            panel.Children.Add(btn);
        }

        private static void RunCmd(string cmd)
        {
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    Common.CadUtils.FocusDwgView();
                    doc.SendStringToExecute(cmd + "\n", true, false, false);
                }
            }
            catch { }
        }

        private static void RunElecDevice(string cat, string name, string spec, double height)
        {
            try
            {
                var all = ElectricalSymbolsManager.GetAllSymbols();
                var sym = all.Find(s => s.NameVi.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        s.Id.IndexOf(cat, StringComparison.OrdinalIgnoreCase) >= 0);
                if (sym == null)
                {
                    sym = new ElectricalSymbolItem
                    {
                        Id = $"SYM_{cat}_{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                        NameVi = name,
                        NameEn = name,
                        Category = cat,
                        SymbolType = cat,
                        DefaultSpec = spec,
                        DefaultHeight = height,
                        Icon = "⚡"
                    };
                }

                InsertSymbolDirectly(sym);
            }
            catch { }
        }

        // ============================================================
        // EVENT HANDLERS
        // ============================================================

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
