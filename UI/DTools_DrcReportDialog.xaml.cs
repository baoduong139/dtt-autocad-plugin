using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DTT_AutoCAD_Plugin.Modules;

namespace DTT_AutoCAD_Plugin.UI
{
    public class DrcIssueViewModel : DrcIssueItem
    {
        public Brush SeverityBackground => (Severity == "ERROR") ? new SolidColorBrush(Color.FromRgb(254, 226, 226)) : new SolidColorBrush(Color.FromRgb(254, 243, 199));
        public Brush SeverityForeground => (Severity == "ERROR") ? new SolidColorBrush(Color.FromRgb(220, 38, 38)) : new SolidColorBrush(Color.FromRgb(217, 119, 6));

        public DrcIssueViewModel(DrcIssueItem item)
        {
            Severity = item.Severity;
            ErrorCode = item.ErrorCode;
            Title = item.Title;
            Details = item.Details;
            Location = item.Location;
            Suggestion = item.Suggestion;
        }
    }

    public partial class DTools_DrcReportDialog : Window
    {
        public bool MarkOnCadRequested { get; private set; } = false;
        private List<DrcIssueItem> _issues;

        public DTools_DrcReportDialog(List<DrcIssueItem> issues)
        {
            InitializeComponent();
            _issues = issues ?? new List<DrcIssueItem>();

            int errors = _issues.Count(i => i.Severity == "ERROR");
            int warnings = _issues.Count(i => i.Severity == "WARNING");

            if (TxtSummary != null)
            {
                TxtSummary.Text = (errors == 0 && warnings == 0)
                    ? "✅ BẢN VẼ HOÀN HẢO: Không phát hiện lỗi thiết kế mạch điện nào!"
                    : $"⚠️ Phát hiện: {errors} Lỗi nghiêm trọng (Error) | {warnings} Cảnh báo (Warning)";
            }

            var vmList = _issues.Select(i => new DrcIssueViewModel(i)).ToList();
            LstIssues.ItemsSource = vmList;
        }

        public static bool ShowReport(List<DrcIssueItem> issues)
        {
            var dlg = new DTools_DrcReportDialog(issues);
            dlg.ShowDialog();
            return dlg.MarkOnCadRequested;
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
                Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnMarkOnCad_Click(object sender, RoutedEventArgs e)
        {
            MarkOnCadRequested = true;
            Close();
        }
    }
}
