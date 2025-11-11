using System.Windows.Controls;
using HydraulicsCalculatorApp.Models;

namespace HydraulicsCalculatorApp.Views
{
    public partial class ResultsPanel : UserControl
    {
        public ResultsPanel()
        {
            InitializeComponent();
        }

        public void UpdateResults(FlowResult result)
        {
            txtY.Text = result.Y.ToString("F4");
            txtA.Text = result.A.ToString("F4");
            txtA0.Text = result.A0.ToString("F4");
            txtRelativeArea.Text = result.RelativeArea.ToString("F4");
            txtPw.Text = result.Pw.ToString("F4");
            txtRh.Text = result.Rh.ToString("F4");
            txtT.Text = result.T.ToString("F4");
            txtV.Text = result.V.ToString("F4");
            txtHv.Text = result.Hv.ToString("F4");
            txtQ.Text = result.Q.ToString("F4");
            txtQ0.Text = result.Q0.ToString("F4");
            txtQRatio.Text = result.QRatio.ToString("F4");
            txtF.Text = result.F.ToString("F2");
            txtTau.Text = result.Tau.ToString("F2");
            txtHe.Text = result.He.ToString("F4");
            txtHf.Text = result.Hf.ToString("F4");
            txtHo.Text = result.Ho.ToString("F4");
            txtHL.Text = result.HL.ToString("F4");
            txtHW.Text = result.HW.ToString("F4");
        }

        public void Clear()
        {
            txtY.Text = "";
            txtA.Text = "";
            txtA0.Text = "";
            txtRelativeArea.Text = "";
            txtPw.Text = "";
            txtRh.Text = "";
            txtT.Text = "";
            txtV.Text = "";
            txtHv.Text = "";
            txtQ.Text = "";
            txtQ0.Text = "";
            txtQRatio.Text = "";
            txtF.Text = "";
            txtTau.Text = "";
            txtHe.Text = "";
            txtHf.Text = "";
            txtHo.Text = "";
            txtHL.Text = "";
            txtHW.Text = "";
        }
    }
}