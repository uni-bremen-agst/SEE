using NUnit.Framework;
using SEE.Controls.Actions;
using SEE.Utils;
using System;
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
                IsOn = true;
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
            /// Returns <see cref="ReversibleAction.Progress.InProgress"/> if this action has 
            /// had an effect, that is, if <see cref="Update"/> has been called before; 
            /// otherwise <see cref="ReversibleAction.Progress.NoEffect"/> is returned.
            /// It will never return <see cref="ReversibleAction.Progress.Completed"/>
            /// because <see cref="Update"/> always yields false.
            /// </summary>
            /// <returns>current progress state</returns>
            public ReversibleAction.Progress CurrentProgress()
            {
                if (UpdateCalls > 0)
                {
                    return ReversibleAction.Progress.InProgress;
                }
                else
                {
                    return ReversibleAction.Progress.NoEffect;
                }
            }

            private class TestActionStateType : ActionStateType 
            {
                public TestActionStateType() : base(TestAction.CreateReversibleAction)
                { }
            }

            private static ActionStateType actionStateType = new TestActionStateType();

            private static ReversibleAction CreateReversibleAction()
            {
                return new TestAction();
            }

            /// <summary>
            /// Returns the <see cref="ActionStateType"/> of this action.
            /// </summary>
            /// <returns>the <see cref="ActionStateType"/> of this action</returns>
            public ActionStateType GetActionStateType()
            {
                return actionStateType;
            }

            public HashSet<string> GetChangedObjects()
            {
                return new HashSet<string>();
            }

            private readonly string id = Guid.NewGuid().ToString();

            public string GetId()
            {                
                return id;
            }
        }

        /// <summary>
        /// The test object.
        /// </summary>
        private ActionHistory hist;

        [SetUp]
        public void SetUp()
        {
            hist = new ActionHistory(syncOverNetwork: false);
            Counter.Reset();
        }

        [Test]        
        public void OneAction()
        {
            /// Note: TestAction is an action that continues forever, that is,
            /// no Update call will ever return true and its progress state
            /// is initially <see cref="ReversibleAction.Progress.NoEffect"/>
            /// and after the first Update call <see cref="ReversibleAction.Progress.InProgress"/>
            /// for the rest of its life.
            TestAction c = new TestAction();
            CheckCalls(c, value: false, awake: 0, start: 0, update: 0, stop: 0);
            hist.Execute(c);
            CheckCalls(c, value: false, awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
            CheckCalls(c, value: true, awake: 1, start: 1, update: 1, stop: 0);
            hist.Update();
            CheckCalls(c, value: true, awake: 1, start: 1, update: 2, stop: 0);
            // c is in progress, but not yet completed. The following Undo call will interrupt c.
            hist.Undo();
            // Because c is interrupted, Undo will trigger a Stop. Because Undo moves
            // c from the UndoStack to the RedoStack, the UndoStack will be empty.
            // c will not receive another Start message.
            CheckCalls(c, value: false, awake: 1, start: 1, update: 2, stop: 1);
            hist.Redo();
            // Because c was not completed, Redo will resume with it.
            // The call of Start will be received by c.
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
            // No Update has occurred so far.
            CheckCalls(c1, value: false, awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
            // Update has set the new value.
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 0);

            hist.Execute(c2);
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: false, awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 1, update: 1, stop: 0);

            hist.Execute(c3);
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c3, value: false, awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c3, value: true, awake: 1, start: 1, update: 1, stop: 0);

            hist.Execute(c4);
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c3, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c3, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c4, value: true, awake: 1, start: 1, update: 1, stop: 0);

            // c4 is undone; execution will resume with c3, because c3 is still 
            // in progress (TestAction.Update() always yields false).
            hist.Undo();
            Assert.AreEqual(3, hist.UndoCount());
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
            CheckCalls(c5, value: false,  awake: 1, start: 1, update: 0, stop: 0);
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
            CheckCalls(c6, value: false,  awake: 1, start: 1, update: 0, stop: 0);
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

            public static void Reset()
            {
                counter = 0;
            }

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

            protected ReversibleAction.Progress currentProgress = ReversibleAction.Progress.NoEffect;

            public ReversibleAction.Progress CurrentProgress()
            {
                return currentProgress;
            }
            
            public abstract ReversibleAction NewInstance();
            public abstract void Redo();
            public abstract void Undo();
            public abstract bool Update();

            public HashSet<string> GetChangedObjects()
            {
                return new HashSet<string>();
            }

            private readonly string id = Guid.NewGuid().ToString();

            public string GetId()
            {
                return id;
            }

            public abstract ActionStateType GetActionStateType();
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
                currentProgress = ReversibleAction.Progress.Completed;
                counter++;
                return true;
            }

            private class IncrementActionStateType : ActionStateType
            {
                public IncrementActionStateType() : base(CreateReversibleAction)
                { }
            }

            private static readonly ActionStateType actionStateType = new IncrementActionStateType();

            private static ReversibleAction CreateReversibleAction()
            {
                return new Increment();
            }

            /// <summary>
            /// Returns the <see cref="ActionStateType"/> of this action.
            /// </summary>
            /// <returns>the <see cref="ActionStateType"/> of this action</returns>
            public override ActionStateType GetActionStateType()
            {
                return actionStateType;
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
                currentProgress = ReversibleAction.Progress.Completed;
                counter--;
                return true;
            }

            private class DecrementActionStateType : ActionStateType
            {
                public DecrementActionStateType() : base(CreateReversibleAction)
                { }
            }

            private static ActionStateType actionStateType = new DecrementActionStateType();

            private static ReversibleAction CreateReversibleAction()
            {
                return new Decrement();
            }

            /// <summary>
            /// Returns the <see cref="ActionStateType"/> of this action.
            /// </summary>
            /// <returns>the <see cref="ActionStateType"/> of this action</returns>
            public override ActionStateType GetActionStateType()
            {
                return actionStateType;
            }
        }

        /// <summary>
        /// Test scenario for a non-continuous action with immediate effect.
        /// Every Update call for Decrement and Increment will yield true.
        /// Thus, their progress state is initially <see cref="ReversibleAction.Progress.NoEffect"/>
        /// and after the first Update call <see cref="ReversibleAction.Progress.Completed"/>
        /// for the rest of their life.
        /// </summary>
        [Test]        
        public void TestCounterAction()
        {            
            hist.Execute(new Increment());
            Assert.AreEqual(0, Counter.Value);
            Assert.AreEqual(1, hist.UndoCount());
            Assert.AreEqual(0, hist.RedoCount());            
            hist.Update();
            Assert.AreEqual(1, Counter.Value);
            Assert.AreEqual(2, hist.UndoCount());
            Assert.AreEqual(0, hist.RedoCount());
            hist.Update();
            Assert.AreEqual(2, Counter.Value);
            Assert.AreEqual(3, hist.UndoCount());
            Assert.AreEqual(0, hist.RedoCount());
            hist.Update();
            Assert.AreEqual(3, Counter.Value);
            Assert.AreEqual(4, hist.UndoCount());
            Assert.AreEqual(0, hist.RedoCount());
            // Because Increment.Update yields true every time it is called,
            // the execution will always continue with a new instance of 
            // Increment. We have had three calls to Update. Including the
            // first Execute, we should have four actions on the UndoStack.
            Assert.AreEqual(4, hist.UndoCount());
            hist.Undo();
            Assert.AreEqual(2, Counter.Value);
            // Undo will remove one completed action and then resume with
            // a new instance of Increment.
            Assert.AreEqual(3, hist.UndoCount()); 
            Assert.AreEqual(1, hist.RedoCount());
            hist.Undo();
            Assert.AreEqual(1, Counter.Value);
            Assert.AreEqual(2, hist.UndoCount());
            Assert.AreEqual(2, hist.RedoCount());
            hist.Undo();
            Assert.AreEqual(0, Counter.Value);
            // The UndoStack has one completed Increment action and one
            // instance of Increment that has had no effect yet.
            // If Undo is called, all actions without any effect will
            // be removed. This leaves the single action with effect,
            // which then is moved from the UndoStack to the RedoStack.
            // Thus, the UndoStack will be empty at this point.
            Assert.AreEqual(0, hist.UndoCount());
            Assert.AreEqual(3, hist.RedoCount());
            hist.Redo();
            Assert.AreEqual(1, Counter.Value);
            // Redo moves an action from the RedoStack to the UndoStack.
            // Because that action was completed, a new instance of 
            // Increment will be put onto the UndoStack that will be used
            // to resume.
            Assert.AreEqual(2, hist.UndoCount());
            Assert.AreEqual(2, hist.RedoCount());
            hist.Redo();
            Assert.AreEqual(2, Counter.Value);
            Assert.AreEqual(3, hist.UndoCount());
            Assert.AreEqual(1, hist.RedoCount());
            hist.Execute(new Decrement());
            // The new instance of the Increment action that was put on
            // the stack due to Redo above has not received any Update call.
            // Hence, it will be popped off the UndoStack. Thus, UndoCount
            // remains the same.
            Assert.AreEqual(2, Counter.Value); // still 2 because no Update was called
            Assert.AreEqual(3, hist.UndoCount());
            Assert.AreEqual(0, hist.RedoCount()); // RedoStack is lost
            hist.Update();
            // Update has completed Decrement. A new instance of Decrement will
            // be put on the Undo stack.
            Assert.AreEqual(1, Counter.Value);
            Assert.AreEqual(4, hist.UndoCount());
            Assert.AreEqual(0, hist.RedoCount());
            // No Update has been called for the new instance of Decrement just put
            // on the UndoStack, hence, the Decrement at the top of the UndoStack
            // has still progress state NoEffect. Thus it will be popped off the UndoStack
            // when the next Increment is added by the following line.
            hist.Execute(new Increment());
            Assert.AreEqual(1, Counter.Value);  // still 1 because no Update was called
            Assert.AreEqual(4, hist.UndoCount());
            Assert.AreEqual(0, hist.RedoCount());
            // Undo without prior Update for the Increment just added; that means we are actually
            // undoing Decrement. The Increment will be popped off the UndoStack. Then the 
            // Decrement will be undone and moved from the UndoStack to the RedoStack.
            // Now a completed Increment will be at the top of the stack. Because it is
            // completed, a new instance of Increment will be added.
            hist.Undo();
            Assert.AreEqual(2, Counter.Value);
            Assert.AreEqual(3, hist.UndoCount());
            Assert.AreEqual(1, hist.RedoCount());
            hist.Undo(); // undoing an Increment
            // No Update has been called for the new instance of the Increment with
            // progress state NoEffect. Hence, it will be popped off the UndoStack.
            // Now the completed Increment will be at the top of the UndoStack again.
            // This is now undone, that is, moved from the UndoStack to the RedoStack.
            // After that a completed Increment (actually the very first Increment
            // executed) is at the top of the UndoStack again, and as a consequence
            // a new instance of Increment is added to the UndoStack in progress state
            // NoEffect.
            Assert.AreEqual(1, Counter.Value);
            Assert.AreEqual(2, hist.UndoCount());
            Assert.AreEqual(2, hist.RedoCount());
            // The current situation is a follows: the UndoStack consists of an
            // Increment with no effect and one completed Increment. The RedoStack
            // has a completed Increment and a completed Decrement.
            hist.Redo(); // re-doing an Increment
            // The Increment with no effect is popped off the UndoStack.
            // The completed Increment is moved from the RedoStack to the UndoStack.
            // Because that Increment is completed, a new instance of Increment
            // with progress state NoEffect will be added to the UndoStack.
            Assert.AreEqual(2, Counter.Value);
            Assert.AreEqual(3, hist.UndoCount());
            Assert.AreEqual(1, hist.RedoCount());
            hist.Redo(); // re-doing a Decrement
            Assert.AreEqual(1, Counter.Value);
            Assert.AreEqual(4, hist.UndoCount());
            Assert.AreEqual(0, hist.RedoCount());
        } 
    } 
}
