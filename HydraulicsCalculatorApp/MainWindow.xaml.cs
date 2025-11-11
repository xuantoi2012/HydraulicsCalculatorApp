using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HydraulicsCalculatorApp
{
    public partial class MainWindow : Window
    {
        private CulvertType currentCulvertType = CulvertType.Circular;
        private bool isInitialized = false;
        private System.Windows.Threading.DispatcherTimer autoCalculateTimer;
        private List<ChartPoint> chartData = new List<ChartPoint>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeAutoCalculateTimer();
            DrawPreview();
            isInitialized = true;
        }

        private enum CulvertType
        {
            Circular,
            Box,
            Trapezoidal
        }

        private class ChartPoint
        {
            public double Y { get; set; }
            public double Q { get; set; }
            public double V { get; set; }
        }

        private void InitializeAutoCalculateTimer()
        {
            autoCalculateTimer = new System.Windows.Threading.DispatcherTimer();
            autoCalculateTimer.Interval = TimeSpan.FromMilliseconds(500);
            autoCalculateTimer.Tick += AutoCalculateTimer_Tick;
        }

        private void AutoCalculateTimer_Tick(object sender, EventArgs e)
        {
            autoCalculateTimer.Stop();
            AutoCalculate();
        }

        private void CulvertTypeChanged(object sender, RoutedEventArgs e)
        {
            if (rbCircular == null || rbBox == null || rbTrapezoidal == null) return;

            if (rbCircular.IsChecked == true)
            {
                currentCulvertType = CulvertType.Circular;
                if (grpCircularInputs != null) grpCircularInputs.Visibility = Visibility.Visible;
                if (grpBoxInputs != null) grpBoxInputs.Visibility = Visibility.Collapsed;
                if (grpTrapezoidalInputs != null) grpTrapezoidalInputs.Visibility = Visibility.Collapsed;
                UpdateStatus("Ready - Circular Pipe Mode");
            }
            else if (rbBox.IsChecked == true)
            {
                currentCulvertType = CulvertType.Box;
                if (grpCircularInputs != null) grpCircularInputs.Visibility = Visibility.Collapsed;
                if (grpBoxInputs != null) grpBoxInputs.Visibility = Visibility.Visible;
                if (grpTrapezoidalInputs != null) grpTrapezoidalInputs.Visibility = Visibility.Collapsed;
                UpdateStatus("Ready - Box Culvert Mode");
            }
            else if (rbTrapezoidal.IsChecked == true)
            {
                currentCulvertType = CulvertType.Trapezoidal;
                if (grpCircularInputs != null) grpCircularInputs.Visibility = Visibility.Collapsed;
                if (grpBoxInputs != null) grpBoxInputs.Visibility = Visibility.Collapsed;
                if (grpTrapezoidalInputs != null) grpTrapezoidalInputs.Visibility = Visibility.Visible;
                UpdateStatus("Ready - Trapezoidal Channel Mode");
            }

            ClearResults();
            DrawPreview();
        }

        private void MaterialChanged(object sender, RoutedEventArgs e)
        {
            if (!isInitialized) return;

            if (sender is DevExpress.Xpf.Editors.ComboBoxEdit combo)
            {
                string selectedMaterial = combo.EditValue?.ToString() ?? "";
                double nValue = ExtractManningN(selectedMaterial);

                if (nValue > 0)
                {
                    if (combo.Name == "cmbCircularMaterial" && txtManningRoughness != null)
                    {
                        txtManningRoughness.EditValue = nValue.ToString("F3");
                    }
                    else if (combo.Name == "cmbBoxMaterial" && txtBoxManningRoughness != null)
                    {
                        txtBoxManningRoughness.EditValue = nValue.ToString("F3");
                    }
                    else if (combo.Name == "cmbTrapMaterial" && txtTrapManningRoughness != null)
                    {
                        txtTrapManningRoughness.EditValue = nValue.ToString("F3");
                    }
                }
            }
        }

        private void EntranceLossChanged(object sender, RoutedEventArgs e)
        {
            if (!isInitialized) return;

            if (sender is DevExpress.Xpf.Editors.ComboBoxEdit combo)
            {
                string selected = combo.EditValue?.ToString() ?? "";
                double keValue = ExtractKe(selected);

                if (keValue >= 0)
                {
                    if (combo.Name == "cmbEntranceLoss" && txtEntranceLossCoeff != null)
                    {
                        txtEntranceLossCoeff.EditValue = keValue.ToString("F2");
                    }
                    else if (combo.Name == "cmbBoxEntranceLoss" && txtBoxEntranceLossCoeff != null)
                    {
                        txtBoxEntranceLossCoeff.EditValue = keValue.ToString("F2");
                    }
                }
            }
        }

        private double ExtractKe(string text)
        {
            int startIndex = text.IndexOf("Ke=");
            if (startIndex > 0)
            {
                int endIndex = text.IndexOf(")", startIndex);
                if (endIndex > 0)
                {
                    string keStr = text.Substring(startIndex + 3, endIndex - startIndex - 3);
                    if (double.TryParse(keStr, out double keValue))
                    {
                        return keValue;
                    }
                }
            }
            return -1;
        }

        private double ExtractManningN(string materialText)
        {
            int startIndex = materialText.IndexOf("n=");
            if (startIndex > 0)
            {
                int endIndex = materialText.IndexOf(")", startIndex);
                if (endIndex > 0)
                {
                    string nValueStr = materialText.Substring(startIndex + 2, endIndex - startIndex - 2);
                    if (double.TryParse(nValueStr, out double nValue))
                    {
                        return nValue;
                    }
                }
            }
            return 0;
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            AutoCalculate();
        }

        private void AutoCalculate()
        {
            try
            {
                switch (currentCulvertType)
                {
                    case CulvertType.Circular:
                        CalculateCircularPipe();
                        break;
                    case CulvertType.Box:
                        CalculateBoxCulvert();
                        break;
                    case CulvertType.Trapezoidal:
                        CalculateTrapezoidalChannel();
                        break;
                }

                UpdateStatus("Calculation completed successfully");
            }
            catch (Exception)
            {
                UpdateStatus("Waiting for valid input...");
            }
        }

        private void CalculateCircularPipe()
        {
            double d0 = Convert.ToDouble(txtPipeDiameter.EditValue);
            double n = Convert.ToDouble(txtManningRoughness.EditValue);
            double S0 = Convert.ToDouble(txtPressureSlope.EditValue) / 100.0;
            double yRatio = Convert.ToDouble(txtRelativeFlowDepth.EditValue) / 100.0;
            double L = Convert.ToDouble(txtCulvertLength.EditValue);
            double Ke = Convert.ToDouble(txtEntranceLossCoeff.EditValue);

            if (d0 <= 0 || n <= 0 || S0 <= 0 || yRatio <= 0 || yRatio > 1.0)
            {
                throw new ArgumentException("Invalid input values");
            }

            // Flow calculations
            double y = d0 * yRatio;
            double theta = 2.0 * Math.Acos((d0 / 2.0 - y) / (d0 / 2.0));
            double a = (d0 * d0 / 8.0) * (theta - Math.Sin(theta));
            double a0 = Math.PI * d0 * d0 / 4.0;
            double aRatio = a / a0;
            double Pw = d0 * theta / 2.0;
            double Rh = a / Pw;
            double T = d0 * Math.Sin(theta / 2.0);
            double v = (1.0 / n) * Math.Pow(Rh, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            double hv = v * v / (2.0 * 9.81);
            double Q = a * v;

            double Rh0 = d0 / 4.0;
            double v0 = (1.0 / n) * Math.Pow(Rh0, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            double Q0 = a0 * v0;
            double QRatio = Q / Q0;
            double F = v / Math.Sqrt(9.81 * a / T);
            double tau = 9810.0 * Rh * S0;

            // Headwater calculations
            double He = Ke * hv; // Entrance loss
            double Hf = n * n * L * v * v / Math.Pow(Rh, 4.0 / 3.0); // Friction loss (Manning)
            double Ho = hv; // Exit loss (assume v²/2g)
            double HL = He + Hf + Ho; // Total head loss
            double HW = y + hv + HL; // Headwater depth

            UpdateResults(y, a, a0, aRatio, Pw, Rh, T, v, hv, F, tau, Q, Q0, QRatio, He, Hf, Ho, HL, HW);
            DrawCircularPipePreview(d0, y);
        }

        private void CalculateBoxCulvert()
        {
            double B = Convert.ToDouble(txtBoxWidth.EditValue);
            double H = Convert.ToDouble(txtBoxHeight.EditValue);
            double y = Convert.ToDouble(txtBoxFlowDepth.EditValue);
            double n = Convert.ToDouble(txtBoxManningRoughness.EditValue);
            double S0 = Convert.ToDouble(txtBoxPressureSlope.EditValue) / 100.0;
            double L = Convert.ToDouble(txtBoxCulvertLength.EditValue);
            double Ke = Convert.ToDouble(txtBoxEntranceLossCoeff.EditValue);

            if (B <= 0 || H <= 0 || y <= 0 || n <= 0 || S0 <= 0 || y > H)
            {
                throw new ArgumentException("Invalid input values");
            }

            double a = B * y;
            double A = B * H;
            double aRatio = a / A;
            double Pw = B + 2 * y;
            double Rh = a / Pw;
            double T = B;

            double v = (1 / n) * Math.Pow(Rh, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            double hv = v * v / (2 * 9.81);
            double Q = a * v;

            double Pw0 = B + 2 * H;
            double Rh0 = A / Pw0;
            double v0 = (1 / n) * Math.Pow(Rh0, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            double Q0 = A * v0;
            double QRatio = Q / Q0;

            double F = v / Math.Sqrt(9.81 * a / T);
            double tau = 9810 * Rh * S0;

            // Headwater calculations
            double He = Ke * hv;
            double Hf = n * n * L * v * v / Math.Pow(Rh, 4.0 / 3.0);
            double Ho = hv;
            double HL = He + Hf + Ho;
            double HW = y + hv + HL;

            UpdateResults(y, a, A, aRatio, Pw, Rh, T, v, hv, F, tau, Q, Q0, QRatio, He, Hf, Ho, HL, HW);
            DrawBoxCulvertPreview(B, H, y);
        }

        private void CalculateTrapezoidalChannel()
        {
            double b = Convert.ToDouble(txtTrapBottomWidth.EditValue);
            double y = Convert.ToDouble(txtTrapFlowDepth.EditValue);
            double m = Convert.ToDouble(txtTrapSideSlope.EditValue);
            double n = Convert.ToDouble(txtTrapManningRoughness.EditValue);
            double S0 = Convert.ToDouble(txtTrapSlope.EditValue) / 100.0;

            if (b <= 0 || y <= 0 || m < 0 || n <= 0 || S0 <= 0)
            {
                throw new ArgumentException("Invalid input values");
            }

            double a = (b + m * y) * y;
            double T = b + 2 * m * y;
            double Pw = b + 2 * y * Math.Sqrt(1 + m * m);
            double Rh = a / Pw;
            double v = (1 / n) * Math.Pow(Rh, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            double hv = v * v / (2 * 9.81);
            double Q = a * v;
            double F = v / Math.Sqrt(9.81 * a / T);
            double tau = 9810 * Rh * S0;

            // For open channel, headwater is just y + hv
            double He = 0;
            double Hf = 0;
            double Ho = 0;
            double HL = 0;
            double HW = y + hv;

            UpdateResults(y, a, a, 1.0, Pw, Rh, T, v, hv, F, tau, Q, Q, 1.0, He, Hf, Ho, HL, HW);
            DrawTrapezoidalPreview(b, y, m);
        }

        private void UpdateResults(double y, double a, double A, double aRatio,
            double Pw, double Rh, double T, double v, double hv, double F,
            double tau, double Q, double Q0, double QRatio,
            double He, double Hf, double Ho, double HL, double HW)
        {
            if (txtResultY != null) txtResultY.Text = y.ToString("F4");
            if (txtResultA != null) txtResultA.Text = a.ToString("F4");
            if (txtResultA0 != null) txtResultA0.Text = A.ToString("F4");
            if (txtResultRelativeArea != null) txtResultRelativeArea.Text = aRatio.ToString("F4");
            if (txtResultPw != null) txtResultPw.Text = Pw.ToString("F4");
            if (txtResultRh != null) txtResultRh.Text = Rh.ToString("F4");
            if (txtResultT != null) txtResultT.Text = T.ToString("F4");
            if (txtResultV != null) txtResultV.Text = v.ToString("F4");
            if (txtResultHv != null) txtResultHv.Text = hv.ToString("F4");
            if (txtResultF != null) txtResultF.Text = F.ToString("F2");
            if (txtResultTau != null) txtResultTau.Text = tau.ToString("F2");
            if (txtResultQ != null) txtResultQ.Text = Q.ToString("F4");
            if (txtResultQ0 != null) txtResultQ0.Text = Q0.ToString("F4");
            if (txtResultQRatio != null) txtResultQRatio.Text = QRatio.ToString("F4");

            // Headwater results
            if (txtResultHe != null) txtResultHe.Text = He.ToString("F4");
            if (txtResultHf != null) txtResultHf.Text = Hf.ToString("F4");
            if (txtResultHo != null) txtResultHo.Text = Ho.ToString("F4");
            if (txtResultHL != null) txtResultHL.Text = HL.ToString("F4");
            if (txtResultHW != null) txtResultHW.Text = HW.ToString("F4");
        }

        // Find y from Q - Reverse Calculate
        private void FindYFromQButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTargetQ.EditValue?.ToString()))
                {
                    DXMessageBox.Show("Please enter target flow Q", "Input Required",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double targetQ = Convert.ToDouble(txtTargetQ.EditValue);

                double foundY = 0;
                double foundQ = 0;
                double foundV = 0;

                switch (currentCulvertType)
                {
                    case CulvertType.Circular:
                        foundY = FindYFromQ_Circular(targetQ, out foundQ, out foundV);
                        break;
                    case CulvertType.Box:
                        foundY = FindYFromQ_Box(targetQ, out foundQ, out foundV);
                        break;
                    case CulvertType.Trapezoidal:
                        foundY = FindYFromQ_Trapezoidal(targetQ, out foundQ, out foundV);
                        break;
                }

                if (foundY > 0)
                {
                    txtReverseResult.Text = $"Flow depth y = {foundY:F4} m\n" +
                                          $"Calculated Q = {foundQ:F4} m³/s\n" +
                                          $"Velocity v = {foundV:F4} m/s\n" +
                                          $"Error = {Math.Abs(foundQ - targetQ):F6} m³/s";

                    // Update input with found value
                    switch (currentCulvertType)
                    {
                        case CulvertType.Circular:
                            double d0 = Convert.ToDouble(txtPipeDiameter.EditValue);
                            txtRelativeFlowDepth.EditValue = (foundY / d0 * 100).ToString("F2");
                            break;
                        case CulvertType.Box:
                            txtBoxFlowDepth.EditValue = foundY.ToString("F4");
                            break;
                        case CulvertType.Trapezoidal:
                            txtTrapFlowDepth.EditValue = foundY.ToString("F4");
                            break;
                    }

                    UpdateStatus($"Found y = {foundY:F4} m for Q = {targetQ:F4} m³/s");
                }
                else
                {
                    txtReverseResult.Text = "Could not find solution for given Q";
                    UpdateStatus("No solution found");
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"Error in reverse calculation: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private double FindYFromQ_Circular(double targetQ, out double foundQ, out double foundV)
        {
            double d0 = Convert.ToDouble(txtPipeDiameter.EditValue);
            double n = Convert.ToDouble(txtManningRoughness.EditValue);
            double S0 = Convert.ToDouble(txtPressureSlope.EditValue) / 100.0;

            double yMin = 0.01 * d0;
            double yMax = 0.99 * d0;
            double tolerance = 0.0001;
            int maxIterations = 100;

            for (int i = 0; i < maxIterations; i++)
            {
                double yMid = (yMin + yMax) / 2.0;
                double theta = 2.0 * Math.Acos((d0 / 2.0 - yMid) / (d0 / 2.0));
                double a = (d0 * d0 / 8.0) * (theta - Math.Sin(theta));
                double Pw = d0 * theta / 2.0;
                double Rh = a / Pw;
                double v = (1.0 / n) * Math.Pow(Rh, 2.0 / 3.0) * Math.Pow(S0, 0.5);
                double Q = a * v;

                if (Math.Abs(Q - targetQ) < tolerance)
                {
                    foundQ = Q;
                    foundV = v;
                    return yMid;
                }

                if (Q < targetQ)
                {
                    yMin = yMid;
                }
                else
                {
                    yMax = yMid;
                }
            }

            double yFinal = (yMin + yMax) / 2.0;
            double thetaFinal = 2.0 * Math.Acos((d0 / 2.0 - yFinal) / (d0 / 2.0));
            double aFinal = (d0 * d0 / 8.0) * (thetaFinal - Math.Sin(thetaFinal));
            double PwFinal = d0 * thetaFinal / 2.0;
            double RhFinal = aFinal / PwFinal;
            foundV = (1.0 / n) * Math.Pow(RhFinal, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            foundQ = aFinal * foundV;
            return yFinal;
        }

        private double FindYFromQ_Box(double targetQ, out double foundQ, out double foundV)
        {
            double B = Convert.ToDouble(txtBoxWidth.EditValue);
            double H = Convert.ToDouble(txtBoxHeight.EditValue);
            double n = Convert.ToDouble(txtBoxManningRoughness.EditValue);
            double S0 = Convert.ToDouble(txtBoxPressureSlope.EditValue) / 100.0;

            double yMin = 0.01;
            double yMax = H * 0.99;
            double tolerance = 0.0001;
            int maxIterations = 100;

            for (int i = 0; i < maxIterations; i++)
            {
                double yMid = (yMin + yMax) / 2.0;
                double a = B * yMid;
                double Pw = B + 2 * yMid;
                double Rh = a / Pw;
                double v = (1.0 / n) * Math.Pow(Rh, 2.0 / 3.0) * Math.Pow(S0, 0.5);
                double Q = a * v;

                if (Math.Abs(Q - targetQ) < tolerance)
                {
                    foundQ = Q;
                    foundV = v;
                    return yMid;
                }

                if (Q < targetQ)
                {
                    yMin = yMid;
                }
                else
                {
                    yMax = yMid;
                }
            }

            double yFinal = (yMin + yMax) / 2.0;
            double aFinal = B * yFinal;
            double PwFinal = B + 2 * yFinal;
            double RhFinal = aFinal / PwFinal;
            foundV = (1.0 / n) * Math.Pow(RhFinal, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            foundQ = aFinal * foundV;
            return yFinal;
        }

        private double FindYFromQ_Trapezoidal(double targetQ, out double foundQ, out double foundV)
        {
            double b = Convert.ToDouble(txtTrapBottomWidth.EditValue);
            double m = Convert.ToDouble(txtTrapSideSlope.EditValue);
            double n = Convert.ToDouble(txtTrapManningRoughness.EditValue);
            double S0 = Convert.ToDouble(txtTrapSlope.EditValue) / 100.0;

            double yMin = 0.01;
            double yMax = 10.0;
            double tolerance = 0.0001;
            int maxIterations = 100;

            for (int i = 0; i < maxIterations; i++)
            {
                double yMid = (yMin + yMax) / 2.0;
                double a = (b + m * yMid) * yMid;
                double Pw = b + 2 * yMid * Math.Sqrt(1 + m * m);
                double Rh = a / Pw;
                double v = (1.0 / n) * Math.Pow(Rh, 2.0 / 3.0) * Math.Pow(S0, 0.5);
                double Q = a * v;

                if (Math.Abs(Q - targetQ) < tolerance)
                {
                    foundQ = Q;
                    foundV = v;
                    return yMid;
                }

                if (Q < targetQ)
                {
                    yMin = yMid;
                }
                else
                {
                    yMax = yMid;
                }
            }

            double yFinal = (yMin + yMax) / 2.0;
            double aFinal = (b + m * yFinal) * yFinal;
            double PwFinal = b + 2 * yFinal * Math.Sqrt(1 + m * m);
            double RhFinal = aFinal / PwFinal;
            foundV = (1.0 / n) * Math.Pow(RhFinal, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            foundQ = aFinal * foundV;
            return yFinal;
        }

        // Generate Q-y Chart
        private void GenerateChartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                chartData.Clear();

                switch (currentCulvertType)
                {
                    case CulvertType.Circular:
                        GenerateCircularChart();
                        break;
                    case CulvertType.Box:
                        GenerateBoxChart();
                        break;
                    case CulvertType.Trapezoidal:
                        GenerateTrapezoidalChart();
                        break;
                }

                DrawChart();
                UpdateStatus($"Chart generated with {chartData.Count} points");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"Error generating chart: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateCircularChart()
        {
            double d0 = Convert.ToDouble(txtPipeDiameter.EditValue);
            double n = Convert.ToDouble(txtManningRoughness.EditValue);
            double S0 = Convert.ToDouble(txtPressureSlope.EditValue) / 100.0;

            int steps = 50;
            for (int i = 1; i < steps; i++)
            {
                double yRatio = (double)i / steps;
                double y = d0 * yRatio;

                double theta = 2.0 * Math.Acos((d0 / 2.0 - y) / (d0 / 2.0));
                double a = (d0 * d0 / 8.0) * (theta - Math.Sin(theta));
                double Pw = d0 * theta / 2.0;
                double Rh = a / Pw;
                double v = (1.0 / n) * Math.Pow(Rh, 2.0 / 3.0) * Math.Pow(S0, 0.5);
                double Q = a * v;

                chartData.Add(new ChartPoint { Y = y, Q = Q, V = v });
            }
        }

        private void GenerateBoxChart()
        {
            double B = Convert.ToDouble(txtBoxWidth.EditValue);
            double H = Convert.ToDouble(txtBoxHeight.EditValue);
            double n = Convert.ToDouble(txtBoxManningRoughness.EditValue);
            double S0 = Convert.ToDouble(txtBoxPressureSlope.EditValue) / 100.0;

            int steps = 50;
            for (int i = 1; i < steps; i++)
            {
                double y = H * i / steps;

                double a = B * y;
                double Pw = B + 2 * y;
                double Rh = a / Pw;
                double v = (1.0 / n) * Math.Pow(Rh, 2.0 / 3.0) * Math.Pow(S0, 0.5);
                double Q = a * v;

                chartData.Add(new ChartPoint { Y = y, Q = Q, V = v });
            }
        }

        private void GenerateTrapezoidalChart()
        {
            double b = Convert.ToDouble(txtTrapBottomWidth.EditValue);
            double m = Convert.ToDouble(txtTrapSideSlope.EditValue);
            double n = Convert.ToDouble(txtTrapManningRoughness.EditValue);
            double S0 = Convert.ToDouble(txtTrapSlope.EditValue) / 100.0;

            double maxY = 5.0;
            int steps = 50;

            for (int i = 1; i <= steps; i++)
            {
                double y = maxY * i / steps;

                double a = (b + m * y) * y;
                double Pw = b + 2 * y * Math.Sqrt(1 + m * m);
                double Rh = a / Pw;
                double v = (1.0 / n) * Math.Pow(Rh, 2.0 / 3.0) * Math.Pow(S0, 0.5);
                double Q = a * v;

                chartData.Add(new ChartPoint { Y = y, Q = Q, V = v });
            }
        }

        private void DrawChart()
        {
            if (canvasChart == null || chartData.Count == 0) return;

            canvasChart.Children.Clear();

            double width = canvasChart.ActualWidth > 0 ? canvasChart.ActualWidth : 800;
            double height = canvasChart.ActualHeight > 0 ? canvasChart.ActualHeight : 500;

            double marginLeft = 80;
            double marginRight = 50;
            double marginTop = 50;
            double marginBottom = 70;

            double chartWidth = width - marginLeft - marginRight;
            double chartHeight = height - marginTop - marginBottom;

            double maxQ = chartData.Max(p => p.Q);
            double maxY = chartData.Max(p => p.Y);
            double maxV = chartData.Max(p => p.V);

            // Draw axes
            Line xAxis = new Line
            {
                X1 = marginLeft,
                Y1 = height - marginBottom,
                X2 = width - marginRight,
                Y2 = height - marginBottom,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            canvasChart.Children.Add(xAxis);

            Line yAxis = new Line
            {
                X1 = marginLeft,
                Y1 = marginTop,
                X2 = marginLeft,
                Y2 = height - marginBottom,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            canvasChart.Children.Add(yAxis);

            // Draw Q-y curve (blue)
            for (int i = 0; i < chartData.Count - 1; i++)
            {
                double x1 = marginLeft + (chartData[i].Q / maxQ) * chartWidth;
                double y1 = height - marginBottom - (chartData[i].Y / maxY) * chartHeight;
                double x2 = marginLeft + (chartData[i + 1].Q / maxQ) * chartWidth;
                double y2 = height - marginBottom - (chartData[i + 1].Y / maxY) * chartHeight;

                Line line = new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 3
                };
                canvasChart.Children.Add(line);
            }

            // Draw V-y curve (red)
            for (int i = 0; i < chartData.Count - 1; i++)
            {
                double x1 = marginLeft + (chartData[i].V / maxV) * chartWidth * 0.5;
                double y1 = height - marginBottom - (chartData[i].Y / maxY) * chartHeight;
                double x2 = marginLeft + (chartData[i + 1].V / maxV) * chartWidth * 0.5;
                double y2 = height - marginBottom - (chartData[i + 1].Y / maxY) * chartHeight;

                Line line = new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                canvasChart.Children.Add(line);
            }

            // Draw grid and labels
            int numGridLines = 10;
            for (int i = 0; i <= numGridLines; i++)
            {
                double yPos = height - marginBottom - (i * chartHeight / numGridLines);
                Line gridLine = new Line
                {
                    X1 = marginLeft,
                    Y1 = yPos,
                    X2 = width - marginRight,
                    Y2 = yPos,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };
                canvasChart.Children.Add(gridLine);

                double yValue = (i * maxY / numGridLines);
                System.Windows.Controls.TextBlock yLabel = new System.Windows.Controls.TextBlock
                {
                    Text = yValue.ToString("F2"),
                    FontSize = 10
                };
                Canvas.SetLeft(yLabel, marginLeft - 50);
                Canvas.SetTop(yLabel, yPos - 8);
                canvasChart.Children.Add(yLabel);

                double xPos = marginLeft + (i * chartWidth / numGridLines);
                Line vGridLine = new Line
                {
                    X1 = xPos,
                    Y1 = marginTop,
                    X2 = xPos,
                    Y2 = height - marginBottom,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };
                canvasChart.Children.Add(vGridLine);

                double qValue = (i * maxQ / numGridLines);
                System.Windows.Controls.TextBlock xLabel = new System.Windows.Controls.TextBlock
                {
                    Text = qValue.ToString("F2"),
                    FontSize = 10
                };
                Canvas.SetLeft(xLabel, xPos - 15);
                Canvas.SetTop(xLabel, height - marginBottom + 10);
                canvasChart.Children.Add(xLabel);
            }

            // Chart title
            System.Windows.Controls.TextBlock title = new System.Windows.Controls.TextBlock
            {
                Text = "Q-y and V-y Relationship Chart",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkBlue
            };
            Canvas.SetLeft(title, width / 2 - 120);
            Canvas.SetTop(title, 10);
            canvasChart.Children.Add(title);

            // X-axis label
            System.Windows.Controls.TextBlock xAxisLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Flow Q (m³/s) / Velocity V (m/s)",
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(xAxisLabel, width / 2 - 100);
            Canvas.SetTop(xAxisLabel, height - 30);
            canvasChart.Children.Add(xAxisLabel);

            // Y-axis label
            System.Windows.Controls.TextBlock yAxisLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Depth y (m)",
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(yAxisLabel, 10);
            Canvas.SetTop(yAxisLabel, height / 2 - 30);
            canvasChart.Children.Add(yAxisLabel);

            // Legend
            Rectangle qLegend = new Rectangle
            {
                Width = 40,
                Height = 3,
                Fill = Brushes.Blue
            };
            Canvas.SetLeft(qLegend, width - marginRight - 150);
            Canvas.SetTop(qLegend, 40);
            canvasChart.Children.Add(qLegend);

            System.Windows.Controls.TextBlock qLegendText = new System.Windows.Controls.TextBlock
            {
                Text = "Q-y curve",
                FontSize = 11
            };
            Canvas.SetLeft(qLegendText, width - marginRight - 100);
            Canvas.SetTop(qLegendText, 33);
            canvasChart.Children.Add(qLegendText);

            Rectangle vLegend = new Rectangle
            {
                Width = 40,
                Height = 2,
                Fill = Brushes.Red
            };
            Canvas.SetLeft(vLegend, width - marginRight - 150);
            Canvas.SetTop(vLegend, 60);
            canvasChart.Children.Add(vLegend);

            System.Windows.Controls.TextBlock vLegendText = new System.Windows.Controls.TextBlock
            {
                Text = "V-y curve",
                FontSize = 11
            };
            Canvas.SetLeft(vLegendText, width - marginRight - 100);
            Canvas.SetTop(vLegendText, 53);
            canvasChart.Children.Add(vLegendText);
        }

        private void ClearChartButton_Click(object sender, RoutedEventArgs e)
        {
            if (canvasChart != null)
            {
                canvasChart.Children.Clear();
                chartData.Clear();
                UpdateStatus("Chart cleared");
            }
        }

        private void ShowChartButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateChartButton_Click(sender, e);
        }

        private void UpdateStatus(string message)
        {
            if (txtStatus != null)
            {
                txtStatus.Text = message;
            }
        }

        // DrawPreview methods from previous code
        private void DrawCircularPipePreview(double d0, double y)
        {
            if (canvasPreview == null) return;
            canvasPreview.Children.Clear();

            double centerX = 225;
            double centerY = 140;
            double radius = 90;

            Ellipse pipeCircle = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(pipeCircle, centerX - radius);
            Canvas.SetTop(pipeCircle, centerY - radius);
            canvasPreview.Children.Add(pipeCircle);

            double R = d0 / 2.0;
            double theta = 2.0 * Math.Acos((R - y) / R);
            double waterSurfaceY = centerY - R + y;

            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = new Point(centerX, centerY + radius);

            ArcSegment leftArc = new ArcSegment
            {
                Point = new Point(centerX - radius * Math.Sin(theta / 2), waterSurfaceY),
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Counterclockwise,
                IsLargeArc = false
            };
            pathFigure.Segments.Add(leftArc);

            LineSegment waterSurfaceLine = new LineSegment
            {
                Point = new Point(centerX + radius * Math.Sin(theta / 2), waterSurfaceY)
            };
            pathFigure.Segments.Add(waterSurfaceLine);

            ArcSegment rightArc = new ArcSegment
            {
                Point = new Point(centerX, centerY + radius),
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Counterclockwise,
                IsLargeArc = false
            };
            pathFigure.Segments.Add(rightArc);

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            Path waterPath = new Path
            {
                Data = pathGeometry,
                Fill = new SolidColorBrush(Color.FromArgb(180, 0, 120, 255)),
                Stroke = Brushes.DarkBlue,
                StrokeThickness = 1
            };
            canvasPreview.Children.Add(waterPath);

            Line waterLine = new Line
            {
                X1 = centerX - radius * Math.Sin(theta / 2),
                Y1 = waterSurfaceY,
                X2 = centerX + radius * Math.Sin(theta / 2),
                Y2 = waterSurfaceY,
                Stroke = Brushes.Blue,
                StrokeThickness = 2.5
            };
            canvasPreview.Children.Add(waterLine);

            System.Windows.Controls.TextBlock label = new System.Windows.Controls.TextBlock
            {
                Text = "Circular Pipe (Cống tròn)",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.DarkBlue
            };
            Canvas.SetLeft(label, 10);
            Canvas.SetTop(label, 10);
            canvasPreview.Children.Add(label);
        }

        private void DrawBoxCulvertPreview(double B, double H, double y)
        {
            if (canvasPreview == null) return;
            canvasPreview.Children.Clear();

            double scale = Math.Min(60, 180 / Math.Max(B, H));
            double offsetX = 100;
            double offsetY = 50;

            double boxWidth = B * scale;
            double boxHeight = H * scale;
            double waterHeight = y * scale;

            Rectangle box = new Rectangle
            {
                Width = boxWidth,
                Height = boxHeight,
                Stroke = Brushes.Black,
                StrokeThickness = 3,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(box, offsetX);
            Canvas.SetTop(box, offsetY);
            canvasPreview.Children.Add(box);

            Rectangle water = new Rectangle
            {
                Width = boxWidth,
                Height = waterHeight,
                Fill = new SolidColorBrush(Color.FromArgb(180, 0, 120, 255)),
                Stroke = Brushes.DarkBlue,
                StrokeThickness = 1
            };
            Canvas.SetLeft(water, offsetX);
            Canvas.SetTop(water, offsetY + boxHeight - waterHeight);
            canvasPreview.Children.Add(water);

            Line waterSurface = new Line
            {
                X1 = offsetX,
                Y1 = offsetY + boxHeight - waterHeight,
                X2 = offsetX + boxWidth,
                Y2 = offsetY + boxHeight - waterHeight,
                Stroke = Brushes.Blue,
                StrokeThickness = 2.5
            };
            canvasPreview.Children.Add(waterSurface);

            System.Windows.Controls.TextBlock label = new System.Windows.Controls.TextBlock
            {
                Text = "Box Culvert (Cống hộp)",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.DarkBlue
            };
            Canvas.SetLeft(label, 10);
            Canvas.SetTop(label, 10);
            canvasPreview.Children.Add(label);
        }

        private void DrawTrapezoidalPreview(double b, double y, double m)
        {
            if (canvasPreview == null) return;
            canvasPreview.Children.Clear();

            double scale = Math.Min(50, 150 / Math.Max(b + 2 * m * y, y));
            double offsetX = 120;
            double offsetY = 70;

            double bottomWidth = b * scale;
            double height = y * scale;
            double sideExtension = m * y * scale;

            PathFigure channelFigure = new PathFigure();
            channelFigure.StartPoint = new Point(offsetX, offsetY + height);
            channelFigure.Segments.Add(new LineSegment(new Point(offsetX + sideExtension, offsetY), true));
            channelFigure.Segments.Add(new LineSegment(new Point(offsetX + sideExtension + bottomWidth, offsetY), true));
            channelFigure.Segments.Add(new LineSegment(new Point(offsetX + sideExtension + bottomWidth + sideExtension, offsetY + height), true));

            PathGeometry channelGeometry = new PathGeometry();
            channelGeometry.Figures.Add(channelFigure);

            Path channelPath = new Path
            {
                Data = channelGeometry,
                Stroke = Brushes.Black,
                StrokeThickness = 3,
                Fill = Brushes.Transparent
            };
            canvasPreview.Children.Add(channelPath);

            PathFigure waterFigure = new PathFigure();
            waterFigure.StartPoint = new Point(offsetX, offsetY + height);
            waterFigure.Segments.Add(new LineSegment(new Point(offsetX + sideExtension, offsetY), true));
            waterFigure.Segments.Add(new LineSegment(new Point(offsetX + sideExtension + bottomWidth, offsetY), true));
            waterFigure.Segments.Add(new LineSegment(new Point(offsetX + sideExtension + bottomWidth + sideExtension, offsetY + height), true));
            waterFigure.Segments.Add(new LineSegment(new Point(offsetX, offsetY + height), true));

            PathGeometry waterGeometry = new PathGeometry();
            waterGeometry.Figures.Add(waterFigure);

            Path waterPath = new Path
            {
                Data = waterGeometry,
                Fill = new SolidColorBrush(Color.FromArgb(180, 0, 120, 255)),
                Stroke = Brushes.DarkBlue,
                StrokeThickness = 1
            };
            canvasPreview.Children.Add(waterPath);

            System.Windows.Controls.TextBlock label = new System.Windows.Controls.TextBlock
            {
                Text = m == 0 ? "Rectangular Channel (Mương chữ nhật)" : "Trapezoidal Channel (Mương hình thang)",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = Brushes.DarkBlue
            };
            Canvas.SetLeft(label, 10);
            Canvas.SetTop(label, 10);
            canvasPreview.Children.Add(label);
        }

        private void DrawPreview(double yRatio = 0.81)
        {
            if (currentCulvertType == CulvertType.Circular && txtPipeDiameter != null)
            {
                try
                {
                    double d0 = Convert.ToDouble(txtPipeDiameter.EditValue);
                    double y = d0 * yRatio;
                    DrawCircularPipePreview(d0, y);
                }
                catch
                {
                    if (canvasPreview != null) canvasPreview.Children.Clear();
                }
            }
        }

        private void ClearResults()
        {
            if (txtResultY != null) txtResultY.Text = "";
            if (txtResultA != null) txtResultA.Text = "";
            if (txtResultA0 != null) txtResultA0.Text = "";
            if (txtResultRelativeArea != null) txtResultRelativeArea.Text = "";
            if (txtResultPw != null) txtResultPw.Text = "";
            if (txtResultRh != null) txtResultRh.Text = "";
            if (txtResultT != null) txtResultT.Text = "";
            if (txtResultV != null) txtResultV.Text = "";
            if (txtResultHv != null) txtResultHv.Text = "";
            if (txtResultF != null) txtResultF.Text = "";
            if (txtResultTau != null) txtResultTau.Text = "";
            if (txtResultQ != null) txtResultQ.Text = "";
            if (txtResultQ0 != null) txtResultQ0.Text = "";
            if (txtResultQRatio != null) txtResultQRatio.Text = "";
            if (txtResultHe != null) txtResultHe.Text = "";
            if (txtResultHf != null) txtResultHf.Text = "";
            if (txtResultHo != null) txtResultHo.Text = "";
            if (txtResultHL != null) txtResultHL.Text = "";
            if (txtResultHW != null) txtResultHW.Text = "";
        }

        private void InputChanged(object sender, RoutedEventArgs e)
        {
            if (!isInitialized) return;
            autoCalculateTimer.Stop();
            autoCalculateTimer.Start();
        }

        private void UnitChanged(object sender, RoutedEventArgs e)
        {
            if (!isInitialized) return;
            UpdateStatus("Unit changed");
        }

        private void CircularPipeButton_Click(object sender, RoutedEventArgs e)
        {
            if (rbCircular != null) rbCircular.IsChecked = true;
        }

        private void BoxCulvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (rbBox != null) rbBox.IsChecked = true;
        }

        private void TrapezoidalButton_Click(object sender, RoutedEventArgs e)
        {
            if (rbTrapezoidal != null) rbTrapezoidal.IsChecked = true;
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            ClearButton_Click(sender, e);
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            DXMessageBox.Show("Open file functionality", "Open", MessageBoxButton.OK);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DXMessageBox.Show("Save file functionality", "Save", MessageBoxButton.OK);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            switch (currentCulvertType)
            {
                case CulvertType.Circular:
                    if (txtPipeDiameter != null) txtPipeDiameter.EditValue = "2.5";
                    if (txtManningRoughness != null) txtManningRoughness.EditValue = "0.013";
                    if (txtPressureSlope != null) txtPressureSlope.EditValue = "0.5";
                    if (txtRelativeFlowDepth != null) txtRelativeFlowDepth.EditValue = "81";
                    if (txtCulvertLength != null) txtCulvertLength.EditValue = "10";
                    if (txtEntranceLossCoeff != null) txtEntranceLossCoeff.EditValue = "0.5";
                    break;
                case CulvertType.Box:
                    if (txtBoxWidth != null) txtBoxWidth.EditValue = "3.0";
                    if (txtBoxHeight != null) txtBoxHeight.EditValue = "2.5";
                    if (txtBoxFlowDepth != null) txtBoxFlowDepth.EditValue = "2.0";
                    if (txtBoxManningRoughness != null) txtBoxManningRoughness.EditValue = "0.013";
                    if (txtBoxPressureSlope != null) txtBoxPressureSlope.EditValue = "0.5";
                    if (txtBoxCulvertLength != null) txtBoxCulvertLength.EditValue = "10";
                    if (txtBoxEntranceLossCoeff != null) txtBoxEntranceLossCoeff.EditValue = "0.5";
                    break;
                case CulvertType.Trapezoidal:
                    if (txtTrapBottomWidth != null) txtTrapBottomWidth.EditValue = "2.0";
                    if (txtTrapFlowDepth != null) txtTrapFlowDepth.EditValue = "1.5";
                    if (txtTrapSideSlope != null) txtTrapSideSlope.EditValue = "1.5";
                    if (txtTrapManningRoughness != null) txtTrapManningRoughness.EditValue = "0.022";
                    if (txtTrapSlope != null) txtTrapSlope.EditValue = "0.5";
                    break;
            }

            ClearResults();
            DrawPreview();
            UpdateStatus("Cleared");
        }

        private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            DXMessageBox.Show("Export to PDF functionality", "Export", MessageBoxButton.OK);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            DXMessageBox.Show(
                "Hydraulics Calculator v1.0\n\n" +
                "Tính toán thủy lực cho:\n" +
                "• Cống tròn (Circular Pipe)\n" +
                "• Cống hộp (Box Culvert)\n" +
                "• Mương hình thang (Trapezoidal Channel)\n\n" +
                "Features:\n" +
                "✓ Auto-calculate on input change\n" +
                "✓ Real-time preview\n" +
                "✓ Material selection with Manning's n\n" +
                "✓ Q-y Chart generation\n" +
                "✓ Reverse calculate: Find y from Q\n" +
                "✓ Headwater depth calculation\n" +
                "✓ TextBox-based results (ready for panel)\n\n" +
                "Developed by: xuantoi2012\n" +
                "© 2025 All rights reserved",
                "About Hydraulics Calculator",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}