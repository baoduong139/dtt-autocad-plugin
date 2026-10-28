using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DTT_AutoCAD_Plugin.Modules;

namespace DTT_AutoCAD_Plugin.UI
{
    public partial class DTools_CabinetBuilderDialog : Window
    {
        public CabinetConfig ResultConfig { get; private set; }
        private bool _isInitialized = false;

        public DTools_CabinetBuilderDialog()
        {
            InitializeComponent();
            InitializePresets();
            _isInitialized = true;
            UpdateLivePreview();
        }

        public bool Build3DMode { get; private set; } = false;

        public static CabinetConfig PromptCabinetConfig(out bool is3DMode)
        {
            var dlg = new DTools_CabinetBuilderDialog();
            bool? res = dlg.ShowDialog();
            is3DMode = dlg.Build3DMode;
            return (res == true) ? dlg.ResultConfig : null;
        }

        public static CabinetConfig PromptCabinetConfig()
        {
            return PromptCabinetConfig(out _);
        }

        private void InitializePresets()
        {
            CboPresets.Items.Add(new ComboBoxItem { Content = "--- Chọn mẫu tủ nhanh ---", IsSelected = true });
            CboPresets.Items.Add(new ComboBoxItem { Content = "📦 Tủ phân phối DB (800x1200x300 - 18 MCB)" });
            CboPresets.Items.Add(new ComboBoxItem { Content = "⚡ Tủ điện tổng MDB (1200x2000x600 - 2 cánh - 36 MCB)" });
            CboPresets.Items.Add(new ComboBoxItem { Content = "🛡️ Tủ điều khiển ISS-1 (600x800x250 - 6 Relay)" });
            CboPresets.Items.Add(new ComboBoxItem { Content = "💡 Tủ chiếu sáng LP (400x600x200 - 8 MCB)" });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TxtHeight.Focus();
            TxtHeight.SelectAll();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                DialogResult = false;
                Close();
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                BtnBuild_Click(sender, e);
            }
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                string tag = rb.Tag.ToString();
                PanelDimensions.Visibility = (tag == "PanelDimensions") ? Visibility.Visible : Visibility.Collapsed;
                PanelStructure.Visibility = (tag == "PanelStructure") ? Visibility.Visible : Visibility.Collapsed;
                PanelComponents.Visibility = (tag == "PanelComponents") ? Visibility.Visible : Visibility.Collapsed;
                PanelTitleBlock.Visibility = (tag == "PanelTitleBlock") ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void CboPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || CboPresets.SelectedIndex <= 0) return;

            int idx = CboPresets.SelectedIndex;
            if (idx == 1) // Tủ phân phối DB
            {
                TxtWidth.Text = "800";
                TxtHeight.Text = "1200";
                TxtDepth.Text = "300";
                TxtPlinth.Text = "100";
                CboDoorCount.SelectedIndex = 0; // 1 cánh
                TxtMccbCount.Text = "1";
                CboMccbRating.SelectedIndex = 2; // 250A
                TxtMcbBranches.Text = "18";
                TxtRelayCount.Text = "4";
                TxtTerminalCount.Text = "24";
                TxtCabinetName.Text = "TỦ PHÂN PHỐI DB-01";
                TxtPanelSize.Text = "1100x720";
            }
            else if (idx == 2) // Tủ điện tổng MDB
            {
                TxtWidth.Text = "1200";
                TxtHeight.Text = "2000";
                TxtDepth.Text = "600";
                TxtPlinth.Text = "150";
                CboDoorCount.SelectedIndex = 1; // 2 cánh
                TxtMccbCount.Text = "1";
                CboMccbRating.SelectedIndex = 4; // 630A
                TxtMcbBranches.Text = "36";
                TxtRelayCount.Text = "8";
                TxtTerminalCount.Text = "48";
                TxtCabinetName.Text = "TỦ ĐIỆN TỔNG MDB-MAIN";
                TxtPanelSize.Text = "1800x1100";
            }
            else if (idx == 3) // Tủ điều khiển ISS-1
            {
                TxtWidth.Text = "600";
                TxtHeight.Text = "800";
                TxtDepth.Text = "250";
                TxtPlinth.Text = "80";
                CboDoorCount.SelectedIndex = 0; // 1 cánh
                TxtMccbCount.Text = "1";
                CboMccbRating.SelectedIndex = 0; // 100A
                TxtMcbBranches.Text = "12";
                TxtRelayCount.Text = "6";
                TxtTerminalCount.Text = "20";
                TxtCabinetName.Text = "TỦ ĐIỆN ISS-1";
                TxtPanelSize.Text = "720x520";
            }
            else if (idx == 4) // Tủ chiếu sáng LP
            {
                TxtWidth.Text = "400";
                TxtHeight.Text = "600";
                TxtDepth.Text = "200";
                TxtPlinth.Text = "0";
                CboDoorCount.SelectedIndex = 0; // 1 cánh
                TxtMccbCount.Text = "1";
                CboMccbRating.SelectedIndex = 0; // 100A
                TxtMcbBranches.Text = "8";
                TxtRelayCount.Text = "2";
                TxtTerminalCount.Text = "12";
                TxtCabinetName.Text = "TỦ CHIẾU SÁNG LP-01";
                TxtPanelSize.Text = "550x350";
            }

