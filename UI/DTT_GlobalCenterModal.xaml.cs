using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Autodesk.AutoCAD.ApplicationServices;
using DTT_AutoCAD_Plugin.Common;
using DTT_AutoCAD_Plugin.Modules;

namespace DTT_AutoCAD_Plugin.UI
{
    public partial class DTT_GlobalCenterModal : Window
    {
        private readonly ModalCategoryConfig _category;
        private List<ModalOptionItem> _allOptions;
        private ModalOptionItem _selectedOption;

        // Quản lý 3D Viewport & Camera
        private PerspectiveCamera _camera;
        private ModelVisual3D _modelVisual;
        private ModelVisual3D _lightVisual;
        private double _cameraDistance = 2600;
        private double _yaw = 0.75;    // ~43 độ
        private double _pitch = 0.45;  // ~26 độ
        private Point3D _targetCenter = new Point3D(0, 1000, 0);

        // Chuột tương tác 3D
        private Point _lastMousePos;
        private bool _isOrbiting = false;
        private bool _isPanning = false;

        // Chế độ xem: true = 3D, false = 2D
        private bool _is3DMode = true;

        // Tham số điều chỉnh nhanh
        private double _currentSize = 900;
        private bool _isFlipped = false;
        private double _openAngleDeg = 45;

        public DTT_GlobalCenterModal(ModalCategoryConfig category)
        {
            InitializeComponent();
            _category = category ?? DTT_ModalRegistry.GetCategory("DYNAMIC_BLOCKS");

            InitHeader();
            Init3DScene();
            LoadOptions();
        }

        #region 1. Khởi Tạo Giao Diện & Danh Sách

        private void InitHeader()
        {
            TxtHeaderIcon.Text = _category.Icon;
            TxtHeaderTitle.Text = _category.Title;
            TxtHeaderSubtitle.Text = _category.Subtitle;
        }

        private void LoadOptions()
        {
            _allOptions = _category.Options.ToList();
            LstOptions.ItemsSource = _allOptions;

            if (_allOptions.Count > 0)
            {
                LstOptions.SelectedIndex = 0;
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = TxtSearch.Text?.Trim().ToLowerInvariant() ?? "";
            bool hasText = !string.IsNullOrEmpty(TxtSearch.Text);

            if (TxtSearchWatermark != null)
                TxtSearchWatermark.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;
            if (BtnClearSearch != null)
                BtnClearSearch.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;

            if (string.IsNullOrEmpty(query))
            {
                LstOptions.ItemsSource = _allOptions;
            }
            else
            {
                var filtered = _allOptions.Where(o =>
                    (o.Title != null && o.Title.ToLowerInvariant().Contains(query)) ||
                    (o.Subtitle != null && o.Subtitle.ToLowerInvariant().Contains(query)) ||
                    (o.Category != null && o.Category.ToLowerInvariant().Contains(query)) ||
                    (o.CommandTag != null && o.CommandTag.ToLowerInvariant().Contains(query))
                ).ToList();

                LstOptions.ItemsSource = filtered;
            }

            if (LstOptions.Items.Count > 0)
            {
                LstOptions.SelectedIndex = 0;
            }
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = "";
            TxtSearch.Focus();
        }

        #endregion

        #region 2. Xử Lý Khi Chọn Cấu Kiện & Tham Số Nhanh

        private void LstOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedOption = LstOptions.SelectedItem as ModalOptionItem;
            if (_selectedOption == null) return;

            // Cập nhật thông tin chi tiết
            TxtSelectedItemName.Text = _selectedOption.Title;
            TxtSelectedDesc.Text = !string.IsNullOrEmpty(_selectedOption.Description)
                ? _selectedOption.Description
                : _selectedOption.Subtitle;
            TxtTargetCmd.Text = "LỆNH: " + _selectedOption.CommandTag + (!string.IsNullOrEmpty(_selectedOption.CommandArgs) ? " " + _selectedOption.CommandArgs : "");

            // Cập nhật tham số kích thước mặc định
            _currentSize = _selectedOption.ParamWidth > 0 ? _selectedOption.ParamWidth : 900;
            _isFlipped = _selectedOption.Flip;
            _openAngleDeg = 45;

            // Sinh các nút kích thước nhanh
            PopulateQuickSizes(_selectedOption);
            UpdateParamLabel();

            // Vẽ lại Preview 2D / 3D
            UpdatePreview();
        }

