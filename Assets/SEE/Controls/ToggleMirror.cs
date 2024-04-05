namespace SEE.Controls
{
    /// <summary>
    /// Toggles the mirror.
    /// </summary>
    /// <remarks>This method is assumed to be attached to the mirror in the scene.</remarks>
    public class ToggleMirror : ToggleChildren
    {
        protected override bool ToggleCondition()
        {
            return SEEInput.ToggleMirror();
        }
    }
}
