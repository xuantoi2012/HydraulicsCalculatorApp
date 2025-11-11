using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HydraulicsCalculatorApp.Models;
using HydraulicsCalculatorApp.Calculators;

namespace HydraulicsCalculatorApp.Views
{
    public partial class InletDesignPanel : UserControl
    {
        private bool isInitialized = false;
        private System.Windows.Threading.DispatcherTimer autoCalculateTimer;
        private InletType currentInletType = InletType.Grate;
        private GutterFlowResult gutterFlowResult;
        private InletCapacityResult inletCapacityResult;

        public InletDesignPanel()
        {
            InitializeComponent();
            InitializeAutoCalculateTimer();
            isInitialized = true;

            // Initial calculation
            AutoCalculate();
        }

        private void InitializeAutoCalculateTimer()
        {
            autoCalculateTimer = new System.Windows.Threading.DispatcherTimer();
            autoCalculateTimer.Interval = TimeSpan.FromMilliseconds(500);
            autoCalculateTimer.Tick += (s, e) =>
            {
                autoCalculateTimer.Stop();
                AutoCalculate();
            };
        }

        private void OnInputChanged(object sender, RoutedEventArgs e)
        {
            if (!isInitialized) return;
            autoCalculateTimer.Stop();
            autoCalculateTimer.Start();
        }

        #region Inlet Type Selection

        private void InletTypeChanged(object sender, RoutedEventArgs e)
        {
            if (!isInitialized) return;

            if (rbGrate.IsChecked == true)
            {
                currentInletType = InletType.Grate;
                grpGrateParams.Visibility = Visibility.Visible;
                grpCurbParams.Visibility = Visibility.Collapsed;
            }
            else if (rbCurb.IsChecked == true)
            {
                currentInletType = InletType.Curb;
                grpGrateParams.Visibility = Visibility.Collapsed;
                grpCurbParams.Visibility = Visibility.Visible;
            }
            else if (rbCombination.IsChecked == true)
            {
                currentInletType = InletType.Combination;
                grpGrateParams.Visibility = Visibility.Visible;
                grpCurbParams.Visibility = Visibility.Visible;
            }
            else if (rbSag.IsChecked == true)
            {
                currentInletType = InletType.Grate; // Sag uses grate
                grpGrateParams.Visibility = Visibility.Visible;
                grpCurbParams.Visibility = Visibility.Collapsed;
            }

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
                // Step 1: Calculate gutter flow
                CalculateGutterFlow();

                // Step 2: Calculate inlet capacity
                CalculateInletCapacity();

                // Step 3: Update visualizations
                DrawCrossSection();
                DrawPlanView();
            }
            catch (Exception ex)
            {
                txtDesignNotes.Text = $"⚠ Error: {ex.Message}";
            }
        }

        private void CalculateGutterFlow()
        {
            double Q = Convert.ToDouble(txtDesignFlow.EditValue);
            double Sx = Convert.ToDouble(txtGutterSlope.EditValue);
            double Sw = Convert.ToDouble(txtStreetSlope.EditValue);
            double S0 = Convert.ToDouble(txtLongSlope.EditValue);
            double n = Convert.ToDouble(txtManningN.EditValue);
            double gutterWidth = Convert.ToDouble(txtGutterWidth.EditValue);

            // Calculate using composite gutter (gutter + street)
            gutterFlowResult = GutterFlowCalculator.CalculateCompositeGutter(
                Q, Sx, Sw, S0, n, gutterWidth, 3.0); // Assume 3m street width

            // Update gutter flow results
            txtResultDepth.Text = gutterFlowResult.Depth.ToString("F4");
            txtResultSpread.Text = gutterFlowResult.Spread.ToString("F3");
            txtResultVelocity.Text = gutterFlowResult.Velocity.ToString("F3");
            txtResultFlowArea.Text = gutterFlowResult.FlowArea.ToString("F4");
            txtResultFroude.Text = gutterFlowResult.FroudeNumber.ToString("F2");
            txtResultSpreadRatio.Text = gutterFlowResult.SpreadRatio.ToString("F1");

            // Check spread limit
            double maxSpread = Convert.ToDouble(txtMaxSpread.EditValue);
            if (gutterFlowResult.Spread > maxSpread)
            {
                txtResultSpread.Background = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                txtResultSpread.Background = new SolidColorBrush(Color.FromRgb(230, 243, 255));
            }
        }

