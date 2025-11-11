using System.Collections.Generic;

namespace HydraulicsCalculatorApp.Helpers
{
    public static class MaterialDatabase
    {
        public static Dictionary<string, double> CircularPipeMaterials = new Dictionary<string, double>
        {
            { "PVC/Plastic pipe", 0.009 },
            { "Concrete pipe, good condition", 0.012 },
            { "Concrete pipe, normal", 0.013 },
            { "Concrete pipe, rough", 0.015 },
            { "Corrugated metal pipe", 0.024 },
            { "Cast iron pipe", 0.013 },
            { "Steel pipe", 0.012 }
        };

        public static Dictionary<string, double> BoxCulvertMaterials = new Dictionary<string, double>
        {
            { "Concrete, trowel finish", 0.011 },
            { "Concrete, good condition", 0.012 },
            { "Concrete, normal", 0.013 },
            { "Concrete, rough", 0.015 },
            { "Brick with cement mortar", 0.014 },
            { "Stone masonry", 0.017 }
        };

        public static Dictionary<string, double> ChannelMaterials = new Dictionary<string, double>
        {
            { "Concrete lined channel", 0.013 },
            { "Earth channel, clean", 0.022 },
            { "Earth channel, gravel", 0.025 },
            { "Earth channel, weeds", 0.030 },
            { "Natural stream, clean", 0.030 },
            { "Natural stream, stones", 0.040 },
            { "Grass-lined channel", 0.035 },
            { "Rock riprap", 0.033 }
        };

        public static Dictionary<string, double> EntranceLossCoefficients = new Dictionary<string, double>
        {
            { "Square edge", 0.5 },
            { "Rounded edge", 0.2 },
            { "Grooved end", 0.2 },
            { "Projecting", 0.9 },
            { "Wingwalls", 0.4 }
        };

        public static double ExtractManningN(string materialText)
        {
            int startIndex = materialText.IndexOf("n=");
            if (startIndex > 0)
            {
                int endIndex = materialText.IndexOf(")", startIndex);
                if (endIndex > 0)
                {
                    string nValueStr = materialText.Substring(startIndex + 2, endIndex - startIndex - 2);
                    if (double.TryParse(nValueStr, out double nValue))
                    {
                        return nValue;
                    }
                }
            }
            return 0;
        }

        public static double ExtractKe(string text)
        {
            int startIndex = text.IndexOf("Ke=");
            if (startIndex > 0)
            {
                int endIndex = text.IndexOf(")", startIndex);
                if (endIndex > 0)
                {
                    string keStr = text.Substring(startIndex + 3, endIndex - startIndex - 3);
                    if (double.TryParse(keStr, out double keValue))
                    {
                        return keValue;
                    }
                }
            }
            return -1;
        }
    }
}