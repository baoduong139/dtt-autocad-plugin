using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Autodesk.AutoCAD.ApplicationServices;
using DTT_AutoCAD_Plugin.Common;
using DTT_AutoCAD_Plugin.Modules;

namespace DTT_AutoCAD_Plugin.UI
{
    /// <summary>
    /// Code-behind cho giao diện WPF Native 3D Cyberpunk DTT_PaletteView.xaml
    /// Hỗ trợ 10 phân hệ chuyên sâu, Tùy biến Giao diện Sáng/Tối, Cỡ chữ, Đa ngôn ngữ
    /// </summary>
    public partial class DTT_PaletteView : UserControl
    {
        private readonly List<StackPanel> _panels = new List<StackPanel>();
        private StackPanel _activePanel;

        public DTT_PaletteView()
        {
            InitializeComponent();
            Loaded += DTT_PaletteView_Loaded;
        }

        private void DTT_PaletteView_Loaded(object sender, RoutedEventArgs e)
        {
            _panels.Clear();
            _panels.Add(PanelDoctor);
            _panels.Add(PanelYqQuick);
            _panels.Add(PanelArch);
            _panels.Add(PanelStruct);
            _panels.Add(PanelMep);
            _panels.Add(PanelElec);
            _panels.Add(PanelSymbols);
            _panels.Add(PanelDraw2D);
            _panels.Add(PanelDim);
            _panels.Add(PanelUtil);
            _panels.Add(Panel3D);
            _panels.Add(PanelSettings);

            _activePanel = PanelDoctor;
            LoadSymbolsGrid();
        }

        /// <summary>
        /// XỬ LÝ SỰ KIỆN CLICK NÚT LỆNH: BẤM LÀ CHẠY 100% (DIRECT EXECUTION FIX)
        /// Khắc phục hoàn toàn lỗi nút không phản hồi nhờ FocusDwgView và lệnh hủy \x03\x03
        /// </summary>
        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string cmdTag = btn.Tag.ToString().Trim();
                if (string.IsNullOrWhiteSpace(cmdTag)) return;

                // 1. Phân định rõ ràng: CHỈ mở Center Modal 2D/3D khi người dùng bấm nút mở KHO THƯ VIỆN DYNAMIC BLOCKS
                // Tránh việc bấm chức năng bảo dưỡng hay lệnh vẽ mà cũng hiện menu gây đơ AutoCAD
                bool isBlockLibraryModal = cmdTag == "DT_INSERTBLOCK" || 
                                           cmdTag == "DTT_INSERTBLOCK" || 
                                           cmdTag == "DT_BLOCK" || 
                                           cmdTag == "DTT_BLOCK" || 
                                           cmdTag == "DT_MODAL" || 
                                           cmdTag == "DTT_MODAL" || 
                                           cmdTag == "DT_CENTER_MODAL" || 
                                           cmdTag == "DT_POPUP" || 
                                           cmdTag == "DT_MENU3D";

                if (isBlockLibraryModal)
                {
                    bool opened = DTT_ModalRegistry.ShowCenterModal("DYNAMIC_BLOCKS");
                    if (opened)
                    {
                        TxtStatus.Text = "📦 Đã chọn và chèn linh kiện từ Thư Viện Kho Blocks";
                    }
                    return;
                }

