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
    public partial class DTools_HousePlanArchDialog : Window
    {
        public HousePlanConfig ResultConfig { get; private set; }

        private double _zoomScale = 1.0;
        private Point _panOffset = new Point(0, 0);
        private bool _isPanning = false;
        private bool _isRotating = false;
        private Point _lastMousePos;
        private bool _is3D = false;

        public DTools_HousePlanArchDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _zoomScale = 1.0;
                _panOffset = new Point(0, 0);
                if (SliderYaw != null) SliderYaw.Value = 45;
                if (SliderPitch != null) SliderPitch.Value = 35;
                UpdateLivePreview();
            }
            catch { }
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed) DragMove();
            }
            catch { }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
            }
            catch
            {
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
            }
            catch
            {
                Close();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    DialogResult = false;
                }
                else if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    BtnDrawCad_Click(sender, e);
                }
            }
            catch { }
        }

        private void Parameter_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            try
            {
                // Show/hide BranchL/BranchW panel for L/T shapes
                int shapeIdx2 = CboShape?.SelectedIndex ?? 0;
                if (BorderBranch != null)
                    BorderBranch.Visibility = (shapeIdx2 == 1 || shapeIdx2 == 2)
                        ? Visibility.Visible : Visibility.Collapsed;

                UpdateLivePreview();
            }
            catch { }
        }

        private void ViewMode_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            try
            {
                _is3D = Rad3D.IsChecked == true;
                UpdateLivePreview();
            }
            catch { }
        }

        private void Rotation_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || !_is3D) return;
            try
            {
                UpdateLivePreview();
            }
            catch { }
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _zoomScale *= 1.25;
                UpdateLivePreview();
            }
            catch { }
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _zoomScale = Math.Max(0.2, _zoomScale / 1.25);
                UpdateLivePreview();
            }
            catch { }
        }

        private void BtnFit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _zoomScale = 1.0;
                _panOffset = new Point(0, 0);
                if (SliderYaw != null) SliderYaw.Value = 45;
                if (SliderPitch != null) SliderPitch.Value = 35;
                UpdateLivePreview();
            }
            catch { }
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                double factor = e.Delta > 0 ? 1.15 : 0.85;
                _zoomScale = Math.Max(0.15, Math.Min(10.0, _zoomScale * factor));
                UpdateLivePreview();
                e.Handled = true;
            }
            catch { }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    _isPanning = true;
                    _lastMousePos = e.GetPosition(PreviewCanvas);
                    PreviewCanvas.CaptureMouse();
                }
                else if (e.RightButton == MouseButtonState.Pressed)
                {
                    _isRotating = true;
                    _lastMousePos = e.GetPosition(PreviewCanvas);
                    PreviewCanvas.CaptureMouse();
                }
            }
            catch { }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                Point curPos = e.GetPosition(PreviewCanvas);
                double dx = curPos.X - _lastMousePos.X;
                double dy = curPos.Y - _lastMousePos.Y;

                if (_isPanning)
                {
                    _panOffset.X += dx;
                    _panOffset.Y += dy;
                    _lastMousePos = curPos;
                    UpdateLivePreview();
                }
                else if (_isRotating && _is3D)
                {
                    double newYaw = (SliderYaw.Value + dx * 0.8) % 360;
                    if (newYaw < 0) newYaw += 360;
                    SliderYaw.Value = newYaw;

                    double newPitch = Math.Max(10, Math.Min(85, SliderPitch.Value - dy * 0.5));
                    SliderPitch.Value = newPitch;

                    _lastMousePos = curPos;
                    UpdateLivePreview();
                }
            }
            catch { }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _isPanning = false;
                _isRotating = false;
                PreviewCanvas.ReleaseMouseCapture();
            }
            catch { }
        }

        private void UpdateLivePreview()
        {
            if (PreviewCanvas == null) return;
            PreviewCanvas.Children.Clear();

            try
            {
                double cW = PreviewCanvas.ActualWidth;
                double cH = PreviewCanvas.ActualHeight;
                if (cW <= 0 || cH <= 0) { cW = 580; cH = 520; }

                double L = Math.Max(1000.0, ParseDouble(TxtL?.Text, 8000.0));
                double W = Math.Max(1000.0, ParseDouble(TxtW?.Text, 5000.0));
                double H = Math.Max(1000.0, ParseDouble(TxtH?.Text, 3300.0));
                double wallT = (CboWallT?.SelectedIndex == 1) ? 300.0 : 220.0;
                double colS = 220.0;
                bool hasCols = ChkColumns?.IsChecked == true;
                bool hasDoor = ChkDoor?.IsChecked == true;
                bool hasWin = ChkWindow?.IsChecked == true;
                bool hasGrids = ChkGrids?.IsChecked == true;
                bool hasElec = ChkElecOverlay?.IsChecked == true;

                int shapeIdx = CboShape?.SelectedIndex ?? 0;

                double areaM2 = (L * W) / 1000000.0;
                if (shapeIdx == 1) areaM2 *= 0.75;
                else if (shapeIdx == 2) areaM2 *= 0.65;
                if (TxtAreaCalc != null) TxtAreaCalc.Text = $"{areaM2:F1} m²";

                double baseScale = Math.Min(cW / (L * 1.55), cH / (W * 1.55)) * _zoomScale;
                Point center = new Point(cW / 2.0 + _panOffset.X, cH / 2.0 + _panOffset.Y);

                double yawRad = (SliderYaw?.Value ?? 45.0) * Math.PI / 180.0;
                double pitchRad = (SliderPitch?.Value ?? 35.0) * Math.PI / 180.0;

                Point ProjectPoint(double x, double y, double z)
                {
                    double cx = x - L / 2.0;
                    double cy = y - W / 2.0;
                    double cz = z;

                    if (!_is3D)
                    {
                        return new Point(center.X + cx * baseScale, center.Y - cy * baseScale);
                    }
                    else
                    {
                        double rx = cx * Math.Cos(yawRad) - cy * Math.Sin(yawRad);
                        double ry = cx * Math.Sin(yawRad) + cy * Math.Cos(yawRad);
                        double isoX = rx;
                        double isoY = ry * Math.Sin(pitchRad) - cz * Math.Cos(pitchRad);
                        return new Point(center.X + isoX * baseScale, center.Y + isoY * baseScale);
                    }
                }

                List<Point> pts2D = new List<Point>();
                if (shapeIdx == 1) // Chữ L
                {
                    pts2D.Add(new Point(0, 0));
                    pts2D.Add(new Point(L, 0));
                    pts2D.Add(new Point(L, W * 0.5));
                    pts2D.Add(new Point(L * 0.5, W * 0.5));
                    pts2D.Add(new Point(L * 0.5, W));
                    pts2D.Add(new Point(0, W));
                }
                else if (shapeIdx == 2) // Chữ T
                {
                    double stepX = L * 0.25;
                    pts2D.Add(new Point(stepX, 0));
                    pts2D.Add(new Point(stepX + L * 0.5, 0));
                    pts2D.Add(new Point(stepX + L * 0.5, W * 0.5));
                    pts2D.Add(new Point(L, W * 0.5));
                    pts2D.Add(new Point(L, W));
                    pts2D.Add(new Point(0, W));
                    pts2D.Add(new Point(0, W * 0.5));
                    pts2D.Add(new Point(stepX, W * 0.5));
                }
                else // Chữ Nhật
                {
                    pts2D.Add(new Point(0, 0));
                    pts2D.Add(new Point(L, 0));
                    pts2D.Add(new Point(L, W));
                    pts2D.Add(new Point(0, W));
                }

                if (_is3D)
                {
                    Polygon floor = new Polygon
                    {
                        Fill = new SolidColorBrush(Color.FromArgb(50, 30, 41, 59)),
                        Stroke = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                        StrokeThickness = 1
                    };
                    for (int i = 0; i < pts2D.Count; i++)
                        floor.Points.Add(ProjectPoint(pts2D[i].X, pts2D[i].Y, 0));
                    PreviewCanvas.Children.Add(floor);

                    for (int i = 0; i < pts2D.Count; i++)
                    {
                        Point p1 = pts2D[i];
                        Point p2 = pts2D[(i + 1) % pts2D.Count];

                        Polygon wallFace = new Polygon
                        {
                            Fill = new SolidColorBrush(Color.FromArgb(90, 2, 132, 199)),
                            Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                            StrokeThickness = 1.2
                        };
                        wallFace.Points.Add(ProjectPoint(p1.X, p1.Y, 0));
                        wallFace.Points.Add(ProjectPoint(p2.X, p2.Y, 0));
                        wallFace.Points.Add(ProjectPoint(p2.X, p2.Y, H));
                        wallFace.Points.Add(ProjectPoint(p1.X, p1.Y, H));
                        PreviewCanvas.Children.Add(wallFace);
                    }
                }
                else
                {
                    Polygon polyWallOut = new Polygon
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                        StrokeThickness = 2.2,
                        Fill = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255))
                    };
                    for (int i = 0; i < pts2D.Count; i++)
                        polyWallOut.Points.Add(ProjectPoint(pts2D[i].X, pts2D[i].Y, 0));
                    PreviewCanvas.Children.Add(polyWallOut);
                }

                if (hasCols)
                {
                    foreach (var p in pts2D)
                    {
                        Point cPt = ProjectPoint(p.X, p.Y, _is3D ? H * 0.5 : 0);
                        Rectangle colRect = new Rectangle
                        {
                            Width = Math.Max(5, colS * baseScale),
                            Height = Math.Max(5, colS * baseScale),
                            Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                            Stroke = new SolidColorBrush(Color.FromRgb(254, 202, 202)),
                            StrokeThickness = 1
                        };
                        Canvas.SetLeft(colRect, cPt.X - colRect.Width / 2.0);
                        Canvas.SetTop(colRect, cPt.Y - colRect.Height / 2.0);
                        PreviewCanvas.Children.Add(colRect);
                    }
                }

                if (hasDoor)
                {
                    Point dStart = ProjectPoint(colS + 300, 0, 0);
                    Point dEnd = ProjectPoint(colS + 1200, 0, 0);

                    Line doorLine = new Line
                    {
                        X1 = dStart.X, Y1 = dStart.Y,
                        X2 = dEnd.X, Y2 = dEnd.Y,
                        Stroke = new SolidColorBrush(Color.FromRgb(234, 179, 8)),
                        StrokeThickness = 3
                    };
                    PreviewCanvas.Children.Add(doorLine);
                }

                if (hasWin)
                {
                    double winW = 1600.0;
                    Point wStart = ProjectPoint(L / 2.0 - winW / 2.0, W, _is3D ? H * 0.4 : 0);
                    Point wEnd = ProjectPoint(L / 2.0 + winW / 2.0, W, _is3D ? H * 0.4 : 0);

                    Line winLine = new Line
                    {
                        X1 = wStart.X, Y1 = wStart.Y,
                        X2 = wEnd.X, Y2 = wEnd.Y,
                        Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                        StrokeThickness = 3.5
                    };
                    PreviewCanvas.Children.Add(winLine);
                }

                if (!_is3D && hasGrids)
                {
                    DrawGridLine2D(PreviewCanvas, ProjectPoint(0, -600, 0), ProjectPoint(0, W + 600, 0), "1");
                    DrawGridLine2D(PreviewCanvas, ProjectPoint(L, -600, 0), ProjectPoint(L, W + 600, 0), "2");
                    DrawGridLine2D(PreviewCanvas, ProjectPoint(-600, 0, 0), ProjectPoint(L + 600, 0, 0), "A");
                    DrawGridLine2D(PreviewCanvas, ProjectPoint(-600, W, 0), ProjectPoint(L + 600, W, 0), "B");
                }

                if (hasElec)
                {
                    int lCount = (CboLightCount?.SelectedIndex == 0) ? 4 :
                                 (CboLightCount?.SelectedIndex == 2) ? 8 :
                                 (CboLightCount?.SelectedIndex == 3) ? 12 : 6;

                    int cols = Math.Max(2, (int)Math.Round(Math.Sqrt(lCount * (L / Math.Max(100.0, W)))));
                    int rows = Math.Max(1, (int)Math.Ceiling((double)lCount / cols));
                    double dx = (L - 1000.0) / (cols + 1);
                    double dy = (W - 1000.0) / (rows + 1);

                    List<Point> lightPts = new List<Point>();
                    for (int r = 1; r <= rows; r++)
                    {
                        for (int c = 1; c <= cols; c++)
                        {
                            if (lightPts.Count >= lCount) break;
                            Point lp = ProjectPoint(500 + c * dx, 500 + r * dy, _is3D ? H : 0);
                            lightPts.Add(lp);

                            Ellipse lightDot = new Ellipse
                            {
                                Width = 10, Height = 10,
                                Fill = new SolidColorBrush(Color.FromRgb(250, 204, 21)),
                                Stroke = new SolidColorBrush(Color.FromRgb(253, 224, 71)),
                                StrokeThickness = 1.5
                            };
                            Canvas.SetLeft(lightDot, lp.X - 5);
                            Canvas.SetTop(lightDot, lp.Y - 5);
                            PreviewCanvas.Children.Add(lightDot);
                        }
                    }

                    for (int i = 0; i < lightPts.Count - 1; i++)
                    {
                        Line wire = new Line
                        {
                            X1 = lightPts[i].X, Y1 = lightPts[i].Y,
                            X2 = lightPts[i + 1].X, Y2 = lightPts[i + 1].Y,
                            Stroke = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                            StrokeDashArray = new DoubleCollection { 3, 2 },
                            StrokeThickness = 1.2
                        };
                        PreviewCanvas.Children.Add(wire);
                    }
                }

                Point roomCenter = ProjectPoint(L / 2.0, W / 2.0, 0);
                TextBlock txtTitle = new TextBlock
                {
                    Text = TxtRoomName?.Text?.ToUpper() ?? "PHÒNG KHÁCH",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(248, 250, 252))
                };
                Canvas.SetLeft(txtTitle, roomCenter.X - 60);
                Canvas.SetTop(txtTitle, roomCenter.Y - 14);
                PreviewCanvas.Children.Add(txtTitle);
            }
            catch { }
        }

        private static void DrawGridLine2D(Canvas canvas, Point p1, Point p2, string label)
        {
            try
            {
                Line grid = new Line
                {
                    X1 = p1.X, Y1 = p1.Y,
                    X2 = p2.X, Y2 = p2.Y,
                    Stroke = new SolidColorBrush(Color.FromArgb(140, 239, 68, 68)),
                    StrokeDashArray = new DoubleCollection { 6, 2, 2, 2 },
                    StrokeThickness = 1
                };
                canvas.Children.Add(grid);

                Ellipse bubble = new Ellipse
                {
                    Width = 18, Height = 18,
                    Stroke = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                    Fill = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    StrokeThickness = 1.2
                };
                Canvas.SetLeft(bubble, p1.X - 9);
                Canvas.SetTop(bubble, p1.Y - 9);
                canvas.Children.Add(bubble);

                TextBlock txt = new TextBlock
                {
                    Text = label,
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(248, 250, 252))
                };
                Canvas.SetLeft(txt, p1.X - 4);
                Canvas.SetTop(txt, p1.Y - 7);
                canvas.Children.Add(txt);
            }
            catch { }
        }

        private void BtnDrawCad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double parsedL = ParseDouble(TxtL?.Text, 8000.0);
                double parsedW = ParseDouble(TxtW?.Text, 5000.0);
                double parsedBranchL = ParseDouble(TxtBranchL?.Text, parsedL * 0.5);
                double parsedBranchW = ParseDouble(TxtBranchW?.Text, parsedW * 0.5);

                ResultConfig = new HousePlanConfig
                {
                    Shape = (CboShape.SelectedIndex == 1) ? HouseShapeKind.LShape :
                            (CboShape.SelectedIndex == 2) ? HouseShapeKind.TShape :
                            (CboShape.SelectedIndex == 3) ? HouseShapeKind.PickBoundary : HouseShapeKind.Rectangle,
                    RoomName = string.IsNullOrWhiteSpace(TxtRoomName?.Text) ? "PHÒNG KHÁCH & SINH HOẠT" : TxtRoomName.Text.Trim(),
                    Length = parsedL,
                    Width  = parsedW,
                    BranchL = Math.Max(500.0, parsedBranchL),
                    BranchW = Math.Max(500.0, parsedBranchW),
                    CeilingHeight = ParseDouble(TxtH?.Text, 3300.0),
                    WallOuterT = (CboWallT.SelectedIndex == 1) ? 300.0 : (CboWallT.SelectedIndex == 2) ? 110.0 : 220.0,
                    WallInnerT = (CboWallInnerT.SelectedIndex == 1) ? 75.0 : 110.0,
                    HasColumns = ChkColumns.IsChecked == true,
                    HasDoor    = ChkDoor.IsChecked == true,
                    DoorType   = (CboDoorKind.SelectedIndex == 1) ? ArchDoorKind.DoubleSwing1400 :
                                 (CboDoorKind.SelectedIndex == 2) ? ArchDoorKind.Sliding1200 : ArchDoorKind.SingleSwing900,
                    HasWindow   = ChkWindow.IsChecked == true,
                    WindowWidth = (CboWinW.SelectedIndex == 1) ? 1200.0 : (CboWinW.SelectedIndex == 2) ? 2000.0 : 1600.0,
                    GenerateGrids       = ChkGrids.IsChecked == true,
                    Generate3LayerDims  = Chk3Dims.IsChecked == true,
                    OverlayElectrical   = ChkElecOverlay.IsChecked == true,
                    LightCount   = (CboLightCount.SelectedIndex == 0) ? 4 :
                                   (CboLightCount.SelectedIndex == 2) ? 8 :
                                   (CboLightCount.SelectedIndex == 3) ? 12 : 6,
                    PlaceDbPanel = true
                };

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi thiết lập thông số: " + ex.Message, "DTools Pro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static double ParseDouble(string str, double fallback)
        {
            if (string.IsNullOrWhiteSpace(str)) return fallback;
            str = str.Trim().Replace(',', '.');
            return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double val) ? val : fallback;
        }
    }
}
