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
    public partial class DTT_StaircaseDialog : Window
    {
        public StaircaseConfig ResultConfig { get; private set; }
        private double _currentZoom = 1.0;

        public DTT_StaircaseDialog()
        {
            InitializeComponent();
            Loaded += (s, e) => UpdateCalculationsAndPreview();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
            else if (e.Key == Key.Enter)
            {
                BtnApply_Click(this, new RoutedEventArgs());
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            ResultConfig = BuildStaircaseConfig();
            DialogResult = true;
            Close();
        }

        private void Param_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
                UpdateCalculationsAndPreview();
        }

        private void CboSteps_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
                UpdateCalculationsAndPreview();
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = Math.Min(3.0, _currentZoom + 0.25);
            ApplyZoom();
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = Math.Max(0.5, _currentZoom - 0.25);
            ApplyZoom();
        }

        private void BtnFitView_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = 1.0;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (StaircaseViewbox != null)
            {
                StaircaseViewbox.LayoutTransform = new ScaleTransform(_currentZoom, _currentZoom);
            }
        }

        private StaircaseConfig BuildStaircaseConfig()
        {
            var cfg = new StaircaseConfig();

            // Dạng thang
            int typeIdx = CboStairType != null ? CboStairType.SelectedIndex : 0;
            if (typeIdx == 1) cfg.Type = StaircaseType.Straight1Flight;
            else if (typeIdx == 2) cfg.Type = StaircaseType.LTurn2Flights;
            else cfg.Type = StaircaseType.UTurn2Flights;

            // Số bậc
            string stepStr = CboSteps != null ? (CboSteps.Text ?? "21") : "21";
            if (int.TryParse(stepStr.Trim(), out int n) && n > 2) cfg.TotalSteps = n;
            else cfg.TotalSteps = 21;

            // Chiều cao tầng
            if (double.TryParse(TxtFloorHeight?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double h) && h > 1000)
                cfg.FloorHeight = h;
            else
                cfg.FloorHeight = 3600;

            // Mặt bậc
            string treadStr = CboTreadDepth != null ? (CboTreadDepth.Text ?? "260") : "260";
            if (double.TryParse(treadStr.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double b) && b > 150)
                cfg.TreadDepth = b;
            else
                cfg.TreadDepth = 260;

            // Rộng vế
            string flightStr = CboFlightWidth != null ? (CboFlightWidth.Text ?? "1000") : "1000";
            if (double.TryParse(flightStr.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double w) && w > 500)
                cfg.FlightWidth = w;
            else
                cfg.FlightWidth = 1000;

            // Chiếu nghỉ
            if (double.TryParse(TxtLandingDepth?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double land) && land > 500)
                cfg.LandingDepth = land;
            else
                cfg.LandingDepth = 1000;

            // Giếng thang
            if (double.TryParse(TxtWellWidth?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double well) && well >= 0)
                cfg.WellWidth = well;
            else
                cfg.WellWidth = 150;

            cfg.ShowStepNumbers = ChkStepNumbers?.IsChecked == true;
            return cfg;
        }

        private void UpdateCalculationsAndPreview()
        {
            if (StaircaseCanvas == null) return;

            var cfg = BuildStaircaseConfig();

            // 1. Tính toán phong thủy Cung Sinh - Lão - Bệnh - Tử
            int remainder = (cfg.TotalSteps - 1) % 4;
            if (remainder < 0) remainder += 4;

            if (remainder == 0)
            {
                TxtFengShui.Text = $"✨ {cfg.TotalSteps} BẬC = CUNG SINH - Cực Tốt (Tài Lộc, Sức Khỏe, Sinh Sôi Thịnh Vượng)";
                TxtFengShui.Foreground = new SolidColorBrush(Color.FromRgb(4, 120, 87));
                BorderFengShui.Background = new SolidColorBrush(Color.FromRgb(236, 253, 245));
                BorderFengShui.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
            else if (remainder == 1)
            {
                TxtFengShui.Text = $"⏳ {cfg.TotalSteps} BẬC = CUNG LÃO - Bình Hòa (Tuổi Thọ, Điềm Đạm)";
                TxtFengShui.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                BorderFengShui.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
                BorderFengShui.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            }
            else if (remainder == 2)
            {
                TxtFengShui.Text = $"⚠️ {cfg.TotalSteps} BẬC = CUNG BỆNH - Nên Cân Nhắc (Gợi ý: Chọn 17, 21, 25 bậc để vào Cung Sinh)";
                TxtFengShui.Foreground = new SolidColorBrush(Color.FromRgb(217, 119, 6));
                BorderFengShui.Background = new SolidColorBrush(Color.FromRgb(255, 251, 235));
                BorderFengShui.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            }
            else
            {
                TxtFengShui.Text = $"❌ {cfg.TotalSteps} BẬC = CUNG TỬ - ĐẠI KỴ THEO PHONG THỦY (Khuyên dùng: 21 bậc hoặc 25 bậc)";
                TxtFengShui.Foreground = new SolidColorBrush(Color.FromRgb(225, 29, 72));
                BorderFengShui.Background = new SolidColorBrush(Color.FromRgb(255, 241, 242));
                BorderFengShui.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 63, 94));
            }

            // 2. Chiều cao cổ bậc h = H / N
            double riserH = cfg.FloorHeight / cfg.TotalSteps;
            TxtRiserHeight.Text = $"h = {riserH:F1} mm";

            // 3. Tiện nghi nhân trắc học Blondel: 2h + b (600 - 650 mm)
            double blondel = 2 * riserH + cfg.TreadDepth;
            if (blondel >= 600 && blondel <= 650)
            {
                TxtBlondel.Text = $"✅ 2h + b = {blondel:F0} mm (Đạt chuẩn tiện nghi nhân trắc học 600 - 650 mm)";
                TxtBlondel.Foreground = new SolidColorBrush(Color.FromRgb(29, 78, 216));
                BorderBlondel.Background = new SolidColorBrush(Color.FromRgb(239, 246, 255));
                BorderBlondel.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
            }
            else if (blondel < 600)
            {
                TxtBlondel.Text = $"⚠️ 2h + b = {blondel:F0} mm (Bậc hơi dốc, nên tăng mặt bậc b hoặc số bậc N)";
                TxtBlondel.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                BorderBlondel.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
                BorderBlondel.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            }
            else
            {
                TxtBlondel.Text = $"⚠️ 2h + b = {blondel:F0} mm (Bậc hơi thoải, bước chân có thể bị hụt)";
                TxtBlondel.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                BorderBlondel.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
                BorderBlondel.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            }

            // 4. Vẽ bản đồ hình học CAD 2D Top-Down Preview thực tế
            Render2DTopDownStaircase(cfg);
        }

        private void Render2DTopDownStaircase(StaircaseConfig cfg)
        {
            StaircaseCanvas.Children.Clear();

            double canvasW = 600;
            double canvasH = 400;
            StaircaseCanvas.Width = canvasW;
            StaircaseCanvas.Height = canvasH;

            // Màu chuẩn CAD
            var brushRail = new SolidColorBrush(Color.FromRgb(234, 179, 8));     // Vàng: Tay vịn & khung vế
            var brushStep = new SolidColorBrush(Color.FromRgb(248, 250, 252));    // Trắng sáng: Nét bậc thang thấy
            var brushHidd = new SolidColorBrush(Color.FromRgb(100, 116, 139));    // Xám: Nét bậc khuất
            var brushPath = new SolidColorBrush(Color.FromRgb(56, 189, 248));     // Cyan: Đường tim bước đi
            var brushDim = new SolidColorBrush(Color.FromRgb(148, 163, 184));     // Nét gióng kích thước
            var brushText = new SolidColorBrush(Color.FromRgb(52, 211, 153));     // Xanh ngọc: Số thứ tự bậc

            if (cfg.Type == StaircaseType.UTurn2Flights)
            {
                int flight1Steps = (cfg.TotalSteps - 1) / 2;
                int flight2Steps = cfg.TotalSteps - 1 - flight1Steps;
                double flightLen = flight1Steps * cfg.TreadDepth;
                double totalLen = flightLen + cfg.LandingDepth;
                double totalWidth = cfg.FlightWidth * 2 + cfg.WellWidth;

                TxtOverallDims.Text = $"Kích thước tổng thể: {totalLen:F0} x {totalWidth:F0} mm • {cfg.TotalSteps} Bậc";

                // Scale hình học khớp vừa Canvas
                double scale = Math.Min((canvasW - 90) / totalLen, (canvasH - 80) / totalWidth);
                double ox = 45;
                double oy = (canvasH - totalWidth * scale) / 2;

                // 1. Khung bao toàn bộ lồng cầu thang (Rail)
                var frameRect = new Rectangle
                {
                    Width = totalLen * scale,
                    Height = totalWidth * scale,
                    Stroke = brushRail,
                    StrokeThickness = 1.8,
                    Fill = new SolidColorBrush(Color.FromArgb(15, 2, 6, 23))
                };
                Canvas.SetLeft(frameRect, ox);
                Canvas.SetTop(frameRect, oy);
                StaircaseCanvas.Children.Add(frameRect);

                // 2. Chiếu nghỉ (Landing)
                double landingX = ox + flightLen * scale;
                var landingLine = new Line
                {
                    X1 = landingX, Y1 = oy,
                    X2 = landingX, Y2 = oy + totalWidth * scale,
                    Stroke = brushStep, StrokeThickness = 1.5
                };
                StaircaseCanvas.Children.Add(landingLine);

                // Khe giếng thang
                double wS = cfg.FlightWidth * scale;
                double wellS = cfg.WellWidth * scale;
                var wellLine1 = new Line { X1 = ox, Y1 = oy + wS, X2 = landingX, Y2 = oy + wS, Stroke = brushRail, StrokeThickness = 1.5 };
                var wellLine2 = new Line { X1 = ox, Y1 = oy + wS + wellS, X2 = landingX, Y2 = oy + wS + wellS, Stroke = brushRail, StrokeThickness = 1.5 };
                var wellEnd = new Line { X1 = landingX, Y1 = oy + wS, X2 = landingX, Y2 = oy + wS + wellS, Stroke = brushRail, StrokeThickness = 1.5 };
                StaircaseCanvas.Children.Add(wellLine1);
                StaircaseCanvas.Children.Add(wellLine2);
                StaircaseCanvas.Children.Add(wellEnd);

                // 3. Các bậc Vế 1 (Đi từ trái sang phải, vế dưới)
                double treadS = cfg.TreadDepth * scale;
                for (int i = 1; i <= flight1Steps; i++)
                {
                    double sx = ox + i * treadS;
                    var stepLine = new Line
                    {
                        X1 = sx, Y1 = oy + wS + wellS,
                        X2 = sx, Y2 = oy + totalWidth * scale,
                        Stroke = brushStep, StrokeThickness = 1.2
                    };
                    StaircaseCanvas.Children.Add(stepLine);

                    if (cfg.ShowStepNumbers)
                    {
                        var txtNum = new TextBlock
                        {
                            Text = i.ToString(),
                            FontSize = Math.Max(8, 10 * scale),
                            FontWeight = FontWeights.Bold,
                            Foreground = brushText
                        };
                        Canvas.SetLeft(txtNum, sx - treadS / 2 - 4);
                        Canvas.SetTop(txtNum, oy + totalWidth * scale - 18);
                        StaircaseCanvas.Children.Add(txtNum);
                    }
                }

                // Số bậc chiếu nghỉ
                int landNum = flight1Steps + 1;
                if (cfg.ShowStepNumbers)
                {
                    var txtLand = new TextBlock
                    {
                        Text = $"Chiếu Nghỉ: Bậc {landNum}",
                        FontSize = 9.5,
                        FontWeight = FontWeights.Bold,
                        Foreground = brushText
                    };
                    Canvas.SetLeft(txtLand, landingX + (cfg.LandingDepth * scale) / 2 - 40);
                    Canvas.SetTop(txtLand, oy + (totalWidth * scale) / 2 - 7);
                    StaircaseCanvas.Children.Add(txtLand);
                }

                // 4. Các bậc Vế 2 (Đi từ phải sang trái, vế trên)
                for (int j = 1; j <= flight2Steps; j++)
                {
                    double sx = landingX - j * treadS;
                    bool isHidden = j > 4; // Sau nét cắt là nét khuất

                    var stepLine = new Line
                    {
                        X1 = sx, Y1 = oy,
                        X2 = sx, Y2 = oy + wS,
                        Stroke = isHidden ? brushHidd : brushStep,
                        StrokeThickness = 1.2
                    };
                    if (isHidden)
                    {
                        stepLine.StrokeDashArray = new DoubleCollection { 3, 2 };
                    }
                    StaircaseCanvas.Children.Add(stepLine);

                    if (cfg.ShowStepNumbers)
                    {
                        int stepNum = landNum + j;
                        var txtNum = new TextBlock
                        {
                            Text = stepNum.ToString(),
                            FontSize = Math.Max(8, 10 * scale),
                            FontWeight = FontWeights.Bold,
                            Foreground = isHidden ? brushHidd : brushText
                        };
                        Canvas.SetLeft(txtNum, sx + treadS / 2 - 4);
                        Canvas.SetTop(txtNum, oy + 4);
                        StaircaseCanvas.Children.Add(txtNum);
                    }
                }

                // 5. Đường nét cắt zigzag tượng trưng
                double cutX = ox + 4.5 * treadS;
                var cutPath = new Polyline
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(244, 63, 94)),
                    StrokeThickness = 1.5,
                    Points = new PointCollection
                    {
                        new Point(cutX - 6, oy - 4),
                        new Point(cutX + 2, oy + wS * 0.45),
                        new Point(cutX - 4, oy + wS * 0.75),
                        new Point(cutX + 6, oy + wS + 4)
                    }
                };
                StaircaseCanvas.Children.Add(cutPath);

                // 6. Đường tim đi bộ (Walking Path) kèm mũi tên
                double pathY1 = oy + wS + wellS + wS / 2;
                double pathY2 = oy + wS / 2;
                double pStartX = ox + 15;
                double pTurnX = landingX + (cfg.LandingDepth * scale) / 2;
                double pEndX = ox + 25;

                // Vòng tròn xuất phát
                var startCircle = new Ellipse
                {
                    Width = 8, Height = 8,
                    Stroke = brushPath, StrokeThickness = 1.5,
                    Fill = brushPath
                };
                Canvas.SetLeft(startCircle, pStartX);
                Canvas.SetTop(startCircle, pathY1 - 4);
                StaircaseCanvas.Children.Add(startCircle);

                // Tuyến đi
                var walkingPath = new Polyline
                {
                    Stroke = brushPath,
                    StrokeThickness = 1.6,
                    Points = new PointCollection
                    {
                        new Point(pStartX + 8, pathY1),
                        new Point(pTurnX, pathY1),
                        new Point(pTurnX, pathY2),
                        new Point(pEndX, pathY2)
                    }
                };
                StaircaseCanvas.Children.Add(walkingPath);

                // Mũi tên kết thúc
                var endArrow = new Polyline
                {
                    Stroke = brushPath,
                    StrokeThickness = 1.8,
                    Points = new PointCollection
                    {
                        new Point(pEndX + 8, pathY2 - 4),
                        new Point(pEndX, pathY2),
                        new Point(pEndX + 8, pathY2 + 4)
                    }
                };
                StaircaseCanvas.Children.Add(endArrow);

                // Chữ LÊN (UP)
                var txtUp = new TextBlock
                {
                    Text = "LÊN (UP)",
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = brushPath
                };
                Canvas.SetLeft(txtUp, pStartX + 14);
                Canvas.SetTop(txtUp, pathY1 - 16);
                StaircaseCanvas.Children.Add(txtUp);

                // Đường kích thước bao ngoài
                DrawDimension(StaircaseCanvas, ox, oy + totalWidth * scale + 15, ox + totalLen * scale, oy + totalWidth * scale + 15, $"{totalLen:F0} mm", brushDim);
                DrawDimension(StaircaseCanvas, ox - 15, oy, ox - 15, oy + totalWidth * scale, $"{totalWidth:F0} mm", brushDim, true);
            }
            else if (cfg.Type == StaircaseType.Straight1Flight)
            {
                double totalLen = cfg.TotalSteps * cfg.TreadDepth;
                double w = cfg.FlightWidth;

                TxtOverallDims.Text = $"Kích thước tổng thể: {totalLen:F0} x {w:F0} mm • {cfg.TotalSteps} Bậc Thẳng";

                double scale = Math.Min((canvasW - 90) / totalLen, (canvasH - 80) / w);
                double ox = 45;
                double oy = (canvasH - w * scale) / 2;

                var frameRect = new Rectangle
                {
                    Width = totalLen * scale,
                    Height = w * scale,
                    Stroke = brushRail,
                    StrokeThickness = 1.8,
                    Fill = new SolidColorBrush(Color.FromArgb(15, 2, 6, 23))
                };
                Canvas.SetLeft(frameRect, ox);
                Canvas.SetTop(frameRect, oy);
                StaircaseCanvas.Children.Add(frameRect);

                double treadS = cfg.TreadDepth * scale;
                for (int i = 1; i < cfg.TotalSteps; i++)
                {
                    double sx = ox + i * treadS;
                    var stepLine = new Line
                    {
                        X1 = sx, Y1 = oy,
                        X2 = sx, Y2 = oy + w * scale,
                        Stroke = brushStep, StrokeThickness = 1.2
                    };
                    StaircaseCanvas.Children.Add(stepLine);

                    if (cfg.ShowStepNumbers)
                    {
                        var txtNum = new TextBlock
                        {
                            Text = i.ToString(),
                            FontSize = Math.Max(8, 10 * scale),
                            FontWeight = FontWeights.Bold,
                            Foreground = brushText
                        };
                        Canvas.SetLeft(txtNum, sx - treadS / 2 - 3);
                        Canvas.SetTop(txtNum, oy + 6);
                        StaircaseCanvas.Children.Add(txtNum);
                    }
                }

                // Walking path
                double midY = oy + (w * scale) / 2;
                var startCircle = new Ellipse { Width = 8, Height = 8, Stroke = brushPath, Fill = brushPath };
                Canvas.SetLeft(startCircle, ox + 15);
                Canvas.SetTop(startCircle, midY - 4);
                StaircaseCanvas.Children.Add(startCircle);

                var walkLine = new Line { X1 = ox + 23, Y1 = midY, X2 = ox + totalLen * scale - 25, Y2 = midY, Stroke = brushPath, StrokeThickness = 1.6 };
                StaircaseCanvas.Children.Add(walkLine);

                var endArrow = new Polyline
                {
                    Stroke = brushPath,
                    StrokeThickness = 1.8,
                    Points = new PointCollection
                    {
                        new Point(ox + totalLen * scale - 33, midY - 4),
                        new Point(ox + totalLen * scale - 25, midY),
                        new Point(ox + totalLen * scale - 33, midY + 4)
                    }
                };
                StaircaseCanvas.Children.Add(endArrow);

                var txtUp = new TextBlock { Text = "LÊN (UP)", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = brushPath };
                Canvas.SetLeft(txtUp, ox + 30);
                Canvas.SetTop(txtUp, midY - 16);
                StaircaseCanvas.Children.Add(txtUp);

                DrawDimension(StaircaseCanvas, ox, oy + w * scale + 15, ox + totalLen * scale, oy + w * scale + 15, $"{totalLen:F0} mm", brushDim);
            }
            else // LTurn2Flights
            {
                int flight1Steps = (cfg.TotalSteps - 1) / 2;
                int flight2Steps = cfg.TotalSteps - 1 - flight1Steps;
                double len1 = flight1Steps * cfg.TreadDepth + cfg.FlightWidth;
                double len2 = flight2Steps * cfg.TreadDepth + cfg.FlightWidth;

                TxtOverallDims.Text = $"Kích thước L-Turn: {len1:F0} x {len2:F0} mm • {cfg.TotalSteps} Bậc";

                double scale = Math.Min((canvasW - 90) / len1, (canvasH - 80) / len2);
                double ox = 45;
                double oy = 35;

                double wS = cfg.FlightWidth * scale;
                double treadS = cfg.TreadDepth * scale;

                // Vế 1 ngang
                var f1 = new Rectangle { Width = len1 * scale, Height = wS, Stroke = brushRail, StrokeThickness = 1.5 };
                Canvas.SetLeft(f1, ox);
                Canvas.SetTop(f1, oy);
                StaircaseCanvas.Children.Add(f1);

                // Vế 2 dọc xuống
                var f2 = new Rectangle { Width = wS, Height = len2 * scale, Stroke = brushRail, StrokeThickness = 1.5 };
                Canvas.SetLeft(f2, ox + (len1 - cfg.FlightWidth) * scale);
                Canvas.SetTop(f2, oy);
                StaircaseCanvas.Children.Add(f2);

                // Chiếu nghỉ góc vuông
                var landSquare = new Rectangle { Width = wS, Height = wS, Stroke = brushStep, StrokeThickness = 1.2, Fill = new SolidColorBrush(Color.FromArgb(20, 16, 185, 129)) };
                Canvas.SetLeft(landSquare, ox + (len1 - cfg.FlightWidth) * scale);
                Canvas.SetTop(landSquare, oy);
                StaircaseCanvas.Children.Add(landSquare);

                // Bậc vế 1
                for (int i = 1; i <= flight1Steps; i++)
                {
                    double sx = ox + i * treadS;
                    var stepLine = new Line { X1 = sx, Y1 = oy, X2 = sx, Y2 = oy + wS, Stroke = brushStep, StrokeThickness = 1.2 };
                    StaircaseCanvas.Children.Add(stepLine);
                }

                // Bậc vế 2
                for (int j = 1; j <= flight2Steps; j++)
                {
                    double sy = oy + wS + j * treadS;
                    var stepLine = new Line { X1 = ox + (len1 - cfg.FlightWidth) * scale, Y1 = sy, X2 = ox + len1 * scale, Y2 = sy, Stroke = brushStep, StrokeThickness = 1.2 };
                    StaircaseCanvas.Children.Add(stepLine);
                }

                DrawDimension(StaircaseCanvas, ox, oy + len2 * scale + 15, ox + len1 * scale, oy + len2 * scale + 15, $"{len1:F0} mm", brushDim);
            }
        }

        private void DrawDimension(Canvas canvas, double x1, double y1, double x2, double y2, string text, Brush brush, bool isVertical = false)
        {
            var line = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = brush, StrokeThickness = 1 };
            canvas.Children.Add(line);

            if (!isVertical)
            {
                var tick1 = new Line { X1 = x1, Y1 = y1 - 4, X2 = x1, Y2 = y1 + 4, Stroke = brush, StrokeThickness = 1 };
                var tick2 = new Line { X1 = x2, Y1 = y2 - 4, X2 = x2, Y2 = y2 + 4, Stroke = brush, StrokeThickness = 1 };
                canvas.Children.Add(tick1);
                canvas.Children.Add(tick2);

                var txt = new TextBlock { Text = text, FontSize = 8.5, Foreground = brush, FontWeight = FontWeights.SemiBold };
                Canvas.SetLeft(txt, (x1 + x2) / 2 - 20);
                Canvas.SetTop(txt, y1 + 3);
                canvas.Children.Add(txt);
            }
            else
            {
                var tick1 = new Line { X1 = x1 - 4, Y1 = y1, X2 = x1 + 4, Y2 = y1, Stroke = brush, StrokeThickness = 1 };
                var tick2 = new Line { X1 = x2 - 4, Y1 = y2, X2 = x2 + 4, Y2 = y2, Stroke = brush, StrokeThickness = 1 };
                canvas.Children.Add(tick1);
                canvas.Children.Add(tick2);

                var txt = new TextBlock { Text = text, FontSize = 8.5, Foreground = brush, FontWeight = FontWeights.SemiBold };
                Canvas.SetLeft(txt, x1 - 45);
                Canvas.SetTop(txt, (y1 + y2) / 2 - 6);
                canvas.Children.Add(txt);
            }
        }
    }
}