                // ============================================================
                // CÁC HỘP THOẠI THAM SỐ CHUYÊN BIỆT (PARAMETRIC DIALOGS)
                // ============================================================
                switch (cmdTag)
                {
                    case "DTT_CABINET_BUILDER":
                        ElectricalTools.CmdCabinetBuilder();
                        TxtStatus.Text = "⚡ Đang mở Bộ Thiết Kế Tủ Điện Vạn Năng";
                        return;

                    case "DTT_ELEC_FLOORPLAN":
                    case "DTT_FLOORPLAN":
                        ElectricalTools.CmdFloorPlanElectricalBuilder();
                        TxtStatus.Text = "🏠 Đang mở Bộ Vẽ Mặt Bằng Điện 2D Full Tính Năng";
                        return;

                    case "DTT_PLC_BUILDER":
                    case "DTT_PLC":
                        ElectricalTools.CmdPlcParametricBuilder();
                        TxtStatus.Text = "🎛️ Đang mở Thư Viện PLC I/O Parametric";
                        return;

                    case "DTT_MOTOR_STARTER":
                    case "DTT_STARTER":
                        ElectricalTools.CmdMotorStarterBuilder();
                        TxtStatus.Text = "⚙️ Đang mở Bộ Mẫu Mạch Khởi Động Động Cơ";
                        return;

                    case "DTT_ELEC_MENU":
                        ElectricalTools.CmdShowElecMenu();
                        TxtStatus.Text = "⚡ Đã mở Thư Viện Ký Hiệu Điện PRO";
                        return;

                    case "DTT_SCAN_TO_CAD":
                        ArchitectureTools.CmdScanToCad();
                        TxtStatus.Text = "🤖 Đã mở Quét Bản Vẽ (Ảnh/PDF) Ra CAD (AI Vision)";
                        return;

                    case "DTT_HOUSE_PLAN":
                    case "DTT_HOUSEPLAN":
                        ArchitectureTools.CmdHousePlanInteractive();
                        TxtStatus.Text = "🏛️ Đã mở Mặt Bằng Nhà Kiến Trúc & Preview 2D/3D";
                        return;

                    case "DTT_WALL_MENU":
                        ArchitectureTools.CmdShowWallMenu();
                        TxtStatus.Text = "🧱 Đã mở Menu Chọn Loại Tường";
                        return;

                    case "DTT_DOOR_MENU":
                        ArchitectureTools.CmdShowDoorMenu();
                        TxtStatus.Text = "🚪 Đã mở Menu Chọn Loại Cửa Đi";
                        return;

                    case "DTT_WIN_MENU":
                        ArchitectureTools.CmdShowWindowMenu();
                        TxtStatus.Text = "🪟 Đã mở Menu Chọn Loại Cửa Sổ";
                        return;

                    case "DTT_STRUCT_MENU":
                        StructureTools.CmdShowStructMenu();
                        TxtStatus.Text = "🏗️ Đã mở Menu Chọn Loại Thép Kết Cấu";
                        return;

                    case "DTT_MEP_MENU":
                        MepTools.CmdShowMepMenu();
                        TxtStatus.Text = "💧 Đã mở Menu Chọn Đường Ống MEP";
                        return;

                    case "DT_OPENBLOCKS":
                    case "DTT_OPENBLOCKS":
                        DTT_DynamicBlockCommands.CmdOpenBlocksFolder();
                        TxtStatus.Text = "📂 Đã mở thư mục Blocks Thư Viện";
                        return;

                    case "DT_MENU":
                    case "DTT_MENU":
                        DtoolsResourceManager.EnsureDToolsTopMenuBar();
                        TxtStatus.Text = "📋 Đã cập nhật và hiển thị Menu 'DTools Pro' trên thanh MenuBar";
                        return;

                    // ============================================================
                    // BA MẠCH VÀ MẶT BẰNG ĐIỆN CHUẨN THỰC TẾ & CHẨN ĐOÁN
                    // ============================================================
                    case "DTT_E_LIGHTING":
                    case "DTT_E_DOL":
                    case "DTT_E_STARDELTA":
                    case "DTT_ELECTRICAL":
                    case "DTT_DIAG":
                        try
                        {
                            Document cadDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                            if (cadDoc != null)
                            {
                                CadUtils.FocusDwgView();
                                cadDoc.SendStringToExecute("\x03\x03" + cmdTag + "\n", true, false, false);
                                TxtStatus.Text = $"⚡ Đang thực thi: {cmdTag}...";
                            }
                        }
                        catch (Exception ex)
                        {
                            TxtStatus.Text = $"❌ Lỗi lệnh {cmdTag}: {ex.Message}";
                        }
                        return;

                    // ============================================================
                    // CÁC LỆNH GỐC AUTOCAD ELECTRICAL NATIVE (TIỆN ÍCH THÔNG MINH)
                    // ============================================================
                    case "AEWIRE":
                    case "AEMULTIBUS":
                    case "AECONNECTCOMP":
                    case "AELADDER":
                    case "AEWIRENO":
                    case "AEXREF":
                    case "AEAUDIT":
                    case "AECOMPONENT":
                    case "AESAVEDCIRCUIT":
                    case "AECIRCBUILDER":
                        try
                        {
                            Document acadeDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                            if (acadeDoc != null)
                            {
                                CadUtils.FocusDwgView();
                                acadeDoc.SendStringToExecute("\x03\x03" + cmdTag + "\n", true, false, false);
                                TxtStatus.Text = $"⚡ Đang kích hoạt lệnh Electrical Native: {cmdTag}";
                            }
                        }
                        catch (Exception ex)
                        {
                            TxtStatus.Text = $"❌ Lỗi lệnh {cmdTag}: {ex.Message}";
                        }
                        return;
                }

