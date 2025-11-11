using HydraulicsCalculatorApp.Models;
using System;
using System.Security.Policy;

namespace HydraulicsCalculatorApp.Calculators
{
    /// <summary>
    /// Calculator for inlet capacity and spacing
    /// Based on HEC-22 Urban Drainage Design Manual
    /// </summary>
    public static class InletCapacityCalculator
    {
        /// <summary>
        /// Calculate grate inlet capacity on grade
        /// </summary>
        public static InletCapacityResult CalculateGrateInlet(
            double Q, double depth, double velocity,
            double grateLength, double grateWidth,
            GrateType grateType, double S0, double cloggingFactor = 0.5)
        {
            var result = new InletCapacityResult
            {
                Type = InletType.Grate,
                Length = grateLength,
                Width = grateWidth,
                ApproachFlow = Q,
                ApproachDepth = depth,
                ApproachVelocity = velocity
            };

            // Froude number
            double Fr = velocity / Math.Sqrt(9.81 * depth);

            // Splash-over velocity (HEC-22 equation)
            double Vo = 0;
            switch (grateType)
            {
                case GrateType.Parallel:
                    Vo = 0.295 * Math.Pow(grateLength, 0.5);
                    break;
                case GrateType.Perpendicular:
                    Vo = 0.540 * Math.Pow(grateLength, 0.5);
                    break;
                case GrateType.P_50_50:
                    Vo = 0.425 * Math.Pow(grateLength, 0.5);
                    break;
            }

            // Frontal flow interception ratio (Rf)
            double Rf = 0;
            if (velocity < Vo)
            {
                Rf = 1.0;
            }
            else
            {
                Rf = 1.0 - 0.09 * Math.Pow(velocity - Vo, 1.8);
                if (Rf < 0) Rf = 0;
            }

            // Side flow interception ratio (Rs)
            double Eo = grateWidth / grateLength;
            double Rs = 1.0 / (1.0 + 0.15 * Math.Pow(velocity, 2.3) / Math.Pow(Eo * grateLength, 1.8));

            // Total interception efficiency
            double E = Rf + Rs * (1 - Rf);

            // Apply clogging factor
            E = E * cloggingFactor;

            // Intercepted flow
            result.InterceptedFlow = Q * E;
            result.BypassFlow = Q - result.InterceptedFlow;
            result.Efficiency = E * 100.0;

            // Calculate spacing
            result.RecommendedSpacing = CalculateInletSpacing(Q, result.InterceptedFlow, S0);
            result.MaxSpacing = result.RecommendedSpacing * 1.5;

            // Design notes
            if (E < 0.5)
            {
                result.DesignNote = "⚠ Low efficiency. Consider larger grate or combination inlet.";
            }
            else if (E > 0.9)
            {
                result.DesignNote = "✓ Good efficiency. Design is adequate.";
            }
            else
            {
                result.DesignNote = "✓ Acceptable efficiency.";
            }

            result.IsCloggingRisk = cloggingFactor < 0.6;

            return result;
        }

        /// <summary>
        /// Calculate curb opening inlet capacity
        /// </summary>
        public static InletCapacityResult CalculateCurbInlet(
            double Q, double depth, double velocity,
            double openingLength, double openingHeight,
            CurbOpeningType openingType, double S0, double depression = 0)
        {
            var result = new InletCapacityResult
            {
                Type = InletType.Curb,
                Length = openingLength,
                Width = openingHeight,
                ApproachFlow = Q,
                ApproachDepth = depth,
                ApproachVelocity = velocity
            };

            double Qm3s = Q / 1000.0; // Convert to m³/s
            double S0Decimal = S0 / 100.0;

            // Calculate interception length (HEC-22)
            double Lt = 0;

            if (openingType == CurbOpeningType.Depressed)
            {
                // With depression
                double a = depression;
                Lt = 0.6 * Math.Pow(Qm3s / (Math.Pow(S0Decimal, 0.5)), 0.42) *
                     Math.Pow(1.0 / (a + depth), 0.3);
            }
            else
            {
                // Without depression (horizontal opening)
                Lt = 0.6 * Math.Pow(Qm3s / (Math.Pow(S0Decimal, 0.5)), 0.42) *
                     Math.Pow(1.0 / depth, 0.3);
            }

            // Efficiency
            double E = 0;
            if (openingLength >= Lt)
            {
                E = 1.0; // 100% interception
            }
            else
            {
                E = Math.Pow(openingLength / Lt, 1.8);
            }

            // Intercepted flow
            result.InterceptedFlow = Q * E;
            result.BypassFlow = Q - result.InterceptedFlow;
            result.Efficiency = E * 100.0;

            // Calculate spacing
            result.RecommendedSpacing = CalculateInletSpacing(Q, result.InterceptedFlow, S0);
            result.MaxSpacing = result.RecommendedSpacing * 1.5;

            // Design notes
            if (openingLength < Lt)
            {
                result.DesignNote = $"⚠ Opening length ({openingLength:F2}m) less than required length ({Lt:F2}m).";
            }
            else
            {
                result.DesignNote = "✓ Opening length is adequate for 100% interception.";
            }

            return result;
        }

