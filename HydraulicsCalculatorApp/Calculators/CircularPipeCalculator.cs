using System;
using HydraulicsCalculatorApp.Models;

namespace HydraulicsCalculatorApp.Calculators
{
    public class CircularPipeCalculator
    {
        public static FlowResult Calculate(double d0, double n, double S0, double yRatio, double L, double Ke)
        {
            if (d0 <= 0 || n <= 0 || S0 <= 0 || yRatio <= 0 || yRatio > 1.0)
            {
                throw new ArgumentException("Invalid input values");
            }

            var result = new FlowResult();

            // Flow calculations
            result.Y = d0 * yRatio;
            double theta = 2.0 * Math.Acos((d0 / 2.0 - result.Y) / (d0 / 2.0));
            result.A = (d0 * d0 / 8.0) * (theta - Math.Sin(theta));
            result.A0 = Math.PI * d0 * d0 / 4.0;
            result.RelativeArea = result.A / result.A0;
            result.Pw = d0 * theta / 2.0;
            result.Rh = result.A / result.Pw;
            result.T = d0 * Math.Sin(theta / 2.0);
            result.V = (1.0 / n) * Math.Pow(result.Rh, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            result.Hv = result.V * result.V / (2.0 * 9.81);
            result.Q = result.A * result.V;

            double Rh0 = d0 / 4.0;
            double v0 = (1.0 / n) * Math.Pow(Rh0, 2.0 / 3.0) * Math.Pow(S0, 0.5);
            result.Q0 = result.A0 * v0;
            result.QRatio = result.Q / result.Q0;
            result.F = result.V / Math.Sqrt(9.81 * result.A / result.T);
            result.Tau = 9810.0 * result.Rh * S0;

            // Headwater calculations
            result.He = Ke * result.Hv;
            result.Hf = n * n * L * result.V * result.V / Math.Pow(result.Rh, 4.0 / 3.0);
            result.Ho = result.Hv;
            result.HL = result.He + result.Hf + result.Ho;
            result.HW = result.Y + result.Hv + result.HL;

            return result;
        }

        public static double FindYFromQ(double targetQ, double d0, double n, double S0, out double foundQ, out double foundV)
        {
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
                    yMin = yMid;
                else
                    yMax = yMid;
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
    }
}