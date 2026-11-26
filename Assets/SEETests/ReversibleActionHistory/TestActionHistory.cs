using NUnit.Framework;
using SEE.Controls.ReversibleActions;
using System;
using System.Collections.Generic;

namespace SEE.ReversibleActionHistory
{
    /// <summary>
    /// Test cases for <see cref="SEE.Utils.ActionHistory"/>.
    /// </summary>
    internal class TestActionHistory
    {
        /// <summary>
        /// Shortcut to a constructor of <see cref="ActionStateType"/> that requires
        /// only a name and <see cref="CreateReversibleAction"/>.
        /// </summary>
        private abstract class TestActionStateType : ActionStateType
        {
            /// <summary>
            /// Constructor allowing to set the <paramref name="name"/> and <see cref="CreateReversible"/>.
            /// </summary>
            /// <param name="name">name of the <see cref="ActionStateType"/>; must be unique</param>
            /// <param name="createReversible">value for <see cref="CreateReversible"/></param>
            protected TestActionStateType(string name, CreateReversibleAction createReversible)
                : base(name: name, description: "", color: UnityEngine.Color.white,
                       icon: ' ', createReversible: createReversible, register: false)
            {
            }
        }

        /// <summary>
        /// A substitute reversible action for testing.
        /// </summary>
        private class TestAction : IReversibleAction
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
                Assert.That(AwakeCalls, Is.EqualTo(1));
                Assert.That(StartCalls, Is.EqualTo(0));
                Assert.That(UpdateCalls, Is.EqualTo(0));
                Assert.That(StopCalls, Is.EqualTo(0));
            }

            public void Start()
            {
                StartCalls++;
                // AwakeCalls has been called once before.
                Assert.That(AwakeCalls, Is.EqualTo(1));
                // The number of Start calls is always one ahead of the number of Stop calls.
                Assert.That(StartCalls, Is.EqualTo(StopCalls + 1),
                            "Start must have been called once more often than Stop.");
            }

            public void Stop()
            {
                StopCalls++;
                // AwakeCalls has been called once before.
                Assert.That(AwakeCalls, Is.EqualTo(1));
                // Each Stop must have a corresponding Start.
                Assert.That(StartCalls, Is.EqualTo(StopCalls),
                            "Start and Stop must have been called equally often.");
            }

            public void Undo()
            {
                IsOn = false;
                UndoCalls++;
                // If Undo is called, Stop must have been called before.
                Assert.That(StopCalls, Is.GreaterThan(0),
                            "Stop must have been called before Undo.");
                Assert.That(StartCalls, Is.EqualTo(StopCalls),
                            "Start and Stop must have been called equally often.");
                Assert.That(UndoCalls, Is.EqualTo(RedoCalls + 1),
                            "Undo must have been called once more often than Redo.");
            }

            public void Redo()
            {
                IsOn = true;
                RedoCalls++;
                Assert.That(UndoCalls, Is.EqualTo(RedoCalls),
                            "Undo and Redo must have been called equally often.");
            }

            public bool Update()
            {
                IsOn = true;
                UpdateCalls++;
                // AwakeCalls has been called once before.
                Assert.That(AwakeCalls, Is.EqualTo(1));
                // The number of Start calls is always one ahead of the number of Stop calls.
                Assert.That(StartCalls, Is.EqualTo(StopCalls + 1),
                            "Start must have been called once more often than Stop.");
                // This action is never finished.
                return false;
            }

            /// <summary>
            /// Returns a new instance of <see cref="TestAction"/>.
            /// </summary>
            /// <returns>new instance</returns>
            public IReversibleAction NewInstance()
            {
                return new TestAction();
            }

            /// <summary>
            /// Returns <see cref="IReversibleAction.Progress.InProgress"/> if this action has
            /// had an effect, that is, if <see cref="Update"/> has been called before;
            /// otherwise <see cref="IReversibleAction.Progress.NoEffect"/> is returned.
            /// It will never return <see cref="IReversibleAction.Progress.Completed"/>
            /// because <see cref="Update"/> always yields false.
            /// </summary>
            /// <returns>current progress state</returns>
            public IReversibleAction.Progress CurrentProgress()
            {
                if (UpdateCalls > 0)
                {
                    return IReversibleAction.Progress.InProgress;
                }
                else
                {
                    return IReversibleAction.Progress.NoEffect;
                }
            }

