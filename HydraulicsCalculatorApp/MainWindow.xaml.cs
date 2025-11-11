using System;
using System.Collections.Generic;
using System.Windows;
using DevExpress.Xpf.Core;
using HydraulicsCalculatorApp.Models;
using HydraulicsCalculatorApp.Calculators;

namespace HydraulicsCalculatorApp
{
    public partial class MainWindow : Window
    {
        private CulvertType currentCulvertType = CulvertType.Circular;
        private bool isInitialized = false;
        private System.Windows.Threading.DispatcherTimer autoCalculateTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeAutoCalculateTimer();
            InitializeEventHandlers();
            isInitialized = true;

            // Initial calculation
            AutoCalculate();
        }

        private void InitializeAutoCalculateTimer()
        {
            autoCalculateTimer = new System.Windows.Threading.DispatcherTimer();
            autoCalculateTimer.Interval = TimeSpan.FromMilliseconds(500);
            autoCalculateTimer.Tick += AutoCalculateTimer_Tick;
        }

        private void InitializeEventHandlers()
        {
            // Wire up input panel events
            circularInputPanel.InputChanged += InputPanel_InputChanged;
            boxInputPanel.InputChanged += InputPanel_InputChanged;
            trapezoidalInputPanel.InputChanged += InputPanel_InputChanged;

            // Wire up chart panel events
            chartPanel.GenerateRequested += ChartPanel_GenerateRequested;
        }

        private void AutoCalculateTimer_Tick(object sender, EventArgs e)
        {
            autoCalculateTimer.Stop();
            AutoCalculate();
        }

        private void InputPanel_InputChanged(object sender, EventArgs e)
        {
            if (!isInitialized) return;

            // Restart timer for auto-calculate
            autoCalculateTimer.Stop();
            autoCalculateTimer.Start();
        }

        #region Culvert Type Selection

        private void CulvertTypeChanged(object sender, RoutedEventArgs e)
        {
            if (!isInitialized) return;

            if (rbCircular.IsChecked == true)
            {
                currentCulvertType = CulvertType.Circular;
                circularInputPanel.Visibility = Visibility.Visible;
                boxInputPanel.Visibility = Visibility.Collapsed;
                trapezoidalInputPanel.Visibility = Visibility.Collapsed;
                txtCurrentMode.Text = "Circular Pipe";
            }
            else if (rbBox.IsChecked == true)
            {
                currentCulvertType = CulvertType.Box;
                circularInputPanel.Visibility = Visibility.Collapsed;
                boxInputPanel.Visibility = Visibility.Visible;
                trapezoidalInputPanel.Visibility = Visibility.Collapsed;
                txtCurrentMode.Text = "Box Culvert";
            }
            else if (rbTrapezoidal.IsChecked == true)
            {
                currentCulvertType = CulvertType.Trapezoidal;
                circularInputPanel.Visibility = Visibility.Collapsed;
                boxInputPanel.Visibility = Visibility.Collapsed;
                trapezoidalInputPanel.Visibility = Visibility.Visible;
                txtCurrentMode.Text = "Trapezoidal Channel";
            }

            resultsPanel.Clear();
            previewPanel.Clear();
            AutoCalculate();
        }

        #endregion

