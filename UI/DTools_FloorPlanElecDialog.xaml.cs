using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DTT_AutoCAD_Plugin.Modules;

namespace DTT_AutoCAD_Plugin.UI
{
    public partial class DTools_FloorPlanElecDialog : Window
    {
        public FloorPlanConfig ResultConfig { get; private set; }
        public bool ExportBomAfterBuild { get; private set; } = false;

        private bool _isInitialized = false;
        private double _zoomFactor = 1.0;
        private Vector _panOffset = new Vector(0, 0);
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private bool _is3dPreview = false;

        public DTools_FloorPlanElecDialog()
        {
            InitializeComponent();
            _isInitialized = true;
            UpdateLivePreview();
        }

        public static FloorPlanConfig PromptConfig(out bool exportBom)
        {
            var dlg = new DTools_FloorPlanElecDialog();
            bool? res = dlg.ShowDialog();
            exportBom = dlg.ExportBomAfterBuild;
            return (res == true) ? dlg.ResultConfig : null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TxtRoomL.Focus();
            TxtRoomL.SelectAll();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                BtnBuild_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                BtnCancel_Click(this, new RoutedEventArgs());
            }
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                string tag = rb.Tag.ToString();
                PanelRoom.Visibility = (tag == "PanelRoom") ? Visibility.Visible : Visibility.Collapsed;
                PanelLight.Visibility = (tag == "PanelLight") ? Visibility.Visible : Visibility.Collapsed;
                PanelPower.Visibility = (tag == "PanelPower") ? Visibility.Visible : Visibility.Collapsed;
                PanelElv.Visibility = (tag == "PanelElv") ? Visibility.Visible : Visibility.Collapsed;
                PanelWiring.Visibility = (tag == "PanelWiring") ? Visibility.Visible : Visibility.Collapsed;
                UpdateLivePreview();
            }
        }

        private void Parameter_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            try
            {
                if (BorderBranchElec != null)
                {
                    bool isBranch = (CboShape?.SelectedIndex == 1 || CboShape?.SelectedIndex == 2);
                    BorderBranchElec.Visibility = isBranch ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch { }
            UpdateLivePreview();
        }

        private void PreviewCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isInitialized)
            {
                UpdateLivePreview();
            }
        }

        // ============================================================
        // ĐIỀU KHIỂN ZOOM / PAN / CHẾ ĐỘ 3D PREVIEW
        // ============================================================

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = Math.Min(4.0, _zoomFactor * 1.25);
            UpdateLivePreview();
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = Math.Max(0.25, _zoomFactor / 1.25);
            UpdateLivePreview();
        }

        private void BtnZoomReset_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = 1.0;
            _panOffset = new Vector(0, 0);
            UpdateLivePreview();
        }

        private void BtnToggle3d_Click(object sender, RoutedEventArgs e)
        {
            _is3dPreview = !_is3dPreview;
            if (BtnToggle3d != null)
            {
                BtnToggle3d.Content = _is3dPreview ? "📐 2D TOP" : "🧊 3D ISO";
            }
            UpdateLivePreview();
        }

        private void PreviewCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double factor = (e.Delta > 0) ? 1.15 : 0.85;
            _zoomFactor = Math.Max(0.2, Math.Min(5.0, _zoomFactor * factor));
            UpdateLivePreview();
        }

        private void PreviewCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Middle)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(PreviewCanvas);
                PreviewCanvas.CaptureMouse();
            }
        }

        private void PreviewCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point cur = e.GetPosition(PreviewCanvas);
                Vector delta = cur - _dragStartPoint;
                _panOffset += delta;
                _dragStartPoint = cur;
                UpdateLivePreview();
            }
        }

        private void PreviewCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                PreviewCanvas.ReleaseMouseCapture();
            }
        }

        // ============================================================
        // VẼ MÔ HÌNH PREVIEW THỜI GIAN THỰC (LIVE VECTOR CANVAS)
        // ============================================================

        private void UpdateLivePreview()
        {
            if (PreviewCanvas == null || PreviewCanvas.ActualWidth <= 0 || PreviewCanvas.ActualHeight <= 0) return;
            PreviewCanvas.Children.Clear();

            if (TxtZoomLevel != null)
            {
                TxtZoomLevel.Text = $"{(_zoomFactor * 100):F0}%";
            }

            double roomL = ParseDouble(TxtRoomL?.Text, 6000.0);
            double roomW = ParseDouble(TxtRoomW?.Text, 4000.0);
            double canvasW = PreviewCanvas.ActualWidth;
            double canvasH = PreviewCanvas.ActualHeight;

            // Tính scale cơ bản vừa khung nhìn
            double baseScale = Math.Min((canvasW - 80.0) / Math.Max(100.0, roomL), (canvasH - 80.0) / Math.Max(100.0, roomW));
            baseScale = Math.Max(0.01, baseScale) * _zoomFactor;

            double drawW = roomL * baseScale;
            double drawH = roomW * baseScale;
            double startX = (canvasW - drawW) / 2.0 + _panOffset.X;
            double startY = (canvasH - drawH) / 2.0 + _panOffset.Y;

            // Tùy chọn trạng thái bật/tắt
            bool hasDoor = ChkDoor?.IsChecked == true;
            bool hasWin = ChkWindow?.IsChecked == true;
            bool placeDb = ChkPlaceDb?.IsChecked == true;
            bool autoSocket = ChkSocketPerimeter?.IsChecked == true;
            bool hasAc = ChkAcUnit?.IsChecked == true;
            bool hasFan = ChkCeilingFan?.IsChecked == true;
            bool hasWh = ChkWaterHeater?.IsChecked == true;
            bool hasLan = ChkDataLan?.IsChecked == true;
            bool hasTv = ChkTvOutlet?.IsChecked == true;
            bool hasSmoke = ChkSmoke?.IsChecked == true;
            bool hasCam = ChkCamera?.IsChecked == true;
            bool hasExit = ChkExitLight?.IsChecked == true;
            bool drawHomeRun = ChkHomeRun?.IsChecked == true;
            int shapeIdx = CboShape?.SelectedIndex ?? 0;
            int lightTypeIdx = CboLightType?.SelectedIndex ?? 0;
            bool isOrtho = (CboWiringStyle?.SelectedIndex == 1);

            // ==========================================
            // CHẾ ĐỘ 1: XEM 3D ISOMETRIC WIREFRAME
            // ==========================================
            if (_is3dPreview)
            {
                Render3dIsometricPreview(roomL, roomW, canvasW, canvasH, baseScale, hasDoor, hasWin, placeDb, hasAc, hasFan);
                return;
            }

            // ==========================================
            // CHẾ ĐỘ 2: XEM 2D MẶT BẰNG CHI TIẾT
            // ==========================================

            // 1. TƯỜNG PHÒNG THEO HÌNH DÁNG
            if (shapeIdx == 1) // Phòng chữ L
            {
                DrawLShapeRoom(startX, startY, drawW, drawH);
            }
            else if (shapeIdx == 2) // Phòng chữ T
            {
                DrawTShapeRoom(startX, startY, drawW, drawH);
            }
            else // Chữ nhật
            {
                var rRoomOut = new Rectangle
                {
                    Width = drawW,
                    Height = drawH,
                    Stroke = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    StrokeThickness = (CboWallT?.SelectedIndex == 1) ? 2.5 : 4.5,
                    Fill = new SolidColorBrush(Color.FromRgb(15, 23, 42))
                };
                Canvas.SetLeft(rRoomOut, startX);
                Canvas.SetTop(rRoomOut, startY);
                PreviewCanvas.Children.Add(rRoomOut);
            }

            // 2. CỬA ĐI (CHỈ VẼ KHI CHKDOOR BẬT)
            double doorX = startX + 25.0;
            double doorY = startY + drawH;
            double doorW = 28.0;
            if (hasDoor)
            {
                // Cánh cửa mở 90 độ
                var doorLine = new Line
                {
                    X1 = doorX,
                    Y1 = doorY,
                    X2 = doorX,
                    Y2 = doorY - doorW,
                    Stroke = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                    StrokeThickness = 2.0
                };
                PreviewCanvas.Children.Add(doorLine);

                // Cung quét mở cửa
                var doorArc = new Path
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                    StrokeThickness = 1.0,
                    StrokeDashArray = new DoubleCollection { 2, 2 },
                    Data = Geometry.Parse($"M {doorX},{doorY - doorW} A {doorW},{doorW} 0 0 1 {doorX + doorW},{doorY}")
                };
                PreviewCanvas.Children.Add(doorArc);

                // Xóa nét tường chân cửa
                var doorHole = new Rectangle
                {
                    Width = doorW,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(15, 23, 42))
                };
                Canvas.SetLeft(doorHole, doorX);
                Canvas.SetTop(doorHole, doorY - 3);
                PreviewCanvas.Children.Add(doorHole);
            }

            // 3. CỬA SỔ (CHỈ VẼ KHI CHKWINDOW BẬT)
            if (hasWin)
            {
                double winW = Math.Max(20.0, drawW * 0.35);
                double winX = startX + (drawW - winW) / 2.0;
                double winY = startY - 3;
                var rWin = new Rectangle
                {
                    Width = winW,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(rWin, winX);
                Canvas.SetTop(rWin, winY);
                PreviewCanvas.Children.Add(rWin);
            }

            // 4. TỦ ĐIỆN TỔNG DB (CHỈ VẼ KHI CHKPLACEDB BẬT)
            Point dbPos = new Point(startX + 8, startY + drawH - 22);
            if (placeDb)
            {
                var rDb = new Rectangle
                {
                    Width = 18,
                    Height = 10,
                    Fill = new SolidColorBrush(Color.FromRgb(14, 165, 233)),
                    Stroke = Brushes.White,
                    StrokeThickness = 1.2
                };
                Canvas.SetLeft(rDb, dbPos.X);
                Canvas.SetTop(rDb, dbPos.Y);
                PreviewCanvas.Children.Add(rDb);

                var txtDb = new TextBlock
                {
                    Text = "DB",
                    FontSize = 7,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };
                Canvas.SetLeft(txtDb, dbPos.X + 3);
                Canvas.SetTop(txtDb, dbPos.Y - 10);
                PreviewCanvas.Children.Add(txtDb);
            }
            else
            {
                // Nếu không đặt DB trong phòng, mũi tên nguồn chỉ ra ngoài hành lang
                dbPos = new Point(startX - 20, startY + drawH * 0.4);
            }

            // 5. HỆ THỐNG CHIẾU SÁNG & RẢI LƯỚI ĐÈN
            double areaM2 = (roomL * roomW) / 1000000.0;
            double targetLux = ParseDouble(TxtTargetLux?.Text, 400.0);
            int lightCount = 6;
            if (ChkAutoLux?.IsChecked == true)
            {
                double lumenPerFix = (lightTypeIdx == 1) ? 4200.0 : (lightTypeIdx == 2) ? 3600.0 : 1100.0;
                double totalReq = (targetLux * areaM2) / (0.65 * 0.80);
                lightCount = Math.Max(2, (int)Math.Ceiling(totalReq / lumenPerFix));
            }

            int cols = Math.Max(2, (int)Math.Round(Math.Sqrt(lightCount * (roomL / Math.Max(1.0, roomW)))));
            int rows = Math.Max(1, (int)Math.Ceiling((double)lightCount / cols));
            double stepX = drawW / (cols + 1);
            double stepY = drawH / (rows + 1);

            List<Point> lightPts = new List<Point>();
            for (int r = 1; r <= rows; r++)
            {
                for (int c = 1; c <= cols; c++)
                {
                    if (lightPts.Count >= lightCount) break;
                    double lx = startX + c * stepX;
                    double ly = startY + r * stepY;
                    Point lp = new Point(lx, ly);
                    lightPts.Add(lp);
                    DrawPreviewLightSymbol(lp, lightTypeIdx);
                }
            }

            // Đèn EXIT
            if (hasExit)
            {
                double exX = hasDoor ? (doorX + 5) : (startX + drawW / 2.0);
                double exY = startY + drawH + 2;
                var rExit = new Rectangle { Width = 16, Height = 6, Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94)) };
                Canvas.SetLeft(rExit, exX);
                Canvas.SetTop(rExit, exY);
                PreviewCanvas.Children.Add(rExit);
            }

            // 6. CÔNG TẮC & ĐI DÂY CHIẾU SÁNG
            Point switchPt = hasDoor ? new Point(doorX + doorW + 10, startY + drawH - 12) : new Point(startX + 25, startY + drawH - 12);
            DrawPreviewDot(switchPt.X, switchPt.Y, Color.FromRgb(249, 115, 22), 8); // Chấm công tắc cam

            if (lightPts.Count > 0)
            {
                DrawPreviewWire(switchPt, lightPts[0], isOrtho);
                for (int i = 0; i < lightPts.Count - 1; i++)
                {
                    DrawPreviewWire(lightPts[i], lightPts[i + 1], isOrtho);
                }

                if (drawHomeRun)
                {
                    DrawPreviewArrow(lightPts[0], dbPos, Color.FromRgb(56, 189, 248), "N1");
                }
            }

            // 7. HỆ THỐNG Ổ CẮM CHÂN TƯỜNG (CHỈ VẼ KHI BẬT)
            int socketCount = 0;
            List<Point> sockPts = new List<Point>();
            if (autoSocket)
            {
                for (double ox = startX + 45; ox < startX + drawW - 35; ox += 50)
                {
                    sockPts.Add(new Point(ox, startY + drawH - 5));
                    DrawPreviewSocket(ox, startY + drawH - 5, 0);
                    sockPts.Add(new Point(ox, startY + 5));
                    DrawPreviewSocket(ox, startY + 5, 180);
                    socketCount += 2;
                }
                for (double oy = startY + 45; oy < startY + drawH - 35; oy += 45)
                {
                    sockPts.Add(new Point(startX + 5, oy));
                    DrawPreviewSocket(startX + 5, oy, 90);
                    sockPts.Add(new Point(startX + drawW - 5, oy));
                    DrawPreviewSocket(startX + drawW - 5, oy, -90);
                    socketCount += 2;
                }

                if (sockPts.Count > 0 && drawHomeRun)
                {
                    DrawPreviewArrow(sockPts[0], dbPos, Color.FromRgb(239, 68, 68), "N2");
                }
            }

            // 8. CÁC THIẾT BỊ ĐỘNG LỰC & ELV (CHỈ VẼ KHI TÍCH CHỌN)
            if (hasAc)
            {
                // Điều hòa nhiệt độ AC
                double acW = 28;
                double acH = 10;
                double acX = startX + drawW - acW - 10;
                double acY = startY + 8;
                var rAc = new Rectangle { Width = acW, Height = acH, Fill = new SolidColorBrush(Color.FromRgb(14, 165, 233)), Stroke = Brushes.White, StrokeThickness = 1 };
                Canvas.SetLeft(rAc, acX);
                Canvas.SetTop(rAc, acY);
                PreviewCanvas.Children.Add(rAc);

                var txtAc = new TextBlock { Text = "AC", FontSize = 7, Foreground = Brushes.White, FontWeight = FontWeights.Bold };
                Canvas.SetLeft(txtAc, acX + 6);
                Canvas.SetTop(txtAc, acY - 1);
                PreviewCanvas.Children.Add(txtAc);

                if (drawHomeRun)
                {
                    DrawPreviewArrow(new Point(acX, acY + acH / 2.0), dbPos, Color.FromRgb(249, 115, 22), "N3");
                }
            }

            if (hasFan)
            {
                // Quạt trần tâm phòng
                double fx = startX + drawW / 2.0;
                double fy = startY + drawH / 2.0;
                DrawPreviewDot(fx, fy, Color.FromRgb(168, 85, 247), 10);
                var fLine1 = new Line { X1 = fx - 14, Y1 = fy, X2 = fx + 14, Y2 = fy, Stroke = new SolidColorBrush(Color.FromRgb(168, 85, 247)), StrokeThickness = 2 };
                var fLine2 = new Line { X1 = fx, Y1 = fy - 14, X2 = fx, Y2 = fy + 14, Stroke = new SolidColorBrush(Color.FromRgb(168, 85, 247)), StrokeThickness = 2 };
                PreviewCanvas.Children.Add(fLine1);
                PreviewCanvas.Children.Add(fLine2);
            }

            if (hasWh)
            {
                // Bình nước nóng
                double whX = startX + 15;
                double whY = startY + 15;
                DrawPreviewDot(whX, whY, Color.FromRgb(6, 182, 212), 12);
                var txtWh = new TextBlock { Text = "WH", FontSize = 7, Foreground = Brushes.White, FontWeight = FontWeights.Bold };
                Canvas.SetLeft(txtWh, whX - 6);
                Canvas.SetTop(txtWh, whY - 5);
                PreviewCanvas.Children.Add(txtWh);
            }

            if (hasLan)
            {
                // Ổ cắm mạng LAN
                double lanX = startX + 55;
                double lanY = startY + drawH - 12;
                DrawPreviewSquare(lanX, lanY, Color.FromRgb(59, 130, 246), "LAN");
            }

            if (hasTv)
            {
                // Ổ cắm TV
                double tvX = startX + 75;
                double tvY = startY + drawH - 12;
                DrawPreviewSquare(tvX, tvY, Color.FromRgb(217, 119, 6), "TV");
            }

            if (hasSmoke)
            {
                // Đầu báo khói trần
                double smX = startX + drawW * 0.72;
                double smY = startY + drawH * 0.72;
                DrawPreviewDot(smX, smY, Color.FromRgb(239, 68, 68), 9);
            }

            if (hasCam)
            {
                // Camera góc phòng
                double cmX = startX + drawW - 14;
                double cmY = startY + drawH - 14;
                DrawPreviewDot(cmX, cmY, Color.FromRgb(236, 72, 153), 8);
            }

            // 9. CẬP NHẬT FOOTER THÔNG TIN
            if (TxtPreviewInfo != null)
            {
                string lightName = (lightTypeIdx == 1) ? "Panel 600" : (lightTypeIdx == 2) ? "Tuýp đôi" : "Downlight";
                string dbText = placeDb ? "Tủ DB: BẬT" : "Tủ DB: TẮT";
                string acText = hasAc ? "AC: BẬT" : "AC: TẮT";
                string sockText = autoSocket ? $"{socketCount} Ổ cắm" : "Không ổ cắm";
                TxtPreviewInfo.Text = $"Phòng: {roomL:F0}x{roomW:F0} mm (S={areaM2:F1}m²) | {lightCount} Đèn {lightName} | {sockText} | {acText} | {dbText}";
            }
        }

        // ============================================================
        // VẼ 3D ISOMETRIC WIREFRAME PREVIEW
        // ============================================================

        private void Render3dIsometricPreview(double L, double W, double cW, double cH, double scale, bool hasDoor, bool hasWin, bool placeDb, bool hasAc, bool hasFan)
        {
            double H = 2800.0; // Cao trần
            double sx = cW / 2.0 + _panOffset.X;
            double sy = cH / 2.0 + 40.0 + _panOffset.Y;

            // Chiếu trục đo Isometric: X hướng chéo phải xuống, Y hướng chéo trái xuống, Z hướng thẳng đứng lên
            Func<double, double, double, Point> project = (x, y, z) =>
            {
                double px = sx + (x - y) * Math.Cos(Math.PI / 6.0) * scale * 0.75;
                double py = sy + (x + y) * Math.Sin(Math.PI / 6.0) * scale * 0.75 - z * scale * 0.75;
                return new Point(px, py);
            };

            // 4 Đỉnh sàn (Z=0)
            Point p0 = project(0, 0, 0);
            Point p1 = project(L, 0, 0);
            Point p2 = project(L, W, 0);
            Point p3 = project(0, W, 0);

            // 4 Đỉnh trần (Z=H)
            Point t0 = project(0, 0, H);
            Point t1 = project(L, 0, H);
            Point t2 = project(L, W, H);
            Point t3 = project(0, W, H);

            // Mặt sàn
            var floor = new Polygon
            {
                Points = new PointCollection { p0, p1, p2, p3 },
                Fill = new SolidColorBrush(Color.FromArgb(160, 15, 23, 42)),
                Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                StrokeThickness = 1.5
            };
            PreviewCanvas.Children.Add(floor);

            // 4 Cột tường đứng
            Draw3dLine(p0, t0, Color.FromRgb(94, 234, 212), 1.5);
            Draw3dLine(p1, t1, Color.FromRgb(94, 234, 212), 1.5);
            Draw3dLine(p2, t2, Color.FromRgb(94, 234, 212), 1.5);
            Draw3dLine(p3, t3, Color.FromRgb(94, 234, 212), 1.5);

            // Viền trần
            Draw3dLine(t0, t1, Color.FromRgb(148, 163, 184), 1.0, true);
            Draw3dLine(t1, t2, Color.FromRgb(148, 163, 184), 1.0, true);
            Draw3dLine(t2, t3, Color.FromRgb(148, 163, 184), 1.0, true);
            Draw3dLine(t3, t0, Color.FromRgb(148, 163, 184), 1.0, true);

            // Cửa đi 3D
            if (hasDoor)
            {
                Point dp0 = project(L * 0.15, 0, 0);
                Point dp1 = project(L * 0.15 + 900, 0, 0);
                Point dpT = project(L * 0.15, 0, 2200);
                Point dpT1 = project(L * 0.15 + 900, 0, 2200);
                Draw3dLine(dp0, dpT, Color.FromRgb(34, 197, 94), 2.0);
                Draw3dLine(dp1, dpT1, Color.FromRgb(34, 197, 94), 2.0);
                Draw3dLine(dpT, dpT1, Color.FromRgb(34, 197, 94), 2.0);
            }

            // Cửa sổ 3D
            if (hasWin)
            {
                Point wp0 = project(L * 0.4, W, 900);
                Point wp1 = project(L * 0.4 + 1500, W, 900);
                Point wpT0 = project(L * 0.4, W, 2200);
                Point wpT1 = project(L * 0.4 + 1500, W, 2200);
                Draw3dLine(wp0, wp1, Color.FromRgb(56, 189, 248), 1.5);
                Draw3dLine(wpT0, wpT1, Color.FromRgb(56, 189, 248), 1.5);
                Draw3dLine(wp0, wpT0, Color.FromRgb(56, 189, 248), 1.5);
                Draw3dLine(wp1, wpT1, Color.FromRgb(56, 189, 248), 1.5);
            }

            // Đèn trần 3D
            for (int r = 1; r <= 2; r++)
            {
                for (int c = 1; c <= 3; c++)
                {
                    Point lp = project(c * (L / 4.0), r * (W / 3.0), H);
                    DrawPreviewDot(lp.X, lp.Y, Color.FromRgb(250, 204, 21), 8);
                }
            }

            if (TxtPreviewInfo != null)
            {
                TxtPreviewInfo.Text = $"[3D ISOMETRIC] Không Gian Phòng: {L:F0} x {W:F0} x {H:F0} mm | Tường 3D | Cửa & Kính Chiếu Trục Đo";
            }
        }

        private void Draw3dLine(Point p1, Point p2, Color col, double thick, bool dashed = false)
        {
            var line = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = new SolidColorBrush(col),
                StrokeThickness = thick
            };
            if (dashed) line.StrokeDashArray = new DoubleCollection { 3, 2 };
            PreviewCanvas.Children.Add(line);
        }

        private void DrawLShapeRoom(double x, double y, double w, double h)
        {
            double rL = Math.Max(1.0, ParseDouble(TxtRoomL?.Text, 6000.0));
            double rW = Math.Max(1.0, ParseDouble(TxtRoomW?.Text, 4000.0));
            double bL = Math.Max(500.0, ParseDouble(TxtBranchL?.Text, rL * 0.5));
            double bW = Math.Max(500.0, ParseDouble(TxtBranchW?.Text, rW * 0.5));
            double ratioX = Math.Min(0.9, Math.Max(0.1, bL / rL));
            double ratioY = Math.Min(0.9, Math.Max(0.1, bW / rW));

            var poly = new Polygon
            {
                Stroke = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                StrokeThickness = 3.5,
                Fill = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Points = new PointCollection
                {
                    new Point(x, y),
                    new Point(x + w, y),
                    new Point(x + w, y + h * ratioY),
                    new Point(x + w * ratioX, y + h * ratioY),
                    new Point(x + w * ratioX, y + h),
                    new Point(x, y + h)
                }
            };
            PreviewCanvas.Children.Add(poly);
        }

        private void DrawTShapeRoom(double x, double y, double w, double h)
        {
            double rL = Math.Max(1.0, ParseDouble(TxtRoomL?.Text, 6000.0));
            double rW = Math.Max(1.0, ParseDouble(TxtRoomW?.Text, 4000.0));
            double bL = Math.Max(500.0, ParseDouble(TxtBranchL?.Text, rL * 0.5));
            double bW = Math.Max(500.0, ParseDouble(TxtBranchW?.Text, rW * 0.5));
            double ratioX = Math.Min(0.9, Math.Max(0.1, bL / rL));
            double ratioY = Math.Min(0.9, Math.Max(0.1, bW / rW));
            double stepRatio = (1.0 - ratioX) / 2.0;

            var poly = new Polygon
            {
                Stroke = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                StrokeThickness = 3.5,
                Fill = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Points = new PointCollection
                {
                    new Point(x + w * stepRatio, y),
                    new Point(x + w * (stepRatio + ratioX), y),
                    new Point(x + w * (stepRatio + ratioX), y + h * ratioY),
                    new Point(x + w, y + h * ratioY),
                    new Point(x + w, y + h),
                    new Point(x, y + h),
                    new Point(x, y + h * ratioY),
                    new Point(x + w * stepRatio, y + h * ratioY)
                }
            };
            PreviewCanvas.Children.Add(poly);
        }

        private void DrawPreviewLightSymbol(Point pt, int type)
        {
            if (type == 1) // Panel 600
            {
                var rect = new Rectangle { Width = 14, Height = 14, Fill = new SolidColorBrush(Color.FromRgb(250, 204, 21)), Stroke = Brushes.Orange, StrokeThickness = 1 };
                Canvas.SetLeft(rect, pt.X - 7);
                Canvas.SetTop(rect, pt.Y - 7);
                PreviewCanvas.Children.Add(rect);
            }
            else if (type == 2) // Tuýp LED
            {
                var rect = new Rectangle { Width = 22, Height = 6, Fill = new SolidColorBrush(Color.FromRgb(250, 204, 21)), Stroke = Brushes.Orange, StrokeThickness = 1 };
                Canvas.SetLeft(rect, pt.X - 11);
                Canvas.SetTop(rect, pt.Y - 3);
                PreviewCanvas.Children.Add(rect);
            }
            else // Downlight
            {
                DrawPreviewDot(pt.X, pt.Y, Color.FromRgb(250, 204, 21), 10);
            }
        }

        private void DrawPreviewWire(Point p1, Point p2, bool isOrtho)
        {
            if (isOrtho)
            {
                var line1 = new Line { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p1.Y, Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)), StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 2 } };
                var line2 = new Line { X1 = p2.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)), StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 2 } };
                PreviewCanvas.Children.Add(line1);
                PreviewCanvas.Children.Add(line2);
            }
            else
            {
                double midX = (p1.X + p2.X) / 2.0;
                double midY = (p1.Y + p2.Y) / 2.0 - 10.0;
                var path = new Path
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3, 2 },
                    Data = Geometry.Parse($"M {p1.X},{p1.Y} Q {midX},{midY} {p2.X},{p2.Y}")
                };
                PreviewCanvas.Children.Add(path);
            }
        }

        private void DrawPreviewArrow(Point from, Point to, Color col, string label)
        {
            Vector v = to - from;
            if (v.Length < 1e-3) return;
            v.Normalize();
            Point tip = from + v * 24;
            var line = new Line { X1 = from.X, Y1 = from.Y, X2 = tip.X, Y2 = tip.Y, Stroke = new SolidColorBrush(col), StrokeThickness = 1.5 };
            PreviewCanvas.Children.Add(line);

            var txt = new TextBlock { Text = label, FontSize = 7, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(col) };
            Canvas.SetLeft(txt, tip.X + 2);
            Canvas.SetTop(txt, tip.Y - 6);
            PreviewCanvas.Children.Add(txt);
        }

        private void DrawPreviewSocket(double x, double y, double rotDeg)
        {
            var arc = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                StrokeThickness = 1.5,
                Fill = new SolidColorBrush(Color.FromArgb(120, 239, 68, 68)),
                Data = Geometry.Parse("M -4,0 A 4,4 0 0 1 4,0 Z"),
                RenderTransform = new RotateTransform(rotDeg)
            };
            Canvas.SetLeft(arc, x);
            Canvas.SetTop(arc, y);
            PreviewCanvas.Children.Add(arc);
        }

        private void DrawPreviewDot(double x, double y, Color color, double size = 6)
        {
            var dot = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 0.5
            };
            Canvas.SetLeft(dot, x - size / 2.0);
            Canvas.SetTop(dot, y - size / 2.0);
            PreviewCanvas.Children.Add(dot);
        }

        private void DrawPreviewSquare(double x, double y, Color color, string label)
        {
            var r = new Rectangle { Width = 10, Height = 10, Fill = new SolidColorBrush(color), Stroke = Brushes.White, StrokeThickness = 0.5 };
            Canvas.SetLeft(r, x - 5);
            Canvas.SetTop(r, y - 5);
            PreviewCanvas.Children.Add(r);

            var txt = new TextBlock { Text = label, FontSize = 6, Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            Canvas.SetLeft(txt, x - 4);
            Canvas.SetTop(txt, y - 12);
            PreviewCanvas.Children.Add(txt);
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

        private void BtnExportBom_Click(object sender, RoutedEventArgs e)
        {
            ExportBomAfterBuild = true;
            BtnBuild_Click(sender, e);
        }

        private void BtnBuild_Click(object sender, RoutedEventArgs e)
        {
            ResultConfig = new FloorPlanConfig
            {
                Shape = (CboShape.SelectedIndex == 1) ? RoomShapeKind.LShape :
                        (CboShape.SelectedIndex == 2) ? RoomShapeKind.TShape :
                        (CboShape.SelectedIndex == 3) ? RoomShapeKind.PickBoundary : RoomShapeKind.Rectangle,
                RoomName = string.IsNullOrWhiteSpace(TxtRoomName.Text) ? "PHÒNG LÀM VIỆC CHÍNH" : TxtRoomName.Text.Trim(),
                RoomLength = ParseDouble(TxtRoomL.Text, 6000.0),
                RoomWidth = ParseDouble(TxtRoomW.Text, 4000.0),
                BranchLength = ParseDouble(TxtBranchL?.Text, Math.Max(1500.0, ParseDouble(TxtRoomL.Text, 6000.0) * 0.5)),
                BranchWidth = ParseDouble(TxtBranchW?.Text, Math.Max(1500.0, ParseDouble(TxtRoomW.Text, 4000.0) * 0.5)),
                WallThickness = (CboWallT.SelectedIndex == 1) ? 110.0 : 220.0,
                CeilingHeight = ParseDouble(TxtCeilingH.Text, 2800.0),
                HasDoor = ChkDoor.IsChecked == true,
                DoorStyle = (CboDoor.SelectedIndex == 1) ? DoorStyleType.Double1200 :
                            (CboDoor.SelectedIndex == 2) ? DoorStyleType.Sliding1000 : DoorStyleType.Single900,
                HasWindow = ChkWindow.IsChecked == true,
                WindowWidth = ParseDouble(TxtWinW?.Text, 1500.0),

                LightType = (CboLightType.SelectedIndex == 1) ? LightingFixtureType.PanelLed600 :
                            (CboLightType.SelectedIndex == 2) ? LightingFixtureType.TubeLedDouble : LightingFixtureType.DownlightLed,
                UseTargetLux = ChkAutoLux.IsChecked == true,
                TargetLux = ParseDouble(TxtTargetLux.Text, 400.0),
                HasExitLight = ChkExitLight.IsChecked == true,

                AutoSocketPerimeter = ChkSocketPerimeter.IsChecked == true,
                SocketSpacing = ParseDouble(TxtSocketSpacing.Text, 2500.0),
                HasAcUnit = ChkAcUnit.IsChecked == true,
                HasCeilingFan = ChkCeilingFan.IsChecked == true,
                HasWaterHeater = ChkWaterHeater.IsChecked == true,

                HasDataLan = ChkDataLan.IsChecked == true,
                HasTvOutlet = ChkTvOutlet.IsChecked == true,
                HasSmokeDetector = ChkSmoke.IsChecked == true,
                HasSecurityCamera = ChkCamera.IsChecked == true,

                WiringStyle = (CboWiringStyle.SelectedIndex == 1) ? WiringRouteStyle.Orthogonal : WiringRouteStyle.CurvedArc,
                SwitchGangs = (CboSwitchGangs.SelectedIndex == 0) ? 1 : (CboSwitchGangs.SelectedIndex == 2) ? 3 : 2,
                DrawHomeRun = ChkHomeRun.IsChecked == true,
                PlaceDbPanel = ChkPlaceDb.IsChecked == true
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
    }
}
