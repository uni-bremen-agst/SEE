namespace SEE.Controls
{
    /// <summary>
    /// Toggles the mirror.
    /// </summary>
    /// <remarks>This method is assumed to be attached to the mirror in the scene.</remarks>
    public class ToggleMirror : ToggleChildren
    {
        /// <summary>
        /// When the user requests to toggle the mirror, the children of the game object
        /// this component is attached to, will be enabled/disabled. The child object
        /// of the game object is the mirror object holding the cam mimicing a mirror.
        /// In other words, this camera is enabled or disabled, respectively.
        /// </summary>
        /// <returns>True if the user requested to toggle the mirror.</returns>
        protected override bool ToggleCondition()
        {
            return SEEInput.ToggleMirror();
        }
    }
}
