using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DTT_AutoCAD_Plugin.UI
{
    public enum PreviewKind
    {
        ThreePhaseWire,
        Wall,
        Door,
        Window,
        BeamSection,
        Rebar,
        Stirrup,
        PanelBoard,
        SingleLine,
        ElectricalDevice,
        DrainPipe,
        Duct,
        Generic
    }

    public class ParameterField
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string DefaultValue { get; set; }
        public string Unit { get; set; }
        public string[] Options { get; set; }

        public ParameterField(string key, string label, string defaultValue, string unit = null, string[] options = null)
        {
            Key = key;
            Label = label;
            DefaultValue = defaultValue;
            Unit = unit;
            Options = options;
        }
    }

    public class ParameterResult
    {
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string GetString(string key, string fallback = "")
        {
            if (Values.TryGetValue(key, out string val) && !string.IsNullOrWhiteSpace(val))
                return val;
            return fallback;
        }

        public double GetDouble(string key, double fallback = 0.0)
        {
            if (Values.TryGetValue(key, out string val) &&
                double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double res))
            {
                return res;
            }
            return fallback;
        }

        public int GetInt(string key, int fallback = 0)
        {
            if (Values.TryGetValue(key, out string val) &&
                int.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out int res))
            {
                return res;
            }
            return fallback;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            if (Values.TryGetValue(key, out string val))
            {
                string s = val.Trim().ToLowerInvariant();
                if (s == "có" || s == "true" || s == "yes" || s == "1") return true;
                if (s == "không" || s == "false" || s == "no" || s == "0") return false;
            }
            return fallback;
        }
    }

    public partial class DTools_FloatingDialog : Window
    {
        private readonly List<ParameterField> _fields = new List<ParameterField>();
        private readonly Dictionary<string, Control> _controls = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase);
        public PreviewKind CurrentPreview { get; set; } = PreviewKind.Generic;
        public ParameterResult Result { get; private set; }

        public DTools_FloatingDialog()
        {
            InitializeComponent();
            Loaded += DTools_FloatingDialog_Loaded;
        }

        public static ParameterResult Prompt(string title, string subtitle, string icon, PreviewKind preview, IEnumerable<ParameterField> fields)
        {
            var dlg = new DTools_FloatingDialog();
            dlg.TxtTitle.Text = title;
            dlg.TxtSubtitle.Text = subtitle;
            dlg.TxtIcon.Text = icon;
            dlg.CurrentPreview = preview;

            foreach (var f in fields)
            {
                dlg.AddField(f);
            }

            bool? dr = dlg.ShowDialog();
            if (dr == true)
            {
                return dlg.Result;
            }
            return null;
        }

        public void AddField(ParameterField field)
        {
            _fields.Add(field);

            StackPanel group = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

            // Label
            string labelText = string.IsNullOrEmpty(field.Unit) ? field.Label : $"{field.Label} ({field.Unit})";
            TextBlock lbl = new TextBlock
            {
                Text = labelText,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // #1E293B
                Margin = new Thickness(0, 0, 0, 3)
            };
            group.ControlsAdd(lbl);

            if (field.Options != null && field.Options.Length > 0)
            {
                ComboBox cmb = new ComboBox
                {
                    Height = 30,
                    FontSize = 11,
                    FontWeight = FontWeights.Medium,
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    Padding = new Thickness(6, 2, 6, 2)
                };

                int selectedIdx = 0;
                for (int i = 0; i < field.Options.Length; i++)
                {
                    cmb.Items.Add(field.Options[i]);
                    if (string.Equals(field.Options[i], field.DefaultValue, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIdx = i;
                    }
                }
                cmb.SelectedIndex = selectedIdx;
                cmb.SelectionChanged += (s, e) => UpdatePreview();

                _controls[field.Key] = cmb;
                group.ControlsAdd(cmb);
            }
            else
            {
                TextBox txt = new TextBox
                {
                    Text = field.DefaultValue ?? "",
                    Height = 30,
                    FontSize = 11.5,
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    Padding = new Thickness(8, 4, 8, 4),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                txt.TextChanged += (s, e) => UpdatePreview();

                _controls[field.Key] = txt;
                group.ControlsAdd(txt);
            }

            PanelInputs.Children.Add(group);
        }

        private void DTools_FloatingDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Auto focus on the first input field & select text immediately
            if (_fields.Count > 0 && _controls.TryGetValue(_fields[0].Key, out Control firstCtrl))
            {
                firstCtrl.Focus();
                if (firstCtrl is TextBox tb)
                {
                    tb.SelectAll();
                }
            }

            UpdatePreview();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnApply_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                BtnCancel_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            Result = new ParameterResult();
            foreach (var f in _fields)
            {
                if (_controls.TryGetValue(f.Key, out Control ctrl))
                {
                    if (ctrl is TextBox tb)
                    {
                        Result.Values[f.Key] = tb.Text.Trim();
                    }
                    else if (ctrl is ComboBox cmb)
                    {
                        Result.Values[f.Key] = cmb.SelectedItem?.ToString().Trim() ?? "";
                    }
                }
            }

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private string GetVal(string key, string fallback = "")
        {
            if (_controls.TryGetValue(key, out Control ctrl))
            {
                if (ctrl is TextBox tb) return tb.Text.Trim();
                if (ctrl is ComboBox cmb) return cmb.SelectedItem?.ToString().Trim() ?? fallback;
            }
            return fallback;
        }

        private double GetNum(string key, double fallback = 0.0)
        {
            string s = GetVal(key);
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                return val;
            return fallback;
        }

        private int GetInt(string key, int fallback = 0)
        {
            return (int)GetNum(key, fallback);
        }

        // ============================================================
        // LIVE VECTOR PREVIEW ENGINE (WPF CANVAS RENDERER)
        // ============================================================

        public void UpdatePreview()
        {
            if (PreviewCanvas == null) return;
            PreviewCanvas.Children.Clear();

            double cw = PreviewCanvas.Width;
            double ch = PreviewCanvas.Height;

            switch (CurrentPreview)
            {
                case PreviewKind.ThreePhaseWire:
                    RenderThreePhasePreview(cw, ch);
                    break;
                case PreviewKind.Wall:
                    RenderWallPreview(cw, ch);
                    break;
                case PreviewKind.Door:
                    RenderDoorPreview(cw, ch);
                    break;
                case PreviewKind.Window:
                    RenderWindowPreview(cw, ch);
                    break;
                case PreviewKind.BeamSection:
                    RenderBeamSectionPreview(cw, ch);
                    break;
                case PreviewKind.Rebar:
                    RenderRebarPreview(cw, ch);
                    break;
                case PreviewKind.PanelBoard:
                    RenderPanelBoardPreview(cw, ch);
                    break;
                default:
                    RenderGenericPreview(cw, ch);
                    break;
            }
        }

        private void RenderThreePhasePreview(double cw, double ch)
        {
            string mode = GetVal("Mode", "1");
            string section = GetVal("Section", "4x6+1x4 mm²");

            if (mode.Contains("2") || mode.Contains("Song Song") || mode.Contains("3 Dây"))
            {
                // VẼ 3 DÂY SONG SONG MÀU PHA CHUẨN
                double spacing = 28.0;
                double yCenter = ch / 2 - 14;

                Color[] phaseColors = new[] {
                    Color.FromRgb(239, 68, 68),   // L1: Đỏ
                    Color.FromRgb(234, 179, 8),   // L2: Vàng
                    Color.FromRgb(16, 185, 129),  // L3: Xanh lục
                    Color.FromRgb(59, 130, 246),  // N: Xanh dương
                    Color.FromRgb(132, 204, 22)   // PE: Vàng xanh
                };
                string[] phaseLabels = new[] { "L1 (A)", "L2 (B)", "L3 (C)", "N", "PE" };

                int count = mode.Contains("5") ? 5 : (mode.Contains("4") ? 4 : 3);
                double startY = ch / 2 - (count * spacing) / 2.0;

                for (int i = 0; i < count; i++)
                {
                    double y = startY + i * spacing;
                    Line wire = new Line
                    {
                        X1 = 20,
                        Y1 = y,
                        X2 = cw - 50,
                        Y2 = y,
                        Stroke = new SolidColorBrush(phaseColors[i % phaseColors.Length]),
                        StrokeThickness = 2.5
                    };
                    PreviewCanvas.Children.Add(wire);

                    TextBlock tag = new TextBlock
                    {
                        Text = phaseLabels[i],
                        FontSize = 9,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(phaseColors[i % phaseColors.Length])
                    };
                    Canvas.SetLeft(tag, cw - 45);
                    Canvas.SetTop(tag, y - 7);
                    PreviewCanvas.Children.Add(tag);
                }
            }
            else
            {
                // VẼ CÁP CHÍNH KÈM VẠCH KÝ HIỆU 3P+N+PE
                Line mainCable = new Line
                {
                    X1 = 20,
                    Y1 = ch / 2,
                    X2 = cw - 20,
                    Y2 = ch / 2,
                    Stroke = new SolidColorBrush(Color.FromRgb(14, 165, 233)),
                    StrokeThickness = 3
                };
                PreviewCanvas.Children.Add(mainCable);

                double mx = cw / 2;
                double my = ch / 2;

                // 3 vạch pha L1, L2, L3 (///)
                for (int i = -1; i <= 1; i++)
                {
                    Line tick = new Line
                    {
                        X1 = mx + i * 12 - 6,
                        Y1 = my + 14,
                        X2 = mx + i * 12 + 6,
                        Y2 = my - 14,
                        Stroke = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                        StrokeThickness = 2
                    };
                    PreviewCanvas.Children.Add(tick);
                }

                // Vạch Trung Tính N (vạch có chấm)
                double nx = mx + 24;
                Line nTick = new Line
                {
                    X1 = nx - 6,
                    Y1 = my + 14,
                    X2 = nx + 6,
                    Y2 = my - 14,
                    Stroke = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    StrokeThickness = 2
                };
                PreviewCanvas.Children.Add(nTick);

                Ellipse dot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246))
                };
                Canvas.SetLeft(dot, nx + 3);
                Canvas.SetTop(dot, my - 17);
                PreviewCanvas.Children.Add(dot);

                // Vạch Nối Đất PE (vạch có gạch ngang)
                double px = mx + 38;
                Line peTick = new Line
                {
                    X1 = px - 6,
                    Y1 = my + 14,
                    X2 = px + 6,
                    Y2 = my - 14,
                    Stroke = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    StrokeThickness = 2
                };
                PreviewCanvas.Children.Add(peTick);

                Line peCross = new Line
                {
                    X1 = px + 2,
                    Y1 = my - 16,
                    X2 = px + 10,
                    Y2 = my - 8,
                    Stroke = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    StrokeThickness = 2
                };
                PreviewCanvas.Children.Add(peCross);

                // Nhãn thông số dây
                TextBlock lblSec = new TextBlock
                {
                    Text = $"3P+N+PE ({section})",
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
                };
                Canvas.SetLeft(lblSec, mx - 40);
                Canvas.SetTop(lblSec, my + 20);
                PreviewCanvas.Children.Add(lblSec);
            }
        }

        private void RenderWallPreview(double cw, double ch)
        {
            double thick = GetNum("Thick", 220);
            double visualThick = Math.Max(12, Math.Min(40, thick * 0.15));

            double y1 = ch / 2 - visualThick / 2;
            double y2 = ch / 2 + visualThick / 2;

            Rectangle wallBg = new Rectangle
            {
                Width = cw - 30,
                Height = visualThick,
                Fill = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11))
            };
            Canvas.SetLeft(wallBg, 15);
            Canvas.SetTop(wallBg, y1);
            PreviewCanvas.Children.Add(wallBg);

            Line l1 = new Line { X1 = 15, Y1 = y1, X2 = cw - 15, Y2 = y1, Stroke = new SolidColorBrush(Color.FromRgb(217, 119, 6)), StrokeThickness = 2 };
            Line l2 = new Line { X1 = 15, Y1 = y2, X2 = cw - 15, Y2 = y2, Stroke = new SolidColorBrush(Color.FromRgb(217, 119, 6)), StrokeThickness = 2 };
            PreviewCanvas.Children.Add(l1);
            PreviewCanvas.Children.Add(l2);

            // Hatch nét gạch
            for (double x = 25; x < cw - 25; x += 18)
            {
                Line hatch = new Line { X1 = x, Y1 = y2, X2 = x + 12, Y2 = y1, Stroke = new SolidColorBrush(Color.FromArgb(160, 217, 119, 6)), StrokeThickness = 1 };
                PreviewCanvas.Children.Add(hatch);
            }

            TextBlock tag = new TextBlock
            {
                Text = $"TƯỜNG DÀY {thick:0} mm",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9))
            };
            Canvas.SetLeft(tag, cw / 2 - 45);
            Canvas.SetTop(tag, y2 + 10);
            PreviewCanvas.Children.Add(tag);
        }

        private void RenderDoorPreview(double cw, double ch)
        {
            double w = GetNum("Width", 900);
            double doorW = 85.0;

            // Tường 2 bên
            Line w1 = new Line { X1 = 20, Y1 = ch / 2, X2 = 50, Y2 = ch / 2, Stroke = new SolidColorBrush(Color.FromRgb(100, 116, 139)), StrokeThickness = 5 };
            Line w2 = new Line { X1 = 50 + doorW, Y1 = ch / 2, X2 = cw - 20, Y2 = ch / 2, Stroke = new SolidColorBrush(Color.FromRgb(100, 116, 139)), StrokeThickness = 5 };
            PreviewCanvas.Children.Add(w1);
            PreviewCanvas.Children.Add(w2);

            // Cánh cửa mở 90 độ
            Line leaf = new Line
            {
                X1 = 50,
                Y1 = ch / 2,
                X2 = 50,
                Y2 = ch / 2 - doorW,
                Stroke = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                StrokeThickness = 3
            };
            PreviewCanvas.Children.Add(leaf);

            // Cung mở cửa
            PathGeometry geom = new PathGeometry();
            PathFigure fig = new PathFigure { StartPoint = new System.Windows.Point(50, ch / 2 - doorW) };
            fig.Segments.Add(new ArcSegment(new System.Windows.Point(50 + doorW, ch / 2), new Size(doorW, doorW), 0, false, SweepDirection.Clockwise, true));
            geom.Figures.Add(fig);

            Path arc = new Path
            {
                Data = geom,
                Stroke = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 3, 2 }
            };
            PreviewCanvas.Children.Add(arc);

            TextBlock tag = new TextBlock
            {
                Text = $"CỬA ĐI D{w:0} (RỘNG {w:0}mm)",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105))
            };
            Canvas.SetLeft(tag, 40);
            Canvas.SetTop(tag, ch / 2 + 18);
            PreviewCanvas.Children.Add(tag);
        }

        private void RenderWindowPreview(double cw, double ch)
        {
            double w = GetNum("Width", 1200);
            double rw = 120;
            double rh = 50;
            double rx = (cw - rw) / 2;
            double ry = (ch - rh) / 2;

            // Khung ngoài
            Rectangle frame = new Rectangle
            {
                Width = rw,
                Height = rh,
                Stroke = new SolidColorBrush(Color.FromRgb(2, 132, 199)),
                StrokeThickness = 2.5,
                Fill = new SolidColorBrush(Color.FromArgb(30, 14, 165, 233))
            };
            Canvas.SetLeft(frame, rx);
            Canvas.SetTop(frame, ry);
            PreviewCanvas.Children.Add(frame);

            // Chia cánh (2 hoặc 3 cánh)
            Line split = new Line
            {
                X1 = rx + rw / 2,
                Y1 = ry,
                X2 = rx + rw / 2,
                Y2 = ry + rh,
                Stroke = new SolidColorBrush(Color.FromRgb(2, 132, 199)),
                StrokeThickness = 2
            };
            PreviewCanvas.Children.Add(split);

            // Gờ tường 2 bên
            Line g1 = new Line { X1 = rx - 20, Y1 = ry + rh / 2, X2 = rx, Y2 = ry + rh / 2, Stroke = new SolidColorBrush(Color.FromRgb(148, 163, 184)), StrokeThickness = 6 };
            Line g2 = new Line { X1 = rx + rw, Y1 = ry + rh / 2, X2 = rx + rw + 20, Y2 = ry + rh / 2, Stroke = new SolidColorBrush(Color.FromRgb(148, 163, 184)), StrokeThickness = 6 };
            PreviewCanvas.Children.Add(g1);
            PreviewCanvas.Children.Add(g2);

            TextBlock tag = new TextBlock
            {
                Text = $"CỬA SỔ S{w:0} ({w:0}mm)",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(3, 105, 161))
            };
            Canvas.SetLeft(tag, rx + 10);
            Canvas.SetTop(tag, ry + rh + 12);
            PreviewCanvas.Children.Add(tag);
        }

        private void RenderBeamSectionPreview(double cw, double ch)
        {
            double b = GetNum("Width", 200);
            double h = GetNum("Height", 350);

            double scale = Math.Min(110.0 / Math.Max(h, 1.0), 90.0 / Math.Max(b, 1.0));
            double rw = b * scale;
            double rh = h * scale;
            double rx = (cw - rw) / 2;
            double ry = (ch - rh) / 2 - 10;

            // Bê tông ngoài
            Rectangle concrete = new Rectangle
            {
                Width = rw,
                Height = rh,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(20, 148, 163, 184))
            };
            Canvas.SetLeft(concrete, rx);
            Canvas.SetTop(concrete, ry);
            PreviewCanvas.Children.Add(concrete);

            // Thép đai
            Rectangle stirrup = new Rectangle
            {
                Width = Math.Max(10, rw - 14),
                Height = Math.Max(10, rh - 14),
                Stroke = new SolidColorBrush(Color.FromRgb(217, 70, 239)),
                StrokeThickness = 1.8
            };
            Canvas.SetLeft(stirrup, rx + 7);
            Canvas.SetTop(stirrup, ry + 7);
            PreviewCanvas.Children.Add(stirrup);

            // 4 Thép chủ góc
            double[][] corners = new double[][] {
                new double[] { rx + 9, ry + 9 },
                new double[] { rx + rw - 15, ry + 9 },
                new double[] { rx + 9, ry + rh - 15 },
                new double[] { rx + rw - 15, ry + rh - 15 }
            };
            foreach (var pt in corners)
            {
                Ellipse rebar = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68))
                };
                Canvas.SetLeft(rebar, pt[0]);
                Canvas.SetTop(rebar, pt[1]);
                PreviewCanvas.Children.Add(rebar);
            }

            TextBlock tag = new TextBlock
            {
                Text = $"DẦM {b:0}x{h:0} mm",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
            };
            Canvas.SetLeft(tag, cw / 2 - 35);
            Canvas.SetTop(tag, ry + rh + 10);
            PreviewCanvas.Children.Add(tag);
        }

        private void RenderRebarPreview(double cw, double ch)
        {
            double d = GetNum("Dia", 18);
            double hookAngle = GetNum("Angle", 135);

            double y = ch / 2;
            Line bar = new Line
            {
                X1 = 45,
                Y1 = y,
                X2 = cw - 45,
                Y2 = y,
                Stroke = new SolidColorBrush(Color.FromRgb(168, 85, 247)),
                StrokeThickness = Math.Max(2, d * 0.2)
            };
            PreviewCanvas.Children.Add(bar);

            // Móc 135 độ
            double hookLen = 22.0;
            Line h1 = new Line
            {
                X1 = 45,
                Y1 = y,
                X2 = 45 - hookLen * 0.7,
                Y2 = y - hookLen * 0.7,
                Stroke = new SolidColorBrush(Color.FromRgb(168, 85, 247)),
                StrokeThickness = bar.StrokeThickness
            };
            Line h2 = new Line
            {
                X1 = cw - 45,
                Y1 = y,
                X2 = cw - 45 + hookLen * 0.7,
                Y2 = y - hookLen * 0.7,
                Stroke = new SolidColorBrush(Color.FromRgb(168, 85, 247)),
                StrokeThickness = bar.StrokeThickness
            };
            PreviewCanvas.Children.Add(h1);
            PreviewCanvas.Children.Add(h2);

            TextBlock tag = new TextBlock
            {
                Text = $"THÉP D{d:0} - MÓC {hookAngle:0}° KHÁNG CHẤN",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(126, 34, 206))
            };
            Canvas.SetLeft(tag, 15);
            Canvas.SetTop(tag, y + 20);
            PreviewCanvas.Children.Add(tag);
        }

        private void RenderPanelBoardPreview(double cw, double ch)
        {
            double w = GetNum("Width", 400);
            double h = GetNum("Height", 600);
            int circ = GetInt("Circuits", 8);

            double rw = 90;
            double rh = 120;
            double rx = (cw - rw) / 2;
            double ry = (ch - rh) / 2 - 5;

            Rectangle outer = new Rectangle
            {
                Width = rw,
                Height = rh,
                Stroke = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(20, 245, 158, 11))
            };
            Canvas.SetLeft(outer, rx);
            Canvas.SetTop(outer, ry);
            PreviewCanvas.Children.Add(outer);

            // Gạch chéo nửa tủ báo hiệu tủ điện
            Line diag = new Line { X1 = rx, Y1 = ry + rh, X2 = rx + rw, Y2 = ry, Stroke = new SolidColorBrush(Color.FromRgb(245, 158, 11)), StrokeThickness = 1.5 };
            PreviewCanvas.Children.Add(diag);

            TextBlock tag = new TextBlock
            {
                Text = $"TỦ ĐIỆN {w:0}x{h:0} ({circ} CB)",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9))
            };
            Canvas.SetLeft(tag, cw / 2 - 45);
            Canvas.SetTop(tag, ry + rh + 10);
            PreviewCanvas.Children.Add(tag);
        }

        private void RenderGenericPreview(double cw, double ch)
        {
            Ellipse circle = new Ellipse
            {
                Width = 70,
                Height = 70,
                Stroke = new SolidColorBrush(Color.FromRgb(2, 132, 199)),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(30, 2, 132, 199))
            };
            Canvas.SetLeft(circle, (cw - 70) / 2);
            Canvas.SetTop(circle, (ch - 70) / 2);
            PreviewCanvas.Children.Add(circle);
        }
    }

    internal static class UIHelperExtensions
    {
        public static void ControlsAdd(this Panel panel, UIElement element)
        {
            panel.Children.Add(element);
        }
    }
}
