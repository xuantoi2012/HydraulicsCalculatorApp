namespace HydraulicsCalculatorApp.Models
{
    public class InletCapacityResult
    {
        // Inlet properties
        public InletType Type { get; set; }
        public double Length { get; set; }             // Chiều dài hố ga (m)
        public double Width { get; set; }              // Chiều rộng hố ga (m)

        // Capacity
        public double InterceptedFlow { get; set; }    // Lưu lượng thu được (L/s)
        public double BypassFlow { get; set; }         // Lưu lượng vượt qua (L/s)
        public double Efficiency { get; set; }         // Hiệu suất thu (%)

        // Design parameters
        public double ApproachFlow { get; set; }       // Lưu lượng đến (L/s)
        public double ApproachDepth { get; set; }      // Độ sâu nước đến (m)
        public double ApproachVelocity { get; set; }   // Vận tốc đến (m/s)

        // Spacing
        public double RecommendedSpacing { get; set; } // Khoảng cách đề xuất (m)
        public double MaxSpacing { get; set; }         // Khoảng cách tối đa (m)
        public double NumberOfInlets { get; set; }     // Số lượng hố ga cần

        // Warnings
        public string DesignNote { get; set; }
        public bool IsCloggingRisk { get; set; }
    }
}