        private void PopulateQuickSizes(ModalOptionItem opt)
        {
            PanelQuickSizes.Children.Clear();

            List<string> sizes = opt.QuickSizes != null && opt.QuickSizes.Count > 0
                ? opt.QuickSizes
                : new List<string> { "800", "900", "1000", "1200" };

            foreach (string sizeStr in sizes)
            {
                Button btn = new Button
                {
                    Content = sizeStr + (double.TryParse(sizeStr, out _) ? "mm" : ""),
                    Tag = sizeStr,
                    Height = 26,
                    Margin = new Thickness(0, 0, 6, 0),
                    Padding = new Thickness(8, 0, 8, 0),
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    Cursor = Cursors.Hand,
                    Background = new SolidColorBrush(sizeStr == _currentSize.ToString() ? Color.FromRgb(2, 132, 199) : Color.FromRgb(30, 41, 59)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                    BorderThickness = new Thickness(1)
                };

                btn.Click += (s, ev) =>
                {
                    string val = (s as Button)?.Tag?.ToString();
                    if (double.TryParse(val, out double sz))
                    {
                        _currentSize = sz;
                    }
                    else
                    {
                        _selectedOption.SelectedSize = val;
                    }

                    // Đổi màu active
                    foreach (var child in PanelQuickSizes.Children)
                    {
                        if (child is Button b)
                        {
                            b.Background = new SolidColorBrush(b == s ? Color.FromRgb(2, 132, 199) : Color.FromRgb(30, 41, 59));
                        }
                    }

                    UpdateParamLabel();
                    UpdatePreview();
                };

                PanelQuickSizes.Children.Add(btn);
            }
        }

        private void BtnToggleFlip_Click(object sender, RoutedEventArgs e)
        {
            _isFlipped = !_isFlipped;
            BtnToggleFlip.Content = _isFlipped ? "🔄 Hướng: Đã lật" : "🔄 Hướng: Chuẩn";
            UpdatePreview();
        }

        private void BtnToggleAngle_Click(object sender, RoutedEventArgs e)
        {
            if (_openAngleDeg == 45) _openAngleDeg = 90;
            else if (_openAngleDeg == 90) _openAngleDeg = 30;
            else if (_openAngleDeg == 30) _openAngleDeg = 0;
            else _openAngleDeg = 45;

            BtnToggleAngle.Content = $"👁️ Góc mở: {_openAngleDeg}°";
            UpdatePreview();
        }

        private void UpdateParamLabel()
        {
            TxtParamInfo.Text = $"Kích thước: {_currentSize}mm";
        }

        #endregion

        #region 3. Khởi Tạo & Quản Lý Viewport 3D Native WPF

        private void Init3DScene()
        {
            // 1. Camera Perspective
            _camera = new PerspectiveCamera
            {
                FieldOfView = 42,
                UpDirection = new Vector3D(0, 1, 0)
            };
            Vp3D.Camera = _camera;

            // 2. Hệ thống ánh sáng Studio chuyên nghiệp
            Model3DGroup lights = new Model3DGroup();
            lights.Children.Add(new AmbientLight(Color.FromRgb(110, 120, 135)));
            lights.Children.Add(new DirectionalLight(Color.FromRgb(255, 255, 255), new Vector3D(-1.2, -2.5, -2.0)));
            lights.Children.Add(new DirectionalLight(Color.FromRgb(160, 185, 210), new Vector3D(1.5, 2.0, 1.0)));
            lights.Children.Add(new DirectionalLight(Color.FromRgb(120, 140, 160), new Vector3D(0, -1.0, 3.0)));

            _lightVisual = new ModelVisual3D { Content = lights };
            Vp3D.Children.Add(_lightVisual);

            // 3. Visual chứa mô hình 3D
            _modelVisual = new ModelVisual3D();
            Vp3D.Children.Add(_modelVisual);

            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            double eyeX = _targetCenter.X + _cameraDistance * Math.Cos(_pitch) * Math.Sin(_yaw);
            double eyeY = _targetCenter.Y + _cameraDistance * Math.Sin(_pitch);
            double eyeZ = _targetCenter.Z + _cameraDistance * Math.Cos(_pitch) * Math.Cos(_yaw);

            _camera.Position = new Point3D(eyeX, eyeY, eyeZ);
            _camera.LookDirection = new Vector3D(_targetCenter.X - eyeX, _targetCenter.Y - eyeY, _targetCenter.Z - eyeZ);
        }

        private void BtnResetView_Click(object sender, RoutedEventArgs e)
        {
            _yaw = 0.75;
            _pitch = 0.45;
            _cameraDistance = 2600;
            _targetCenter = new Point3D(0, 1000, 0);
            UpdateCameraPosition();
        }

        #endregion

        #region 4. Tương Tác Chuột 3D: Xoay (Orbit), Di Chuyển (Pan), Phóng To (Zoom)

        private void Vp3D_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePos = e.GetPosition(Vp3D);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isOrbiting = true;
                Vp3D.CaptureMouse();
                Vp3D.Cursor = Cursors.SizeAll;
            }
            else if (e.RightButton == MouseButtonState.Pressed || e.MiddleButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                Vp3D.CaptureMouse();
                Vp3D.Cursor = Cursors.ScrollAll;
            }
        }

