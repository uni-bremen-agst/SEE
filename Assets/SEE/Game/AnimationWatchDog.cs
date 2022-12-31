using SEE.Game.Evolution;

namespace SEE.Game
{
    public partial class EvolutionRenderer
    {
        /// <summary>
        /// A watchdog for outstanding animations whose completion needs to be awaited
        /// until a particular method can be triggered.
        /// </summary>
        private abstract class AnimationWatchDog : CountingJoin
        {
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

            }
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
            }

            /// <summary>
            /// If there are no more other animations to be awaited, <see cref="Phase2AddNewAndExistingGraphElements"/>
            /// will be called.
            /// </summary>
            protected override void Continue()
            {
                evolutionRenderer.Phase2AddNewAndExistingGraphElements(next);
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
                this.next = next;
                Skip();
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
            /// If there are no more other animations to be awaited, <see cref="OnAnimationsFinished"/>
            /// will be called.
            /// </summary>
            protected override void Continue()
            {
                evolutionRenderer.OnAnimationsFinished();
            }
        }
    }
}
