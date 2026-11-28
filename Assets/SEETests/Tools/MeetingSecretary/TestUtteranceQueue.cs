using NUnit.Framework;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Tests for <see cref="UtteranceQueue{T}"/>.
    /// </summary>
    internal class TestUtteranceQueue
    {
        /// <summary>
        /// Utterances leave the queue in the order in which they were added.
        /// </summary>
        [Test]
        public void TestOrder()
        {
            UtteranceQueue<int> queue = new();
            Assert.That(queue.Enqueue(1), Is.False);
            Assert.That(queue.Enqueue(2), Is.False);
            Assert.That(queue.Count, Is.EqualTo(2));

            Assert.That(queue.TryDequeue(out int first), Is.True);
            Assert.That(first, Is.EqualTo(1));
            Assert.That(queue.TryDequeue(out int second), Is.True);
            Assert.That(second, Is.EqualTo(2));
            Assert.That(queue.TryDequeue(out int _), Is.False);
        }

        /// <summary>
        /// If more than the maximal number of utterances are waiting,
        /// the oldest ones are dropped and counted.
        /// </summary>
        [Test]
        public void TestDropsOldest()
        {
            UtteranceQueue<int> queue = new();
            for (int i = 1; i <= UtteranceQueue<int>.MaxWaiting; i++)
            {
                Assert.That(queue.Enqueue(i), Is.False);
            }
            Assert.That(queue.Dropped, Is.EqualTo(0));

            Assert.That(queue.Enqueue(UtteranceQueue<int>.MaxWaiting + 1), Is.True);
            Assert.That(queue.Enqueue(UtteranceQueue<int>.MaxWaiting + 2), Is.True);

            Assert.That(queue.Count, Is.EqualTo(UtteranceQueue<int>.MaxWaiting));
            Assert.That(queue.Dropped, Is.EqualTo(2));
            // The two oldest utterances (1 and 2) are gone.
            Assert.That(queue.TryDequeue(out int oldest), Is.True);
            Assert.That(oldest, Is.EqualTo(3));
        }

        /// <summary>
        /// <see cref="UtteranceQueue{T}.Clear"/> removes all utterances without
        /// counting them as dropped.
        /// </summary>
        [Test]
        public void TestClear()
        {
            UtteranceQueue<int> queue = new();
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Clear();

            Assert.That(queue.Count, Is.EqualTo(0));
            Assert.That(queue.Dropped, Is.EqualTo(0));
            Assert.That(queue.TryDequeue(out int _), Is.False);
        }
    }
}
