using System;
using System.Drawing;
using System.Windows.Forms.Integration;
using Autodesk.AutoCAD.Windows;
using DTools.Core;

namespace DTT_AutoCAD_Plugin.UI
{
    /// <summary>
    /// QUẢN LÝ THANH DOCKABLE PALETTE (PALETTESET) CHỨA WPF NATIVE 100%
    /// Áp dụng mô hình Lazy Loading & Safe Instantiation: Chỉ tạo khi người dùng gọi lệnh
    /// </summary>
    public static class DttPaletteManager
    {
        private static PaletteSet _paletteSet;
        private static ElementHost _elementHost;
        private static DTT_PaletteView _paletteView;
        private static readonly Guid PaletteGuid = new Guid("4A7E8D3C-1F2B-4E5A-9D6C-8E7B6A5F4C3D");
        private static readonly object _lock = new object();

        public static bool IsCreated => _paletteSet != null;

        public static bool IsVisible => _paletteSet != null && _paletteSet.Visible;

        public static DTT_PaletteView PaletteView => _paletteView;

        /// <summary>
        /// Bật/Tắt (Toggle) thanh Palette DTT một cách an toàn
        /// </summary>
        public static void TogglePalette()
        {
            try
            {
                lock (_lock)
                {
                    if (_paletteSet == null)
                    {
                        bool created = CreatePalette();
                        if (!created || _paletteSet == null) return;
                        _paletteSet.Visible = true;
                        DToolsConfiguration.IsPaletteInitialized = true;
                        DToolsLogger.LogInfo("Palette", "Khởi tạo PaletteSet lần đầu thành công.");
                        return;
                    }

                    _paletteSet.Visible = !_paletteSet.Visible;
                }
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogError("Palette", "Lỗi khi Toggle PaletteSet", ex);
            }
        }

        /// <summary>
        /// Hiển thị thanh Palette DTT
        /// </summary>
        public static void ShowPalette()
        {
            try
            {
                lock (_lock)
                {
                    if (_paletteSet == null)
                    {
                        bool created = CreatePalette();
                        if (!created || _paletteSet == null) return;
                        DToolsConfiguration.IsPaletteInitialized = true;
                        DToolsLogger.LogInfo("Palette", "Khởi tạo PaletteSet on-demand thành công.");
                    }

                    _paletteSet.Visible = true;
                    try
                    {
                        if (_paletteSet.AutoRollUp)
                        {
                            _paletteSet.AutoRollUp = false;
                        }
                    }
                    catch { }
                }
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogError("Palette", "Lỗi khi Show PaletteSet", ex);
            }
        }

        public static void SelectTab(string panelName)
        {
            try
            {
                ShowPalette();
                _paletteView?.SelectTab(panelName);
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogError("Palette", $"Lỗi khi chuyển Tab sang '{panelName}'", ex);
            }
        }

        private static bool CreatePalette()
        {
            try
            {
                _paletteSet = new PaletteSet("DTT CAD TOOLSET - 3D NATIVE PRO", PaletteGuid)
                {
                    Style = PaletteSetStyles.ShowAutoHideButton |
                            PaletteSetStyles.ShowCloseButton |
                            PaletteSetStyles.ShowPropertiesMenu,
                    Dock = DockSides.Right, // Neo cố định bên phải màn hình AutoCAD
                    DockEnabled = DockSides.Left | DockSides.Right,
                    MinimumSize = new Size(300, 500),
                    Size = new Size(340, 760)
                };

                _paletteView = new DTT_PaletteView();
                _elementHost = new ElementHost
                {
                    Dock = System.Windows.Forms.DockStyle.Fill,
                    Child = _paletteView
                };

                _paletteSet.Add("Trung Tâm Công Cụ DTT", _elementHost);
                _paletteSet.KeepFocus = false; // Tránh cướp phím gõ lệnh của AutoCAD
                return true;
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogFatal("Palette", "Không thể tạo PaletteSet hoặc UserControl WPF", ex);
                _paletteSet = null;
                _elementHost = null;
                _paletteView = null;
                return false;
            }
        }

        public static void CloseAndDispose()
        {
            try
            {
                lock (_lock)
                {
                    if (_paletteSet != null)
                    {
                        _paletteSet.Visible = false;
                        _paletteSet.Dispose();
                        _paletteSet = null;
                    }
                    if (_elementHost != null)
                    {
                        _elementHost.Dispose();
                        _elementHost = null;
                    }
                    _paletteView = null;
                    DToolsConfiguration.IsPaletteInitialized = false;
                }
            }
            catch (System.Exception ex)
            {
                DToolsLogger.LogError("Palette", "Lỗi khi giải phóng PaletteSet", ex);
            }
        }
    }
}
