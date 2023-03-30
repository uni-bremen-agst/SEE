namespace SEE.Game.Evolution
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
    }
}