        private void Vp3D_MouseMove(object sender, MouseEventArgs e)
        {
            Point current = e.GetPosition(Vp3D);
            double dx = current.X - _lastMousePos.X;
            double dy = current.Y - _lastMousePos.Y;
            _lastMousePos = current;

            if (_isOrbiting)
            {
                // Xoay quanh tâm (Orbit / Rotate)
                _yaw -= dx * 0.012;
                _pitch = Math.Max(-1.35, Math.Min(1.35, _pitch + dy * 0.012));
                UpdateCameraPosition();
            }
            else if (_isPanning)
            {
                // Di chuyển camera (Pan)
                double factor = _cameraDistance / 1400.0;
                double rightX = Math.Cos(_yaw);
                double rightZ = -Math.Sin(_yaw);

                _targetCenter = new Point3D(
                    _targetCenter.X - (dx * rightX) * factor,
                    _targetCenter.Y + (dy * factor),
                    _targetCenter.Z - (dx * rightZ) * factor
                );
                UpdateCameraPosition();
            }
        }

        private void Vp3D_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isOrbiting = false;
            _isPanning = false;
            Vp3D.ReleaseMouseCapture();
            Vp3D.Cursor = Cursors.Hand;
        }

        private void Vp3D_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Phóng to / Thu nhỏ (Zoom In / Out)
            _cameraDistance = Math.Max(400, Math.Min(12000, _cameraDistance - e.Delta * 2.2));
            UpdateCameraPosition();
        }

        #endregion

        #region 5. Chuyển Đổi Chế Độ Xem 2D (Flat) / 3D (Interactive)

        private void BtnView2D_Click(object sender, RoutedEventArgs e)
        {
            _is3DMode = false;
            Border2D.Visibility = Visibility.Visible;
            BtnView2D.Background = new SolidColorBrush(Color.FromRgb(2, 132, 199));
            BtnView2D.Foreground = Brushes.White;
            BtnView3D.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            BtnView3D.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));

            UpdatePreview();
        }

        private void BtnView3D_Click(object sender, RoutedEventArgs e)
        {
            _is3DMode = true;
            Border2D.Visibility = Visibility.Collapsed;
            BtnView3D.Background = new SolidColorBrush(Color.FromRgb(2, 132, 199));
            BtnView3D.Foreground = Brushes.White;
            BtnView2D.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            BtnView2D.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));

            UpdatePreview();
        }

        #endregion

        #region 6. Cập Nhật Mô Hình Preview 2D / 3D Theo Lựa Chọn

        private void UpdatePreview()
        {
            if (_selectedOption == null) return;

            if (_is3DMode)
            {
                Render3DPreview();
            }
            else
            {
                Render2DPreview();
            }
        }

        private void Render3DPreview()
        {
            Model3DGroup group = new Model3DGroup();

            switch (_selectedOption.PreviewType)
            {
                case PreviewModelType.Door:
                    group = MeshBuilder3D.BuildDoor3D(_currentSize, 2200, _openAngleDeg, _isFlipped);
                    _targetCenter = new Point3D(0, 1100, 0);
                    _cameraDistance = Math.Max(2200, _currentSize * 2.6);
                    break;

                case PreviewModelType.Column:
                    bool isRound = _selectedOption.Id.Contains("ROUND");
                    group = MeshBuilder3D.BuildColumn3D(_currentSize, 3000, isRound);
                    _targetCenter = new Point3D(0, 1500, 0);
                    _cameraDistance = 3800;
                    break;

                case PreviewModelType.Elevation:
                    group = MeshBuilder3D.BuildElevation3D(1000, _selectedOption.SelectedSize);
                    _targetCenter = new Point3D(50, 600, 0);
                    _cameraDistance = 1800;
                    break;

                case PreviewModelType.Cabinet:
                    group = MeshBuilder3D.BuildCabinet3D(_currentSize, 1800, 400);
                    _targetCenter = new Point3D(0, 1000, 0);
                    _cameraDistance = 2600;
                    break;

                case PreviewModelType.Pipe:
                    group = MeshBuilder3D.BuildPipe3D(_currentSize, 1500);
                    _targetCenter = new Point3D(0, 0, 0);
                    _cameraDistance = 1600;
                    break;

                case PreviewModelType.Wall:
                    group = MeshBuilder3D.BuildWall3D(2000, _currentSize, 1200);
                    _targetCenter = new Point3D(0, 600, 0);
                    _cameraDistance = 2400;
                    break;

                default:
                    // Mô hình mặc định (Dynamic Block Component Box)
                    group.Children.Add(MeshBuilder3D.CreateBox(new Point3D(0, 500, 0), 800, 1000, 500, Color.FromRgb(2, 132, 199)));
                    group.Children.Add(MeshBuilder3D.CreateCylinder(new Point3D(0, 1000, 0), 150, 300, Color.FromRgb(14, 165, 233)));
                    _targetCenter = new Point3D(0, 500, 0);
                    _cameraDistance = 2200;
                    break;
            }

            _modelVisual.Content = group;
            UpdateCameraPosition();
        }

        private void Render2DPreview()
        {
            Canvas2D.Children.Clear();
            double cw = Canvas2D.ActualWidth > 0 ? Canvas2D.ActualWidth : 480;
            double ch = Canvas2D.ActualHeight > 0 ? Canvas2D.ActualHeight : 300;
            double midX = cw / 2.0;
            double midY = ch / 2.0;

            SolidColorBrush strokeCyan = new SolidColorBrush(Color.FromRgb(2, 132, 199));
            SolidColorBrush strokeOrange = new SolidColorBrush(Color.FromRgb(217, 119, 6));
            SolidColorBrush strokeWhite = new SolidColorBrush(Color.FromRgb(241, 245, 249));

            switch (_selectedOption.PreviewType)
            {
                case PreviewModelType.Door:
                    // Mặt bằng cửa đi 2D (Khuôn + Cánh + Cung mở 90°)
                    double dw = 140; // tỉ lệ hiển thị
                    // Khuôn trái & phải
                    AddRect2D(midX - dw / 2 - 8, midY + 30, 8, 16, strokeCyan, Brushes.Transparent);
                    AddRect2D(midX + dw / 2, midY + 30, 8, 16, strokeCyan, Brushes.Transparent);
                    // Cánh cửa mở 90 độ
                    AddLine2D(midX - dw / 2, midY + 30, midX - dw / 2, midY + 30 - dw, strokeOrange, 2.5);
                    // Cung mở cửa tròn
                    AddArc2D(new Point(midX - dw / 2, midY + 30 - dw), new Point(midX + dw / 2, midY + 30), dw, strokeCyan);
                    // Ký hiệu kích thước
                    AddText2D($"RỘNG {_currentSize}mm (MẶT BẰNG 2D)", midX, midY + 70, strokeWhite, 11, true);
                    break;

                case PreviewModelType.Column:
                    // Mặt cắt cột vuông 2D kèm đường tâm trục
                    double cs = 110;
                    AddRect2D(midX - cs / 2, midY - cs / 2, cs, cs, strokeCyan, new SolidColorBrush(Color.FromArgb(40, 2, 132, 199)));
                    // Trục chữ thập
                    AddLine2D(midX - cs, midY, midX + cs, midY, strokeOrange, 1.2, true);
                    AddLine2D(midX, midY - cs, midX, midY + cs, strokeOrange, 1.2, true);
                    AddText2D($"CỘT {_currentSize}x{_currentSize}mm", midX, midY + cs / 2 + 35, strokeWhite, 11, true);
                    break;

                case PreviewModelType.Elevation:
                    // Mốc cao độ 2D: đường dóng ngang + tam giác ngược
                    AddLine2D(midX - 120, midY, midX + 120, midY, strokeCyan, 2.0);
                    // Tam giác chỉ cốt
                    Polygon tri = new Polygon
                    {
                        Points = new PointCollection { new Point(midX - 25, midY), new Point(midX + 25, midY), new Point(midX, midY + 35) },
                        Stroke = strokeOrange,
                        StrokeThickness = 2,
                        Fill = new SolidColorBrush(Color.FromArgb(80, 217, 119, 6))
                    };
                    Canvas2D.Children.Add(tri);
                    AddText2D(_selectedOption.SelectedSize, midX, midY - 25, strokeOrange, 16, true);
                    AddText2D("CAO ĐỘ MẶT SÀN HOÀN THIỆN", midX, midY + 65, strokeWhite, 10, true);
                    break;

                default:
                    // Hình chữ nhật ký hiệu chung
                    AddRect2D(midX - 90, midY - 60, 180, 120, strokeCyan, new SolidColorBrush(Color.FromArgb(40, 2, 132, 199)));
                    AddLine2D(midX - 90, midY - 60, midX + 90, midY + 60, strokeOrange, 1);
                    AddLine2D(midX - 90, midY + 60, midX + 90, midY - 60, strokeOrange, 1);
                    AddText2D(_selectedOption.Title, midX, midY + 80, strokeWhite, 11, true);
                    break;
            }
        }

        private void AddRect2D(double x, double y, double w, double h, Brush stroke, Brush fill)
        {
            Rectangle r = new Rectangle { Width = w, Height = h, Stroke = stroke, StrokeThickness = 1.8, Fill = fill };
            Canvas.SetLeft(r, x);
            Canvas.SetTop(r, y);
            Canvas2D.Children.Add(r);
        }

        private void AddLine2D(double x1, double y1, double x2, double y2, Brush stroke, double thick, bool dashed = false)
        {
            Line l = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = stroke, StrokeThickness = thick };
            if (dashed) l.StrokeDashArray = new DoubleCollection { 4, 3 };
            Canvas2D.Children.Add(l);
        }

        private void AddArc2D(Point start, Point end, double radius, Brush stroke)
        {
            PathFigure fig = new PathFigure { StartPoint = start };
            fig.Segments.Add(new ArcSegment { Point = end, Size = new Size(radius, radius), SweepDirection = SweepDirection.Clockwise });
            PathGeometry geom = new PathGeometry();
            geom.Figures.Add(fig);
            Path path = new Path { Data = geom, Stroke = stroke, StrokeThickness = 1.5, StrokeDashArray = new DoubleCollection { 3, 2 } };
            Canvas2D.Children.Add(path);
        }

        private void AddText2D(string text, double x, double y, Brush fg, double size, bool center = false)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                Foreground = fg,
                FontSize = size,
                FontWeight = FontWeights.Bold
            };
            if (center)
            {
                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                x -= tb.DesiredSize.Width / 2.0;
            }
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            Canvas2D.Children.Add(tb);
        }

        #endregion

        #region 7. Xử Lý Phím Tắt, Kéo Thả & Thực Thi Lệnh CAD

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                BtnCancel_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                BtnConfirm_Click(sender, e);
                e.Handled = true;
            }
        }

        private void LstOptions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_selectedOption != null)
            {
                BtnConfirm_Click(sender, e);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOption == null)
            {
                Close();
                return;
            }

            DialogResult = true;
            Close();

            // Thực thi lệnh CAD tương ứng
            ExecuteSelectedCadCommand(_selectedOption);
        }

        private void ExecuteSelectedCadCommand(ModalOptionItem opt)
        {
            try
            {
                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return;

                // 1. Chuyển Focus về màn hình vẽ AutoCAD
                CadUtils.FocusDwgView();

                // 2. Chạy Custom Action nếu có
                if (opt.CustomAction != null)
                {
                    opt.CustomAction.Invoke();
                    return;
                }

                string cmdTag = opt.CommandTag?.Trim() ?? "";

                // 3. Nếu là Cột Kiến Trúc / Kết Cấu -> Chèn tức thì không hỏi dòng lệnh!
                if (opt.PreviewType == PreviewModelType.Column || 
                    cmdTag.Equals("DT_COLUMN", StringComparison.OrdinalIgnoreCase) || 
                    cmdTag.Equals("DTT_COLUMN", StringComparison.OrdinalIgnoreCase))
                {
                    double sz = _currentSize > 0 ? _currentSize : (opt.ParamWidth > 0 ? opt.ParamWidth : 220);
                    BlockPlacementContext.TargetBlockName = "Architectural_Columns";
                    BlockPlacementContext.Width = sz;
                    BlockPlacementContext.Height = sz;
                    BlockPlacementContext.TagText = $"C{sz}x{sz}";
                    doc.SendStringToExecute("DT_FAST_PLACE\n", true, false, false);
                    return;
                }

                // 4. Nếu là Cửa Đi Kiến Trúc -> Chèn tức thì không hỏi dòng lệnh!
                if (opt.PreviewType == PreviewModelType.Door || 
                    cmdTag.Equals("DT_DOOR", StringComparison.OrdinalIgnoreCase) || 
                    cmdTag.Equals("DTT_DOOR", StringComparison.OrdinalIgnoreCase))
                {
                    double sz = _currentSize > 0 ? _currentSize : (opt.ParamWidth > 0 ? opt.ParamWidth : 900);
                    BlockPlacementContext.TargetBlockName = "Architectural_Doors";
                    BlockPlacementContext.Width = sz;
                    BlockPlacementContext.Flip = _isFlipped;
                    BlockPlacementContext.Angle = _openAngleDeg;
                    BlockPlacementContext.TagText = $"D ({sz}x2200)";
                    doc.SendStringToExecute("DT_FAST_PLACE\n", true, false, false);
                    return;
                }

                // 5. Nếu là Ký Hiệu Cao Độ -> Chèn tức thì không hỏi dòng lệnh!
                if (opt.PreviewType == PreviewModelType.Elevation || 
                    cmdTag.Equals("DT_ELEVATION", StringComparison.OrdinalIgnoreCase) || 
                    cmdTag.Equals("DTT_ELEVATION", StringComparison.OrdinalIgnoreCase))
                {
                    BlockPlacementContext.TargetBlockName = "Annotations";
                    BlockPlacementContext.ElevationText = !string.IsNullOrWhiteSpace(opt.SelectedSize) ? opt.SelectedSize : "+0.000";
                    doc.SendStringToExecute("DT_FAST_PLACE\n", true, false, false);
                    return;
                }

                // 6. Nếu là Dynamic Block chung
                if (cmdTag.Equals("DT_INSERTBLOCK", StringComparison.OrdinalIgnoreCase) || 
                    cmdTag.Equals("DTT_INSERTBLOCK", StringComparison.OrdinalIgnoreCase))
                {
                    string blockName = !string.IsNullOrWhiteSpace(opt.CommandArgs) ? opt.CommandArgs : (opt.Id ?? "Architectural_Doors");
                    BlockPlacementContext.TargetBlockName = blockName;
                    BlockPlacementContext.Width = _currentSize > 0 ? _currentSize : opt.ParamWidth;
                    BlockPlacementContext.Flip = _isFlipped;
                    doc.SendStringToExecute("DT_FAST_PLACE\n", true, false, false);
                    return;
                }

                // 7. Gửi lệnh an toàn
                string formatted = cmdTag + "\n";
                doc.SendStringToExecute(formatted, true, false, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi thực thi lệnh '{opt.CommandTag}': {ex.Message}", "DTools Lỗi CAD");
            }
        }

        #endregion
    }
}
