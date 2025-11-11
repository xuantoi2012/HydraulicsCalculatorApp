using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HydraulicsCalculatorApp.Models;

namespace HydraulicsCalculatorApp.Views
{
    public partial class ChartPanel : UserControl
    {
        public event EventHandler GenerateRequested;

        private List<ChartPoint> chartData = new List<ChartPoint>();

        public ChartPanel()
        {
            InitializeComponent();
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            GenerateRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        public void SetChartData(List<ChartPoint> data)
        {
            chartData = data;
            DrawChart();
            txtChartInfo.Text = $"Chart with {data.Count} points";
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

            // Draw grid
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
                TextBlock yLabel = new TextBlock
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
                TextBlock xLabel = new TextBlock
                {
                    Text = qValue.ToString("F2"),
                    FontSize = 10
                };
                Canvas.SetLeft(xLabel, xPos - 15);
                Canvas.SetTop(xLabel, height - marginBottom + 10);
                canvasChart.Children.Add(xLabel);
            }

            // Title
            TextBlock title = new TextBlock
            {
                Text = "Q-y and V-y Relationship Chart",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkBlue
            };
            Canvas.SetLeft(title, width / 2 - 120);
            Canvas.SetTop(title, 10);
            canvasChart.Children.Add(title);

            // Axis labels
            TextBlock xAxisLabel = new TextBlock
            {
                Text = "Flow Q (m³/s) / Velocity V (m/s)",
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(xAxisLabel, width / 2 - 100);
            Canvas.SetTop(xAxisLabel, height - 30);
            canvasChart.Children.Add(xAxisLabel);

            TextBlock yAxisLabel = new TextBlock
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

            TextBlock qLegendText = new TextBlock
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

            TextBlock vLegendText = new TextBlock
            {
                Text = "V-y curve",
                FontSize = 11
            };
            Canvas.SetLeft(vLegendText, width - marginRight - 100);
            Canvas.SetTop(vLegendText, 53);
            canvasChart.Children.Add(vLegendText);
        }

        public void Clear()
        {
            canvasChart.Children.Clear();
            chartData.Clear();
            txtChartInfo.Text = "";
        }
    }
}