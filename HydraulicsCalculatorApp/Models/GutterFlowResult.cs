namespace HydraulicsCalculatorApp.Models
{
    public class GutterFlowResult
    {
        // Gutter geometry
        public double Width { get; set; }              // Chiều rộng rãnh (m)
        public double CrossSlope { get; set; }         // Độ dốc ngang (%)
        public double LongSlope { get; set; }          // Độ dốc dọc (%)
        public double Depth { get; set; }              // Độ sâu nước (m)

        // Flow properties
        public double FlowArea { get; set; }           // Diện tích (m²)
        public double WettedPerimeter { get; set; }    // Chu vi ướt (m)
        public double HydraulicRadius { get; set; }    // Bán kính thủy lực (m)
        public double Velocity { get; set; }           // Vận tốc (m/s)
        public double Flow { get; set; }               // Lưu lượng (m³/s hoặc L/s)
        public double FroudeNumber { get; set; }       // Số Froude

        // Spread
        public double Spread { get; set; }             // Độ lan rộng nước (m)
        public double SpreadRatio { get; set; }        // Tỷ lệ lan rộng (%)
    }
}