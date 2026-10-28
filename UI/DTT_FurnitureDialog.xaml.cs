using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using DTT_AutoCAD_Plugin.Modules;

namespace DTT_AutoCAD_Plugin.UI
{
    public partial class DTT_FurnitureDialog : Window
    {
        public FurnitureLayoutConfig ResultConfig { get; private set; }
        private double _currentZoom = 1.0;

        public DTT_FurnitureDialog()
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
            ResultConfig = BuildFurnitureConfig();
            DialogResult = true;
            Close();
        }

        private void Param_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
                UpdateCalculationsAndPreview();
        }

        private void CboRoomType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            int idx = CboRoomType.SelectedIndex;
            if (idx == 0) // Living Room
            {
                TxtRoomLength.Text = "5000";
                TxtRoomWidth.Text = "4000";
                TxtRoomDesc.Text = "Bộ nội thất: Sofa góc L 2600x1800, Bàn trà 1200x600, Kệ Tivi 2000x400 (Tivi 65 inch), Thảm trang trí";
                if (CboLayer != null) CboLayer.SelectedIndex = 0;
            }
            else if (idx == 1) // Bedroom
            {
                TxtRoomLength.Text = "4200";
                TxtRoomWidth.Text = "3600";
                TxtRoomDesc.Text = "Bộ nội thất: Giường King 1800x2000, 2 Tab đầu giường 500x450, Tủ quần áo trượt 2400x600, Bàn trang điểm";
                if (CboLayer != null) CboLayer.SelectedIndex = 0;
            }
            else if (idx == 2) // Kitchen
            {
                TxtRoomLength.Text = "4500";
                TxtRoomWidth.Text = "3200";
                TxtRoomDesc.Text = "Bộ nội thất: Tủ bếp góc chữ L 3000x2400, Chậu rửa chén đôi, Bếp điện từ 4 vùng, Tủ lạnh Side-by-Side, Bàn ăn 6 ghế";
                if (CboLayer != null) CboLayer.SelectedIndex = 0;
            }
            else if (idx == 3) // WC
            {
                TxtRoomLength.Text = "2600";
                TxtRoomWidth.Text = "2000";
                TxtRoomDesc.Text = "Bộ thiết bị: Bàn lavabo đá 1000x550, Bồn cầu liền khối 700x400, Vách kính tắm đứng 1000x1000, Thoát sàn";
                if (CboLayer != null) CboLayer.SelectedIndex = 1;
            }
            else if (idx == 4) // Office
            {
                TxtRoomLength.Text = "4000";
                TxtRoomWidth.Text = "3500";
                TxtRoomDesc.Text = "Bộ nội thất: Bàn giám đốc 1800x850, Ghế xoay công thái học, Tủ tài liệu / kệ sách 2000x400, 2 Ghế tiếp khách";
                if (CboLayer != null) CboLayer.SelectedIndex = 0;
            }

            UpdateCalculationsAndPreview();
        }

        private void BtnPickBoundary_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    var ed = doc.Editor;
                    var ppr1 = ed.GetPoint("\n[DTools] Chọn góc thứ nhất của phòng: ");
                    if (ppr1.Status == PromptStatus.OK)
                    {
                        var pco = new PromptCornerOptions("\n[DTools] Chọn góc đối diện của phòng: ", ppr1.Value);
                        var ppr2 = ed.GetCorner(pco);
                        if (ppr2.Status == PromptStatus.OK)
                        {
                            double len = Math.Abs(ppr2.Value.X - ppr1.Value.X);
                            double wid = Math.Abs(ppr2.Value.Y - ppr1.Value.Y);
                            if (len > 500 && wid > 500)
                            {
                                TxtRoomLength.Text = Math.Max(len, wid).ToString("F0");
                                TxtRoomWidth.Text = Math.Min(len, wid).ToString("F0");
                            }
                        }
                    }
                }
            }
            catch { }
            finally
            {
                Show();
                UpdateCalculationsAndPreview();
            }
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
            if (FurnitureViewbox != null)
            {
                FurnitureViewbox.LayoutTransform = new ScaleTransform(_currentZoom, _currentZoom);
            }
        }

        private FurnitureLayoutConfig BuildFurnitureConfig()
        {
            var cfg = new FurnitureLayoutConfig();

            int typeIdx = CboRoomType != null ? CboRoomType.SelectedIndex : 0;
            switch (typeIdx)
            {
                case 0: cfg.RoomType = FurnitureRoomType.LivingRoom; break;
                case 1: cfg.RoomType = FurnitureRoomType.MasterBedroom; break;
                case 2: cfg.RoomType = FurnitureRoomType.KitchenDining; break;
                case 3: cfg.RoomType = FurnitureRoomType.RestroomWc; break;
                case 4: cfg.RoomType = FurnitureRoomType.HomeOffice; break;
            }

            if (double.TryParse(TxtRoomLength?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double len) && len > 500)
                cfg.RoomLength = len;
            else
                cfg.RoomLength = 5000;

            if (double.TryParse(TxtRoomWidth?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double wid) && wid > 500)
                cfg.RoomWidth = wid;
            else
                cfg.RoomWidth = 4000;

            if (double.TryParse(TxtWallClearance?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double clr) && clr >= 0)
                cfg.WallClearance = clr;
            else
                cfg.WallClearance = 80;

            if (double.TryParse(TxtWalkwayMin?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double walk) && walk >= 0)
                cfg.WalkwayMin = walk;
            else
                cfg.WalkwayMin = 900;

            int rotIdx = CboRotation != null ? CboRotation.SelectedIndex : 0;
            cfg.RotationDeg = rotIdx * 90.0;

            cfg.DrawWalls = ChkDrawWalls?.IsChecked == true;
            cfg.DrawDimensions = ChkDimensions?.IsChecked == true;

            int layerIdx = CboLayer != null ? CboLayer.SelectedIndex : 0;
            cfg.TargetLayer = layerIdx == 1 ? "A-SANR" : (layerIdx == 2 ? "A-FURN-HIGH" : "A-FURN");

            return cfg;
        }

        private void UpdateCalculationsAndPreview()
        {
            if (FurnitureCanvas == null) return;

            var cfg = BuildFurnitureConfig();

            double areaM2 = (cfg.RoomLength * cfg.RoomWidth) / 1000000.0;
            TxtFurnitureDims.Text = $"Kích thước phòng: {cfg.RoomLength:F0} x {cfg.RoomWidth:F0} mm (Góc xoay: {cfg.RotationDeg}°)";
            TxtRoomArea.Text = $"Diện tích: {areaM2:F1} m² • Hành lang tối thiểu: {cfg.WalkwayMin:F0}mm (Chuẩn)";

            Render2DTopDownFurniture(cfg);
        }

        private void Render2DTopDownFurniture(FurnitureLayoutConfig cfg)
        {
            FurnitureCanvas.Children.Clear();

            double canvasW = 600;
            double canvasH = 440;
            FurnitureCanvas.Width = canvasW;
            FurnitureCanvas.Height = canvasH;

            // Bút vẽ CAD
            var brushWall = new SolidColorBrush(Color.FromRgb(148, 163, 184));    // Xám tường đôi
            var brushWallInner = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            var brushFurn = new SolidColorBrush(Color.FromRgb(245, 158, 11));     // Màu cam gỗ ấm: A-FURN
            var brushAccent = new SolidColorBrush(Color.FromRgb(56, 189, 248));   // Cyan thiết bị
            var brushText = new SolidColorBrush(Color.FromRgb(241, 245, 249));    // Trắng chữ
            var brushDim = new SolidColorBrush(Color.FromRgb(100, 116, 139));     // Nét đo kích thước
            var brushRug = new SolidColorBrush(Color.FromRgb(71, 85, 105));       // Nét đứt thảm

            double roomL = cfg.RoomLength;
            double roomW = cfg.RoomWidth;

            double scale = Math.Min((canvasW - 100) / roomL, (canvasH - 90) / roomW);
            double ox = 50;
            double oy = (canvasH - roomW * scale) / 2;

            double rw = roomL * scale;
            double rh = roomW * scale;

            // 1. TƯỜNG PHÒNG (NÉT TƯỜNG ĐÔI 220mm) & CỬA RA VÀO
            if (cfg.DrawWalls)
            {
                double wallThick = 220.0 * scale;
                var wallOuter = new Rectangle
                {
                    Width = rw + wallThick * 2,
                    Height = rh + wallThick * 2,
                    Stroke = brushWall,
                    StrokeThickness = 2.0,
                    Fill = brushWallInner
                };
                Canvas.SetLeft(wallOuter, ox - wallThick);
                Canvas.SetTop(wallOuter, oy - wallThick);
                FurnitureCanvas.Children.Add(wallOuter);

                var roomFloor = new Rectangle
                {
                    Width = rw,
                    Height = rh,
                    Stroke = brushWall,
                    StrokeThickness = 1.0,
                    Fill = new SolidColorBrush(Color.FromArgb(20, 2, 6, 23))
                };
                Canvas.SetLeft(roomFloor, ox);
                Canvas.SetTop(roomFloor, oy);
                FurnitureCanvas.Children.Add(roomFloor);

                // Cửa ra vào (Door swing opening 900mm) ở góc dưới bên phải
                double doorW = 900.0 * scale;
                var doorLeaf = new Line
                {
                    X1 = ox + rw - doorW, Y1 = oy + rh,
                    X2 = ox + rw - doorW, Y2 = oy + rh - doorW,
                    Stroke = brushAccent, StrokeThickness = 1.5
                };
                FurnitureCanvas.Children.Add(doorLeaf);

                // Cung mở cửa (Quarter circle arc)
                var doorArc = new Path
                {
                    Stroke = brushAccent,
                    StrokeThickness = 1.0,
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                var geom = new PathGeometry();
                var fig = new PathFigure { StartPoint = new System.Windows.Point(ox + rw, oy + rh) };
                fig.Segments.Add(new ArcSegment(new System.Windows.Point(ox + rw - doorW, oy + rh - doorW), new Size(doorW, doorW), 0, false, SweepDirection.Counterclockwise, true));
                geom.Figures.Add(fig);
                doorArc.Data = geom;
                FurnitureCanvas.Children.Add(doorArc);
            }

            // 2. VẼ TỔ HỢP NỘI THẤT CHUẨN TỈ LỆ THEO TỪNG LOẠI PHÒNG
            switch (cfg.RoomType)
            {
                case FurnitureRoomType.LivingRoom:
                    RenderLivingRoom(ox, oy, rw, rh, scale, brushFurn, brushAccent, brushRug, brushText);
                    break;
                case FurnitureRoomType.MasterBedroom:
                    RenderBedroom(ox, oy, rw, rh, scale, brushFurn, brushAccent, brushRug, brushText);
                    break;
                case FurnitureRoomType.KitchenDining:
                    RenderKitchen(ox, oy, rw, rh, scale, brushFurn, brushAccent, brushText);
                    break;
                case FurnitureRoomType.RestroomWc:
                    RenderBathroom(ox, oy, rw, rh, scale, brushFurn, brushAccent, brushText);
                    break;
                case FurnitureRoomType.HomeOffice:
                    RenderOffice(ox, oy, rw, rh, scale, brushFurn, brushAccent, brushText);
                    break;
            }

            // 3. ĐƯỜNG ĐO KÍCH THƯỚC TỔNG THỂ L x W
            if (cfg.DrawDimensions)
            {
                DrawDimension(FurnitureCanvas, ox, oy + rh + 18, ox + rw, oy + rh + 18, $"{roomL:F0} mm (L)", brushDim);
                DrawDimension(FurnitureCanvas, ox - 18, oy, ox - 18, oy + rh, $"{roomW:F0} mm (W)", brushDim, true);
            }
        }

        private void RenderLivingRoom(double ox, double oy, double rw, double rh, double scale, Brush brushFurn, Brush brushAccent, Brush brushRug, Brush brushText)
        {
            // Thảm phòng khách (3000 x 2200 mm) ở giữa
            double rugW = 3000 * scale;
            double rugH = 2200 * scale;
            var rug = new Rectangle
            {
                Width = rugW, Height = rugH,
                Stroke = brushRug, StrokeThickness = 1.2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(12, 56, 189, 248))
            };
            Canvas.SetLeft(rug, ox + (rw - rugW) / 2);
            Canvas.SetTop(rug, oy + (rh - rugH) / 2);
            FurnitureCanvas.Children.Add(rug);

            // 1. Sofa Góc Chữ L (2600 x 1800 x sâu 900 mm)
            double sofaX = ox + 40 * scale;
            double sofaY = oy + 40 * scale;
            double sw = 2600 * scale;
            double sh = 1800 * scale;
            double sd = 900 * scale;

            var sofaPoly = new Polygon
            {
                Stroke = brushFurn, StrokeThickness = 1.8,
                Fill = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)),
                Points = new PointCollection
                {
                    new System.Windows.Point(sofaX, sofaY),
                    new System.Windows.Point(sofaX + sw, sofaY),
                    new System.Windows.Point(sofaX + sw, sofaY + sd),
                    new System.Windows.Point(sofaX + sd, sofaY + sd),
                    new System.Windows.Point(sofaX + sd, sofaY + sh),
                    new System.Windows.Point(sofaX, sofaY + sh)
                }
            };
            FurnitureCanvas.Children.Add(sofaPoly);

            // Nét đệm lưng sofa
            var sofaBack1 = new Line { X1 = sofaX + 15 * scale, Y1 = sofaY + 15 * scale, X2 = sofaX + sw - 15 * scale, Y2 = sofaY + 15 * scale, Stroke = brushFurn, StrokeThickness = 1 };
            var sofaBack2 = new Line { X1 = sofaX + 15 * scale, Y1 = sofaY + 15 * scale, X2 = sofaX + 15 * scale, Y2 = sofaY + sh - 15 * scale, Stroke = brushFurn, StrokeThickness = 1 };
            FurnitureCanvas.Children.Add(sofaBack1);
            FurnitureCanvas.Children.Add(sofaBack2);

            // 2. Bàn trà đôi (1200 x 600 mm) bo góc
            double tableX = sofaX + sd + 25 * scale;
            double tableY = sofaY + sd + 15 * scale;
            var coffeeTable = new Rectangle
            {
                Width = 1200 * scale, Height = 600 * scale,
                RadiusX = 8, RadiusY = 8,
                Stroke = brushFurn, StrokeThickness = 1.5,
                Fill = new SolidColorBrush(Color.FromArgb(50, 245, 158, 11))
            };
            Canvas.SetLeft(coffeeTable, tableX);
            Canvas.SetTop(coffeeTable, tableY);
            FurnitureCanvas.Children.Add(coffeeTable);

            // 3. Kệ Tivi 2000x400 mm + Tivi 65 inch
            double tvX = ox + rw - 2200 * scale;
            double tvY = oy + rh - 450 * scale;
            var tvStand = new Rectangle
            {
                Width = 2000 * scale, Height = 400 * scale,
                Stroke = brushFurn, StrokeThickness = 1.5,
                Fill = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11))
            };
            Canvas.SetLeft(tvStand, tvX);
            Canvas.SetTop(tvStand, tvY);
            FurnitureCanvas.Children.Add(tvStand);

            // Màn hình TV 65" (nét đậm cyan)
            var tvScreen = new Line
            {
                X1 = tvX + 300 * scale, Y1 = tvY + 200 * scale,
                X2 = tvX + 1700 * scale, Y2 = tvY + 200 * scale,
                Stroke = brushAccent, StrokeThickness = 3
            };
            FurnitureCanvas.Children.Add(tvScreen);

            var txtTv = new TextBlock { Text = "KỆ TV 65\"", FontSize = 8.5, FontWeight = FontWeights.Bold, Foreground = brushText };
            Canvas.SetLeft(txtTv, tvX + 750 * scale);
            Canvas.SetTop(txtTv, tvY + 60 * scale);
            FurnitureCanvas.Children.Add(txtTv);
        }

        private void RenderBedroom(double ox, double oy, double rw, double rh, double scale, Brush brushFurn, Brush brushAccent, Brush brushRug, Brush brushText)
        {
            // 1. Giường King 1800 x 2000 mm
            double bedW = 1800 * scale;
            double bedH = 2000 * scale;
            double bedX = ox + (rw - bedW) / 2;
            double bedY = oy + 60 * scale;

            var bed = new Rectangle
            {
                Width = bedW, Height = bedH,
                Stroke = brushFurn, StrokeThickness = 1.8,
                Fill = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11))
            };
            Canvas.SetLeft(bed, bedX);
            Canvas.SetTop(bed, bedY);
            FurnitureCanvas.Children.Add(bed);

            // Đầu giường đệm (Headboard)
            var headboard = new Rectangle
            {
                Width = bedW + 200 * scale, Height = 120 * scale,
                Stroke = brushFurn, StrokeThickness = 1.5,
                Fill = new SolidColorBrush(Color.FromArgb(80, 245, 158, 11))
            };
            Canvas.SetLeft(headboard, bedX - 100 * scale);
            Canvas.SetTop(headboard, bedY - 120 * scale);
            FurnitureCanvas.Children.Add(headboard);

            // 2 Gối nằm 600x400
            double pillowW = 600 * scale;
            double pillowH = 400 * scale;
            var pillow1 = new Rectangle { Width = pillowW, Height = pillowH, RadiusX = 4, RadiusY = 4, Stroke = brushText, StrokeThickness = 1 };
            Canvas.SetLeft(pillow1, bedX + 150 * scale);
            Canvas.SetTop(pillow1, bedY + 80 * scale);
            FurnitureCanvas.Children.Add(pillow1);

            var pillow2 = new Rectangle { Width = pillowW, Height = pillowH, RadiusX = 4, RadiusY = 4, Stroke = brushText, StrokeThickness = 1 };
            Canvas.SetLeft(pillow2, bedX + bedW - 750 * scale);
            Canvas.SetTop(pillow2, bedY + 80 * scale);
            FurnitureCanvas.Children.Add(pillow2);

            // 2 Tab đầu giường 500x450
            double tabW = 500 * scale;
            double tabH = 450 * scale;
            var tabL = new Rectangle { Width = tabW, Height = tabH, Stroke = brushFurn, StrokeThickness = 1.2 };
            Canvas.SetLeft(tabL, bedX - tabW - 80 * scale);
            Canvas.SetTop(tabL, bedY);
            FurnitureCanvas.Children.Add(tabL);

            var tabR = new Rectangle { Width = tabW, Height = tabH, Stroke = brushFurn, StrokeThickness = 1.2 };
            Canvas.SetLeft(tabR, bedX + bedW + 80 * scale);
            Canvas.SetTop(tabR, bedY);
            FurnitureCanvas.Children.Add(tabR);

            // Đèn ngủ tròn trên Tab
            var lampL = new Ellipse { Width = 140 * scale, Height = 140 * scale, Stroke = brushAccent, StrokeThickness = 1.2 };
            Canvas.SetLeft(lampL, bedX - tabW / 2 - 150 * scale);
            Canvas.SetTop(lampL, bedY + 150 * scale);
            FurnitureCanvas.Children.Add(lampL);

            var lampR = new Ellipse { Width = 140 * scale, Height = 140 * scale, Stroke = brushAccent, StrokeThickness = 1.2 };
            Canvas.SetLeft(lampR, bedX + bedW + tabW / 2 + 10 * scale);
            Canvas.SetTop(lampR, bedY + 150 * scale);
            FurnitureCanvas.Children.Add(lampR);

            // 2. Tủ quần áo trượt lớn (2400 x 600 mm)
            double wardW = 2400 * scale;
            double wardH = 600 * scale;
            var ward = new Rectangle { Width = wardW, Height = wardH, Stroke = brushFurn, StrokeThickness = 1.5 };
            Canvas.SetLeft(ward, ox + rw - wardW - 50 * scale);
            Canvas.SetTop(ward, oy + rh - wardH - 50 * scale);
            FurnitureCanvas.Children.Add(ward);

            // Đường chia cánh tủ trượt
            var wardLine = new Line { X1 = ox + rw - wardW / 2 - 50 * scale, Y1 = oy + rh - wardH - 50 * scale, X2 = ox + rw - wardW / 2 - 50 * scale, Y2 = oy + rh - 50 * scale, Stroke = brushFurn, StrokeThickness = 1 };
            FurnitureCanvas.Children.Add(wardLine);

            var txtWard = new TextBlock { Text = "TỦ ÁO 2.4m", FontSize = 8.5, FontWeight = FontWeights.Bold, Foreground = brushText };
            Canvas.SetLeft(txtWard, ox + rw - wardW / 2 - 80 * scale);
            Canvas.SetTop(txtWard, oy + rh - wardH / 2 - 60 * scale);
            FurnitureCanvas.Children.Add(txtWard);
        }

        private void RenderKitchen(double ox, double oy, double rw, double rh, double scale, Brush brushFurn, Brush brushAccent, Brush brushText)
        {
            // 1. Tủ Bếp Góc Chữ L (3000 x 2400 x sâu 600)
            double cw = 3000 * scale;
            double ch = 2400 * scale;
            double cd = 600 * scale;

            var counter = new Polygon
            {
                Stroke = brushFurn, StrokeThickness = 1.8,
                Fill = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)),
                Points = new PointCollection
                {
                    new System.Windows.Point(ox + 40 * scale, oy + 40 * scale),
                    new System.Windows.Point(ox + 40 * scale + cw, oy + 40 * scale),
                    new System.Windows.Point(ox + 40 * scale + cw, oy + 40 * scale + cd),
                    new System.Windows.Point(ox + 40 * scale + cd, oy + 40 * scale + cd),
                    new System.Windows.Point(ox + 40 * scale + cd, oy + 40 * scale + ch),
                    new System.Windows.Point(ox + 40 * scale, oy + 40 * scale + ch)
                }
            };
            FurnitureCanvas.Children.Add(counter);

            // Chậu rửa đôi 800x450
            double sinkX = ox + 40 * scale + 1000 * scale;
            double sinkY = oy + 40 * scale + 75 * scale;
            var sink = new Rectangle { Width = 800 * scale, Height = 450 * scale, RadiusX = 4, RadiusY = 4, Stroke = brushAccent, StrokeThickness = 1.2 };
            Canvas.SetLeft(sink, sinkX);
            Canvas.SetTop(sink, sinkY);
            FurnitureCanvas.Children.Add(sink);

            // Bếp từ 4 vùng 750x450
            double stoveX = ox + 40 * scale + 2100 * scale;
            double stoveY = oy + 40 * scale + 75 * scale;
            var stove = new Rectangle { Width = 750 * scale, Height = 450 * scale, Stroke = new SolidColorBrush(Color.FromRgb(244, 63, 94)), StrokeThickness = 1.2 };
            Canvas.SetLeft(stove, stoveX);
            Canvas.SetTop(stove, stoveY);
            FurnitureCanvas.Children.Add(stove);

            // 2. Bàn ăn 6 ghế (1600 x 850 mm)
            double tableW = 1600 * scale;
            double tableH = 850 * scale;
            double tableX = ox + rw - tableW - 120 * scale;
            double tableY = oy + rh / 2 - tableH / 2;

            var table = new Rectangle { Width = tableW, Height = tableH, RadiusX = 6, RadiusY = 6, Stroke = brushFurn, StrokeThickness = 1.5, Fill = new SolidColorBrush(Color.FromArgb(50, 245, 158, 11)) };
            Canvas.SetLeft(table, tableX);
            Canvas.SetTop(table, tableY);
            FurnitureCanvas.Children.Add(table);

            // 6 ghế ăn 450x450
            double chairSize = 420 * scale;
            for (int i = 0; i < 3; i++)
            {
                double cx = tableX + (i * 500 + 80) * scale;
                var chairTop = new Rectangle { Width = chairSize, Height = chairSize, RadiusX = 3, RadiusY = 3, Stroke = brushFurn, StrokeThickness = 1 };
                Canvas.SetLeft(chairTop, cx);
                Canvas.SetTop(chairTop, tableY - chairSize - 20 * scale);
                FurnitureCanvas.Children.Add(chairTop);

                var chairBottom = new Rectangle { Width = chairSize, Height = chairSize, RadiusX = 3, RadiusY = 3, Stroke = brushFurn, StrokeThickness = 1 };
                Canvas.SetLeft(chairBottom, cx);
                Canvas.SetTop(chairBottom, tableY + tableH + 20 * scale);
                FurnitureCanvas.Children.Add(chairBottom);
            }

            var txtTable = new TextBlock { Text = "BÀN ĂN 6 GHẾ", FontSize = 8.5, FontWeight = FontWeights.Bold, Foreground = brushText };
            Canvas.SetLeft(txtTable, tableX + 450 * scale);
            Canvas.SetTop(txtTable, tableY + 350 * scale);
            FurnitureCanvas.Children.Add(txtTable);
        }

        private void RenderBathroom(double ox, double oy, double rw, double rh, double scale, Brush brushFurn, Brush brushAccent, Brush brushText)
        {
            // 1. Vách kính tắm đứng 1000 x 1000 mm ở góc trên trái
            double showerSize = 1000 * scale;
            var shower = new Rectangle
            {
                Width = showerSize, Height = showerSize,
                Stroke = brushAccent, StrokeThickness = 1.8,
                Fill = new SolidColorBrush(Color.FromArgb(20, 56, 189, 248))
            };
            Canvas.SetLeft(shower, ox + 40 * scale);
            Canvas.SetTop(shower, oy + 40 * scale);
            FurnitureCanvas.Children.Add(shower);

            // Ga thoát sàn tròn
            var drain = new Ellipse { Width = 100 * scale, Height = 100 * scale, Stroke = brushAccent, StrokeThickness = 1.2 };
            Canvas.SetLeft(drain, ox + 40 * scale + showerSize / 2 - 50 * scale);
            Canvas.SetTop(drain, oy + 40 * scale + showerSize / 2 - 50 * scale);
            FurnitureCanvas.Children.Add(drain);

            // 2. Bồn cầu 700x400 mm
            double wcW = 400 * scale;
            double wcH = 700 * scale;
            double wcX = ox + 40 * scale + showerSize + 250 * scale;
            double wcY = oy + 40 * scale;

            var tank = new Rectangle { Width = wcW, Height = 220 * scale, Stroke = brushAccent, StrokeThickness = 1.5 };
            Canvas.SetLeft(tank, wcX);
            Canvas.SetTop(tank, wcY);
            FurnitureCanvas.Children.Add(tank);

            var bowl = new Ellipse { Width = wcW, Height = 480 * scale, Stroke = brushAccent, StrokeThickness = 1.5 };
            Canvas.SetLeft(bowl, wcX);
            Canvas.SetTop(bowl, wcY + 220 * scale);
            FurnitureCanvas.Children.Add(bowl);

            // 3. Bàn Lavabo đá 1000x550 mm
            double lavaW = 1000 * scale;
            double lavaH = 550 * scale;
            double lavaX = ox + rw - lavaW - 50 * scale;
            double lavaY = oy + 40 * scale;

            var counter = new Rectangle { Width = lavaW, Height = lavaH, Stroke = brushAccent, StrokeThickness = 1.5 };
            Canvas.SetLeft(counter, lavaX);
            Canvas.SetTop(counter, lavaY);
            FurnitureCanvas.Children.Add(counter);

            var basin = new Ellipse { Width = 550 * scale, Height = 380 * scale, Stroke = brushAccent, StrokeThickness = 1.2 };
            Canvas.SetLeft(basin, lavaX + 225 * scale);
            Canvas.SetTop(basin, lavaY + 85 * scale);
            FurnitureCanvas.Children.Add(basin);

            var txtWc = new TextBlock { Text = "BÀN LAVABO ĐÁ", FontSize = 8.5, FontWeight = FontWeights.Bold, Foreground = brushText };
            Canvas.SetLeft(txtWc, lavaX + 200 * scale);
            Canvas.SetTop(txtWc, lavaY + lavaH + 20 * scale);
            FurnitureCanvas.Children.Add(txtWc);
        }

        private void RenderOffice(double ox, double oy, double rw, double rh, double scale, Brush brushFurn, Brush brushAccent, Brush brushText)
        {
            // 1. Bàn Giám Đốc (1800 x 850 mm)
            double deskW = 1800 * scale;
            double deskH = 850 * scale;
            double deskX = ox + (rw - deskW) / 2;
            double deskY = oy + 60 * scale;

            var desk = new Rectangle
            {
                Width = deskW, Height = deskH,
                Stroke = brushFurn, StrokeThickness = 1.8,
                Fill = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11))
            };
            Canvas.SetLeft(desk, deskX);
            Canvas.SetTop(desk, deskY);
            FurnitureCanvas.Children.Add(desk);

            // Màn hình máy tính & Laptop
            var pc = new Rectangle { Width = 550 * scale, Height = 120 * scale, Stroke = brushAccent, StrokeThickness = 1.5 };
            Canvas.SetLeft(pc, deskX + deskW / 2 - 275 * scale);
            Canvas.SetTop(pc, deskY + 150 * scale);
            FurnitureCanvas.Children.Add(pc);

            // Ghế xoay công thái học 600x600 mm
            double chairSize = 600 * scale;
            var chair = new Ellipse { Width = chairSize, Height = chairSize, Stroke = brushFurn, StrokeThickness = 1.5 };
            Canvas.SetLeft(chair, deskX + deskW / 2 - chairSize / 2);
            Canvas.SetTop(chair, deskY - chairSize - 40 * scale);
            FurnitureCanvas.Children.Add(chair);

            // 2 Ghế đón khách 500x500 mm phía trước bàn
            var guest1 = new Rectangle { Width = 500 * scale, Height = 500 * scale, RadiusX = 4, RadiusY = 4, Stroke = brushFurn, StrokeThickness = 1.2 };
            Canvas.SetLeft(guest1, deskX + 150 * scale);
            Canvas.SetTop(guest1, deskY + deskH + 60 * scale);
            FurnitureCanvas.Children.Add(guest1);

            var guest2 = new Rectangle { Width = 500 * scale, Height = 500 * scale, RadiusX = 4, RadiusY = 4, Stroke = brushFurn, StrokeThickness = 1.2 };
            Canvas.SetLeft(guest2, deskX + deskW - 650 * scale);
            Canvas.SetTop(guest2, deskY + deskH + 60 * scale);
            FurnitureCanvas.Children.Add(guest2);

            // 2. Kệ tủ tài liệu / kệ sách lớn (2000 x 400 mm)
            double bookW = 2000 * scale;
            double bookH = 400 * scale;
            var book = new Rectangle { Width = bookW, Height = bookH, Stroke = brushFurn, StrokeThickness = 1.5 };
            Canvas.SetLeft(book, ox + rw - bookW - 50 * scale);
            Canvas.SetTop(book, oy + rh - bookH - 50 * scale);
            FurnitureCanvas.Children.Add(book);

            var txtBook = new TextBlock { Text = "TỦ TÀI LIỆU 2.0m", FontSize = 8.5, FontWeight = FontWeights.Bold, Foreground = brushText };
            Canvas.SetLeft(txtBook, ox + rw - bookW / 2 - 100 * scale);
            Canvas.SetTop(txtBook, oy + rh - bookH / 2 - 50 * scale);
            FurnitureCanvas.Children.Add(txtBook);
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
                Canvas.SetLeft(txt, (x1 + x2) / 2 - 25);
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
                Canvas.SetLeft(txt, x1 - 50);
                Canvas.SetTop(txt, (y1 + y2) / 2 - 6);
                canvas.Children.Add(txt);
            }
        }
    }
}
