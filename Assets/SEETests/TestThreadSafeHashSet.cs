using NUnit.Framework;
using System.Threading;

namespace SEE.Utils
{
     /// <summary>
    /// Tests for <see cref="ThreadSafeHashSet{T}"/>.
    /// </summary>
    public class TestThreadSafeHashSet
    {
        /// <summary>
        /// Basic test for adding elements to the set and iterating over them.
        /// No threads are involved.
        /// </summary>
        [Test]
        public void TestAdd()
        {
            ThreadSafeHashSet<int> set = new();
            Assert.That(set.Add(1));
            Assert.That(set.Add(2));
            Assert.That(!set.Add(1));
            Assert.That(!set.Add(2));

            foreach (int i in set)
            {
                Assert.That(i == 1 || i == 2);
            }
        }

        /// <summary>
        /// Two threads add elements to the set concurrently.
        /// </summary>
        [Test]
        public void TestConcurrentAdding()
        {
            ThreadSafeHashSet<int> set = new();
            const int total = 150_000;
            Thread t1 = new(new ThreadStart(new AdderThread(set, 1, total).Run));
            Thread t2 = new(new ThreadStart(new AdderThread(set, total / 2, total).Run));
            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();
            Assert.AreEqual(total, set.Count);
        }

        /// <summary>
        /// Two threads add elements to the set and another reads them concurrently.
        /// </summary>
        [Test]
        public void TestConcurrentAddingAndReading()
        {
            ThreadSafeHashSet<int> set = new();
            const int total = 10_000;
            Thread a1 = new(new ThreadStart(new AdderThread(set, 1, total / 2).Run));
            Thread a2 = new(new ThreadStart(new AdderThread(set, 1, total).Run));
            ReaderThread reader = new(set, 1, total);
            Thread r = new(new ThreadStart(reader.Run));

            a1.Start();
            r.Start();

            r.Join();

            a2.Start();

            a2.Join();
            a1.Join();

            Assert.AreEqual(total, set.Count);
        }
    }

    /// <summary>
    /// Common superclass for <see cref="AdderThread"/> and <see cref="ReaderThread"/>.
    /// </summary>
    public abstract class SetThread
    {
        protected readonly int min;
        protected readonly int max;
        protected readonly ThreadSafeHashSet<int> set;

        public SetThread(ThreadSafeHashSet<int> set, int min, int max)
        {
            this.set = set;
            this.min = min;
            this.max = max;
        }
    }

    /// <summary>
    /// A thread that adds elements to a set.
    /// </summary>
    public class AdderThread : SetThread
    {
        public AdderThread(ThreadSafeHashSet<int> set, int min, int max)
            : base(set, min, max)
        {
        }

        public void Run()
        {
            for (int i = min; i <= max; i++)
            {
                set.Add(i);
            }
        }
    }

    /// <summary>
    /// A thread that reads elements from a set.
    /// </summary>
    public class ReaderThread : SetThread
    {
        public ReaderThread(ThreadSafeHashSet<int> set, int min, int max)
            : base(set, min, max)
        {
        }

        public void Run()
        {
            int attempts = 0;
            int value = Sum(set);
            while (attempts <= 100 && Finished(value))
            {
                value = Sum(set);
                attempts++;
            }
        }

        private bool Finished(int sum)
        {
            return sum != (max - min + 1) * (min + max) / 2;
        }

        private static int Sum(ThreadSafeHashSet<int> set)
        {
            int sum = 0;
            foreach (int i in set)
            {
                sum += i;
            }
            return sum;
        }
    }
}
