using System;
using UnityEngine;

namespace Dissonance.Audio.Playback
{
    internal struct DesyncCalculator
    {
        private const int MaxAllowedDesyncMillis = 1000;
        private const float MaximumPlaybackAdjustment = 0.15f;

        internal int DesyncMilliseconds { get; private set; }
        internal float CorrectedPlaybackSpeed
        {
            get { return CalculateCorrectionFactor(DesyncMilliseconds); }
        }

        internal void Update(TimeSpan ideal, TimeSpan actual)
        {
            DesyncMilliseconds = CalculateDesync(ideal, actual);
        }

        /// <summary>
        /// Inform the desync calculator that the desync should be changed by the given amount (due to a skip in the audio playback)
        /// </summary>
        /// <param name="deltaDesyncMilliseconds"></param>
        internal void Skip(int deltaDesyncMilliseconds)
        {
            DesyncMilliseconds += deltaDesyncMilliseconds;
        }

        #region static helpers
        private static int CalculateDesync(TimeSpan idealPlaybackPosition, TimeSpan actualPlaybackPosition)
        {
            var desync = idealPlaybackPosition - actualPlaybackPosition;

            // allow for jitter on the output, of the unity audio thread tick rate (20ms)
            const int allowedError = 29;

            //If desync is large enough reduce it by the allowed jitter amount, otherwise just clamp straight to zero
            if (desync.TotalMilliseconds > allowedError)
                return (int)desync.TotalMilliseconds - allowedError;
            if (desync.TotalMilliseconds < -allowedError)
                return (int)desync.TotalMilliseconds + allowedError;
            else
                return 0;
        }

        private static float CalculateCorrectionFactor(float desyncMilliseconds)
        {
            var alpha = Mathf.Clamp(desyncMilliseconds / MaxAllowedDesyncMillis, -1, 1);
            return 1 + Mathf.LerpUnclamped(0, MaximumPlaybackAdjustment, alpha);
        }
        #endregion
    }
}
