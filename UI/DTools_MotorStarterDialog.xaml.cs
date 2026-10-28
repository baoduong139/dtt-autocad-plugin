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
    public partial class DTools_MotorStarterDialog : Window
    {
        public MotorStarterConfig ResultConfig { get; private set; }
        private bool _isInitialized = false;

        public DTools_MotorStarterDialog()
        {
            InitializeComponent();
            _isInitialized = true;
            UpdateCalculationAndPreview();
        }

        public static MotorStarterConfig PromptConfig()
        {
            var dlg = new DTools_MotorStarterDialog();
            bool? res = dlg.ShowDialog();
            return (res == true) ? dlg.ResultConfig : null;
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
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                BtnDraw_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                BtnClose_Click(this, new RoutedEventArgs());
            }
        }

        private void Parameter_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitialized)
            {
                UpdateCalculationAndPreview();
            }
        }

        private void StarterCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isInitialized)
            {
                UpdateCalculationAndPreview();
            }
        }

        private void UpdateCalculationAndPreview()
        {
            var cfg = BuildConfigFromUi();
            cfg.CalculateComponents();

            // Hiển thị kết quả tính toán
            if (TxtRatedCurrent != null)
                TxtRatedCurrent.Text = $"Dòng điện định mức Idm: {cfg.RatedCurrent:F1} A";
            if (TxtCbChoice != null)
                TxtCbChoice.Text = $"Aptomat bảo vệ: {cfg.CbModel}";
            if (TxtKmChoice != null)
                TxtKmChoice.Text = $"Khởi động từ: {cfg.ContactorModel}";
            if (TxtFrChoice != null)
                TxtFrChoice.Text = $"Rơ-le nhiệt: {cfg.ThermalRelayModel}";
            if (TxtCableChoice != null)
                TxtCableChoice.Text = $"Cỡ cáp cấp nguồn: {cfg.CableSize}";

            // Vẽ Canvas Preview sơ đồ mạch
            RenderStarterPreview(cfg);
        }

        private MotorStarterConfig BuildConfigFromUi()
        {
            return new MotorStarterConfig
            {
                Type = (CboStarterType?.SelectedIndex == 1) ? MotorStarterType.StarDelta :
                       (CboStarterType?.SelectedIndex == 2) ? MotorStarterType.VFD :
                       (CboStarterType?.SelectedIndex == 3) ? MotorStarterType.SoftStarter : MotorStarterType.DOL,
                MotorTag = string.IsNullOrWhiteSpace(TxtMotorTag?.Text) ? "M1" : TxtMotorTag.Text.Trim(),
                MotorName = string.IsNullOrWhiteSpace(TxtMotorName?.Text) ? "ĐỘNG CƠ BƠM" : TxtMotorName.Text.Trim(),
                PowerKw = ParseDouble(TxtPowerKw?.Text, 7.5),
                Voltage = ParseDouble(TxtVoltage?.Text, 380.0),
                ControlVoltage = (CboCtrlVolt?.SelectedIndex == 1) ? "24VDC" : "220VAC"
            };
        }

        private void RenderStarterPreview(MotorStarterConfig cfg)
        {
            if (StarterCanvas == null || StarterCanvas.ActualWidth <= 0 || StarterCanvas.ActualHeight <= 0) return;
            StarterCanvas.Children.Clear();

            double cw = StarterCanvas.ActualWidth;
            double ch = StarterCanvas.ActualHeight;

            // 1. Phía trái: Mạch Động Lực (Power Circuit)
            double pX = 80.0;
            double pTopY = 40.0;
            double pBotY = ch - 60.0;

            // 3 Dây pha đỏ, vàng, xanh
            Color[] pCols = { Color.FromRgb(239, 68, 68), Color.FromRgb(234, 179, 8), Color.FromRgb(59, 130, 246) };
            for (int i = 0; i < 3; i++)
            {
                double px = pX + i * 25.0;
                var line = new Line
                {
                    X1 = px, Y1 = pTopY,
                    X2 = px, Y2 = pBotY - 35.0,
                    Stroke = new SolidColorBrush(pCols[i]),
                    StrokeThickness = 2.0
                };
                StarterCanvas.Children.Add(line);
            }

            // Aptomat QF1
            DrawBoxWithText(pX - 10, pTopY + 30, 70, 25, "QF1", Color.FromRgb(250, 204, 21));

            // Contactor KM1
            DrawBoxWithText(pX - 10, pTopY + 80, 70, 25, "KM1", Color.FromRgb(56, 189, 248));

            // Rơ-le nhiệt F1
            DrawBoxWithText(pX - 10, pTopY + 130, 70, 22, "F1", Color.FromRgb(249, 115, 22));

            // Động cơ M1
            double mcX = pX + 25.0;
            double mcY = pBotY - 20.0;
            var mCircle = new Ellipse
            {
                Width = 40, Height = 40,
                Stroke = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(100, 34, 197, 94))
            };
            Canvas.SetLeft(mCircle, mcX - 20);
            Canvas.SetTop(mCircle, mcY - 20);
            StarterCanvas.Children.Add(mCircle);

            var mTxt = new TextBlock { Text = $"{cfg.MotorTag}\n3~", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextAlignment = TextAlignment.Center };
            Canvas.SetLeft(mTxt, mcX - 12);
            Canvas.SetTop(mTxt, mcY - 12);
            StarterCanvas.Children.Add(mTxt);

            // 2. Phía phải: Mạch Điều Khiển (Control Circuit)
            double cX = 260.0;
            double cTopY = pTopY + 10;
            double cBotY = pBotY - 20;

            // Thanh nguồn trên và dưới
            var topBus = new Line { X1 = cX, Y1 = cTopY, X2 = cw - 30, Y2 = cTopY, Stroke = Brushes.Cyan, StrokeThickness = 1.5 };
            var botBus = new Line { X1 = cX, Y1 = cBotY, X2 = cw - 30, Y2 = cBotY, Stroke = Brushes.Cyan, StrokeThickness = 1.5 };
            StarterCanvas.Children.Add(topBus);
            StarterCanvas.Children.Add(botBus);

            // Nhánh 1: Start/Stop/KM1
            double b1X = cX + 40.0;
            var b1Line = new Line { X1 = b1X, Y1 = cTopY, X2 = b1X, Y2 = cBotY, Stroke = Brushes.Cyan, StrokeThickness = 1.2 };
            StarterCanvas.Children.Add(b1Line);

            DrawBoxWithText(b1X - 20, cTopY + 25, 40, 16, "STOP", Color.FromRgb(239, 68, 68));
            DrawBoxWithText(b1X - 20, cTopY + 55, 40, 16, "START", Color.FromRgb(34, 197, 94));

            // Cuộn hút KM1
            var kmCoil = new Ellipse { Width = 26, Height = 26, Stroke = Brushes.Yellow, StrokeThickness = 1.5, Fill = new SolidColorBrush(Color.FromArgb(120, 250, 204, 21)) };
            Canvas.SetLeft(kmCoil, b1X - 13);
            Canvas.SetTop(kmCoil, cBotY - 45);
            StarterCanvas.Children.Add(kmCoil);

            var kmTxt = new TextBlock { Text = "KM1", FontSize = 7.5, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            Canvas.SetLeft(kmTxt, b1X - 9);
            Canvas.SetTop(kmTxt, cBotY - 37);
            StarterCanvas.Children.Add(kmTxt);

            // Nhánh 2: Đèn báo RUN
            double b2X = cX + 110.0;
            var b2Line = new Line { X1 = b2X, Y1 = cTopY + 55, X2 = b2X, Y2 = cBotY, Stroke = Brushes.Cyan, StrokeThickness = 1 };
            StarterCanvas.Children.Add(b2Line);

            var runDot = new Ellipse { Width = 20, Height = 20, Stroke = Brushes.Green, StrokeThickness = 1.5, Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94)) };
            Canvas.SetLeft(runDot, b2X - 10);
            Canvas.SetTop(runDot, cBotY - 42);
            StarterCanvas.Children.Add(runDot);

            // Nhánh 3: Đèn báo TRIP
            double b3X = cX + 170.0;
            var b3Line = new Line { X1 = b3X, Y1 = cTopY + 25, X2 = b3X, Y2 = cBotY, Stroke = Brushes.Cyan, StrokeThickness = 1 };
            StarterCanvas.Children.Add(b3Line);

            var tripDot = new Ellipse { Width = 20, Height = 20, Stroke = Brushes.Orange, StrokeThickness = 1.5, Fill = new SolidColorBrush(Color.FromRgb(234, 179, 8)) };
            Canvas.SetLeft(tripDot, b3X - 10);
            Canvas.SetTop(tripDot, cBotY - 42);
            StarterCanvas.Children.Add(tripDot);
        }

        private void DrawBoxWithText(double x, double y, double w, double h, string text, Color color)
        {
            var rect = new Rectangle
            {
                Width = w, Height = h,
                Fill = new SolidColorBrush(Color.FromArgb(80, color.R, color.G, color.B)),
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 1.2
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            StarterCanvas.Children.Add(rect);

            var txt = new TextBlock
            {
                Text = text,
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Width = w,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(txt, x);
            Canvas.SetTop(txt, y + (h - 12) / 2.0);
            StarterCanvas.Children.Add(txt);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnDraw_Click(object sender, RoutedEventArgs e)
        {
            ResultConfig = BuildConfigFromUi();
            DialogResult = true;
            Close();
        }

        private static double ParseDouble(string str, double fallback)
        {
            if (string.IsNullOrWhiteSpace(str)) return fallback;
            str = str.Trim().Replace(',', '.');
            return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double val) ? val : fallback;
        }
    }
}
