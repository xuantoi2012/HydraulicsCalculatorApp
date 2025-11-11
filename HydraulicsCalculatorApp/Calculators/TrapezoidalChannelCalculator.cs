using System;
using HydraulicsCalculatorApp.Models;

namespace HydraulicsCalculatorApp.Calculators
{
    public class TrapezoidalChannelCalculator
    {
        public static FlowResult Calculate(double b, double y, double m, double n, double S0)
        {
            if (b <= 0 || y <= 0 || m < 0 || n <= 0 || S0 <= 0)
            {
                throw new ArgumentException("Invalid input values");
            }

            var result = new FlowResult();

            result.Y = y;
            result.A = (b + m * y) * y;
            result.A0 = result.A; // Open channel
            result.RelativeArea = 1.0;
            result.T = b + 2 * m * y;
            result.Pw = b + 2 * y * Math.Sqrt(1 + m * m);
            result.Rh = result.A / result.Pw;
            result.V = (1 / n) * Math.Pow(result.Rh, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            result.Hv = result.V * result.V / (2 * 9.81);
            result.Q = result.A * result.V;
            result.Q0 = result.Q;
            result.QRatio = 1.0;
            result.F = result.V / Math.Sqrt(9.81 * result.A / result.T);
            result.Tau = 9810 * result.Rh * S0;

            result.He = 0;
            result.Hf = 0;
            result.Ho = 0;
            result.HL = 0;
            result.HW = result.Y + result.Hv;

            return result;
        }

        public static double FindYFromQ(double targetQ, double b, double m, double n, double S0, out double foundQ, out double foundV)
        {
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
                    yMin = yMid;
                else
                    yMax = yMid;
            }

            double yFinal = (yMin + yMax) / 2.0;
            double aFinal = (b + m * yFinal) * yFinal;
            double PwFinal = b + 2 * yFinal * Math.Sqrt(1 + m * m);
            double RhFinal = aFinal / PwFinal;
            foundV = (1.0 / n) * Math.Pow(RhFinal, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            foundQ = aFinal * foundV;
            return yFinal;
        }
    }
}