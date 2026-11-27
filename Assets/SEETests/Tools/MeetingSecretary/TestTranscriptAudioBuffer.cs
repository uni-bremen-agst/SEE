using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Tests for <see cref="TranscriptAudioBuffer"/>.
    /// </summary>
    internal class TestTranscriptAudioBuffer
    {
        /// <summary>
        /// What was written is read in the same order.
        /// </summary>
        [Test]
        public void TestWriteThenRead()
        {
            TranscriptAudioBuffer buffer = new(10);
            buffer.Write(new float[] { 1, 2, 3 }, 0, 3);
            Assert.That(buffer.Count, Is.EqualTo(3));

            float[] destination = new float[10];
            Assert.That(buffer.Read(destination), Is.EqualTo(3));
            Assert.That(Take(destination, 3), Is.EqualTo(new float[] { 1, 2, 3 }));
            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.That(buffer.DroppedSamples, Is.EqualTo(0));
        }

        /// <summary>
        /// Only the requested part of the source array is written.
        /// </summary>
        [Test]
        public void TestWriteWithOffset()
        {
            TranscriptAudioBuffer buffer = new(10);
            buffer.Write(new float[] { 1, 2, 3, 4, 5 }, 1, 3);

            float[] destination = new float[10];
            Assert.That(buffer.Read(destination), Is.EqualTo(3));
            Assert.That(Take(destination, 3), Is.EqualTo(new float[] { 2, 3, 4 }));
        }

        /// <summary>
        /// Reading from an empty buffer yields nothing.
        /// </summary>
        [Test]
        public void TestReadEmpty()
        {
            TranscriptAudioBuffer buffer = new(10);
            Assert.That(buffer.Read(new float[10]), Is.EqualTo(0));
        }

        /// <summary>
        /// If the destination is too small, the rest is returned by the next read.
        /// </summary>
        [Test]
        public void TestReadInParts()
        {
            TranscriptAudioBuffer buffer = new(10);
            buffer.Write(new float[] { 1, 2, 3, 4, 5 }, 0, 5);

            float[] destination = new float[3];
            Assert.That(buffer.Read(destination), Is.EqualTo(3));
            Assert.That(destination, Is.EqualTo(new float[] { 1, 2, 3 }));
            Assert.That(buffer.Read(destination), Is.EqualTo(2));
            Assert.That(Take(destination, 2), Is.EqualTo(new float[] { 4, 5 }));
        }

        /// <summary>
        /// Samples written across the end of the internal array are read correctly.
        /// </summary>
        [Test]
        public void TestWrapAround()
        {
            TranscriptAudioBuffer buffer = new(5);
            float[] destination = new float[5];

            // Move the start of the stored samples close to the end of the array.
            buffer.Write(new float[] { 1, 2, 3, 4 }, 0, 4);
            Assert.That(buffer.Read(destination), Is.EqualTo(4));

            buffer.Write(new float[] { 5, 6, 7 }, 0, 3);
            Assert.That(buffer.Read(destination), Is.EqualTo(3));
            Assert.That(Take(destination, 3), Is.EqualTo(new float[] { 5, 6, 7 }));
            Assert.That(buffer.DroppedSamples, Is.EqualTo(0));
        }

        /// <summary>
        /// If the buffer is full, the oldest samples are dropped and counted.
        /// </summary>
        [Test]
        public void TestOverflowDropsOldest()
        {
            TranscriptAudioBuffer buffer = new(5);
            buffer.Write(new float[] { 1, 2, 3, 4 }, 0, 4);
            buffer.Write(new float[] { 5, 6, 7 }, 0, 3);

            Assert.That(buffer.Count, Is.EqualTo(5));
            Assert.That(buffer.DroppedSamples, Is.EqualTo(2));

            float[] destination = new float[5];
            Assert.That(buffer.Read(destination), Is.EqualTo(5));
            Assert.That(destination, Is.EqualTo(new float[] { 3, 4, 5, 6, 7 }));
        }

        /// <summary>
        /// If more samples are written at once than the buffer can hold,
        /// only the newest ones are kept.
        /// </summary>
        [Test]
        public void TestWriteMoreThanCapacity()
        {
            TranscriptAudioBuffer buffer = new(3);
            buffer.Write(new float[] { 9 }, 0, 1);
            buffer.Write(new float[] { 1, 2, 3, 4, 5 }, 0, 5);

            // Dropped: the one stored sample and the two oldest of the second write.
            Assert.That(buffer.DroppedSamples, Is.EqualTo(3));

            float[] destination = new float[3];
            Assert.That(buffer.Read(destination), Is.EqualTo(3));
            Assert.That(destination, Is.EqualTo(new float[] { 3, 4, 5 }));
        }

        /// <summary>
        /// <see cref="TranscriptAudioBuffer.Clear"/> removes all samples without
        /// counting them as dropped.
        /// </summary>
        [Test]
        public void TestClear()
        {
            TranscriptAudioBuffer buffer = new(5);
            buffer.Write(new float[] { 1, 2, 3 }, 0, 3);
            buffer.Clear();

            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.That(buffer.DroppedSamples, Is.EqualTo(0));
            Assert.That(buffer.Read(new float[5]), Is.EqualTo(0));

            // The buffer still works after it was cleared.
            buffer.Write(new float[] { 4, 5 }, 0, 2);
            float[] destination = new float[5];
            Assert.That(buffer.Read(destination), Is.EqualTo(2));
            Assert.That(Take(destination, 2), Is.EqualTo(new float[] { 4, 5 }));
        }

        /// <summary>
        /// One thread writes while this thread reads. If the buffer is large enough,
        /// every sample arrives exactly once and in order.
        /// </summary>
        [Test]
        public void TestWriterThread()
        {
            const int chunkSize = 480;
            const int chunks = 400;
            const int total = chunks * chunkSize;
            TranscriptAudioBuffer buffer = new(total);

            Thread writer = new(() =>
            {
                float[] chunk = new float[chunkSize];
                for (int c = 0; c < chunks; c++)
                {
                    // The samples are the numbers 0, 1, 2, ... so that their order can be checked.
                    for (int i = 0; i < chunkSize; i++)
                    {
                        chunk[i] = c * chunkSize + i;
                    }
                    buffer.Write(chunk, 0, chunkSize);
                }
            });
            writer.Start();

            float[] destination = new float[1000];
            int received = 0;
            bool inOrder = true;
            // The writer is checked first: once it has finished, the count is final.
            while (writer.IsAlive || buffer.Count > 0)
            {
                int count = buffer.Read(destination);
                for (int i = 0; i < count; i++)
                {
                    if (destination[i] != received + i)
                    {
                        inOrder = false;
                    }
                }
                received += count;
            }
            writer.Join();

            Assert.That(inOrder, Is.True, "Samples must arrive exactly once and in order.");
            Assert.That(received, Is.EqualTo(total));
            Assert.That(buffer.DroppedSamples, Is.EqualTo(0));
        }

        /// <summary>
        /// Returns the first <paramref name="count"/> elements of <paramref name="array"/>.
        /// </summary>
        /// <param name="array">The array whose first elements are requested.</param>
        /// <param name="count">The number of elements to return.</param>
        /// <returns>The first <paramref name="count"/> elements.</returns>
        private static float[] Take(float[] array, int count)
        {
            List<float> result = new();
            for (int i = 0; i < count; i++)
            {
                result.Add(array[i]);
            }
            return result.ToArray();
        }
    }
}