            private class MyTestActionStateType : TestActionStateType
            {
                public MyTestActionStateType() : base(nameof(MyTestActionStateType), TestAction.CreateReversibleAction)
                { }
            }

            private static ActionStateType actionStateType = new MyTestActionStateType();

            private static IReversibleAction CreateReversibleAction()
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

        /// <summary>
        /// Verifies that an action with preliminary effects can be undone and restarted with a fresh instance.
        /// </summary>
        [Test]
        public void OneAction()
        {
            // TestAction never finishes; Update changes its state from NoEffect to InProgress.
            TestAction c = new();
            CheckCalls(c, value: false, awake: 0, start: 0, update: 0, stop: 0);
            hist.Execute(c);
            CheckCalls(c, value: false, awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
            CheckCalls(c, value: true, awake: 1, start: 1, update: 1, stop: 0);
            hist.Update();
            CheckCalls(c, value: true, awake: 1, start: 1, update: 2, stop: 0);

            // Undo stops c, removes its preliminary effect, and makes it available for Redo.
            hist.Undo();
            CheckCalls(c, value: false, awake: 1, start: 1, update: 2, stop: 1);
            Assert.That(hist.UndoCount(), Is.EqualTo(0));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            // Redo starts a new action instead of restoring the old effects.
            hist.Redo();
            TestAction restarted = (TestAction)hist.CurrentAction;
            Assert.That(restarted, Is.Not.SameAs(c));
            CheckCalls(c, value: false, awake: 1, start: 1, update: 2, stop: 1);
            CheckCalls(restarted, value: false, awake: 1, start: 1, update: 0, stop: 0);
            Assert.That(hist.UndoCount(), Is.EqualTo(1));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));

