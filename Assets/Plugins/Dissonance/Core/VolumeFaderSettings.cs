using System;
using UnityEngine;

namespace Dissonance
{
    [Serializable]
    public class VolumeFaderSettings
    {
        /// <summary>
        /// Get or set the target volume to play at
        /// </summary>
        [SerializeField] private float _volume;
        public float Volume
        {
            get { return _volume; }
            set { _volume = value; }
        }

        /// <summary>
        /// Get or set the amount of time required to fade in from zero to the target volume.
        /// </summary>
        [SerializeField] private long _fadeInTicks;
        public TimeSpan FadeIn
        {
            get { return new TimeSpan(_fadeInTicks); }
            set { _fadeInTicks = value.Ticks; }
        }

        /// <summary>
        /// Get or set the amount of time required to fade out from target volume to zero
        /// </summary>
        [SerializeField] private long _fadeOutTicks;
        public TimeSpan FadeOut
        {
            get { return new TimeSpan(_fadeOutTicks); }
            set { _fadeOutTicks = value.Ticks; }
        }
    }
}
