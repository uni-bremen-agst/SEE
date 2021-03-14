using NUnit.Framework;
using SEE.Utils;

namespace SEETests
{
    /// <summary>
    /// Test cases for <see cref="SEE.Utils.ActionHistory"/>.
    /// </summary>
    class TestActionHistory
    {
        /// <summary>
        /// A substitute reversible action for testing.
        /// </summary>
        private class TestAction : ReversibleAction
        {
            public bool IsOn = false;

            public int AwakeCalls = 0;
            public int StartCalls = 0;            
            public int UpdateCalls = 0;
            public int StopCalls = 0;

            private int UndoCalls = 0;
            private int RedoCalls = 0;

            public void Awake()
            {
                AwakeCalls++;
                // Called exactly once before any other method.
                Assert.AreEqual(1, AwakeCalls);
                Assert.AreEqual(0, StartCalls);
                Assert.AreEqual(0, UpdateCalls);
                Assert.AreEqual(0, StopCalls);
            }

            public void Start()
            {
                StartCalls++;
                IsOn = true;
                // AwakeCalls has been called once before.
                Assert.AreEqual(1, AwakeCalls);
                // The number of Start calls is always one ahead of the number of Stop calls.
                Assert.AreEqual(StartCalls, StopCalls + 1);
            }

            public void Stop()
            {
                StopCalls++;
                // AwakeCalls has been called once before.
                Assert.AreEqual(1, AwakeCalls);
                // Each Stop must have a corresponding Start.
                Assert.AreEqual(StartCalls, StopCalls);
            }

            public void Undo()
            {
                IsOn = false;
                UndoCalls++;
                // If Undo is called, Stop must have been called before.
                Assert.That(StopCalls > 0);
                Assert.AreEqual(StartCalls, StopCalls);
                Assert.That(UndoCalls == RedoCalls + 1);
            }

            public void Redo()
            {
                IsOn = true;
                RedoCalls++;
                Assert.That(UndoCalls == RedoCalls);
            }

            public void Update()
            {
                UpdateCalls++;
                // AwakeCalls has been called once before.
                Assert.AreEqual(1, AwakeCalls);
                // The number of Start calls is always one ahead of the number of Stop calls.
                Assert.AreEqual(StartCalls, StopCalls + 1);
            }
        }

        /// <summary>
        /// The test object.
        /// </summary>
        private ActionHistory hist;

        [SetUp]
        public void SetUp()
        {
            hist = new ActionHistory();
        }

        [Test]
        public void EmptyHistory()
        {
            Assert.Throws<EmptyActionHistoryException>(() => hist.Undo());
            Assert.Throws<EmptyUndoHistoryException>(() => hist.Redo());
        }

        [Test]
        public void OneAction()
        {
            TestAction c = new TestAction();           
            CheckCalls(c, value: false, awake: 0, start: 0, update: 0, stop: 0);
            hist.Execute(c);
            CheckCalls(c, value: true, awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
            CheckCalls(c, value: true, awake: 1, start: 1, update: 1, stop: 0);
            hist.Update();
            CheckCalls(c, value: true, awake: 1, start: 1, update: 2, stop: 0);
            hist.Undo();
            CheckCalls(c, value: false, awake: 1, start: 1, update: 2, stop: 1);
            hist.Redo();
            CheckCalls(c, value: true, awake: 1, start: 2, update: 2, stop: 1);
        }

        [Test]
        public void MultipleActions()
        {
            TestAction c1 = new TestAction();
            TestAction c2 = new TestAction();
            TestAction c3 = new TestAction();
            TestAction c4 = new TestAction();

            CheckCalls(c1, value: false, awake: 0, start: 0, update: 0, stop: 0);
            CheckCalls(c2, value: false, awake: 0, start: 0, update: 0, stop: 0);
            CheckCalls(c3, value: false, awake: 0, start: 0, update: 0, stop: 0);
            CheckCalls(c4, value: false, awake: 0, start: 0, update: 0, stop: 0);

            hist.Execute(c1);
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 0);

            hist.Execute(c2);
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 1, update: 1, stop: 0);

            hist.Execute(c3);
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c3, value: true, awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c3, value: true, awake: 1, start: 1, update: 1, stop: 0);