        private void CalculateInletCapacity()
        {
            double Q = Convert.ToDouble(txtDesignFlow.EditValue);
            double S0 = Convert.ToDouble(txtLongSlope.EditValue);

            if (rbSag.IsChecked == true)
            {
                // Sag inlet calculation
                double grateLength = Convert.ToDouble(txtGrateLength.EditValue);
                double grateWidth = Convert.ToDouble(txtGrateWidth.EditValue);
                double perimeterLength = 2 * (grateLength + grateWidth);

                inletCapacityResult = InletCapacityCalculator.CalculateSagInlet(
                    Q, gutterFlowResult.Depth, grateLength, grateWidth, perimeterLength);
            }
            else
            {
                switch (currentInletType)
                {
                    case InletType.Grate:
                        CalculateGrateInlet(Q, S0);
                        break;

                    case InletType.Curb:
                        CalculateCurbInlet(Q, S0);
                        break;

                    case InletType.Combination:
                        CalculateCombinationInlet(Q, S0);
                        break;
                }
            }

            // Update inlet capacity results
            txtResultIntercepted.Text = inletCapacityResult.InterceptedFlow.ToString("F2");
            txtResultBypass.Text = inletCapacityResult.BypassFlow.ToString("F2");
            txtResultEfficiency.Text = inletCapacityResult.Efficiency.ToString("F1");
            txtResultClogging.Text = inletCapacityResult.IsCloggingRisk ? "⚠ High" : "✓ Low";

            // Color code efficiency
            if (inletCapacityResult.Efficiency >= 80)
            {
                txtResultEfficiency.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));
            }
            else if (inletCapacityResult.Efficiency >= 50)
            {
                txtResultEfficiency.Background = new SolidColorBrush(Colors.LightYellow);
            }
            else
            {
                txtResultEfficiency.Background = new SolidColorBrush(Colors.LightCoral);
            }

            // Update design notes
            UpdateDesignNotes();
        }

        private void CalculateGrateInlet(double Q, double S0)
        {
            double grateLength = Convert.ToDouble(txtGrateLength.EditValue);
            double grateWidth = Convert.ToDouble(txtGrateWidth.EditValue);
            double cloggingFactor = Convert.ToDouble(txtCloggingFactor.EditValue);

            GrateType grateType = GrateType.P_50_50;
            string selectedGrate = cmbGrateType.EditValue?.ToString() ?? "";
            if (selectedGrate.Contains("Parallel")) grateType = GrateType.Parallel;
            else if (selectedGrate.Contains("Perpendicular")) grateType = GrateType.Perpendicular;

            inletCapacityResult = InletCapacityCalculator.CalculateGrateInlet(
                Q, gutterFlowResult.Depth, gutterFlowResult.Velocity,
                grateLength, grateWidth, grateType, S0, cloggingFactor);
        }

        private void CalculateCurbInlet(double Q, double S0)
        {
            double curbLength = Convert.ToDouble(txtCurbLength.EditValue);
            double curbHeight = Convert.ToDouble(txtCurbHeight.EditValue);
            double depression = Convert.ToDouble(txtDepression.EditValue);

            CurbOpeningType openingType = CurbOpeningType.Horizontal;
            string selectedType = cmbCurbType.EditValue?.ToString() ?? "";
            if (selectedType.Contains("Vertical")) openingType = CurbOpeningType.Vertical;
            else if (selectedType.Contains("Depressed")) openingType = CurbOpeningType.Depressed;

            inletCapacityResult = InletCapacityCalculator.CalculateCurbInlet(
                Q, gutterFlowResult.Depth, gutterFlowResult.Velocity,
                curbLength, curbHeight, openingType, S0, depression);
        }

        private void CalculateCombinationInlet(double Q, double S0)
        {
            double grateLength = Convert.ToDouble(txtGrateLength.EditValue);
            double grateWidth = Convert.ToDouble(txtGrateWidth.EditValue);
            double curbLength = Convert.ToDouble(txtCurbLength.EditValue);
            double curbHeight = Convert.ToDouble(txtCurbHeight.EditValue);
            double cloggingFactor = Convert.ToDouble(txtCloggingFactor.EditValue);

            GrateType grateType = GrateType.P_50_50;
            string selectedGrate = cmbGrateType.EditValue?.ToString() ?? "";
            if (selectedGrate.Contains("Parallel")) grateType = GrateType.Parallel;
            else if (selectedGrate.Contains("Perpendicular")) grateType = GrateType.Perpendicular;

            inletCapacityResult = InletCapacityCalculator.CalculateCombinationInlet(
                Q, gutterFlowResult.Depth, gutterFlowResult.Velocity,
                grateLength, grateWidth, curbLength, curbHeight,
                grateType, S0, cloggingFactor);
        }

        #endregion

        #region Spacing Analysis

        private void AnalyzeSpacingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (inletCapacityResult == null)
                {
                    AutoCalculate();
                }

                double streetLength = Convert.ToDouble(txtStreetLength.EditValue);
                double singleInletCapacity = inletCapacityResult.InterceptedFlow;
                double totalFlow = Convert.ToDouble(txtDesignFlow.EditValue);

                // Determine max spacing based on area type
                double maxSpacing = 50; // Default
                string areaType = cmbAreaType.EditValue?.ToString() ?? "";
                if (areaType.Contains("Residential")) maxSpacing = 45;
                else if (areaType.Contains("Commercial")) maxSpacing = 25;
                else if (areaType.Contains("Industrial")) maxSpacing = 35;

                // Calculate number of inlets needed
                int numInlets = InletCapacityCalculator.CalculateNumberOfInlets(
                    streetLength, totalFlow, singleInletCapacity, maxSpacing);

                // Calculate actual spacing
                double actualSpacing = streetLength / numInlets;

                // Update results
                txtResultRecommendedSpacing.Text = actualSpacing.ToString("F1");
                txtResultMaxSpacing.Text = maxSpacing.ToString("F0");
                txtResultNumInlets.Text = numInlets.ToString();

                // Update plan view
                DrawPlanView();

                // Add to design notes
                txtDesignNotes.Text += $"\n\n📏 SPACING ANALYSIS:\n" +
                    $"• Street length: {streetLength:F0} m\n" +
                    $"• Number of inlets: {numInlets}\n" +
                    $"• Actual spacing: {actualSpacing:F1} m\n" +
                    $"• Max allowable: {maxSpacing:F0} m\n" +
                    $"• Status: {(actualSpacing <= maxSpacing ? "✓ OK" : "⚠ Exceeds limit")}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in spacing analysis: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Design Notes

        private void UpdateDesignNotes()
        {
            string notes = "📋 DESIGN SUMMARY:\n\n";

            // Gutter flow analysis
            notes += "🌊 GUTTER FLOW:\n";
            notes += $"• Flow depth: {gutterFlowResult.Depth:F3} m\n";
            notes += $"• Spread: {gutterFlowResult.Spread:F2} m\n";
            notes += $"• Velocity: {gutterFlowResult.Velocity:F2} m/s\n";
            notes += $"• Froude number: {gutterFlowResult.FroudeNumber:F2}";

            if (gutterFlowResult.FroudeNumber < 1.0)
                notes += " (Subcritical ✓)\n";
            else
                notes += " (Supercritical ⚠)\n";

            double maxSpread = Convert.ToDouble(txtMaxSpread.EditValue);
            if (gutterFlowResult.Spread > maxSpread)
            {
                notes += $"⚠ Spread exceeds limit ({maxSpread:F2}m). Consider:\n";
                notes += "  - Reduce inlet spacing\n";
                notes += "  - Increase gutter capacity\n";
                notes += "  - Add more inlets\n";
            }

            notes += "\n🚰 INLET CAPACITY:\n";
            notes += $"• Type: {GetInletTypeDescription()}\n";
            notes += $"• Intercepted flow: {inletCapacityResult.InterceptedFlow:F2} L/s\n";
            notes += $"• Bypass flow: {inletCapacityResult.BypassFlow:F2} L/s\n";
            notes += $"• Efficiency: {inletCapacityResult.Efficiency:F1}%\n";
            notes += $"• {inletCapacityResult.DesignNote}\n";

            if (inletCapacityResult.IsCloggingRisk)
            {
                notes += "\n⚠ CLOGGING RISK:\n";
                notes += "• Regular maintenance required\n";
                notes += "• Consider debris screens\n";
                notes += "• Increase clogging factor in design\n";
            }

            notes += "\n💡 RECOMMENDATIONS:\n";
            if (inletCapacityResult.Efficiency < 50)
            {
                notes += "⚠ Low efficiency - consider:\n";
                notes += "  • Larger inlet dimensions\n";
                notes += "  • Combination inlet type\n";
                notes += "  • Reduced spacing between inlets\n";
            }
            else if (inletCapacityResult.Efficiency < 80)
            {
                notes += "✓ Acceptable design\n";
                notes += "  • Monitor bypass flow\n";
                notes += "  • Consider future expansion\n";
            }
            else
            {
                notes += "✓ Excellent efficiency\n";
                notes += "  • Design meets requirements\n";
                notes += "  • Good hydraulic performance\n";
            }

            // Design standards reference
            notes += "\n📖 DESIGN STANDARDS:\n";
            notes += "• Based on: HEC-22 Urban Drainage Design Manual\n";
            notes += "• Manning's equation for open channel flow\n";
            notes += "• AASHTO Model Drainage Manual\n";

            txtDesignNotes.Text = notes;
        }

        private string GetInletTypeDescription()
        {
            if (rbGrate.IsChecked == true) return "Grate Inlet (On-grade)";
            if (rbCurb.IsChecked == true) return "Curb Opening Inlet";
            if (rbCombination.IsChecked == true) return "Combination (Grate + Curb)";
            if (rbSag.IsChecked == true) return "Sag Inlet (Low point)";
            return "Unknown";
        }

        #endregion

        #region Visualization

        private void DrawCrossSection()
        {
            if (canvasCrossSection == null || gutterFlowResult == null) return;

            canvasCrossSection.Children.Clear();

            double width = canvasCrossSection.ActualWidth > 0 ? canvasCrossSection.ActualWidth : 400;
            double height = canvasCrossSection.ActualHeight > 0 ? canvasCrossSection.ActualHeight : 200;

            double scale = 80; // pixels per meter
            double offsetX = 50;
            double offsetY = height - 40;

            // Draw street surface
            double gutterWidth = Convert.ToDouble(txtGutterWidth.EditValue);
            double streetWidth = 3.0; // Assume 3m street width
            double totalWidth = gutterWidth + streetWidth;

            Line streetLine = new Line
            {
                X1 = offsetX,
                Y1 = offsetY,
                X2 = offsetX + totalWidth * scale,
                Y2 = offsetY,
                Stroke = Brushes.Black,
                StrokeThickness = 3
            };
            canvasCrossSection.Children.Add(streetLine);

            // Draw gutter slope
            double Sx = Convert.ToDouble(txtGutterSlope.EditValue) / 100.0;
            double gutterDrop = gutterWidth * Sx * scale;

            Line gutterSlope = new Line
            {
                X1 = offsetX,
                Y1 = offsetY,
                X2 = offsetX + gutterWidth * scale,
                Y2 = offsetY - gutterDrop,
                Stroke = Brushes.Brown,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            canvasCrossSection.Children.Add(gutterSlope);

            // Draw street slope
            double Sw = Convert.ToDouble(txtStreetSlope.EditValue) / 100.0;
            double streetDrop = streetWidth * Sw * scale;

            Line streetSlope = new Line
            {
                X1 = offsetX + gutterWidth * scale,
                Y1 = offsetY - gutterDrop,
                X2 = offsetX + totalWidth * scale,
                Y2 = offsetY - gutterDrop - streetDrop,
                Stroke = Brushes.Brown,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            canvasCrossSection.Children.Add(streetSlope);

            // Draw water surface (triangular flow)
            double spread = gutterFlowResult.Spread;
            if (spread > totalWidth) spread = totalWidth;

            double waterDepthAtCurb = gutterFlowResult.Depth * scale;

            PathFigure waterFigure = new PathFigure();
            waterFigure.StartPoint = new Point(offsetX, offsetY);

            if (spread <= gutterWidth)
            {
                // Water only in gutter
                double waterTop = offsetX + spread * scale;
                double waterDepthAtTop = spread * Sx * scale;
                waterFigure.Segments.Add(new LineSegment(new Point(waterTop, offsetY - waterDepthAtTop), true));
                waterFigure.Segments.Add(new LineSegment(new Point(waterTop, offsetY), true));
            }
            else
            {
                // Water in gutter + street
                double gutterEndX = offsetX + gutterWidth * scale;
                waterFigure.Segments.Add(new LineSegment(new Point(gutterEndX, offsetY - gutterDrop), true));

                double remainingSpread = spread - gutterWidth;
                double waterEndX = gutterEndX + remainingSpread * scale;
                double waterDepthAtEnd = gutterDrop + remainingSpread * Sw * scale;
                waterFigure.Segments.Add(new LineSegment(new Point(waterEndX, offsetY - waterDepthAtEnd), true));
                waterFigure.Segments.Add(new LineSegment(new Point(waterEndX, offsetY), true));
            }

            waterFigure.Segments.Add(new LineSegment(new Point(offsetX, offsetY), true));

            PathGeometry waterGeometry = new PathGeometry();
            waterGeometry.Figures.Add(waterFigure);

            Path waterPath = new Path
            {
                Data = waterGeometry,
                Fill = new SolidColorBrush(Color.FromArgb(150, 0, 120, 255)),
                Stroke = Brushes.Blue,
                StrokeThickness = 2
            };
            canvasCrossSection.Children.Add(waterPath);

            // Draw curb
            Rectangle curb = new Rectangle
            {
                Width = 8,
                Height = 25,
                Fill = Brushes.Gray,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            Canvas.SetLeft(curb, offsetX - 8);
            Canvas.SetTop(curb, offsetY - 25);
            canvasCrossSection.Children.Add(curb);

            // Draw inlet (if grate)
            if (currentInletType == InletType.Grate || currentInletType == InletType.Combination)
            {
                double grateWidth = Convert.ToDouble(txtGrateWidth.EditValue);
                Rectangle grate = new Rectangle
                {
                    Width = grateWidth * scale,
                    Height = 15,
                    Fill = Brushes.DarkGray,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                Canvas.SetLeft(grate, offsetX + 5);
                Canvas.SetTop(grate, offsetY - 8);
                canvasCrossSection.Children.Add(grate);

                // Grate bars
                for (int i = 0; i < 4; i++)
                {
                    Line bar = new Line
                    {
                        X1 = offsetX + 5 + (i * grateWidth * scale / 4),
                        Y1 = offsetY - 8,
                        X2 = offsetX + 5 + (i * grateWidth * scale / 4),
                        Y2 = offsetY + 7,
                        Stroke = Brushes.White,
                        StrokeThickness = 2
                    };
                    canvasCrossSection.Children.Add(bar);
                }
            }

            // Labels
            TextBlock spreadLabel = new TextBlock
            {
                Text = $"Spread = {gutterFlowResult.Spread:F2} m",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Blue
            };
            Canvas.SetLeft(spreadLabel, offsetX + spread * scale / 2 - 40);
            Canvas.SetTop(spreadLabel, 10);
            canvasCrossSection.Children.Add(spreadLabel);

            TextBlock depthLabel = new TextBlock
            {
                Text = $"d = {gutterFlowResult.Depth:F3} m",
                FontSize = 10,
                Foreground = Brushes.DarkBlue
            };
            Canvas.SetLeft(depthLabel, offsetX + 10);
            Canvas.SetTop(depthLabel, offsetY - waterDepthAtCurb - 20);
            canvasCrossSection.Children.Add(depthLabel);

            TextBlock velocityLabel = new TextBlock
            {
                Text = $"v = {gutterFlowResult.Velocity:F2} m/s",
                FontSize = 10,
                Foreground = Brushes.DarkGreen
            };
            Canvas.SetLeft(velocityLabel, offsetX + 10);
            Canvas.SetTop(velocityLabel, offsetY - waterDepthAtCurb / 2);
            canvasCrossSection.Children.Add(velocityLabel);

            // Title
            TextBlock title = new TextBlock
            {
                Text = "Street Cross Section / Mặt cắt ngang đường",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkBlue
            };
            Canvas.SetLeft(title, width / 2 - 120);
            Canvas.SetTop(title, 5);
            canvasCrossSection.Children.Add(title);
        }

        private void DrawPlanView()
        {
            if (canvasPlanView == null || inletCapacityResult == null) return;

            canvasPlanView.Children.Clear();

            double width = canvasPlanView.ActualWidth > 0 ? canvasPlanView.ActualWidth : 400;
            double height = canvasPlanView.ActualHeight > 0 ? canvasPlanView.ActualHeight : 200;

            double streetLength = Convert.ToDouble(txtStreetLength.EditValue);
            double scale = Math.Min((width - 100) / streetLength, 3.0);

            double offsetX = 50;
            double offsetY = height / 2;

            // Draw street centerline
            Line centerline = new Line
            {
                X1 = offsetX,
                Y1 = offsetY,
                X2 = offsetX + streetLength * scale,
                Y2 = offsetY,
                Stroke = Brushes.Gray,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 10, 5 }
            };
            canvasPlanView.Children.Add(centerline);

            // Draw street edges
            Line edge1 = new Line
            {
                X1 = offsetX,
                Y1 = offsetY - 20,
                X2 = offsetX + streetLength * scale,
                Y2 = offsetY - 20,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            canvasPlanView.Children.Add(edge1);

            Line edge2 = new Line
            {
                X1 = offsetX,
                Y1 = offsetY + 20,
                X2 = offsetX + streetLength * scale,
                Y2 = offsetY + 20,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            canvasPlanView.Children.Add(edge2);

            // Draw inlets
            int numInlets = 1;
            double spacing = 50;

            if (!string.IsNullOrEmpty(txtResultNumInlets.Text))
            {
                int.TryParse(txtResultNumInlets.Text, out numInlets);
            }

            if (!string.IsNullOrEmpty(txtResultRecommendedSpacing.Text))
            {
                double.TryParse(txtResultRecommendedSpacing.Text, out spacing);
            }

            for (int i = 0; i < numInlets; i++)
            {
                double inletX = offsetX + (i * spacing * scale) + (spacing * scale / 2);

                // Draw inlet rectangle
                Rectangle inlet = new Rectangle
                {
                    Width = 15,
                    Height = 25,
                    Fill = Brushes.DarkBlue,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                Canvas.SetLeft(inlet, inletX - 7.5);
                Canvas.SetTop(inlet, offsetY - 32);
                canvasPlanView.Children.Add(inlet);

                // Inlet number
                TextBlock inletLabel = new TextBlock
                {
                    Text = $"#{i + 1}",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };
                Canvas.SetLeft(inletLabel, inletX - 8);
                Canvas.SetTop(inletLabel, offsetY - 25);
                canvasPlanView.Children.Add(inletLabel);

                // Spacing dimension
                if (i > 0)
                {
                    double prevInletX = offsetX + ((i - 1) * spacing * scale) + (spacing * scale / 2);
                    Line dimLine = new Line
                    {
                        X1 = prevInletX,
                        Y1 = offsetY + 35,
                        X2 = inletX,
                        Y2 = offsetY + 35,
                        Stroke = Brushes.Red,
                        StrokeThickness = 1
                    };
                    canvasPlanView.Children.Add(dimLine);

                    TextBlock spacingLabel = new TextBlock
                    {
                        Text = $"{spacing:F1}m",
                        FontSize = 10,
                        Foreground = Brushes.Red,
                        FontWeight = FontWeights.Bold
                    };
                    Canvas.SetLeft(spacingLabel, (prevInletX + inletX) / 2 - 20);
                    Canvas.SetTop(spacingLabel, offsetY + 40);
                    canvasPlanView.Children.Add(spacingLabel);
                }
            }

            // Draw flow direction arrow
            Polygon arrow = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(offsetX + streetLength * scale + 20, offsetY),
                    new Point(offsetX + streetLength * scale + 10, offsetY - 5),
                    new Point(offsetX + streetLength * scale + 10, offsetY + 5)
                },
                Fill = Brushes.Blue,
                Stroke = Brushes.DarkBlue,
                StrokeThickness = 1
            };
            canvasPlanView.Children.Add(arrow);

            TextBlock flowLabel = new TextBlock
            {
                Text = "Flow →",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Blue
            };
            Canvas.SetLeft(flowLabel, offsetX + streetLength * scale + 25);
            Canvas.SetTop(flowLabel, offsetY - 10);
            canvasPlanView.Children.Add(flowLabel);

            // Title
            TextBlock title = new TextBlock
            {
                Text = "Inlet Layout / Bố trí hố thu",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkBlue
            };
            Canvas.SetLeft(title, width / 2 - 80);
            Canvas.SetTop(title, 5);
            canvasPlanView.Children.Add(title);

            // Summary info
            TextBlock summary = new TextBlock
            {
                Text = $"Street Length: {streetLength:F0}m | Inlets: {numInlets} | Spacing: {spacing:F1}m",
                FontSize = 10,
                Foreground = Brushes.DarkGreen
            };
            Canvas.SetLeft(summary, offsetX);
            Canvas.SetTop(summary, height - 25);
            canvasPlanView.Children.Add(summary);
        }

        #endregion
    }
}