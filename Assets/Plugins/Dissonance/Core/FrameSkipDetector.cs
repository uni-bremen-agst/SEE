using System;

namespace Dissonance
{
    /// <summary>
    /// Detects long frames and reports them as a skip. Expoentially backs off a circuit breaker to prevent detecting multiple frame skips consecutively
    /// </summary>
    internal struct FrameSkipDetector
    {
        #region fields and properties
        private static readonly string MetricFrameTime = Metrics.MetricName("FrameTime");

        private readonly float _maxFrameTime;

        private readonly float _minimumBreakerDuration;
        private readonly float _maxBreakerDuration;
        private readonly float _breakerResetPerSecond;

        private float _breakerCloseTimer;
        private float _currentBreakerDuration;
        private bool _breakerClosed;

        internal bool IsBreakerClosed
        {
            get { return _breakerClosed; }
        }
        #endregion

        /// <summary>
        /// Detects frame skips by inspecting delta time since last frame. Applies exponential backoff to a circuit breaker so it will not detect skips sequentially.
        /// </summary>
        /// <param name="maxFrameTime">Any time greater than this will be reported as a skip</param>
        /// <param name="minimumBreakerDuration">Initial duration of the breaker</param>
        /// <param name="maxBreakerDuration">Maximum duration of the breaker</param>
        /// <param name="breakerResetPerSecond">How much the breaker time reduces per second</param>
        public FrameSkipDetector(TimeSpan maxFrameTime, TimeSpan minimumBreakerDuration, TimeSpan maxBreakerDuration, TimeSpan breakerResetPerSecond)
        {
            _maxFrameTime = (float)maxFrameTime.TotalSeconds;
            _minimumBreakerDuration = (float)minimumBreakerDuration.TotalSeconds;
            _maxBreakerDuration = (float)maxBreakerDuration.TotalSeconds;
            _breakerResetPerSecond = (float)breakerResetPerSecond.TotalSeconds;

            _breakerClosed = true;
            _breakerCloseTimer = 0;
            _currentBreakerDuration = _minimumBreakerDuration;
        }

        public bool IsFrameSkip(float deltaTime)
        {
            Metrics.Sample(MetricFrameTime, deltaTime);

            var skip = deltaTime > _maxFrameTime;
            var report = skip && _breakerClosed;

            UpdateBreaker(skip, deltaTime);

            return report;
        }

        private void UpdateBreaker(bool skip, float dt)
        {
            if (skip)
            {
                //If there's a frame skip open the circuit breaker
                _breakerClosed = false;

                //Exponentially backoff breaker duration while frames are skipping
                _currentBreakerDuration = Math.Min(_currentBreakerDuration * 2, _maxBreakerDuration);
            }
            else
            {
                //Linearly reduce the duration while no skipping is occuring
                _currentBreakerDuration = Math.Max(_currentBreakerDuration - _breakerResetPerSecond * dt, _minimumBreakerDuration);
            }

            //Update the timer, if it's long enough then close the breaker again
            _breakerCloseTimer += dt;
            if (_breakerCloseTimer >= _currentBreakerDuration)
            {
                _breakerCloseTimer = 0;
                _breakerClosed = true;
            }
        }
    }
}