            hist.Execute(c4);
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c3, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c4, value: true, awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c3, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c4, value: true, awake: 1, start: 1, update: 1, stop: 0);

            // c4 is undone; c3 is running
            hist.Undo();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c3, value: true,  awake: 1, start: 2, update: 1, stop: 1);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            hist.Update();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c3, value: true,  awake: 1, start: 2, update: 2, stop: 1);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);

            // c3 is undone; c2 is running
            hist.Undo();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 2, update: 1, stop: 1);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            hist.Update();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 2, update: 2, stop: 1);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);

            // c3 is redone
            hist.Redo();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c3, value: true,  awake: 1, start: 3, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            hist.Update();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c3, value: true,  awake: 1, start: 3, update: 3, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);

            // c4 is redone
            hist.Redo();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c3, value: true,  awake: 1, start: 3, update: 3, stop: 3);
            CheckCalls(c4, value: true,  awake: 1, start: 2, update: 1, stop: 1);
            hist.Update();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c3, value: true,  awake: 1, start: 3, update: 3, stop: 3);
            CheckCalls(c4, value: true,  awake: 1, start: 2, update: 2, stop: 1);

            // c4 is undone; c3 is running
            hist.Undo();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c3, value: true,  awake: 1, start: 4, update: 3, stop: 3);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            hist.Update();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c3, value: true,  awake: 1, start: 4, update: 4, stop: 3);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);

            // c3 is undone; c2 is running
            hist.Undo();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 3, update: 2, stop: 2);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            hist.Update();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 3, update: 3, stop: 2);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);

            // new action is executed => c3 and c4 are lost
            TestAction c5 = new TestAction();
            CheckCalls(c5, value: false, awake: 0, start: 0, update: 0, stop: 0);
            hist.Execute(c5);
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 3, update: 3, stop: 3);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c5, value: true,  awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 3, update: 3, stop: 3);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c5, value: true,  awake: 1, start: 1, update: 1, stop: 0);

            // c5 is undone; c2 is running
            hist.Undo();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 4, update: 3, stop: 3);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);
            hist.Update();
            CheckCalls(c1, value: true,  awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true,  awake: 1, start: 4, update: 4, stop: 3);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);

            // c2 is undone; c1 is running
            hist.Undo();
            CheckCalls(c1, value: true,  awake: 1, start: 2, update: 1, stop: 1);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);
            hist.Update();
            CheckCalls(c1, value: true,  awake: 1, start: 2, update: 2, stop: 1);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);

            // c1 is undone, action history is empty
            hist.Undo();
            CheckCalls(c1, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);
            hist.Update();
            CheckCalls(c1, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);

            // new action is executed => c1, c2, and c5 are lost
            TestAction c6 = new TestAction();
            CheckCalls(c6, value: false, awake: 0, start: 0, update: 0, stop: 0);
            hist.Execute(c6);
            CheckCalls(c1, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c6, value: true,  awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
            CheckCalls(c1, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c6, value: true,  awake: 1, start: 1, update: 1, stop: 0);

            // c6 is undone
            hist.Undo();
            CheckCalls(c1, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c6, value: false, awake: 1, start: 1, update: 1, stop: 1);
            hist.Update();
            CheckCalls(c1, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 4, update: 4, stop: 4);
            CheckCalls(c4, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c6, value: false, awake: 1, start: 1, update: 1, stop: 1);
        }

        /// <summary>
        /// Checks the number of expected calls for given <paramref name="counter"/>.
        /// </summary>
        /// <param name="counter">counters whose calls are to be checked</param>
        /// <param name="value">expected Value of <paramref name="counter"/></param>
        /// <param name="awake">expected number of Awake() calls</param>
        /// <param name="start">expected number of Start() calls</param>
        /// <param name="update">expected number of Update() calls</param>
        /// <param name="stop">expected number of Stop() calls</param>
        private static void CheckCalls(TestAction counter, bool value, int awake, int start, int update, int stop)
        {
            Assert.AreEqual(value, counter.IsOn, "IsOn not matching");
            Assert.AreEqual(awake, counter.AwakeCalls, "awake calls not matching");
            Assert.AreEqual(start, counter.StartCalls, "start calls not matching");
            Assert.AreEqual(update, counter.UpdateCalls, "update calls not matching");
            Assert.AreEqual(stop, counter.StopCalls, "stop calls not matching");
        }
    }
}
