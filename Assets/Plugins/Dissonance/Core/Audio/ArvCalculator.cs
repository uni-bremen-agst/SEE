using System;

namespace Dissonance.Audio
{
    internal struct ArvCalculator
    {
        public float ARV { get; private set; }

        public void Reset()
        {
            ARV = 0;
        }

        public void Update(ArraySegment<float> samples)
        {
            if (samples.Array == null)
                throw new ArgumentNullException("samples");

            float sum = 0;
            for (var i = 0; i < samples.Count; i++)
                sum += Math.Abs(samples.Array[samples.Offset + i]);

            ARV = sum / samples.Count;
        }
    }
}
