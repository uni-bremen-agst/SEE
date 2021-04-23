using Assets.SEE.Utils;
using NUnit.Framework;
using SEE.Controls.Actions;
using SEE.Utils;
using System.Collections.Generic;

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

            public bool Update()
            {
                UpdateCalls++;
                // AwakeCalls has been called once before.
                Assert.AreEqual(1, AwakeCalls);
                // The number of Start calls is always one ahead of the number of Stop calls.
                Assert.AreEqual(StartCalls, StopCalls + 1);
                // This action is never finished.
                return false;
            }

            /// <summary>
            /// Returns a new instance of <see cref="TestAction"/>.
            /// </summary>
            /// <returns>new instance</returns>
            public ReversibleAction NewInstance()
            {
                return new TestAction();
            }

            /// <summary>
            /// This action has always had an effect.
            /// </summary>
            /// <returns></returns>
            public bool HadEffect()
            {
                return true;
            }

            /// <summary>
            /// Returns the <see cref="ActionStateType"/> of this action.
            /// </summary>
            /// <returns>the <see cref="ActionStateType"/> of this action</returns>
            public ActionStateType GetActionStateType()
            {
                throw new System.NotImplementedException();
            }

            public List<string> GetChangedObjects()
            {
                throw new System.NotImplementedException();
            }

            public string GetId()
            {
                throw new System.NotImplementedException();
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

      /*  [Test]
        public void EmptyHistory()
        {
            Assert.Throws<EmptyActionHistoryException>(() => hist.Undo());
            Assert.Throws<EmptyUndoHistoryException>(() => hist.Redo());
        } */

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

        /// <summary>
        /// Provides a counter.
        /// </summary>
        private abstract class Counter : ReversibleAction
        {
            protected static int counter = 0;

            public static int Value { get => counter; }

            public void Awake()
            {
                // nothing to be done
            }

            public void Start()
            {
                // nothing to be done
            }

            public void Stop()
            {
                // nothing to be done
            }

            protected bool hadEffect = false;
            public bool HadEffect()
            {
                return hadEffect;
            }
            
            public abstract ReversibleAction NewInstance();
            public abstract void Redo();
            public abstract void Undo();
            public abstract bool Update();

            public ActionStateType GetActionStateType()
            {
                throw new System.NotImplementedException();
            }

            public List<string> GetChangedObjects()
            {
                throw new System.NotImplementedException();
            }

            public string GetId()
            {
                throw new System.NotImplementedException();
            }
        }

        /// <summary>
        /// Increments counter.
        /// </summary>
        private class Increment : Counter
        {
            public override ReversibleAction NewInstance()
            {
                return new Increment();
            }

            public override void Redo()
            {
                counter++;
            }

            public override void Undo()
            {
                counter--;
            }

            public override bool Update()
            {
                hadEffect = true;
                counter++;
                return true;
            }
        }

        /// <summary>
        /// Decrements counter.
        /// </summary>
        private class Decrement : Counter
        {
            public override ReversibleAction NewInstance()
            {
                return new Decrement();
            }

            public override void Redo()
            {
                counter--;
            }

            public override void Undo()
            {
                counter++;
            }

            public override bool Update()
            {
                hadEffect = true;
                counter--;
                return true;
            }
        }

        /// <summary>
        /// Test scenario for a non-continuous action with immediate effect.
        /// </summary>
       /* [Test]        
        public void TestCounterAction()
        {
            hist.Execute(new Increment());
            Assert.AreEqual(0, Counter.Value);
            Assert.AreEqual(1, hist.UndoCount);
            Assert.AreEqual(0, hist.RedoCount);            
            hist.Update();
            Assert.AreEqual(1, Counter.Value);
            Assert.AreEqual(2, hist.UndoCount);
            Assert.AreEqual(0, hist.RedoCount);
            hist.Update();
            Assert.AreEqual(2, Counter.Value);
            Assert.AreEqual(3, hist.UndoCount);
            Assert.AreEqual(0, hist.RedoCount);
            hist.Update();
            Assert.AreEqual(3, Counter.Value);
            Assert.AreEqual(4, hist.UndoCount);
            Assert.AreEqual(0, hist.RedoCount);
            hist.Undo();
            Assert.AreEqual(2, Counter.Value);
            // because the effect takes place only if Update was called, there are only two actions on the stack
            Assert.AreEqual(2, hist.UndoCount); 
            Assert.AreEqual(1, hist.RedoCount);
            hist.Undo();
            Assert.AreEqual(1, Counter.Value);
            Assert.AreEqual(1, hist.UndoCount);
            Assert.AreEqual(2, hist.RedoCount);
            hist.Undo();
            Assert.AreEqual(0, Counter.Value);
            Assert.AreEqual(0, hist.UndoCount);
            Assert.AreEqual(3, hist.RedoCount);
            hist.Redo();
            Assert.AreEqual(1, Counter.Value);
            Assert.AreEqual(1, hist.UndoCount);
            Assert.AreEqual(2, hist.RedoCount);
            hist.Redo();
            Assert.AreEqual(2, Counter.Value);
            Assert.AreEqual(2, hist.UndoCount);
            Assert.AreEqual(1, hist.RedoCount);
            hist.Execute(new Decrement());
            Assert.AreEqual(2, Counter.Value); // still 2 because no Update was called
            Assert.AreEqual(3, hist.UndoCount);
            Assert.AreEqual(0, hist.RedoCount); // RedoStack is lost
            hist.Update();
            Assert.AreEqual(1, Counter.Value);
            Assert.AreEqual(4, hist.UndoCount);
            Assert.AreEqual(0, hist.RedoCount);
            hist.Execute(new Increment());
            Assert.AreEqual(1, Counter.Value);  // still 1 because no Update was called
            Assert.AreEqual(5, hist.UndoCount);
            Assert.AreEqual(0, hist.RedoCount);
            // Undo without prior Update; that means we are actually undoing Decrement
            hist.Undo();
            Assert.AreEqual(2, Counter.Value);
            Assert.AreEqual(2, hist.UndoCount); // 2 because we have have removed the Increment without effect and then Decrement
            Assert.AreEqual(1, hist.RedoCount);
            hist.Undo(); // undoing an Increment
            Assert.AreEqual(1, Counter.Value);
            Assert.AreEqual(1, hist.UndoCount);
            Assert.AreEqual(2, hist.RedoCount);
            hist.Redo(); // re-doing an Increment
            Assert.AreEqual(2, Counter.Value);
            Assert.AreEqual(2, hist.UndoCount);
            Assert.AreEqual(1, hist.RedoCount);
            hist.Redo(); // re-doing a Decrement
            Assert.AreEqual(1, Counter.Value);
            Assert.AreEqual(3, hist.UndoCount);
            Assert.AreEqual(0, hist.RedoCount);
        } */
    } 
}
