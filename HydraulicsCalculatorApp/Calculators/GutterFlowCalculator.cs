using System;
using HydraulicsCalculatorApp.Models;

namespace HydraulicsCalculatorApp.Calculators
{
    /// <summary>
    /// Calculator for gutter flow using Manning's equation
    /// Based on HEC-22 Urban Drainage Design Manual
    /// </summary>
    public static class GutterFlowCalculator
    {
        /// <summary>
        /// Calculate gutter flow properties
        /// </summary>
        /// <param name="Q">Flow rate (L/s)</param>
        /// <param name="Sx">Cross slope (%)</param>
        /// <param name="S0">Longitudinal slope (%)</param>
        /// <param name="n">Manning's n</param>
        /// <param name="gutterWidth">Width of gutter (m)</param>
        /// <returns>GutterFlowResult</returns>
        public static GutterFlowResult CalculateFlow(double Q, double Sx, double S0, double n, double gutterWidth)
        {
            // Convert units
            double Qm3s = Q / 1000.0; // L/s to m³/s
            double SxDecimal = Sx / 100.0;
            double S0Decimal = S0 / 100.0;

            var result = new GutterFlowResult
            {
                Width = gutterWidth,
                CrossSlope = Sx,
                LongSlope = S0,
                Flow = Q
            };

            // Calculate spread (T) using Manning's equation for triangular gutter
            // Q = (Ku/n) * Sx^(5/3) * S0^(1/2) * T^(8/3)
            // Ku = 0.376 for SI units
            double Ku = 0.376;

            // Solve for T (spread)
            // T = [(Q * n) / (Ku * Sx^(5/3) * S0^(1/2))]^(3/8)
            double T = Math.Pow(
                (Qm3s * n) / (Ku * Math.Pow(SxDecimal, 5.0 / 3.0) * Math.Pow(S0Decimal, 0.5)),
                3.0 / 8.0
            );

            result.Spread = T;
            result.SpreadRatio = (T / gutterWidth) * 100.0;

            // Calculate depth at curb
            double d = SxDecimal * T;
            result.Depth = d;

            // Calculate flow area (triangular)
            double A = 0.5 * T * d;
            result.FlowArea = A;

            // Calculate wetted perimeter
            double P = T * Math.Sqrt(1 + SxDecimal * SxDecimal);
            result.WettedPerimeter = P;

            // Hydraulic radius
            result.HydraulicRadius = A / P;

            // Velocity
            result.Velocity = Qm3s / A;

            // Froude number
            double yHydraulic = A / T; // Mean hydraulic depth
            result.FroudeNumber = result.Velocity / Math.Sqrt(9.81 * yHydraulic);

            return result;
        }

        /// <summary>
        /// Calculate required gutter width for given flow and max spread
        /// </summary>
        public static double CalculateRequiredGutterWidth(double Q, double Sx, double S0, double n, double maxSpread)
        {
            var result = CalculateFlow(Q, Sx, S0, n, maxSpread);
            return result.Spread;
        }

        /// <summary>
        /// Calculate gutter capacity for composite section (gutter + street)
        /// </summary>
        public static GutterFlowResult CalculateCompositeGutter(
            double Q, double Sx, double Sw, double S0, double n,
            double gutterWidth, double streetWidth)
        {
            // Sx: Cross slope of gutter
            // Sw: Cross slope of street
            // This uses the composite section method from HEC-22

            var result = new GutterFlowResult
            {
                Width = gutterWidth,
                CrossSlope = Sx,
                LongSlope = S0,
                Flow = Q
            };

            // Convert units
            double Qm3s = Q / 1000.0;
            double SxDecimal = Sx / 100.0;
            double SwDecimal = Sw / 100.0;
            double S0Decimal = S0 / 100.0;

            // Eo = ratio of cross slopes
            double Eo = SwDecimal / SxDecimal;

            // Calculate spread using composite section formula
            double Ku = 0.376;

            // For composite section: Q = Qs + Qw
            // Using iterative method to find spread
            double T = 1.0; // Initial guess
            double tolerance = 0.0001;
            int maxIterations = 100;

            for (int i = 0; i < maxIterations; i++)
            {
                double Qcalc = CalculateCompositeFlow(T, gutterWidth, Sx, Sw, S0, n);

                if (Math.Abs(Qcalc - Qm3s) < tolerance)
                {
                    result.Spread = T;
                    break;
                }

                // Adjust T
                if (Qcalc < Qm3s)
                    T += 0.1;
                else
                    T -= 0.05;

                if (i == maxIterations - 1)
                    result.Spread = T; // Use best approximation
            }

            // Calculate other properties
            double d = SxDecimal * gutterWidth + SwDecimal * (result.Spread - gutterWidth);
            result.Depth = d;

            // Calculate areas
            double Ag = 0.5 * gutterWidth * gutterWidth * SxDecimal; // Gutter area
            double Aw = 0.5 * (result.Spread - gutterWidth) * (result.Spread - gutterWidth) * SwDecimal; // Street area
            result.FlowArea = Ag + Aw;

            // Velocity
            result.Velocity = Qm3s / result.FlowArea;

            // Spread ratio
            result.SpreadRatio = (result.Spread / (gutterWidth + streetWidth)) * 100.0;

            return result;
        }

        private static double CalculateCompositeFlow(double T, double W, double Sx, double Sw, double S0, double n)
        {
            double SxDecimal = Sx / 100.0;
            double SwDecimal = Sw / 100.0;
            double S0Decimal = S0 / 100.0;
            double Ku = 0.376;

            if (T <= W)
            {
                // Flow only in gutter
                return (Ku / n) * Math.Pow(SxDecimal, 5.0 / 3.0) * Math.Pow(S0Decimal, 0.5) * Math.Pow(T, 8.0 / 3.0);
            }
            else
            {
                // Flow in gutter + street
                double Qs = (Ku / n) * Math.Pow(SxDecimal, 5.0 / 3.0) * Math.Pow(S0Decimal, 0.5) * Math.Pow(W, 8.0 / 3.0);
                double Ts = T - W;
                double Qw = (Ku / n) * Math.Pow(SwDecimal, 5.0 / 3.0) * Math.Pow(S0Decimal, 0.5) * Math.Pow(Ts, 8.0 / 3.0);
                return Qs + Qw;
            }
        }
    }
}