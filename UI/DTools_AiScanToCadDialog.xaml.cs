using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DTT_AutoCAD_Plugin.Modules;
using WinForms = System.Windows.Forms;

namespace DTT_AutoCAD_Plugin.UI
{
    public partial class DTools_AiScanToCadDialog : Window
    {
        public AiCadReconstructionData ResultData { get; private set; }
        private double _zoomFactor = 1.0;
        private string _selectedImagePath = "";

        public DTools_AiScanToCadDialog()
        {
            InitializeComponent();
            Loaded += DTools_AiScanToCadDialog_Loaded;
        }

        private void DTools_AiScanToCadDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var models = AiRasterToCadEngine.GetInstalledOllamaModels();
                CboOllamaModel.Items.Clear();
                if (models != null && models.Count > 0)
                {
                    foreach (var m in models)
                    {
                        CboOllamaModel.Items.Add(m);
                    }

                    string best = models.Find(m => m.IndexOf("vision", StringComparison.OrdinalIgnoreCase) >= 0 
                                                || m.IndexOf("moondream", StringComparison.OrdinalIgnoreCase) >= 0
                                                || m.IndexOf("llava", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!string.IsNullOrEmpty(best))
                        CboOllamaModel.SelectedItem = best;
                    else
                        CboOllamaModel.SelectedIndex = 0;
                }
                else
                {
                    CboOllamaModel.Items.Add("llama3.2-vision:latest");
                    CboOllamaModel.Items.Add("moondream");
                    CboOllamaModel.Items.Add("llava");
                    CboOllamaModel.SelectedIndex = 0;
                }
            }
            catch
            {
                CboOllamaModel.Items.Add("llama3.2-vision:latest");
                CboOllamaModel.SelectedIndex = 0;
            }
        }

        public static AiCadReconstructionData PromptReconstruction()
        {
            var dlg = new DTools_AiScanToCadDialog();
            bool? res = dlg.ShowDialog();
            if (res == true && dlg.ResultData != null)
            {
                return dlg.ResultData;
            }
            return null;
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Mode_Changed(object sender, RoutedEventArgs e)
        {
            if (PanelCvSettings == null || PanelOllamaSettings == null) return;

            bool isCv = RbComputerVision.IsChecked == true;
            PanelCvSettings.Visibility = isCv ? Visibility.Visible : Visibility.Collapsed;
            PanelOllamaSettings.Visibility = isCv ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var ofd = new WinForms.OpenFileDialog())
            {
                ofd.Title = "Chọn file ảnh bản vẽ kỹ thuật hoặc mặt bằng";
                ofd.Filter = "Ảnh bản vẽ (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Tất cả tệp (*.*)|*.*";
                if (ofd.ShowDialog() == WinForms.DialogResult.OK)
                {
                    _selectedImagePath = ofd.FileName;
                    TxtFilePath.Text = _selectedImagePath;
                }
            }
        }

