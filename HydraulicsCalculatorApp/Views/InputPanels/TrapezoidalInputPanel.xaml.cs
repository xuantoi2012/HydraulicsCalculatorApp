using System;
using System.Windows;
using System.Windows.Controls;
using HydraulicsCalculatorApp.Helpers;

namespace HydraulicsCalculatorApp.Views.InputPanels
{
    public partial class TrapezoidalInputPanel : UserControl
    {
        public event EventHandler InputChanged;

        public TrapezoidalInputPanel()
        {
            InitializeComponent();
        }

        public double BottomWidth => Convert.ToDouble(txtBottomWidth.EditValue);
        public double FlowDepth => Convert.ToDouble(txtFlowDepth.EditValue);
        public double SideSlope => Convert.ToDouble(txtSideSlope.EditValue);
        public double ManningN => Convert.ToDouble(txtManningRoughness.EditValue);
        public double Slope => Convert.ToDouble(txtSlope.EditValue);

        public void SetFlowDepth(double value)
        {
            txtFlowDepth.EditValue = value.ToString("F4");
        }

        public void Clear()
        {
            txtBottomWidth.EditValue = "2.0";
            txtFlowDepth.EditValue = "1.5";
            txtSideSlope.EditValue = "1.5";
            txtManningRoughness.EditValue = "0.022";
            txtSlope.EditValue = "0.5";
        }

        private void OnInputChanged(object sender, RoutedEventArgs e)
        {
            InputChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnMaterialChanged(object sender, RoutedEventArgs e)
        {
            string selectedMaterial = cmbMaterial.EditValue?.ToString() ?? "";
            double nValue = MaterialDatabase.ExtractManningN(selectedMaterial);
            if (nValue > 0)
            {
                txtManningRoughness.EditValue = nValue.ToString("F3");
            }
        }
    }
}