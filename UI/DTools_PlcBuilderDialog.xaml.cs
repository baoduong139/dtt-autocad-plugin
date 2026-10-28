using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DTT_AutoCAD_Plugin.Modules;

namespace DTT_AutoCAD_Plugin.UI
{
    public partial class DTools_PlcBuilderDialog : Window
    {
        public PlcParametricConfig ResultPlcConfig { get; private set; }
        public bool TriggerAutoPanelSync { get; private set; } = false;
        public List<SchematicComponentItem> ScannedComponents { get; private set; }

        public DTools_PlcBuilderDialog(List<SchematicComponentItem> initialComponents = null)
        {
            InitializeComponent();
            ScannedComponents = initialComponents ?? SmartSyncEngine.ScanSchematicComponents(Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument);
            ListSyncItems.ItemsSource = ScannedComponents;
        }

        public static PlcParametricConfig PromptPlcBuilder(out bool autoPanelSync, out List<SchematicComponentItem> components)
        {
            var dlg = new DTools_PlcBuilderDialog();
            bool? res = dlg.ShowDialog();
            autoPanelSync = dlg.TriggerAutoPanelSync;
            components = dlg.ScannedComponents;
            return (res == true && !dlg.TriggerAutoPanelSync) ? dlg.ResultPlcConfig : null;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                DialogResult = false;
                Close();
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
                PanelPlc.Visibility = (tag == "PanelPlc") ? Visibility.Visible : Visibility.Collapsed;
                PanelSync.Visibility = (tag == "PanelSync") ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void CboBrand_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TxtModel == null) return;

            int idx = CboBrand.SelectedIndex;
            if (idx == 0) // Siemens S7-1200
            {
                TxtModel.Text = "CPU 1214C DC/DC/DC";
                TxtDiCount.Text = "14";
                TxtDoCount.Text = "10";
                TxtAiCount.Text = "2";
                LblPlcHeader.Text = "SIEMENS SIMATIC S7-1200 CPU 1214C";
            }
            else if (idx == 1) // Siemens S7-1500
            {
                TxtModel.Text = "CPU 1511-1 PN";
                TxtDiCount.Text = "32";
                TxtDoCount.Text = "32";
                TxtAiCount.Text = "4";
                LblPlcHeader.Text = "SIEMENS SIMATIC S7-1500 CPU 1511-1 PN";
            }
            else if (idx == 2) // Mitsubishi FX5U
            {
                TxtModel.Text = "FX5U-32MT/ES";
                TxtDiCount.Text = "16";
                TxtDoCount.Text = "16";
                TxtAiCount.Text = "2";
                LblPlcHeader.Text = "MITSUBISHI MELSEC iQ-F FX5U-32MT";
            }
            else if (idx == 3) // Omron CP1H
            {
                TxtModel.Text = "CP1H-X40DT-D";
                TxtDiCount.Text = "24";
                TxtDoCount.Text = "16";
                TxtAiCount.Text = "4";
                LblPlcHeader.Text = "OMRON SYSMAC CP1H X40DT-D 40 I/O";
            }
            else if (idx == 4) // Schneider M221
            {
                TxtModel.Text = "TM221CE24T";
                TxtDiCount.Text = "14";
                TxtDoCount.Text = "10";
                TxtAiCount.Text = "2";
                LblPlcHeader.Text = "SCHNEIDER MODICON M221 ETHERNET";
            }
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

        private void BtnAutoPanelSync_Click(object sender, RoutedEventArgs e)
        {
            TriggerAutoPanelSync = true;
            DialogResult = true;
            Close();
        }

        private void BtnInsertPlc_Click(object sender, RoutedEventArgs e)
        {
            PlcBrand brand = PlcBrand.Siemens_S7_1200;
            if (CboBrand.SelectedIndex == 1) brand = PlcBrand.Siemens_S7_1500;
            else if (CboBrand.SelectedIndex == 2) brand = PlcBrand.Mitsubishi_FX5U;
            else if (CboBrand.SelectedIndex == 3) brand = PlcBrand.Omron_CP1H;
            else if (CboBrand.SelectedIndex == 4) brand = PlcBrand.Schneider_M221;

            ResultPlcConfig = new PlcParametricConfig
            {
                Brand = brand,
                ModelName = string.IsNullOrWhiteSpace(TxtModel.Text) ? "CPU 1214C" : TxtModel.Text.Trim(),
                TagName = string.IsNullOrWhiteSpace(TxtTag.Text) ? "PLC-01" : TxtTag.Text.Trim(),
                DigitalInputsCount = ParseInt(TxtDiCount.Text, 14),
                DigitalOutputsCount = ParseInt(TxtDoCount.Text, 10),
                AnalogInputsCount = ParseInt(TxtAiCount.Text, 2),
                PowerSupply = (CboPower.SelectedIndex == 1) ? "100-240VAC" : "24VDC"
            };

            TriggerAutoPanelSync = false;
            DialogResult = true;
            Close();
        }

        private static int ParseInt(string str, int fallback)
        {
            if (string.IsNullOrWhiteSpace(str)) return fallback;
            return int.TryParse(str.Trim(), out int val) ? val : fallback;
        }
    }
}
