using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HydraulicsCalculatorApp.Models;

namespace HydraulicsCalculatorApp.Views
{
    public partial class PreviewPanel : UserControl
    {
        public PreviewPanel()
        {
            InitializeComponent();
        }

        public void DrawCircularPipe(double d0, double y)
        {
            canvasPreview.Children.Clear();

            double centerX = 180;
            double centerY = 125;
            double radius = 80;

            // Draw pipe circle
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

            // Draw water
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

            // Water surface line
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

            // Label
            TextBlock label = new TextBlock
            {
                Text = "Circular Pipe (Cống tròn)",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.DarkBlue
            };
            Canvas.SetLeft(label, 10);
            Canvas.SetTop(label, 10);
            canvasPreview.Children.Add(label);

            // Depth label
            double fillRatio = y / d0 * 100;
            TextBlock depthLabel = new TextBlock
            {
                Text = $"y = {y:F3} m ({fillRatio:F1}%)",
                FontSize = 10,
                Foreground = Brushes.Blue,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(depthLabel, 10);
            Canvas.SetTop(depthLabel, 30);
            canvasPreview.Children.Add(depthLabel);
        }

        public void DrawBoxCulvert(double B, double H, double y)
        {
            canvasPreview.Children.Clear();

            double scale = Math.Min(50, 150 / Math.Max(B, H));
            double offsetX = 80;
            double offsetY = 40;

            double boxWidth = B * scale;
            double boxHeight = H * scale;
            double waterHeight = y * scale;

            // Draw box
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

            // Draw water
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

            // Water surface
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

            // Label
            TextBlock label = new TextBlock
            {
                Text = "Box Culvert (Cống hộp)",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.DarkBlue
            };
            Canvas.SetLeft(label, 10);
            Canvas.SetTop(label, 10);
            canvasPreview.Children.Add(label);

            // Dimensions
            double fillRatio = y / H * 100;
            TextBlock dimLabel = new TextBlock
            {
                Text = $"B={B:F2}m, H={H:F2}m\ny={y:F2}m ({fillRatio:F1}%)",
                FontSize = 10,
                Foreground = Brushes.Blue,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(dimLabel, 10);
            Canvas.SetTop(dimLabel, 30);
            canvasPreview.Children.Add(dimLabel);
        }

        public void DrawTrapezoidal(double b, double y, double m)
        {
            canvasPreview.Children.Clear();

            double scale = Math.Min(40, 120 / Math.Max(b + 2 * m * y, y));
            double offsetX = 100;
            double offsetY = 60;

            double bottomWidth = b * scale;
            double height = y * scale;
            double sideExtension = m * y * scale;

            // Draw channel outline
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

            // Draw water
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

            // Label
            TextBlock label = new TextBlock
            {
                Text = m == 0 ? "Rectangular (Chữ nhật)" : "Trapezoidal (Hình thang)",
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Foreground = Brushes.DarkBlue
            };
            Canvas.SetLeft(label, 10);
            Canvas.SetTop(label, 10);
            canvasPreview.Children.Add(label);

            // Dimensions
            TextBlock dimLabel = new TextBlock
            {
                Text = $"b={b:F2}m, y={y:F2}m\nm={m:F1} (slope)",
                FontSize = 10,
                Foreground = Brushes.Blue,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(dimLabel, 10);
            Canvas.SetTop(dimLabel, 30);
            canvasPreview.Children.Add(dimLabel);
        }

        public void Clear()
        {
            canvasPreview.Children.Clear();
        }
    }
}