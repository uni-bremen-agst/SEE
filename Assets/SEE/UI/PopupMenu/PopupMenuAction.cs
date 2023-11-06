using System;
using UnityEngine.Events;

namespace SEE.UI.PopupMenu
{
    /// <summary>
    /// An action that can be added to a <see cref="PopupMenu"/>.
    /// </summary>
    /// <param name="Name">The name of the action.</param>
    /// <param name="Action">The action to be executed when the user clicks on the action.</param>
    /// <param name="IconGlyph">The unicode glyph of the FontAwesome v6 icon
    /// that should be displayed next to the action.</param>
    /// <param name="priority">The priority of the action. Actions with a higher priority
    /// are displayed first.</param>
    public record PopupMenuAction(string Name, Action Action, char IconGlyph, int priority = default);
}