        private void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RbComputerVision.IsChecked == true)
                {
                    if (string.IsNullOrWhiteSpace(_selectedImagePath) || !File.Exists(_selectedImagePath))
                    {
                        MessageBox.Show("Vui lòng chọn file ảnh bản vẽ hợp lệ trước khi phân tích!", "DTools Thông Báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    double scale = ParseDouble(TxtScaleMm.Text, 15.0);
                    int thresh = (int)ParseDouble(TxtThreshold.Text, 160.0);
                    int minLen = (int)ParseDouble(TxtMinLength.Text, 25.0);
                    bool snap = ChkOrthoSnap.IsChecked == true;

                    TxtStats.Text = "⏳ Đang phân tích ma trận điểm ảnh và vector hóa...";
                    ResultData = AiRasterToCadEngine.VectorizeImageBitmap(_selectedImagePath, scale, thresh, minLen, snap);
                }
                else
                {
                    string json = TxtJsonInput.Text != null ? TxtJsonInput.Text.Trim() : "";

                    // Nếu người dùng chưa có JSON hoặc đang dán System Prompt
                    if (string.IsNullOrWhiteSpace(json) || !json.StartsWith("{"))
                    {
                        var ask = MessageBox.Show(
                            "Chưa có dữ liệu JSON từ AI Vision (hoặc bạn đang dán System Prompt mẫu).\n\n" +
                            "👉 Bạn có muốn chuyển sang chế độ '⚡ Computer Vision Vectorizer' để quét nét tự động trực tiếp từ ảnh ngay bây giờ không?",
                            "DTools Gợi Ý Xử Lý Nhanh",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (ask == MessageBoxResult.Yes)
                        {
                            RbComputerVision.IsChecked = true;
                            Mode_Changed(null, null);
                            BtnAnalyze_Click(sender, e);
                            return;
                        }
                        return;
                    }

                    double scale = ParseDouble(TxtScaleMm.Text, 15.0);
                    ResultData = AiRasterToCadEngine.ParseOllamaVisionJson(json, scale);
                }

                RenderPreview();

                if (ResultData != null && (ResultData.Walls.Count > 0 || ResultData.Doors.Count > 0 || ResultData.Texts.Count > 0))
                {
                    BtnInjectCad.IsEnabled = true;
                    TxtStats.Text = $"✅ Phát hiện: {ResultData.Walls.Count} nét tường, {ResultData.Doors.Count} cửa, {ResultData.Windows.Count} cửa sổ, {ResultData.Texts.Count} nhãn chữ.";
                }
                else
                {
                    TxtStats.Text = "⚠️ Không tìm thấy đối tượng nào. Hãy thử điều chỉnh lại ngưỡng nhị phân hoặc kiểm tra file ảnh.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi phân tích: {ex.Message}", "DTools Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStats.Text = "❌ Lỗi: " + ex.Message;
            }
        }

        private void BtnCallOllama_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_selectedImagePath) || !File.Exists(_selectedImagePath))
                {
                    MessageBox.Show("Vui lòng chọn file ảnh bản vẽ trước khi gọi Ollama!", "DTools Thông Báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string url = TxtOllamaUrl.Text.Trim();
                string model = CboOllamaModel.Text != null ? CboOllamaModel.Text.Trim() : "";
                if (string.IsNullOrEmpty(model)) model = "llama3.2-vision";

                TxtStats.Text = "⏳ Đang gửi ảnh tới Ollama Vision AI (vui lòng chờ 10-30 giây)...";
                string jsonResp = AiRasterToCadEngine.CallLocalOllamaVision(_selectedImagePath, url, model);

                TxtJsonInput.Text = jsonResp;
                TxtStats.Text = "✅ Đã nhận phản hồi từ Ollama! Đang tự động phân tích vector...";

                BtnAnalyze_Click(sender, e);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                var ask = MessageBox.Show(
                    $"{msg}\n\n" +
                    "👉 BẠN CÓ MUỐN CHUYỂN SANG QUÉT NÉT NGAY BÂY GIỜ KHÔNG?\n" +
                    "Chọn 'Yes' để tự động dùng '⚡ Computer Vision Vectorizer' quét trực tiếp file ảnh này mà KHÔNG CẦN bất kỳ mô hình AI nào!",
                    "DTools - Thông Báo Xử Lý",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (ask == MessageBoxResult.Yes)
                {
                    RbComputerVision.IsChecked = true;
                    Mode_Changed(null, null);
                    BtnAnalyze_Click(sender, e);
                }
                TxtStats.Text = "❌ " + msg;
            }
        }

        private void BtnPasteJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    TxtJsonInput.Text = Clipboard.GetText();
                    BtnAnalyze_Click(sender, e);
                }
            }
            catch { }
        }

        private void BtnCopyPrompt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(AiRasterToCadEngine.OllamaVisionSystemPrompt);
                MessageBox.Show("Đã sao chép System Prompt chuẩn của Ollama Vision vào Clipboard!\nBạn có thể dán vào Ollama WebUI hoặc script Python của mình.", "DTools Pro", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }
        }

        private void RenderPreview()
        {
            if (PreviewCanvas == null || ResultData == null) return;
            PreviewCanvas.Children.Clear();

            double cw = PreviewCanvas.ActualWidth;
            double ch = PreviewCanvas.ActualHeight;
            if (cw <= 0 || ch <= 0) return;

            // Tìm bounding box tổng thể
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var w in ResultData.Walls)
            {
                minX = Math.Min(minX, Math.Min(w.Start.X, w.End.X));
                minY = Math.Min(minY, Math.Min(w.Start.Y, w.End.Y));
                maxX = Math.Max(maxX, Math.Max(w.Start.X, w.End.X));
                maxY = Math.Max(maxY, Math.Max(w.Start.Y, w.End.Y));
            }

            if (minX == double.MaxValue)
            {
                minX = 0; minY = 0; maxX = 10000; maxY = 8000;
            }

            double dataW = Math.Max(100.0, maxX - minX);
            double dataH = Math.Max(100.0, maxY - minY);

            double scale = Math.Min((cw - 40.0) / dataW, (ch - 40.0) / dataH) * _zoomFactor;
            double ox = (cw - (dataW * scale)) / 2.0;
            double oy = (ch - (dataH * scale)) / 2.0;

            // Vẽ Walls (Nét xanh biển / cyan)
            foreach (var w in ResultData.Walls)
            {
                double x1 = ox + (w.Start.X - minX) * scale;
                double y1 = ch - (oy + (w.Start.Y - minY) * scale);
                double x2 = ox + (w.End.X - minX) * scale;
                double y2 = ch - (oy + (w.End.Y - minY) * scale);

                var line = new Line
                {
                    X1 = x1, Y1 = y1,
                    X2 = x2, Y2 = y2,
                    Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    StrokeThickness = 1.8
                };
                PreviewCanvas.Children.Add(line);
            }

            // Vẽ Doors (Nét vàng)
            foreach (var d in ResultData.Doors)
            {
                double dx = ox + (d.InsertPoint.X - minX) * scale;
                double dy = ch - (oy + (d.InsertPoint.Y - minY) * scale);
                double dw = Math.Max(10.0, d.Width * scale);

                var arcBox = new Rectangle
                {
                    Width = dw, Height = dw,
                    Stroke = new SolidColorBrush(Color.FromRgb(250, 204, 21)),
                    StrokeThickness = 1.2
                };
                Canvas.SetLeft(arcBox, dx);
                Canvas.SetTop(arcBox, dy - dw);
                PreviewCanvas.Children.Add(arcBox);
            }

            // Vẽ Windows (Nét xanh lá)
            foreach (var win in ResultData.Windows)
            {
                double wx1 = ox + (win.Start.X - minX) * scale;
                double wy1 = ch - (oy + (win.Start.Y - minY) * scale);
                double wx2 = ox + (win.End.X - minX) * scale;
                double wy2 = ch - (oy + (win.End.Y - minY) * scale);

                var wLine = new Line
                {
                    X1 = wx1, Y1 = wy1,
                    X2 = wx2, Y2 = wy2,
                    Stroke = new SolidColorBrush(Color.FromRgb(74, 222, 128)),
                    StrokeThickness = 2.5
                };
                PreviewCanvas.Children.Add(wLine);
            }

            // Vẽ Texts
            foreach (var txt in ResultData.Texts)
            {
                double tx = ox + (txt.Position.X - minX) * scale;
                double ty = ch - (oy + (txt.Position.Y - minY) * scale);

                var tb = new TextBlock
                {
                    Text = txt.Content,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249))
                };
                Canvas.SetLeft(tb, tx);
                Canvas.SetTop(tb, ty - 12);
                PreviewCanvas.Children.Add(tb);
            }
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = Math.Min(4.0, _zoomFactor * 1.25);
            RenderPreview();
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = Math.Max(0.25, _zoomFactor / 1.25);
            RenderPreview();
        }

        private void BtnZoomReset_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = 1.0;
            RenderPreview();
        }

        private void BtnInjectCad_Click(object sender, RoutedEventArgs e)
        {
            if (ResultData == null || (ResultData.Walls.Count == 0 && ResultData.Doors.Count == 0 && ResultData.Texts.Count == 0))
            {
                MessageBox.Show("Chưa có nét vẽ nào được phân tích để chèn vào AutoCAD!", "DTools Thông Báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