            UpdateLivePreview();
        }

        private void Parameter_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitialized)
            {
                UpdateLivePreview();
            }
        }

        private void PreviewCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isInitialized)
            {
                UpdateLivePreview();
            }
        }

        // ============================================================
        // VẼ MÔ HÌNH XEM TRƯỚC TRỰC QUAN (LIVE VECTOR PREVIEW)
        // ============================================================

        private void UpdateLivePreview()
        {
            if (PreviewCanvas == null) return;
            PreviewCanvas.Children.Clear();

            double cvW = PreviewCanvas.ActualWidth;
            double cvH = PreviewCanvas.ActualHeight;
            if (cvW < 100 || cvH < 100) return;

            double w = ParseDouble(TxtWidth?.Text, 800.0);
            double h = ParseDouble(TxtHeight?.Text, 1200.0);
            double d = ParseDouble(TxtDepth?.Text, 300.0);
            double plinth = ParseDouble(TxtPlinth?.Text, 100.0);
            int doorCount = (CboDoorCount?.SelectedIndex == 1) ? 2 : 1;
            string cabName = TxtCabinetName?.Text ?? "TỦ ĐIỆN";

            double totalH = h + plinth;
            // Vẽ 2 mặt: Mặt trước và Mặt trong cạnh nhau
            double combinedW = w * 2.0 + 80.0;
            double scale = Math.Min((cvW - 40.0) / combinedW, (cvH - 40.0) / totalH);
            if (scale <= 0.001) scale = 0.2;

            double scaledW = w * scale;
            double scaledH = h * scale;
            double scaledPlinth = plinth * scale;
            double scaledTotalH = totalH * scale;

            double startY = (cvH - scaledTotalH) / 2.0;
            double frontX = (cvW - (scaledW * 2.0 + 30.0)) / 2.0;
            double innerX = frontX + scaledW + 30.0;

            // 1. VẼ MẶT TRƯỚC (FRONT VIEW)
            // Chân đế
            if (scaledPlinth > 0)
            {
                var rPlinth = new Rectangle
                {
                    Width = scaledW,
                    Height = scaledPlinth,
                    Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    StrokeThickness = 1.2,
                    Fill = new SolidColorBrush(Color.FromRgb(30, 41, 59))
                };
                Canvas.SetLeft(rPlinth, frontX);
                Canvas.SetTop(rPlinth, startY + scaledH);
                PreviewCanvas.Children.Add(rPlinth);
            }

            // Vỏ tủ
            var rBody = new Rectangle
            {
                Width = scaledW,
                Height = scaledH,
                Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                StrokeThickness = 1.8,
                Fill = new SolidColorBrush(Color.FromRgb(15, 23, 42))
            };
            Canvas.SetLeft(rBody, frontX);
            Canvas.SetTop(rBody, startY);
            PreviewCanvas.Children.Add(rBody);

            // Cánh tủ
            double dInset = 4.0;
            if (doorCount == 1)
            {
                var rDoor = new Rectangle
                {
                    Width = scaledW - dInset * 2,
                    Height = scaledH - dInset * 2,
                    Stroke = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    StrokeThickness = 1.0
                };
                Canvas.SetLeft(rDoor, frontX + dInset);
                Canvas.SetTop(rDoor, startY + dInset);
                PreviewCanvas.Children.Add(rDoor);

                // Khóa
                var lockCir = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(251, 191, 36))
                };
                Canvas.SetLeft(lockCir, frontX + scaledW - 12);
                Canvas.SetTop(lockCir, startY + scaledH / 2.0);
                PreviewCanvas.Children.Add(lockCir);
            }
            else
            {
                double sw = (scaledW - dInset * 2 - 2) / 2.0;
                var rD1 = new Rectangle { Width = sw, Height = scaledH - dInset * 2, Stroke = new SolidColorBrush(Color.FromRgb(16, 185, 129)), StrokeThickness = 1 };
                var rD2 = new Rectangle { Width = sw, Height = scaledH - dInset * 2, Stroke = new SolidColorBrush(Color.FromRgb(16, 185, 129)), StrokeThickness = 1 };
                Canvas.SetLeft(rD1, frontX + dInset);
                Canvas.SetTop(rD1, startY + dInset);
                Canvas.SetLeft(rD2, frontX + dInset + sw + 2);
                Canvas.SetTop(rD2, startY + dInset);
                PreviewCanvas.Children.Add(rD1);
                PreviewCanvas.Children.Add(rD2);
            }

            // Biển tên tủ
            var rTag = new Rectangle
            {
                Width = scaledW * 0.6,
                Height = 12,
                Stroke = new SolidColorBrush(Color.FromRgb(251, 191, 36)),
                Fill = new SolidColorBrush(Color.FromRgb(30, 41, 59))
            };
            Canvas.SetLeft(rTag, frontX + (scaledW - scaledW * 0.6) / 2.0);
            Canvas.SetTop(rTag, startY + 8);
            PreviewCanvas.Children.Add(rTag);

            // 3 Đèn báo pha
            double lampR = 4.0;
            double cLamp = frontX + scaledW / 2.0;
            AddPreviewLamp(cLamp - 10, startY + 26, lampR, Color.FromRgb(239, 68, 68)); // Đỏ
            AddPreviewLamp(cLamp, startY + 26, lampR, Color.FromRgb(245, 158, 11));     // Vàng
            AddPreviewLamp(cLamp + 10, startY + 26, lampR, Color.FromRgb(16, 185, 129)); // Xanh

            // Nhãn text mặt trước
            var lblF = new TextBlock
            {
                Text = "MẶT TRƯỚC",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
            };
            Canvas.SetLeft(lblF, frontX + scaledW / 2.0 - 24);
            Canvas.SetTop(lblF, startY + scaledTotalH + 4);
            PreviewCanvas.Children.Add(lblF);

            // 2. VẼ MẶT TRONG / TẤM GÁ (INNER PLATE VIEW)
            var rInBody = new Rectangle
            {
                Width = scaledW,
                Height = scaledH,
                Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                StrokeThickness = 1.2
            };
            Canvas.SetLeft(rInBody, innerX);
            Canvas.SetTop(rInBody, startY);
            PreviewCanvas.Children.Add(rInBody);

            // Tấm gá (Plate)
            var rPlate = new Rectangle
            {
                Width = scaledW - 14,
                Height = scaledH - 14,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Fill = new SolidColorBrush(Color.FromRgb(30, 41, 59))
            };
            Canvas.SetLeft(rPlate, innerX + 7);
            Canvas.SetTop(rPlate, startY + 7);
            PreviewCanvas.Children.Add(rPlate);

            // MCCB khối
            double mccbW = scaledW * 0.38;
            double mccbH = scaledH * 0.18;
            var rMccb = new Rectangle
            {
                Width = mccbW,
                Height = mccbH,
                Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                Stroke = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(rMccb, innerX + (scaledW - mccbW) / 2.0);
            Canvas.SetTop(rMccb, startY + 18);
            PreviewCanvas.Children.Add(rMccb);

            // Các hàng MCB
            int rows = 3;
            double rowStep = (scaledH - mccbH - 50) / rows;
            for (int r = 0; r < rows; r++)
            {
                double ry = startY + mccbH + 30 + r * rowStep;
                // DIN Rail
                var rail = new Line
                {
                    X1 = innerX + 16,
                    X2 = innerX + scaledW - 16,
                    Y1 = ry + 8,
                    Y2 = ry + 8,
                    Stroke = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    StrokeThickness = 1.5
                };
                PreviewCanvas.Children.Add(rail);

                // Dàn tép MCB
                int mcbs = 6;
                double mcbW = 6.0;
                double mcbH = 14.0;
                double mcbStartX = innerX + (scaledW - mcbs * 8.0) / 2.0;
                for (int m = 0; m < mcbs; m++)
                {
                    var rMcb = new Rectangle
                    {
                        Width = mcbW,
                        Height = mcbH,
                        Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                        Stroke = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                        StrokeThickness = 0.5
                    };
                    Canvas.SetLeft(rMcb, mcbStartX + m * 8.0);
                    Canvas.SetTop(rMcb, ry);
                    PreviewCanvas.Children.Add(rMcb);
                }
            }

            // Dàn Terminal Block ở đáy
            var tbLine = new Line
            {
                X1 = innerX + 16,
                X2 = innerX + scaledW - 16,
                Y1 = startY + scaledH - 16,
                Y2 = startY + scaledH - 16,
                Stroke = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                StrokeThickness = 3
            };
            PreviewCanvas.Children.Add(tbLine);

            // Nhãn text mặt trong
            var lblI = new TextBlock
            {
                Text = "TẤM GÁ LINH KIỆN",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
            };
            Canvas.SetLeft(lblI, innerX + scaledW / 2.0 - 36);
            Canvas.SetTop(lblI, startY + scaledTotalH + 4);
            PreviewCanvas.Children.Add(lblI);

            // Cập nhật thông tin dưới footer
            if (TxtPreviewInfo != null)
            {
                TxtPreviewInfo.Text = $"Kích thước: {w:F0} x {totalH:F0} x {d:F0} mm | Tủ {doorCount} cánh | MCCB {CboMccbRating?.Text} | {TxtMcbBranches?.Text} MCB | {cabName}";
            }
        }

        private void AddPreviewLamp(double cx, double cy, double r, Color color)
        {
            var el = new Ellipse
            {
                Width = r * 2,
                Height = r * 2,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 0.6
            };
            Canvas.SetLeft(el, cx - r);
            Canvas.SetTop(el, cy - r);
            PreviewCanvas.Children.Add(el);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnBuild3D_Click(object sender, RoutedEventArgs e)
        {
            Build3DMode = true;
            BtnBuild_Click(sender, e);
        }

        private void BtnBuild_Click(object sender, RoutedEventArgs e)
        {
            ResultConfig = new CabinetConfig
            {
                // Tab 1
                Height = ParseDouble(TxtHeight.Text, 1200.0),
                Width = ParseDouble(TxtWidth.Text, 800.0),
                Depth = ParseDouble(TxtDepth.Text, 300.0),
                PlinthHeight = ParseDouble(TxtPlinth.Text, 100.0),
                DrawFront = ChkFront.IsChecked == true,
                DrawInnerDoor = ChkInner.IsChecked == true,
                DrawSide = ChkSide.IsChecked == true,
                DrawRear = ChkRear.IsChecked == true,
                DrawTopBottom = ChkTopBottom.IsChecked == true,
                AutoDimension = ChkAutoDim.IsChecked == true,
                DimTextHeight = ParseDouble(TxtDimTextHeight.Text, 25.0),
                DimScale = ParseDouble(TxtDimScale.Text, 1.0),

                // Tab 2
                DoorCount = (CboDoorCount.SelectedIndex == 1) ? 2 : 1,
                LockPosition = (CboLockPos.SelectedIndex == 1) ? "Trái" : (CboLockPos.SelectedIndex == 2) ? "Giữa" : "Phải",
                HasLouver = ChkLouver.IsChecked == true,
                LouverWidth = ParseDouble(TxtLouverW.Text, 220.0),
                LouverHeight = ParseDouble(TxtLouverH.Text, 120.0),
                PartitionRows = ParseInt(TxtRows.Text, 3),
                PartitionCols = ParseInt(TxtCols.Text, 1),
                HasWiringDuct = ChkWiringDuct.IsChecked == true,

                // Tab 3
                MccbCount = ParseInt(TxtMccbCount.Text, 1),
                MccbRating = (CboMccbRating.SelectedItem is ComboBoxItem item) ? item.Content.ToString() : "250A 3P",
                McbBranches = ParseInt(TxtMcbBranches.Text, 18),
                RelayCount = ParseInt(TxtRelayCount.Text, 6),
                TerminalCount = ParseInt(TxtTerminalCount.Text, 24),
                HasPilotLamps = ChkPilotLamps.IsChecked == true,

                // Tab 4
                CabinetName = string.IsNullOrWhiteSpace(TxtCabinetName.Text) ? "TỦ ĐIỆN ISS-1" : TxtCabinetName.Text.Trim(),
                PanelSize = string.IsNullOrWhiteSpace(TxtPanelSize.Text) ? "1500x1300" : TxtPanelSize.Text.Trim(),
                IpRating = string.IsNullOrWhiteSpace(TxtIpRating.Text) ? "IP54" : TxtIpRating.Text.Trim(),
                Voltage = string.IsNullOrWhiteSpace(TxtVoltage.Text) ? "380V/220V - 50Hz" : TxtVoltage.Text.Trim(),
                RatedCurrent = string.IsNullOrWhiteSpace(TxtRatedCurrent.Text) ? "250A" : TxtRatedCurrent.Text.Trim(),
                DrawTitleBlock = ChkDrawTitleBlock.IsChecked == true
            };

            DialogResult = true;
            Close();
        }

        private static double ParseDouble(string str, double fallback)
        {
            if (string.IsNullOrWhiteSpace(str)) return fallback;
            str = str.Trim().Replace(',', '.');
            return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double val) ? val : fallback;
        }

        private static int ParseInt(string str, int fallback)
        {
            if (string.IsNullOrWhiteSpace(str)) return fallback;
            str = str.Trim();
            return int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out int val) ? val : fallback;
        }
    }
}
