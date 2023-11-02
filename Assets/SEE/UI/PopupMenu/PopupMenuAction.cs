using UnityEngine.Events;

namespace SEE.UI.PopupMenu
{
    /// <summary>
    /// An action that can be added to a <see cref="PopupMenu"/>.
    /// </summary>
    /// <param name="Name">The name of the action.</param>
    /// <param name="Action">The action to be executed when the user clicks on the action.</param>
    /// <param name="IconPath">A path to an icon that should be displayed next to the action.</param>
    public record PopupMenuAction(string Name, UnityAction Action, string IconPath);
}