        /// <summary>
        /// Calculate combination inlet capacity
        /// </summary>
        public static InletCapacityResult CalculateCombinationInlet(
            double Q, double depth, double velocity,
            double grateLength, double grateWidth,
            double curbLength, double curbHeight,
            GrateType grateType, double S0, double cloggingFactor = 0.5)
        {
            // Calculate grate capacity
            var grateResult = CalculateGrateInlet(Q, depth, velocity, grateLength, grateWidth,
                grateType, S0, cloggingFactor);

            // Bypass flow from grate goes to curb opening
            double bypassToCurb = grateResult.BypassFlow;

            // Calculate curb opening capacity for bypass flow
            var curbResult = CalculateCurbInlet(bypassToCurb, depth, velocity,
                curbLength, curbHeight, CurbOpeningType.Horizontal, S0);

            // Combine results
            var result = new InletCapacityResult
            {
                Type = InletType.Combination,
                Length = Math.Max(grateLength, curbLength),
                Width = grateWidth,
                ApproachFlow = Q,
                ApproachDepth = depth,
                ApproachVelocity = velocity
            };

            // Total intercepted flow
            result.InterceptedFlow = grateResult.InterceptedFlow + curbResult.InterceptedFlow;
            result.BypassFlow = Q - result.InterceptedFlow;
            result.Efficiency = (result.InterceptedFlow / Q) * 100.0;

            // Calculate spacing
            result.RecommendedSpacing = CalculateInletSpacing(Q, result.InterceptedFlow, S0);
            result.MaxSpacing = result.RecommendedSpacing * 1.5;

            // Design notes
            result.DesignNote = $"✓ Combination inlet: Grate captures {grateResult.Efficiency:F1}%, " +
                              $"Curb captures {curbResult.Efficiency:F1}% of bypass. " +
                              $"Total efficiency: {result.Efficiency:F1}%";

            result.IsCloggingRisk = cloggingFactor < 0.6;

            return result;
        }

        /// <summary>
        /// Calculate recommended inlet spacing along street
        /// </summary>
        private static double CalculateInletSpacing(double totalFlow, double inletCapacity, double slope)
        {
            // Based on allowable spread and drainage area
            // Typical spacing: 30-60m for residential, 15-30m for commercial

            if (inletCapacity <= 0) return 30; // Default minimum

            // Number of inlets needed
            double numInlets = Math.Ceiling(totalFlow / inletCapacity);

            // Assume catchment area length (this would normally be provided)
            // For now, use empirical relationship
            double assumedCatchmentLength = 100; // meters

            double spacing = assumedCatchmentLength / numInlets;

            // Apply limits
            if (spacing < 15) spacing = 15;   // Minimum spacing
            if (spacing > 60) spacing = 60;   // Maximum spacing

            return spacing;
        }

        /// <summary>
        /// Calculate number of inlets required for a street segment
        /// </summary>
        public static int CalculateNumberOfInlets(double streetLength, double totalFlow,
            double singleInletCapacity, double maxSpacing = 50)
        {
            // Calculate based on capacity
            int numByCapacity = (int)Math.Ceiling(totalFlow / singleInletCapacity);

            // Calculate based on spacing
            int numBySpacing = (int)Math.Ceiling(streetLength / maxSpacing);

            // Use the larger value
            return Math.Max(numByCapacity, numBySpacing);
        }

        /// <summary>
        /// Calculate sag inlet capacity (at low point)
        /// </summary>
        public static InletCapacityResult CalculateSagInlet(
            double Q, double depth,
            double grateLength, double grateWidth,
            double perimeterLength)
        {
            var result = new InletCapacityResult
            {
                Type = InletType.Grate,
                Length = grateLength,
                Width = grateWidth,
                ApproachFlow = Q,
                ApproachDepth = depth
            };

            double Qm3s = Q / 1000.0;

            // Weir flow (shallow depth)
            double Cw = 1.66; // Weir coefficient
            double Qweir = Cw * perimeterLength * Math.Pow(depth, 1.5);

            // Orifice flow (deep depth)
            double Co = 0.67; // Orifice coefficient
            double Ao = grateLength * grateWidth; // Opening area
            double Qorifice = Co * Ao * Math.Sqrt(2 * 9.81 * depth);

            // Use minimum (controlling condition)
            double Qcapacity = Math.Min(Qweir, Qorifice);

            result.InterceptedFlow = Math.Min(Q, Qcapacity * 1000); // Convert back to L/s
            result.BypassFlow = Q - result.InterceptedFlow;
            result.Efficiency = (result.InterceptedFlow / Q) * 100.0;

            if (result.Efficiency >= 100)
            {
                result.DesignNote = "✓ Inlet has adequate capacity for sag location.";
            }
            else
            {
                result.DesignNote = $"⚠ Inlet undersized. Ponding depth will increase. Consider larger inlet.";
            }

            return result;
        }
    }
}