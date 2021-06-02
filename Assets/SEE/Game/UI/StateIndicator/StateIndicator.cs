namespace SEE.Game.UI.StateIndicator
{
    /// <summary>
    /// An indicator of the current action run by the player.
    /// </summary>
    public class StateIndicator : AbstractStateIndicator
    {
        /// <summary>
        /// Sets the <see cref="Title"/> and <see cref="PREFAB"/> of this state indicator.
        /// </summary>
        private void Awake()
        {
            Title = "StateIndicator";
            PREFAB = "Prefabs/UI/ModePanel";
        }

        /// <summary>
        /// Adds the indicator prefab and parents it to the UI Canvas.
        /// </summary>
        protected override void StartDesktop()
        {
            StartDesktopInit();
        }
    }
}
