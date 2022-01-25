using System;

namespace Dissonance.Editor
{
    internal static class Helpers
    {
        public const float MinDecibels = -60;

        public static float ToDecibels(float multiplier)
        {
            if (multiplier <= 0)
                return MinDecibels;
            return (float)(20 * Math.Log10(multiplier));
        }

        public static float FromDecibels(float db)
        {
            if (db <= MinDecibels)
                return 0;
            return (float)Math.Pow(10, db / 20);
        }
    }
}