            hist.Update();
            CheckCalls(restarted, value: true, awake: 1, start: 1, update: 1, stop: 0);
            CheckCalls(c, value: false, awake: 1, start: 1, update: 2, stop: 1);
        }

        /// <summary>
        /// Verifies that Redo restarts an interrupted action without restoring its effects
        /// or changing the effects of a previously completed action.
        /// </summary>
        [Test]
        public void RedoInterruptedActionStartsFresh()
        {
            hist.Execute(new Increment());
            hist.Update();
            Assert.That(Counter.Value, Is.EqualTo(1));

            TestAction interruptedAction = new();
            hist.Execute(interruptedAction);
            hist.Update();
            CheckCalls(interruptedAction, value: true, awake: 1, start: 1, update: 1, stop: 0);
            Assert.That(interruptedAction.CurrentProgress(), Is.EqualTo(IReversibleAction.Progress.InProgress));

            hist.Undo();
            CheckCalls(interruptedAction, value: false, awake: 1, start: 1, update: 1, stop: 1);
            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            hist.Redo();
            Assert.That(hist.CurrentAction, Is.InstanceOf<TestAction>());
            TestAction restartedAction = (TestAction)hist.CurrentAction;
            Assert.That(restartedAction, Is.Not.SameAs(interruptedAction));
            CheckCalls(interruptedAction, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(restartedAction, value: false, awake: 1, start: 1, update: 0, stop: 0);
            Assert.That(restartedAction.CurrentProgress(), Is.EqualTo(IReversibleAction.Progress.NoEffect));
            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));
        }

        /// <summary>
        /// Verifies that the first completed action can be undone and redone
        /// when no other action is activated between Undo and Redo.
        /// </summary>
        [Test]
        public void FirstCompletedActionCanBeRedoneDirectly()
        {
            hist.Execute(new Increment());
            hist.Update();

            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));

            hist.Undo();

            Assert.That(Counter.Value, Is.EqualTo(0));
            Assert.That(hist.UndoCount(), Is.EqualTo(0));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            hist.Redo();

            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));
        }

        /// <summary>
        /// Verifies that automatically activating an unused default tool
        /// after undoing the first completed action does not discard Redo.
        /// This reproduces the relevant action-history sequence in PlayerMenu.
        /// </summary>
        [Test]
        public void FirstActionRedoSurvivesDefaultToolActivation()
        {
            hist.Execute(new Increment());
            hist.Update();

            Assert.That(Counter.Value, Is.EqualTo(1));

            hist.Undo();

            Assert.That(Counter.Value, Is.EqualTo(0));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            // PlayerMenu activates a default action when the undo
            // history becomes empty. This substitute has no effect.
            hist.Execute(new TestAction());

            Assert.That(hist.CurrentAction.CurrentProgress(),
                        Is.EqualTo(IReversibleAction.Progress.NoEffect));

            Assert.That(hist.RedoCount(), Is.EqualTo(1),
                        "Activating an unused default tool must preserve Redo.");

            hist.Redo();

            Assert.That(Counter.Value, Is.EqualTo(1));
        }

        /// <summary>
        /// Verifies that selecting another tool without performing a new
        /// modification preserves the redo history of a completed action.
        /// </summary>
        [Test]
        public void SwitchingToUnusedToolPreservesRedoHistory()
        {
            hist.Execute(new Increment());
            hist.Update();
            hist.Update();

            Assert.That(Counter.Value, Is.EqualTo(2));

            hist.Undo();

            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            // Switch to another tool without executing its operation.
            hist.Execute(new Decrement());

            Assert.That(hist.CurrentAction.CurrentProgress(),
                        Is.EqualTo(IReversibleAction.Progress.NoEffect));

            Assert.That(hist.RedoCount(), Is.EqualTo(1),
                        "An unused tool must not discard undone actions.");

            hist.Redo();

            Assert.That(Counter.Value, Is.EqualTo(2));
        }

        /// <summary>
        /// Verifies that choosing an unused tool preserves the option to restart an interrupted action.
        /// </summary>
        [Test]
        public void SwitchingToolsPreservesRedoOfInProgressAction()
        {
            TestAction action = new();

            hist.Execute(action);
            hist.Update();
            CheckCalls(action, value: true, awake: 1, start: 1, update: 1, stop: 0);
            Assert.That(action.CurrentProgress(), Is.EqualTo(IReversibleAction.Progress.InProgress));

            hist.Undo();
            CheckCalls(action, value: false, awake: 1, start: 1, update: 1, stop: 1);
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            TestAction unusedTool = new();
            hist.Execute(unusedTool);
            CheckCalls(unusedTool, value: false, awake: 1, start: 1, update: 0, stop: 0);
            Assert.That(hist.CurrentAction.CurrentProgress(), Is.EqualTo(IReversibleAction.Progress.NoEffect));
            Assert.That(hist.RedoCount(), Is.EqualTo(1), "An unused tool must preserve an interrupted action.");

            hist.Redo();
            TestAction restarted = (TestAction)hist.CurrentAction;
            Assert.That(restarted, Is.Not.SameAs(action));
            Assert.That(restarted, Is.Not.SameAs(unusedTool));
            CheckCalls(action, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(unusedTool, value: false, awake: 1, start: 1, update: 0, stop: 1);
            CheckCalls(restarted, value: false, awake: 1, start: 1, update: 0, stop: 0);
            Assert.That(hist.RedoCount(), Is.EqualTo(0));
        }

        /// <summary>
        /// Verifies that a newly completed modification invalidates
        /// the redo history after an earlier action was undone.
        /// </summary>
        [Test]
        public void NewEffectiveActionInvalidatesRedoHistory()
        {
            hist.Execute(new Increment());
            hist.Update();
            hist.Update();

            Assert.That(Counter.Value, Is.EqualTo(2));

            hist.Undo();

            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            hist.Execute(new Decrement());
            hist.Update();

            Assert.That(Counter.Value, Is.EqualTo(0));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));

            Assert.Throws<RedoImpossible>(() => hist.Redo());
        }

        /// <summary>
        /// Verifies that a newly started action invalidates Redo when
        /// its first Update produces a preliminary effect.
        /// </summary>
        [Test]
        public void NewInProgressActionInvalidatesRedoHistory()
        {
            hist.Execute(new Increment());
            hist.Update();

            Assert.That(Counter.Value, Is.EqualTo(1));

            hist.Undo();

            Assert.That(Counter.Value, Is.EqualTo(0));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            TestAction newAction = new();
            hist.Execute(newAction);

            Assert.That(hist.RedoCount(), Is.EqualTo(1));
            Assert.That(newAction.CurrentProgress(),
                        Is.EqualTo(IReversibleAction.Progress.NoEffect));

            hist.Update();

            Assert.That(newAction.CurrentProgress(),
                        Is.EqualTo(IReversibleAction.Progress.InProgress));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));

            Assert.Throws<RedoImpossible>(() => hist.Redo());
        }

        /// <summary>
        /// Verifies that updating a resumed action preserves Redo when its progress state stays InProgress.
        /// An interrupted action is restarted with a fresh instance rather than replaying its effects.
        /// </summary>
        [Test]
        public void ResumingInProgressActionPreservesRedoHistory()
        {
            TestAction first = new();
            TestAction second = new();

            hist.Execute(first);
            hist.Update();
            CheckCalls(first, value: true, awake: 1, start: 1, update: 1, stop: 0);

            hist.Execute(second);
            hist.Update();
            CheckCalls(first, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(second, value: true, awake: 1, start: 1, update: 1, stop: 0);

            hist.Undo();
            CheckCalls(first, value: true, awake: 1, start: 2, update: 1, stop: 1);
            CheckCalls(second, value: false, awake: 1, start: 1, update: 1, stop: 1);
            Assert.That(first.CurrentProgress(), Is.EqualTo(IReversibleAction.Progress.InProgress));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            // The current test action has no new visible change when updated again.
            hist.Update();
            CheckCalls(first, value: true, awake: 1, start: 2, update: 2, stop: 1);
            CheckCalls(second, value: false, awake: 1, start: 1, update: 1, stop: 1);
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            hist.Redo();
            TestAction restartedSecond = (TestAction)hist.CurrentAction;
            Assert.That(restartedSecond, Is.Not.SameAs(second));
            CheckCalls(first, value: true, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(second, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(restartedSecond, value: false, awake: 1, start: 1, update: 0, stop: 0);
            Assert.That(hist.RedoCount(), Is.EqualTo(0));
        }

        /// <summary>
        /// Verifies that a new effect reported outside ActionHistory.Update
        /// invalidates the redo history.
        /// </summary>
        [Test]
        public void ExternalEffectInvalidatesRedoHistory()
        {
            hist.Execute(new Increment());
            hist.Update();
            hist.Undo();

            Assert.That(Counter.Value, Is.EqualTo(0));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            Increment externalAction = new();
            hist.Execute(externalAction);

            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            externalAction.ApplyExternalEffect();
            hist.InvalidateRedoHistory(externalAction);

            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));

            Assert.Throws<RedoImpossible>(() => hist.Redo());

            hist.Undo();

            Assert.That(Counter.Value, Is.EqualTo(0));
        }

        /// <summary>
        /// Verifies that an action without an effect or an inactive action
        /// cannot invalidate the existing redo history.
        /// </summary>
        [Test]
        public void InactiveOrUnusedActionDoesNotInvalidateRedoHistory()
        {
            Increment completedAction = new();

            hist.Execute(completedAction);
            hist.Update();
            hist.Undo();

            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            Decrement unusedAction = new();
            hist.Execute(unusedAction);

            hist.InvalidateRedoHistory(unusedAction);

            Assert.That(hist.RedoCount(), Is.EqualTo(1),
                "An unused action must not invalidate Redo.");

            hist.InvalidateRedoHistory(completedAction);

            Assert.That(hist.RedoCount(), Is.EqualTo(1),
                "An inactive action must not invalidate Redo.");

            hist.Redo();

            Assert.That(Counter.Value, Is.EqualTo(1));
        }


        /// <summary>
        /// Verifies the complete lifecycle of multiple actions, including undo, fresh redo restarts,
        /// preservation of earlier effects, and invalidation of remaining redo entries.
        /// </summary>
        [Test]
        public void MultipleActions()
        {
            TestAction c1 = new();
            TestAction c2 = new();
            TestAction c3 = new();
            TestAction c4 = new();

            CheckCalls(c1, value: false, awake: 0, start: 0, update: 0, stop: 0);
            CheckCalls(c2, value: false, awake: 0, start: 0, update: 0, stop: 0);
            CheckCalls(c3, value: false, awake: 0, start: 0, update: 0, stop: 0);
            CheckCalls(c4, value: false, awake: 0, start: 0, update: 0, stop: 0);

            hist.Execute(c1);
            CheckCalls(c1, value: false, awake: 1, start: 1, update: 0, stop: 0);
            hist.Update();
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

            // Undo c4 and resume c3.
            hist.Undo();
            Assert.That(hist.UndoCount(), Is.EqualTo(3));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c3, value: true, awake: 1, start: 2, update: 1, stop: 1);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            hist.Update();
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c3, value: true, awake: 1, start: 2, update: 2, stop: 1);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);

            // Undo c3 and resume c2.
            hist.Undo();
            Assert.That(hist.UndoCount(), Is.EqualTo(2));
            Assert.That(hist.RedoCount(), Is.EqualTo(2));
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 2, update: 1, stop: 1);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            hist.Update();
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 2, update: 2, stop: 1);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);

            // Redo c3 starts a fresh action without restoring the old preliminary effect.
            hist.Redo();
            TestAction restartedC3 = (TestAction)hist.CurrentAction;
            Assert.That(restartedC3, Is.Not.SameAs(c3));
            Assert.That(hist.UndoCount(), Is.EqualTo(3));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(restartedC3, value: false, awake: 1, start: 1, update: 0, stop: 0);

            // Redo c4 before the fresh c3 produces any new effect.
            hist.Redo();
            TestAction restartedC4 = (TestAction)hist.CurrentAction;
            Assert.That(restartedC4, Is.Not.SameAs(c4));
            Assert.That(hist.UndoCount(), Is.EqualTo(3));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(restartedC3, value: false, awake: 1, start: 1, update: 0, stop: 1);
            CheckCalls(restartedC4, value: false, awake: 1, start: 1, update: 0, stop: 0);

            hist.Update();
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(restartedC3, value: false, awake: 1, start: 1, update: 0, stop: 1);
            CheckCalls(restartedC4, value: true, awake: 1, start: 1, update: 1, stop: 0);

            // Undo restarted c4; the unused restarted c3 is skipped.
            hist.Undo();
            Assert.That(hist.CurrentAction, Is.SameAs(c2));
            Assert.That(hist.UndoCount(), Is.EqualTo(2));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 3, update: 2, stop: 2);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(restartedC3, value: false, awake: 1, start: 1, update: 0, stop: 1);
            CheckCalls(restartedC4, value: false, awake: 1, start: 1, update: 1, stop: 1);

            // A newly selected unused action preserves the remaining redo.
            TestAction c5 = new();
            CheckCalls(c5, value: false, awake: 0, start: 0, update: 0, stop: 0);
            hist.Execute(c5);
            Assert.That(hist.RedoCount(), Is.EqualTo(1));
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 3, update: 2, stop: 3);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 0, stop: 0);

            // A new effect from c5 invalidates the remaining redo history.
            hist.Update();
            Assert.That(hist.RedoCount(), Is.EqualTo(0));
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 3, update: 2, stop: 3);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c5, value: true, awake: 1, start: 1, update: 1, stop: 0);

            // Undo c5 and resume c2.
            hist.Undo();
            Assert.That(hist.CurrentAction, Is.SameAs(c2));
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 4, update: 2, stop: 3);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);

            hist.Update();
            CheckCalls(c1, value: true, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c2, value: true, awake: 1, start: 4, update: 3, stop: 3);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);

            // Undo c2 and resume c1.
            hist.Undo();
            Assert.That(hist.CurrentAction, Is.SameAs(c1));
            CheckCalls(c1, value: true, awake: 1, start: 2, update: 1, stop: 1);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 3, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);

            hist.Update();
            CheckCalls(c1, value: true, awake: 1, start: 2, update: 2, stop: 1);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 3, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);

            // Undo c1 and verify that no effect-bearing action remains.
            hist.Undo();
            Assert.That(hist.UndoCount(), Is.EqualTo(0));
            Assert.That(hist.RedoCount(), Is.EqualTo(3));
            CheckCalls(c1, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 3, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);

            hist.Update();
            CheckCalls(c1, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 3, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);

            // An unused c6 does not invalidate undone actions.
            TestAction c6 = new();
            CheckCalls(c6, value: false, awake: 0, start: 0, update: 0, stop: 0);
            hist.Execute(c6);
            Assert.That(hist.RedoCount(), Is.EqualTo(3));
            CheckCalls(c1, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 3, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c6, value: false, awake: 1, start: 1, update: 0, stop: 0);

            hist.Update();
            Assert.That(hist.RedoCount(), Is.EqualTo(0));
            CheckCalls(c1, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 3, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c6, value: true, awake: 1, start: 1, update: 1, stop: 0);

            // Undo c6 and verify that its previous effect stays undone.
            hist.Undo();
            CheckCalls(c1, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 3, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c6, value: false, awake: 1, start: 1, update: 1, stop: 1);
            Assert.That(hist.UndoCount(), Is.EqualTo(0));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            hist.Update();
            CheckCalls(c1, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c2, value: false, awake: 1, start: 4, update: 3, stop: 4);
            CheckCalls(c3, value: false, awake: 1, start: 2, update: 2, stop: 2);
            CheckCalls(c4, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c5, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(c6, value: false, awake: 1, start: 1, update: 1, stop: 1);

            // Redo c6 starts over rather than restoring the old effect.
            hist.Redo();
            TestAction restartedC6 = (TestAction)hist.CurrentAction;
            Assert.That(restartedC6, Is.Not.SameAs(c6));
            CheckCalls(c6, value: false, awake: 1, start: 1, update: 1, stop: 1);
            CheckCalls(restartedC6, value: false, awake: 1, start: 1, update: 0, stop: 0);
            Assert.That(hist.RedoCount(), Is.EqualTo(0));

            hist.Update();
            CheckCalls(restartedC6, value: true, awake: 1, start: 1, update: 1, stop: 0);
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
            Assert.That(counter.IsOn, Is.EqualTo(value), "IsOn not matching");
            Assert.That(counter.AwakeCalls, Is.EqualTo(awake), "awake calls not matching");
            Assert.That(counter.StartCalls, Is.EqualTo(start), "start calls not matching");
            Assert.That(counter.UpdateCalls, Is.EqualTo(update), "update calls not matching");
            Assert.That(counter.StopCalls, Is.EqualTo(stop), "stop calls not matching");
        }

        /// <summary>
        /// Provides a counter.
        /// </summary>
        private abstract class Counter : IReversibleAction
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

            protected IReversibleAction.Progress currentProgress = IReversibleAction.Progress.NoEffect;

            public IReversibleAction.Progress CurrentProgress()
            {
                return currentProgress;
            }

            public abstract IReversibleAction NewInstance();
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
            public override IReversibleAction NewInstance()
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

            /// <summary>
            /// Simulates a modification reported outside ActionHistory.Update.
            /// </summary>
            public void ApplyExternalEffect()
            {
                currentProgress = IReversibleAction.Progress.InProgress;
                counter++;
            }

            public override bool Update()
            {
                currentProgress = IReversibleAction.Progress.Completed;
                counter++;
                return true;
            }

            private class IncrementActionStateType : TestActionStateType
            {
                public IncrementActionStateType() : base(nameof(IncrementActionStateType), CreateReversibleAction)
                { }
            }

            private static readonly ActionStateType actionStateType = new IncrementActionStateType();

            private static IReversibleAction CreateReversibleAction()
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
            public override IReversibleAction NewInstance()
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
                currentProgress = IReversibleAction.Progress.Completed;
                counter--;
                return true;
            }

            private class DecrementActionStateType : TestActionStateType
            {
                public DecrementActionStateType() : base(nameof(DecrementActionStateType), CreateReversibleAction)
                { }
            }

            private static ActionStateType actionStateType = new DecrementActionStateType();

            private static IReversibleAction CreateReversibleAction()
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
        /// Tests that <see cref="ActionHistory.ActionStateChanged"/> is raised when the
        /// action state type changes.
        /// </summary>
        [Test]
        public void TestActionStateChanged()
        {
            int eventCalls = 0;
            ActionStateType previousState = null;
            ActionStateType currentState = null;

            hist.ActionStateChanged += (previous, current) =>
            {
                eventCalls++;
                previousState = previous;
                currentState = current;
            };

            Increment increment = new();
            hist.Execute(increment);

            Assert.That(eventCalls, Is.EqualTo(1));
            Assert.That(previousState, Is.Null);
            Assert.That(currentState, Is.SameAs(increment.GetActionStateType()));

            Decrement decrement = new();
            hist.Execute(decrement);

            Assert.That(eventCalls, Is.EqualTo(2));
            Assert.That(previousState, Is.SameAs(increment.GetActionStateType()));
            Assert.That(currentState, Is.SameAs(decrement.GetActionStateType()));
        }

        /// <summary>
        /// Tests that <see cref="ActionHistory.ActionStateChanged"/> is not raised when
        /// the action instance changes while its action state type remains the same.
        /// </summary>
        [Test]
        public void TestActionStateUnchanged()
        {
            int eventCalls = 0;

            hist.ActionStateChanged += (_, _) =>
            {
                eventCalls++;
            };

            hist.Execute(new Increment());

            Assert.That(eventCalls, Is.EqualTo(1));

            hist.Execute(new Increment());

            Assert.That(eventCalls, Is.EqualTo(1));

            hist.Update();

            Assert.That(eventCalls, Is.EqualTo(1));

            hist.Update();

            Assert.That(eventCalls, Is.EqualTo(1));
        }

        /// <summary>
        /// Tests that undoing and redoing actions raises
        /// <see cref="ActionHistory.ActionStateChanged"/> when the action state type changes.
        /// </summary>
        [Test]
        public void TestActionStateChangedOnUndoAndRedo()
        {
            int eventCalls = 0;
            ActionStateType previousState = null;
            ActionStateType currentState = null;

            hist.ActionStateChanged += (previous, current) =>
            {
                eventCalls++;
                previousState = previous;
                currentState = current;
            };

            Increment increment = new();
            hist.Execute(increment);
            hist.Update();

            Decrement decrement = new();
            hist.Execute(decrement);
            hist.Update();

            Assert.That(eventCalls, Is.EqualTo(2));

            hist.Undo();

            Assert.That(eventCalls, Is.EqualTo(3));
            Assert.That(previousState, Is.SameAs(decrement.GetActionStateType()));
            Assert.That(currentState, Is.SameAs(increment.GetActionStateType()));

            hist.Redo();

            Assert.That(eventCalls, Is.EqualTo(4));
            Assert.That(previousState, Is.SameAs(increment.GetActionStateType()));
            Assert.That(currentState, Is.SameAs(decrement.GetActionStateType()));
        }

        /// <summary>
        /// Tests that <see cref="ActionHistory.ActionStateChanged"/> reports a null action
        /// state when undoing the last remaining action.
        /// </summary>
        [Test]
        public void TestActionStateChangedToNullOnUndo()
        {
            int eventCalls = 0;
            ActionStateType previousState = null;
            ActionStateType currentState = null;

            hist.ActionStateChanged += (previous, current) =>
            {
                eventCalls++;
                previousState = previous;
                currentState = current;
            };

            Increment increment = new();
            hist.Execute(increment);
            hist.Update();

            Assert.That(eventCalls, Is.EqualTo(1));

            hist.Undo();

            Assert.That(eventCalls, Is.EqualTo(2));
            Assert.That(previousState, Is.SameAs(increment.GetActionStateType()));
            Assert.That(currentState, Is.Null);
        }

        /// <summary>
        /// Tests that <see cref="ActionHistory.ActionStateChanged"/> is raised before
        /// <see cref="IReversibleAction.Awake"/> is called for the new action.
        /// </summary>
        [Test]
        public void TestActionStateChangedBeforeAwake()
        {
            TestAction action = new();
            int eventCalls = 0;

            hist.ActionStateChanged += (_, current) =>
            {
                eventCalls++;
                Assert.That(current, Is.SameAs(action.GetActionStateType()));
                Assert.That(action.AwakeCalls, Is.EqualTo(0));
            };

            hist.Execute(action);

            Assert.That(eventCalls, Is.EqualTo(1));
            Assert.That(action.AwakeCalls, Is.EqualTo(1));
        }

        /// <summary>
        /// Tests the action history with non-continuous actions that complete on Update.
        /// Activating an unused action preserves Redo, while a new modification invalidates it.
        /// </summary>
        [Test]
        public void TestCounterAction()
        {
            hist.Execute(new Increment());
            Assert.That(Counter.Value, Is.EqualTo(0));
            Assert.That(hist.UndoCount(), Is.EqualTo(1));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));

            hist.Update();
            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.UndoCount(), Is.EqualTo(2));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));

            hist.Update();
            Assert.That(Counter.Value, Is.EqualTo(2));
            Assert.That(hist.UndoCount(), Is.EqualTo(3));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));

            hist.Update();
            Assert.That(Counter.Value, Is.EqualTo(3));
            Assert.That(hist.UndoCount(), Is.EqualTo(4));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));

            hist.Undo();
            Assert.That(Counter.Value, Is.EqualTo(2));
            Assert.That(hist.UndoCount(), Is.EqualTo(3));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            hist.Undo();
            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.UndoCount(), Is.EqualTo(2));
            Assert.That(hist.RedoCount(), Is.EqualTo(2));

            hist.Undo();
            Assert.That(Counter.Value, Is.EqualTo(0));
            Assert.That(hist.UndoCount(), Is.EqualTo(0));
            Assert.That(hist.RedoCount(), Is.EqualTo(3));

            hist.Redo();
            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.UndoCount(), Is.EqualTo(2));
            Assert.That(hist.RedoCount(), Is.EqualTo(2));

            hist.Redo();
            Assert.That(Counter.Value, Is.EqualTo(2));
            Assert.That(hist.UndoCount(), Is.EqualTo(3));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            // Activating Decrement does not yet modify the counter.
            // The remaining redo entry must therefore be preserved.
            hist.Execute(new Decrement());

            Assert.That(Counter.Value, Is.EqualTo(2));
            Assert.That(hist.UndoCount(), Is.EqualTo(3));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            // The first actual Decrement invalidates the redo history.
            hist.Update();

            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.UndoCount(), Is.EqualTo(4));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));

            // An unused Increment does not affect the counter.
            hist.Execute(new Increment());

            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.UndoCount(), Is.EqualTo(4));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));

            // Undo skips the unused Increment and undoes Decrement.
            hist.Undo();

            Assert.That(Counter.Value, Is.EqualTo(2));
            Assert.That(hist.UndoCount(), Is.EqualTo(3));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            // Undo the preceding Increment.
            hist.Undo();

            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.UndoCount(), Is.EqualTo(2));
            Assert.That(hist.RedoCount(), Is.EqualTo(2));

            // Redo the Increment.
            hist.Redo();

            Assert.That(Counter.Value, Is.EqualTo(2));
            Assert.That(hist.UndoCount(), Is.EqualTo(3));
            Assert.That(hist.RedoCount(), Is.EqualTo(1));

            // Redo Decrement.
            hist.Redo();

            Assert.That(Counter.Value, Is.EqualTo(1));
            Assert.That(hist.UndoCount(), Is.EqualTo(4));
            Assert.That(hist.RedoCount(), Is.EqualTo(0));
        }
    }
}
