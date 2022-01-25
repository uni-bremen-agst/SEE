using UnityEngine;

namespace Dissonance.Datastructures
{
    internal class WindowDeviationCalculator
        : BaseWindowCalculator<float>
    {
        #region fields and properties
        private float _sum;
        private float _sumOfSquares;

        /// <summary>
        /// Standard deviation of latency. 68.2% of values should be within 1 std deviation of the mean
        /// </summary>
        public float StdDev { get; private set; }

        public float Mean { get; private set; }

        public float Confidence
        {
            get { return Count / (float)Capacity; }
        }
        #endregion

        #region constructor
        public WindowDeviationCalculator(uint size)
            : base(size)
        {
        }
        #endregion

        protected override void Updated(float? removed, float added)
        {
            //Update accumulated values to remove old value
            if (removed.HasValue)
            {
                _sum -= removed.Value;
                _sumOfSquares -= removed.Value * removed.Value;
            }

            //Update accumulated values with new value
            _sum += added;
            _sumOfSquares += added * added;

            //Update calculated values
            StdDev = CalculateDeviation(_sum / Count, _sumOfSquares / Count);
            Mean = _sum / Count;
        }

        private float CalculateDeviation(float mean, float meanOfSquares)
        {
            //early exit for the special cases of a near empty buffer
            if (Count <= 1)
                return 0;

            //variance is E(X^2) - E(X)^2
            var variance = meanOfSquares - mean * mean;

            //std dev = Sqrt(variance)
            return Mathf.Sqrt(Mathf.Max(0, variance));
        }

        public override void Clear()
        {
            _sum = 0;
            _sumOfSquares = 0;
            StdDev = 0;

            base.Clear();
        }
    }
}
