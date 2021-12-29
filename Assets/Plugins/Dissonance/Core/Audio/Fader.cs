using UnityEngine;

namespace Dissonance.Audio
{
    internal struct Fader
    {
        public float Volume { get; private set; }

        private float _fadeTime;
        public float EndVolume { get; private set; }
        public float StartVolume { get; private set; }

        private float _elapsedTime;

        public void Update(float dt)
        {
            _elapsedTime += dt;

            Volume = CalculateVolume();
        }

        private float CalculateVolume()
        {
            if (_fadeTime <= 0 || _elapsedTime >= _fadeTime)
                return EndVolume;

            var t = _elapsedTime / _fadeTime;
            var v = Mathf.Lerp(StartVolume, EndVolume, t);

            return v;
        }

        /// <summary>
        /// Begin a fade from the current volume to the given target value
        /// </summary>
        /// <param name="target">volume to transition to</param>
        /// <param name="duration">How many seconds the transition should take</param>
        public void FadeTo(float target, float duration)
        {
            _fadeTime = duration;
            _elapsedTime = 0;

            //Fade from current value to target value
            StartVolume = Volume;
            EndVolume = target;

            // Handle the special case where we're trying to instantly transition to a volume
            if (duration <= 0)
                Volume = EndVolume;
        }
    }
}