        #region Calculate Methods

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            AutoCalculate();
        }

        private void AutoCalculate()
        {
            try
            {
                FlowResult result = null;

                switch (currentCulvertType)
                {
                    case CulvertType.Circular:
                        result = CircularPipeCalculator.Calculate(
                            circularInputPanel.Diameter,
                            circularInputPanel.ManningN,
                            circularInputPanel.Slope / 100.0,
                            circularInputPanel.RelativeDepth / 100.0,
                            circularInputPanel.Length,
                            circularInputPanel.EntranceCoeff
                        );
                        previewPanel.DrawCircularPipe(circularInputPanel.Diameter, result.Y);
                        break;

                    case CulvertType.Box:
                        result = BoxCulvertCalculator.Calculate(
                            boxInputPanel.Width,
                            boxInputPanel.Height,
                            boxInputPanel.FlowDepth,
                            boxInputPanel.ManningN,
                            boxInputPanel.Slope / 100.0,
                            boxInputPanel.Length,
                            boxInputPanel.EntranceCoeff
                        );
                        previewPanel.DrawBoxCulvert(boxInputPanel.Width, boxInputPanel.Height, result.Y);
                        break;

                    case CulvertType.Trapezoidal:
                        result = TrapezoidalChannelCalculator.Calculate(
                            trapezoidalInputPanel.BottomWidth,
                            trapezoidalInputPanel.FlowDepth,
                            trapezoidalInputPanel.SideSlope,
                            trapezoidalInputPanel.ManningN,
                            trapezoidalInputPanel.Slope / 100.0
                        );
                        previewPanel.DrawTrapezoidal(
                            trapezoidalInputPanel.BottomWidth,
                            result.Y,
                            trapezoidalInputPanel.SideSlope
                        );
                        break;
                }

                if (result != null)
                {
                    resultsPanel.UpdateResults(result);
                    UpdateStatus($"✓ Calculated: Q = {result.Q:F4} m³/s, HW = {result.HW:F4} m");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"⚠ Error: {ex.Message}");
            }
        }

        #endregion

        #region Reverse Calculate (Find y from Q)

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
                        foundY = CircularPipeCalculator.FindYFromQ(
                            targetQ,
                            circularInputPanel.Diameter,
                            circularInputPanel.ManningN,
                            circularInputPanel.Slope / 100.0,
                            out foundQ,
                            out foundV
                        );

                        // Update input with found value
                        double yRatio = (foundY / circularInputPanel.Diameter) * 100.0;
                        circularInputPanel.SetRelativeDepth(yRatio);
                        break;

                    case CulvertType.Box:
                        foundY = BoxCulvertCalculator.FindYFromQ(
                            targetQ,
                            boxInputPanel.Width,
                            boxInputPanel.Height,
                            boxInputPanel.ManningN,
                            boxInputPanel.Slope / 100.0,
                            out foundQ,
                            out foundV
                        );
                        boxInputPanel.SetFlowDepth(foundY);
                        break;

                    case CulvertType.Trapezoidal:
                        foundY = TrapezoidalChannelCalculator.FindYFromQ(
                            targetQ,
                            trapezoidalInputPanel.BottomWidth,
                            trapezoidalInputPanel.SideSlope,
                            trapezoidalInputPanel.ManningN,
                            trapezoidalInputPanel.Slope / 100.0,
                            out foundQ,
                            out foundV
                        );
                        trapezoidalInputPanel.SetFlowDepth(foundY);
                        break;
                }

                if (foundY > 0)
                {
                    txtReverseResult.Text = $"✓ Found: y = {foundY:F4} m\n" +
                                          $"Q = {foundQ:F4} m³/s\n" +
                                          $"v = {foundV:F4} m/s\n" +
                                          $"Error = {Math.Abs(foundQ - targetQ):F6} m³/s";

                    UpdateStatus($"✓ Found y = {foundY:F4} m for Q = {targetQ:F4} m³/s");
                }
                else
                {
                    txtReverseResult.Text = "⚠ Could not find solution";
                    UpdateStatus("No solution found");
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowInletDesignButton_Click(object sender, RoutedEventArgs e)
        {
            // Create new window for inlet design
            var inletWindow = new Window
            {
                Title = "Street Inlet Design / Thiết kế hố thu nước mặt đường",
                Width = 1400,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = new Views.InletDesignPanel()
            };

            inletWindow.ShowDialog();
        }

        #endregion

        #region Chart Generation

        private void ChartPanel_GenerateRequested(object sender, EventArgs e)
        {
            try
            {
                List<ChartPoint> chartData = new List<ChartPoint>();

                switch (currentCulvertType)
                {
                    case CulvertType.Circular:
                        chartData = GenerateCircularChart();
                        break;
                    case CulvertType.Box:
                        chartData = GenerateBoxChart();
                        break;
                    case CulvertType.Trapezoidal:
                        chartData = GenerateTrapezoidalChart();
                        break;
                }

                chartPanel.SetChartData(chartData);
                UpdateStatus($"✓ Chart generated with {chartData.Count} points");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"Error generating chart: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<ChartPoint> GenerateCircularChart()
        {
            List<ChartPoint> data = new List<ChartPoint>();
            double d0 = circularInputPanel.Diameter;
            double n = circularInputPanel.ManningN;
            double S0 = circularInputPanel.Slope / 100.0;

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

                data.Add(new ChartPoint { Y = y, Q = Q, V = v });
            }

            return data;
        }

        private List<ChartPoint> GenerateBoxChart()
        {
            List<ChartPoint> data = new List<ChartPoint>();
            double B = boxInputPanel.Width;
            double H = boxInputPanel.Height;
            double n = boxInputPanel.ManningN;
            double S0 = boxInputPanel.Slope / 100.0;

            int steps = 50;
            for (int i = 1; i < steps; i++)
            {
                double y = H * i / steps;

                double a = B * y;
                double Pw = B + 2 * y;
                double Rh = a / Pw;
                double v = (1.0 / n) * Math.Pow(Rh, 2.0 / 3.0) * Math.Pow(S0, 0.5);
                double Q = a * v;

                data.Add(new ChartPoint { Y = y, Q = Q, V = v });
            }

            return data;
        }

        private List<ChartPoint> GenerateTrapezoidalChart()
        {
            List<ChartPoint> data = new List<ChartPoint>();
            double b = trapezoidalInputPanel.BottomWidth;
            double m = trapezoidalInputPanel.SideSlope;
            double n = trapezoidalInputPanel.ManningN;
            double S0 = trapezoidalInputPanel.Slope / 100.0;

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

                data.Add(new ChartPoint { Y = y, Q = Q, V = v });
            }

            return data;
        }

        #endregion

        #region Menu and Toolbar Handlers

        private void CircularPipeButton_Click(object sender, RoutedEventArgs e)
        {
            rbCircular.IsChecked = true;
        }

        private void BoxCulvertButton_Click(object sender, RoutedEventArgs e)
        {
            rbBox.IsChecked = true;
        }

        private void TrapezoidalButton_Click(object sender, RoutedEventArgs e)
        {
            rbTrapezoidal.IsChecked = true;
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            switch (currentCulvertType)
            {
                case CulvertType.Circular:
                    circularInputPanel.Clear();
                    break;
                case CulvertType.Box:
                    boxInputPanel.Clear();
                    break;
                case CulvertType.Trapezoidal:
                    trapezoidalInputPanel.Clear();
                    break;
            }

            resultsPanel.Clear();
            previewPanel.Clear();
            chartPanel.Clear();
            txtTargetQ.EditValue = "";
            txtReverseResult.Text = "";
            UpdateStatus("✓ Cleared - Ready for new calculation");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DXMessageBox.Show("Save functionality - To be implemented", "Save",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            DXMessageBox.Show(
                "Hydraulics Calculator v2.0\n" +
                "Modular Architecture Edition\n\n" +
                "Tính toán thủy lực cho:\n" +
                "• 🔵 Cống tròn (Circular Pipe)\n" +
                "• ⬜ Cống hộp (Box Culvert)\n" +
                "• 🔷 Mương hình thang (Trapezoidal Channel)\n\n" +
                "Features:\n" +
                "✓ Auto-calculate on input change\n" +
                "✓ Real-time preview with color-coded water\n" +
                "✓ Material database with Manning's n\n" +
                "✓ Q-y Chart generation\n" +
                "✓ Reverse calculate: Find y from Q\n" +
                "✓ Headwater depth calculation\n" +
                "✓ Modular UserControl architecture\n" +
                "✓ Clean separation of concerns\n\n" +
                "Architecture:\n" +
                "• Models: Data structures\n" +
                "• Calculators: Business logic\n" +
                "• Views: Reusable UI components\n" +
                "• Helpers: Utilities & databases\n\n" +
                "Developed by: xuantoi2012\n" +
                "© 2025 All rights reserved\n\n" +
                "Built with DevExpress WPF Controls",
                "About Hydraulics Calculator",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion

        #region Helper Methods

        private void UpdateStatus(string message)
        {
            if (txtStatus != null)
            {
                txtStatus.Text = message;
            }
        }

        #endregion
    }
}