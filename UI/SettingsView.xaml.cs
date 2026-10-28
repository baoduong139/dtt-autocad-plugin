using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.AutoCAD.ApplicationServices;
using DTT_AutoCAD_Plugin.Common;
using DTT_AutoCAD_Plugin.Modules;

namespace DTT_AutoCAD_Plugin.UI
{
    /// <summary>
    /// Code-behind cho bảng Cài Đặt (SettingsView.xaml)
    /// Hỗ trợ chuyển đổi Theme Dynamic (Dark Slate, Light Studio, Cyberpunk), Cỡ chữ và Gọi lệnh Hệ thống
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public event Action<string> OnThemeChanged;
        public event Action<double> OnFontSizeChanged;

        public SettingsView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Mặc định nạp Dark Slate Pro
            ApplyDarkSlateTheme();
        }

        #region Theme Switching Logic

        private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || CmbTheme == null) return;

            switch (CmbTheme.SelectedIndex)
            {
                case 0: // 🌑 Dark Slate Pro
                    ApplyDarkSlateTheme();
                    OnThemeChanged?.Invoke("DarkSlate");
                    break;

                case 1: // ☀️ Light Studio
                    ApplyLightStudioTheme();
                    OnThemeChanged?.Invoke("LightStudio");
                    break;

                case 2: // 🌌 Cyberpunk Dark
                    ApplyCyberpunkTheme();
                    OnThemeChanged?.Invoke("Cyberpunk");
                    break;
            }
        }

        public void ApplyDarkSlateTheme()
        {
            ApplyThemeColors(
                paletteBg: "#0F172A",      // Slate 900
                cardBg: "#1E293B",         // Slate 800
                cardBorder: "#334155",     // Slate 700
                textPrimary: "#F8FAFC",    // Slate 50
                textSecondary: "#94A3B8",  // Slate 400
                accent: "#0EA5E9",         // Sky Blue 500
                accentHover: "#38BDF8",    // Sky Blue 400
                controlBg: "#0F172A",      // Slate 900
                controlBorder: "#334155"   // Slate 700
            );
        }

        public void ApplyLightStudioTheme()
        {
            ApplyThemeColors(
                paletteBg: "#F8FAFC",      // Slate 50
                cardBg: "#FFFFFF",         // White
                cardBorder: "#E2E8F0",     // Slate 200
                textPrimary: "#0F172A",    // Slate 900
                textSecondary: "#64748B",  // Slate 500
                accent: "#0284C7",         // Sky Blue 600
                accentHover: "#0EA5E9",    // Sky Blue 500
                controlBg: "#F1F5F9",      // Slate 100
                controlBorder: "#CBD5E1"   // Slate 300
            );
        }

        public void ApplyCyberpunkTheme()
        {
            ApplyThemeColors(
                paletteBg: "#0B0F19",
                cardBg: "#111827",
                cardBorder: "#1F2937",
                textPrimary: "#F9FAFB",
                textSecondary: "#9CA3AF",
                accent: "#00F2FE",
                accentHover: "#4FACFE",
                controlBg: "#0B0F19",
                controlBorder: "#1F2937"
            );
        }

        private void ApplyThemeColors(string paletteBg, string cardBg, string cardBorder,
                                      string textPrimary, string textSecondary,
                                      string accent, string accentHover,
                                      string controlBg, string controlBorder)
        {
            try
            {
                Color cPaletteBg = (Color)ColorConverter.ConvertFromString(paletteBg);
                Color cCardBg = (Color)ColorConverter.ConvertFromString(cardBg);
                Color cCardBorder = (Color)ColorConverter.ConvertFromString(cardBorder);
                Color cTextPrimary = (Color)ColorConverter.ConvertFromString(textPrimary);
                Color cTextSecondary = (Color)ColorConverter.ConvertFromString(textSecondary);
                Color cAccent = (Color)ColorConverter.ConvertFromString(accent);
                Color cAccentHover = (Color)ColorConverter.ConvertFromString(accentHover);
                Color cControlBg = (Color)ColorConverter.ConvertFromString(controlBg);
                Color cControlBorder = (Color)ColorConverter.ConvertFromString(controlBorder);

                Resources["ThemePaletteBg"] = new SolidColorBrush(cPaletteBg);
                Resources["ThemeCardBg"] = new SolidColorBrush(cCardBg);
                Resources["ThemeCardBorder"] = new SolidColorBrush(cCardBorder);
                Resources["ThemeTextPrimary"] = new SolidColorBrush(cTextPrimary);
                Resources["ThemeTextSecondary"] = new SolidColorBrush(cTextSecondary);
                Resources["ThemeAccent"] = new SolidColorBrush(cAccent);
                Resources["ThemeAccentHover"] = new SolidColorBrush(cAccentHover);
                Resources["ThemeControlBg"] = new SolidColorBrush(cControlBg);
                Resources["ThemeControlBorder"] = new SolidColorBrush(cControlBorder);

                RootSettingsControl.Background = (SolidColorBrush)Resources["ThemePaletteBg"];
            }
            catch { }
        }

        #endregion

        #region Font Size & Language

        private void CmbFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || CmbFontSize == null) return;

            double newSize = 11.5;
            switch (CmbFontSize.SelectedIndex)
            {
                case 0: newSize = 10.0; break;
                case 1: newSize = 11.5; break;
                case 2: newSize = 13.5; break;
                case 3: newSize = 15.0; break;
            }

            OnFontSizeChanged?.Invoke(newSize);
        }

        private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || CmbLanguage == null) return;
            // Xử lý chuyển đổi đa ngôn ngữ nếu cần
        }

        #endregion

        #region Button Action Handlers

        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string cmdTag)
            {
                try
                {
                    switch (cmdTag)
                    {
                        case "DT_MENU":
                        case "DTT_MENU":
                            DtoolsResourceManager.EnsureDToolsTopMenuBar();
                            break;

                        case "DT_YQREFIX":
                        case "DTT_YQREFIX":
                            DTT_MaintenanceTools.CmdYqArchHealthCheckAndRefix();
                            break;

                        case "DT_CLEAN":
                        case "DTT_CLEAN":
                            DTT_MaintenanceTools.CmdAutoPurgeAndAudit();
                            break;

                        default:
                            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                            if (doc != null)
                            {
                                CadUtils.FocusDwgView();
                                doc.SendStringToExecute("\x03\x03" + cmdTag.Trim() + "\n", true, false, false);
                            }
                            break;
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Lỗi thực thi lệnh: " + ex.Message, "DTools Pro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}
