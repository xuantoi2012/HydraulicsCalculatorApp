using System;
using HydraulicsCalculatorApp.Models;

namespace HydraulicsCalculatorApp.Calculators
{
    public class BoxCulvertCalculator
    {
        public static FlowResult Calculate(double B, double H, double y, double n, double S0, double L, double Ke)
        {
            if (B <= 0 || H <= 0 || y <= 0 || n <= 0 || S0 <= 0 || y > H)
            {
                throw new ArgumentException("Invalid input values");
            }

            var result = new FlowResult();

            result.Y = y;
            result.A = B * y;
            result.A0 = B * H;
            result.RelativeArea = result.A / result.A0;
            result.Pw = B + 2 * y;
            result.Rh = result.A / result.Pw;
            result.T = B;

            result.V = (1 / n) * Math.Pow(result.Rh, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            result.Hv = result.V * result.V / (2 * 9.81);
            result.Q = result.A * result.V;

            double Pw0 = B + 2 * H;
            double Rh0 = result.A0 / Pw0;
            double v0 = (1 / n) * Math.Pow(Rh0, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            result.Q0 = result.A0 * v0;
            result.QRatio = result.Q / result.Q0;

            result.F = result.V / Math.Sqrt(9.81 * result.A / result.T);
            result.Tau = 9810 * result.Rh * S0;

            result.He = Ke * result.Hv;
            result.Hf = n * n * L * result.V * result.V / Math.Pow(result.Rh, 4.0 / 3.0);
            result.Ho = result.Hv;
            result.HL = result.He + result.Hf + result.Ho;
            result.HW = result.Y + result.Hv + result.HL;

            return result;
        }

        public static double FindYFromQ(double targetQ, double B, double H, double n, double S0, out double foundQ, out double foundV)
        {
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
                    yMin = yMid;
                else
                    yMax = yMid;
            }

            double yFinal = (yMin + yMax) / 2.0;
            double aFinal = B * yFinal;
            double PwFinal = B + 2 * yFinal;
            double RhFinal = aFinal / PwFinal;
            foundV = (1.0 / n) * Math.Pow(RhFinal, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            foundQ = aFinal * foundV;
            return yFinal;
        }
    }
}