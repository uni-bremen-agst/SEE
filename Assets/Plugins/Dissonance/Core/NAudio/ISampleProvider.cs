// Justification: copied from NAudio, and we want to make the minimal changes possible
// ReSharper disable All

using Dissonance;
using JetBrains.Annotations;

namespace NAudio.Wave
{
    internal interface ISampleProvider
    {
        /// <summary>
        /// Gets the WaveFormat of this Sample Provider.
        /// </summary>
        /// <value>The wave format.</value>
        [NotNull] WaveFormat WaveFormat { get; }

        /// <summary>
        /// Fill the specified buffer with 32 bit floating point samples
        /// </summary>
        /// <param name="buffer">The buffer to fill with samples.</param>
        /// <param name="offset">Offset into buffer</param>
        /// <param name="count">The number of samples to read</param>
        /// <returns>the number of samples written to the buffer.</returns>
        int Read([NotNull] float[] buffer, int offset, int count);
    }
}
