using System;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// A ring buffer of audio samples that is written by an audio thread and
    /// read by Unity's main thread. If it is full, the oldest samples are dropped.
    /// </summary>
    /// <remarks>The samples are kept in memory only. This class is thread safe.</remarks>
    public class TranscriptAudioBuffer
    {
        /// <summary>
        /// How many seconds of audio a buffer should be able to hold. This must be more
        /// than the longest time the main thread may be blocked, e.g., while loading a city.
        /// </summary>
        public const int DefaultSeconds = 30;

        /// <summary>
        /// The stored samples. The oldest one is at index <see cref="start"/>.
        /// </summary>
        private readonly float[] samples;

        /// <summary>
        /// Index of the oldest sample in <see cref="samples"/>.
        /// </summary>
        private int start;

        /// <summary>
        /// Number of samples currently stored.
        /// </summary>
        private int count;

        /// <summary>
        /// Backing field of <see cref="DroppedSamples"/>.
        /// </summary>
        private long droppedSamples;

        /// <summary>
        /// Protects all fields against concurrent access by the writing and the reading thread.
        /// </summary>
        private readonly object mutex = new();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capacity">The maximal number of samples that can be stored.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is not positive.</exception>
        public TranscriptAudioBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be positive.");
            }
            samples = new float[capacity];
        }

        /// <summary>
        /// The maximal number of samples that can be stored.
        /// </summary>
        public int Capacity => samples.Length;

        /// <summary>
        /// The number of samples currently stored.
        /// </summary>
        public int Count
        {
            get
            {
                lock (mutex)
                {
                    return count;
                }
            }
        }

        /// <summary>
        /// The total number of samples that were dropped because the buffer was full.
        /// </summary>
        public long DroppedSamples
        {
            get
            {
                lock (mutex)
                {
                    return droppedSamples;
                }
            }
        }

        /// <summary>
        /// Appends <paramref name="length"/> elements of <paramref name="source"/> starting
        /// at <paramref name="offset"/>. If there is not enough room, the oldest samples
        /// are dropped and counted in <see cref="DroppedSamples"/>.
        /// </summary>
        /// <param name="source">The array containing the samples to be appended.</param>
        /// <param name="offset">The index of the first sample in <paramref name="source"/>.</param>
        /// <param name="length">The number of samples to be appended.</param>
        public void Write(float[] source, int offset, int length)
        {
            lock (mutex)
            {
                if (length > samples.Length)
                {
                    // Only the newest samples fit at all.
                    int tooMany = length - samples.Length;
                    droppedSamples += tooMany;
                    offset += tooMany;
                    length = samples.Length;
                }

                int overflow = count + length - samples.Length;
                if (overflow > 0)
                {
                    start = (start + overflow) % samples.Length;
                    count -= overflow;
                    droppedSamples += overflow;
                }

                // The free space may be split by the end of the array.
                int end = (start + count) % samples.Length;
                int firstPart = Math.Min(length, samples.Length - end);
                Array.Copy(source, offset, samples, end, firstPart);
                Array.Copy(source, offset + firstPart, samples, 0, length - firstPart);
                count += length;
            }
        }

        /// <summary>
        /// Moves the oldest samples into <paramref name="destination"/>, at most as many
        /// as fit into it.
        /// </summary>
        /// <param name="destination">Where to store the samples.</param>
        /// <returns>The number of samples stored in <paramref name="destination"/>.</returns>
        public int Read(float[] destination)
        {
            lock (mutex)
            {
                int length = Math.Min(count, destination.Length);

                // The stored samples may be split by the end of the array.
                int firstPart = Math.Min(length, samples.Length - start);
                Array.Copy(samples, start, destination, 0, firstPart);
                Array.Copy(samples, 0, destination, firstPart, length - firstPart);

                start = (start + length) % samples.Length;
                count -= length;
                return length;
            }
        }

        /// <summary>
        /// Removes all stored samples. They are not counted in <see cref="DroppedSamples"/>.
        /// </summary>
        public void Clear()
        {
            lock (mutex)
            {
                start = 0;
                count = 0;
            }
        }
    }
}
