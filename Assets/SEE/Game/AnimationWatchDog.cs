using SEE.DataModel.DG;
using SEE.Game.Evolution;
using System;

namespace SEE.Game
{
    public partial class EvolutionRenderer
    {
        /// <summary>
        /// A watchdog for outstanding animations whose completion needs to be awaited
        /// until a particular method can be triggered.
        /// </summary>
        private abstract class AnimationWatchDog
        {
            /// <summary>
            /// The number of outstanding animations that need to be awaited before
            /// a particular method can be called.
            /// </summary>
            protected int outstandingAnimations;

            /// <summary>
            /// The <see cref="EvolutionRenderer"/> whose method should be called
            /// when there are no more outstanding animations.
            /// </summary>
            protected readonly EvolutionRenderer evolutionRenderer;

            /// <summary>
            /// Constructor setting the <see cref="EvolutionRenderer"/> whose method should
            /// be called when there are no more outstanding animations. The number of
            /// outstanding animations is assumed to be zero at this point. The correct
            /// value can be set by <see cref="Await(int)"/> later.
            /// </summary>
            /// <param name="evolutionRenderer"><see cref="EvolutionRenderer"/> whose method should be called
            /// when there are no more outstanding animations</param>
            public AnimationWatchDog(EvolutionRenderer evolutionRenderer)
            {
                this.evolutionRenderer = evolutionRenderer;
                outstandingAnimations = 0;
            }

            /// <summary>
            /// Sets the <paramref name="numberOfAnimations"/> to be waited for until the
            /// particular method should be called.
            /// </summary>
            /// <param name="numberOfAnimations">the number of animations to be awaited</param>
            public void Await(int numberOfAnimations)
            {
                outstandingAnimations = numberOfAnimations;
            }

            /// <summary>
            /// Signals this <see cref="AnimationWatchDog"/> that one animation has been completed.
            /// If there are no more other animations to be awaited, the particular method will be
            /// called. That method depends upon the concrete subclass.
            /// </summary>
            public abstract void Finished();
        }

        /// <summary>
        /// A watchdog awaiting all animations of the first phase to be finished. The first
        /// phase is dedicated to the deletion of graph elements not present in the next graph.
        /// When all deletion animations have completed, <see cref="Phase2AddNewAndExistingGraphElements"/>
        /// will be called.
        /// </summary>
        private class Phase1AnimationWatchDog : AnimationWatchDog
        {
            /// <summary>
            /// The next graph to be shown.
            /// </summary>
            private LaidOutGraph next;

            /// <summary>
            /// Constructor setting the <see cref="EvolutionRenderer"/> whose method should
            /// be called when there are no more outstanding animations. The number of
            /// outstanding animations is assumed to be zero at this point. The correct
            /// value can be set by <see cref="Await(int)"/> later.
            /// </summary>
            /// <param name="evolutionRenderer"><see cref="EvolutionRenderer"/> whose method should be called
            /// when there are no more outstanding animations</param>
            public Phase1AnimationWatchDog(EvolutionRenderer evolutionRenderer)
                : base(evolutionRenderer)
            { }

            /// <summary>
            /// Sets the <paramref name="numberOfAnimations"/> to be waited for until the
            /// <see cref="Phase2AddNewAndExistingGraphElements"/> should be called.
            /// </summary>
            /// <param name="numberOfAnimations">the number of animations to be awaited</param>
            public void Await(int numberOfAnimations, LaidOutGraph next)
            {
                Await(numberOfAnimations);
                this.next = next;
            }

            /// <summary>
            /// Signals this <see cref="Phase1AnimationWatchDog"/> that one animation has been completed.
            /// If there are no more other animations to be awaited, <see cref="Phase2AddNewAndExistingGraphElements"/>
            /// will be called.
            /// </summary>
            public override void Finished()
            {
                outstandingAnimations--;
                if (outstandingAnimations == 0)
                {
                    evolutionRenderer.Phase2AddNewAndExistingGraphElements(next);
                }
            }

            /// <summary>
            /// Tells this <see cref="Phase1AnimationWatchDog"/> to skip the waiting for
            /// outstanding animations. <see cref="Phase2AddNewAndExistingGraphElements"/>
            /// will be called immediately. <paramref name="next"/> will be passed as
            /// argument to <see cref="Phase2AddNewAndExistingGraphElements"/>.
            /// </summary>
            /// <param name="next">the next graph to be shown</param>
            public void Skip(LaidOutGraph next)
            {
                outstandingAnimations = 0;
                evolutionRenderer.Phase2AddNewAndExistingGraphElements(next);
            }
        }

        /// <summary>
        /// A watchdog awaiting all animations of the second phase to be finished. The second
        /// phase is dedicated to drawing all graph elements present in the graph next to
        /// be drawn.When all deletion animations have completed, <see cref="OnAnimationsFinished"/>
        /// will be called.
        /// </summary>
        private class Phase2AnimationWatchDog : AnimationWatchDog
        {
            /// <summary>
            /// Constructor setting the <see cref="EvolutionRenderer"/> whose method should
            /// be called when there are no more outstanding animations. The number of
            /// outstanding animations is assumed to be zero at this point. The correct
            /// value can be set by <see cref="Await(int)"/> later.
            /// </summary>
            /// <param name="evolutionRenderer"><see cref="EvolutionRenderer"/> whose method should be called
            /// when there are no more outstanding animations</param>
            public Phase2AnimationWatchDog(EvolutionRenderer evolutionRenderer)
                : base(evolutionRenderer)
            { }

            /// <summary>
            /// Signals this <see cref="Phase2AnimationWatchDog"/> that one animation has been completed.
            /// If there are no more other animations to be awaited, <see cref="OnAnimationsFinished"/>
            /// will be called.
            /// </summary>
            public override void Finished()
            {
                outstandingAnimations--;
                if (outstandingAnimations == 0)
                {
                    evolutionRenderer.OnAnimationsFinished();
                }
            }
        }
    }
}