                // ============================================================
                // LỆNH CAD THÔNG THƯỜNG - Gửi trực tiếp đến AutoCAD
                // ============================================================
                try
                {
                    Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                    if (doc != null)
                    {
                        // 1. Tự động kiểm tra nạp YQArch nếu là lệnh thuộc hệ thống YQArch
                        string tagUpper = cmdTag.ToUpperInvariant().Trim();
                        if (tagUpper.StartsWith("Q") || tagUpper.StartsWith("YQ") || tagUpper.StartsWith("WW") || tagUpper.StartsWith("TW") ||
                            tagUpper.StartsWith("AD") || tagUpper.StartsWith("AW") || tagUpper.StartsWith("ZZ") || tagUpper.StartsWith("DD") ||
                            tagUpper.StartsWith("ER") || tagUpper.StartsWith("SS") || tagUpper.StartsWith("BB") || tagUpper.StartsWith("TT") ||
                            tagUpper.StartsWith("EW") || tagUpper.StartsWith("CW") || tagUpper.StartsWith("VW") || tagUpper.StartsWith("BFB") ||
                            tagUpper.StartsWith("ZMJ") || tagUpper.StartsWith("LTJ") || tagUpper.StartsWith("DTJ") || tagUpper.StartsWith("XWW") ||
                            tagUpper.StartsWith("JWW") || tagUpper.StartsWith("BWW") || tagUpper.StartsWith("ZXD") || tagUpper.StartsWith("TTR"))
                        {
                            EnsureYqArchLoaded(doc);
                        }

                        // 2. Focus về khung vẽ AutoCAD
                        CadUtils.FocusDwgView();

                        // 3. Kích hoạt lệnh mới
                        string formattedCmd = cmdTag.EndsWith("  ")
                            ? cmdTag.Trim() + "\n\n"
                            : cmdTag.Trim() + "\n";
                        doc.SendStringToExecute(formattedCmd, true, false, false);

                        TxtStatus.Text = $"⚡ Đang kích hoạt: {cmdTag.Trim()}";
                    }
                    else
                    {
                        TxtStatus.Text = "❌ Lỗi: Không tìm thấy bản vẽ AutoCAD hiện hành!";
                    }
                }
                catch (Exception ex)
                {
                    TxtStatus.Text = $"❌ Lỗi thực thi: {ex.Message}";
                }
            }
        }

        private static bool _yqLoaded = false;
        public static void EnsureYqArchLoaded(Document doc)
        {
            if (_yqLoaded || doc == null) return;
            try
            {
                DtoolsResourceManager.InitializeYQArchInternal();
                _yqLoaded = true;
            }
            catch { }
        }

        private void LoadYqArchManual()
        {
            try
            {
                DTT_MaintenanceTools.CmdYqArchHealthCheckAndRefix();
                _yqLoaded = true;
                TxtStatus.Text = "✅ Đã nạp thành công hệ thống YQArch Tiếng Việt!";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "❌ Lỗi nạp YQArch: " + ex.Message;
            }
        }

        public void SelectTab(string panelName)
        {
            foreach (var panel in _panels)
            {
                if (panel.Name == panelName)
                {
                    panel.Visibility = Visibility.Visible;
                    _activePanel = panel;
                }
                else
                {
                    panel.Visibility = Visibility.Collapsed;
                }
            }

            if (panelName == "PanelYqQuick" && TabYqQuick != null)
                TabYqQuick.IsChecked = true;
        }

        /// <summary>
        /// Chuyển đổi qua lại giữa các Tab phân hệ
        /// </summary>
        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rbtn && rbtn.Tag != null)
            {
                string targetPanelName = rbtn.Tag.ToString();

                foreach (var panel in _panels)
                {
                    if (panel.Name == targetPanelName)
                    {
                        panel.Visibility = Visibility.Visible;
                        _activePanel = panel;
                        if (targetPanelName == "PanelSymbols")
                        {
                            LoadSymbolsGrid();
                        }
                    }
                    else
                    {
                        panel.Visibility = Visibility.Collapsed;
                    }
                }

                // Nếu đang có từ khóa tìm kiếm thì áp dụng lọc lại
                if (!string.IsNullOrWhiteSpace(TxtSearch.Text))
                {
                    ApplySearchFilter(TxtSearch.Text.Trim().ToLower());
                }
            }
        }

        /// <summary>
        /// TÌM KIẾM NHANH (LIVE SEARCH): Gõ đến đâu lọc nút lệnh đến đó
        /// </summary>
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = TxtSearch.Text.Trim().ToLower();
            _currentSymbolKeyword = query;
            LoadSymbolsGrid();
            ApplySearchFilter(query);
        }

        private void ApplySearchFilter(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                foreach (var panel in _panels)
                {
                    panel.Visibility = (panel == _activePanel) ? Visibility.Visible : Visibility.Collapsed;
                    RestoreAllButtons(panel);
                }
                TxtStatus.Text = "Sẵn sàng thực thi 100% Native...";
                return;
            }

            int matchCount = 0;
            foreach (var panel in _panels)
            {
                if (panel == PanelSettings) continue; // Không lọc trong cài đặt

                bool panelHasMatch = false;
                List<Button> buttons = FindLogicalChildren<Button>(panel);

                foreach (var btn in buttons)
                {
                    string btnContent = ExtractButtonText(btn);
                    string btnTag = btn.Tag?.ToString() ?? "";
                    string btnTip = btn.ToolTip?.ToString() ?? "";

                    bool isMatch = btnContent.ToLower().Contains(query) ||
                                  btnTag.ToLower().Contains(query) ||
                                  btnTip.ToLower().Contains(query);

                    btn.Visibility = isMatch ? Visibility.Visible : Visibility.Collapsed;
                    if (isMatch)
                    {
                        panelHasMatch = true;
                        matchCount++;
                    }
                }

                panel.Visibility = panelHasMatch ? Visibility.Visible : Visibility.Collapsed;
            }

            TxtStatus.Text = $"Tìm thấy {matchCount} lệnh phù hợp với '{query}'";
        }

        private void RestoreAllButtons(StackPanel panel)
        {
            if (panel == null) return;
            List<Button> buttons = FindLogicalChildren<Button>(panel);
            foreach (var btn in buttons)
            {
                btn.Visibility = Visibility.Visible;
            }
        }

        private string ExtractButtonText(Button btn)
        {
            if (btn.Content is string s) return s;
            if (btn.Content is TextBlock tb) return tb.Text;
            if (btn.Content is StackPanel sp)
            {
                string combined = "";
                foreach (var child in sp.Children)
                {
                    if (child is TextBlock t) combined += " " + t.Text;
                }
                return combined;
            }
            return "";
        }

        #region TÙY BIẾN CÀI ĐẶT: CỠ CHỮ, CHỦ ĐỀ SÁNG/TỐI, ĐA NGÔN NGỮ

        /// <summary>
        /// Thay đổi Cỡ Chữ toàn bộ giao diện Palette
        /// </summary>
        private void CmbFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            double newSize = 11.0;
            switch (CmbFontSize.SelectedIndex)
            {
                case 0: newSize = 10.0; break;  // Nhỏ
                case 1: newSize = 11.5; break;  // Tiêu chuẩn
                case 2: newSize = 13.5; break;  // Lớn
                case 3: newSize = 15.0; break;  // Rất lớn
            }

            // Áp dụng cỡ chữ cho tất cả các nút
            List<Button> allButtons = FindLogicalChildren<Button>(MainContentGrid);
            foreach (var btn in allButtons)
            {
                btn.FontSize = newSize;
                btn.Height = newSize * 3.0; // Tự co giãn chiều cao nút theo cỡ chữ
            }

            TxtStatus.Text = $"Đã cập nhật cỡ chữ giao diện: {newSize} px";
        }

        /// <summary>
        /// Thay đổi Chủ Đề Sáng / Tối (Theme Mode)
        /// </summary>
        private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            switch (CmbTheme.SelectedIndex)
            {
                case 0: // 🌑 Dark Slate Pro (Xám Đậm & Xanh Dương Hiện Đại)
                    ApplyThemeColors(
                        bgDark: "#0F172A",
                        cardBg: "#1E293B",
                        border: "#334155",
                        textPrimary: "#F8FAFC",
                        textSecondary: "#94A3B8",
                        accent: "#0EA5E9",
                        accentHover: "#38BDF8",
                        controlBg: "#0F172A",
                        controlBorder: "#334155",
                        sidebarBg: "#1E293B",
                        tabNormal: "#64748B",
                        tabActiveText: "#00D2FF",
                        tabActiveBg: "#0F172A"
                    );
                    break;

                case 1: // ☀️ Light Studio (Trắng Sáng & Xanh)
                    ApplyThemeColors(
                        bgDark: "#F8FAFC",
                        cardBg: "#FFFFFF",
                        border: "#E2E8F0",
                        textPrimary: "#0F172A",
                        textSecondary: "#64748B",
                        accent: "#0284C7",
                        accentHover: "#0EA5E9",
                        controlBg: "#F1F5F9",
                        controlBorder: "#CBD5E1",
                        sidebarBg: "#FFFFFF",
                        tabNormal: "#64748B",
                        tabActiveText: "#0284C7",
                        tabActiveBg: "#F0F9FF"
                    );
                    break;

                case 2: // 🌌 Cyberpunk Dark (Navy & Neon Cyan)
                    ApplyThemeColors(
                        bgDark: "#0B0F19",
                        cardBg: "#111827",
                        border: "#1F2937",
                        textPrimary: "#F9FAFB",
                        textSecondary: "#9CA3AF",
                        accent: "#00F2FE",
                        accentHover: "#4FACFE",
                        controlBg: "#0B0F19",
                        controlBorder: "#1F2937",
                        sidebarBg: "#111827",
                        tabNormal: "#6B7280",
                        tabActiveText: "#00F2FE",
                        tabActiveBg: "#0B0F19"
                    );
                    break;
            }
        }

        private void ApplyThemeColors(string bgDark, string cardBg, string border, string textPrimary, string textSecondary,
                                      string accent, string accentHover, string controlBg, string controlBorder,
                                      string sidebarBg, string tabNormal, string tabActiveText, string tabActiveBg)
        {
            try
            {
                Color cBg = (Color)ColorConverter.ConvertFromString(bgDark);
                Color cCard = (Color)ColorConverter.ConvertFromString(cardBg);
                Color cBorder = (Color)ColorConverter.ConvertFromString(border);
                Color cText = (Color)ColorConverter.ConvertFromString(textPrimary);
                Color cSubText = (Color)ColorConverter.ConvertFromString(textSecondary);
                Color cAccent = (Color)ColorConverter.ConvertFromString(accent);
                Color cAccentHover = (Color)ColorConverter.ConvertFromString(accentHover);
                Color cControlBg = (Color)ColorConverter.ConvertFromString(controlBg);
                Color cControlBorder = (Color)ColorConverter.ConvertFromString(controlBorder);
                Color cSidebarBg = (Color)ColorConverter.ConvertFromString(sidebarBg);
                Color cTabNormal = (Color)ColorConverter.ConvertFromString(tabNormal);
                Color cTabActiveText = (Color)ColorConverter.ConvertFromString(tabActiveText);
                Color cTabActiveBg = (Color)ColorConverter.ConvertFromString(tabActiveBg);

                // Cập nhật DynamicResource của UserControl để toàn bộ giao diện tự đổi màu
                Resources["ThemePaletteBg"] = new SolidColorBrush(cBg);
                Resources["ThemeCardBg"] = new SolidColorBrush(cCard);
                Resources["ThemeCardBorder"] = new SolidColorBrush(cBorder);
                Resources["ThemeTextPrimary"] = new SolidColorBrush(cText);
                Resources["ThemeTextSecondary"] = new SolidColorBrush(cSubText);
                Resources["ThemeAccent"] = new SolidColorBrush(cAccent);
                Resources["ThemeAccentHover"] = new SolidColorBrush(cAccentHover);
                Resources["ThemeControlBg"] = new SolidColorBrush(cControlBg);
                Resources["ThemeControlBorder"] = new SolidColorBrush(cControlBorder);
                Resources["ThemeSidebarBg"] = new SolidColorBrush(cSidebarBg);
                Resources["ThemeTabNormalText"] = new SolidColorBrush(cTabNormal);
                Resources["ThemeTabActiveText"] = new SolidColorBrush(cTabActiveText);
                Resources["ThemeTabActiveBg"] = new SolidColorBrush(cTabActiveBg);

                // Đồng bộ trực tiếp các khung chính
                RootPaletteControl.Background = (SolidColorBrush)Resources["ThemePaletteBg"];
                HeaderBorder.Background = (SolidColorBrush)Resources["ThemeCardBg"];
                HeaderBorder.BorderBrush = (SolidColorBrush)Resources["ThemeCardBorder"];
                SidebarBorder.Background = (SolidColorBrush)Resources["ThemeSidebarBg"];
                SidebarBorder.BorderBrush = (SolidColorBrush)Resources["ThemeControlBorder"];
                FooterBorder.Background = (SolidColorBrush)Resources["ThemeCardBg"];
                FooterBorder.BorderBrush = (SolidColorBrush)Resources["ThemeCardBorder"];
                LblAppTitle.Foreground = (SolidColorBrush)Resources["ThemeTextPrimary"];

                TxtStatus.Text = $"⚡ Đã chuyển chủ đề: {CmbTheme.Text}";
            }
            catch { }
        }

        /// <summary>
        /// Thay đổi Ngôn Ngữ Hiển Thị (Tiếng Việt / English)
        /// </summary>
        private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            bool isEng = (CmbLanguage.SelectedIndex == 1);
            if (isEng)
            {
                LblAppTitle.Text = "DTT CAD TOOLSET PRO";
                TxtSearch.ToolTip = "Quick Search Command...";
                TxtStatus.Text = "Ready to execute 100% Native CAD Commands...";
            }
            else
            {
                LblAppTitle.Text = "DTT CAD TOOLSET";
                TxtSearch.ToolTip = "Tìm kiếm lệnh nhanh...";
                TxtStatus.Text = "Sẵn sàng thực thi lệnh 100% Native...";
            }
        }

        #endregion

        // ============================================================
        // KHO THƯ VIỆN KÝ HIỆU & LINH KIỆN TỦ ĐIỆN (GRID ICONS VIEW)
        // ============================================================

        private string _currentSymbolCategory = "All";
        private string _currentSymbolKeyword = "";

        private void LoadSymbolsGrid()
        {
            if (SymbolsGridPanel == null) return;
            SymbolsGridPanel.Children.Clear();

            var symbols = ElectricalSymbolsManager.SearchSymbols(_currentSymbolKeyword, _currentSymbolCategory);
            foreach (var sym in symbols)
            {
                var card = CreateSymbolCard(sym);
                SymbolsGridPanel.Children.Add(card);
            }
        }

        private Border CreateSymbolCard(ElectricalSymbolItem sym)
        {
            var border = new Border
            {
                Width = 126,
                Height = 64,
                Margin = new Thickness(2, 2, 2, 2),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = $"{sym.NameVi}\n{sym.NameEn} (ID: {sym.Id})\n👉 Click để chèn vào bản vẽ AutoCAD"
            };

            var sp = new StackPanel
            {
                Margin = new Thickness(6, 4, 6, 4),
                VerticalAlignment = VerticalAlignment.Center
            };

            var topGrid = new Grid();
            var txtIcon = new TextBlock
            {
                Text = sym.Icon ?? "⚡",
                FontSize = 15,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var txtCat = new TextBlock
            {
                Text = sym.Category,
                FontSize = 8,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            topGrid.Children.Add(txtIcon);
            topGrid.Children.Add(txtCat);

            var txtNameVi = new TextBlock
            {
                Text = sym.NameVi,
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            };

            var txtNameEn = new TextBlock
            {
                Text = sym.NameEn,
                FontSize = 8,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            sp.Children.Add(topGrid);
            sp.Children.Add(txtNameVi);
            sp.Children.Add(txtNameEn);
            border.Child = sp;

            border.MouseEnter += (s, e) =>
            {
                border.Background = new SolidColorBrush(Color.FromRgb(240, 249, 255));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(2, 132, 199));
            };
            border.MouseLeave += (s, e) =>
            {
                border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            };

            border.MouseLeftButtonDown += (s, e) =>
            {
                OnSymbolCardClick(sym);
            };

            return border;
        }

        private void OnSymbolCardClick(ElectricalSymbolItem sym)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null || sym == null) return;

            ElectricalPlacementContext.TargetSymbol = sym;
            ElectricalPlacementContext.Scale = 1.0;
            ElectricalPlacementContext.Rotation = 0.0;

            TxtStatus.Text = $"👉 Chọn vị trí đặt ký hiệu [{sym.NameVi}] trên bản vẽ CAD...";
            CadUtils.FocusDwgView();
            doc.SendStringToExecute("\x03\x03DTT_PLACE_SYMBOL\n", true, false, false);
        }

        private void BtnFilterCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                _currentSymbolCategory = btn.Tag.ToString();

                ResetFilterButton(BtnFilterAll, false);
                ResetFilterButton(BtnFilterPower, false);
                ResetFilterButton(BtnFilterProt, false);
                ResetFilterButton(BtnFilterCont, false);
                ResetFilterButton(BtnFilterMotor, false);
                ResetFilterButton(BtnFilterRelay, false);
                ResetFilterButton(BtnFilterBtn, false);
                ResetFilterButton(BtnFilterLight, false);
                ResetFilterButton(BtnFilterPanel, false);
                ResetFilterButton(btn, true);

                LoadSymbolsGrid();
            }
        }

        private void ResetFilterButton(Button b, bool isActive)
        {
            if (b == null) return;
            b.Background = new SolidColorBrush(isActive ? Color.FromRgb(2, 132, 199) : Color.FromRgb(255, 255, 255));
            b.Foreground = new SolidColorBrush(isActive ? Color.FromRgb(255, 255, 255) : Color.FromRgb(51, 65, 85));
            b.BorderThickness = new Thickness(isActive ? 0 : 1);
        }

        private static List<T> FindLogicalChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            List<T> list = new List<T>();
            if (depObj == null) return list;

            int count = VisualTreeHelper.GetChildrenCount(depObj);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T typedChild)
                {
                    list.Add(typedChild);
                }
                list.AddRange(FindLogicalChildren<T>(child));
            }
            return list;
        }
    }
}
