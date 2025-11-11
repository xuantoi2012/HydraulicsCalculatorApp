namespace HydraulicsCalculatorApp.Models
{
    public class FlowResult
    {
        // Flow parameters
        public double Y { get; set; }
        public double A { get; set; }
        public double A0 { get; set; }
        public double RelativeArea { get; set; }
        public double Pw { get; set; }
        public double Rh { get; set; }
        public double T { get; set; }

        // Velocity and flow
        public double V { get; set; }
        public double Hv { get; set; }
        public double Q { get; set; }
        public double Q0 { get; set; }
        public double QRatio { get; set; }

        // Hydraulic characteristics
        public double F { get; set; }
        public double Tau { get; set; }

        // Headwater
        public double He { get; set; }
        public double Hf { get; set; }
        public double Ho { get; set; }
        public double HL { get; set; }
        public double HW { get; set; }
    }
}