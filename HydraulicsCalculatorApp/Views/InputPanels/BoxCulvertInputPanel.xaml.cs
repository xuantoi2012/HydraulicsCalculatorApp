using System;
using System.Windows;
using System.Windows.Controls;
using HydraulicsCalculatorApp.Helpers;

namespace HydraulicsCalculatorApp.Views.InputPanels
{
    public partial class BoxCulvertInputPanel : UserControl
    {
        public event EventHandler InputChanged;

        public BoxCulvertInputPanel()
        {
            InitializeComponent();
        }

        public double Width => Convert.ToDouble(txtWidth.EditValue);
        public double Height => Convert.ToDouble(txtHeight.EditValue);
        public double ManningN => Convert.ToDouble(txtManningRoughness.EditValue);
        public double Slope => Convert.ToDouble(txtSlope.EditValue);
        public double FlowDepth => Convert.ToDouble(txtFlowDepth.EditValue);
        public double Length => Convert.ToDouble(txtLength.EditValue);
        public double EntranceCoeff => Convert.ToDouble(txtEntranceCoeff.EditValue);

        public void SetFlowDepth(double value)
        {
            txtFlowDepth.EditValue = value.ToString("F4");
        }

        public void Clear()
        {
            txtWidth.EditValue = "3.0";
            txtHeight.EditValue = "2.5";
            txtManningRoughness.EditValue = "0.013";
            txtSlope.EditValue = "0.5";
            txtFlowDepth.EditValue = "2.0";
            txtLength.EditValue = "10";
            txtEntranceCoeff.EditValue = "0.5";
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

        private void OnEntranceLossChanged(object sender, RoutedEventArgs e)
        {
            string selected = cmbEntranceLoss.EditValue?.ToString() ?? "";
            double keValue = MaterialDatabase.ExtractKe(selected);
            if (keValue >= 0)
            {
                txtEntranceCoeff.EditValue = keValue.ToString("F2");
            }
        }
    }
}