using System;

namespace SEE.UI.PopupMenu
{
    /// <summary>
    /// An entry in a <see cref="PopupMenu"/>.
    /// <param name="Priority">The priority of the entry. Entries with a higher priority
    /// are displayed first.</param>
    /// </summary>
    public abstract record PopupMenuEntry(int Priority);

    /// <summary>
    /// An action that can be added to a <see cref="PopupMenu"/>.
    /// </summary>
    /// <param name="Name">The name of the action.</param>
    /// <param name="Action">The action to be executed when the user clicks on the action.</param>
    /// <param name="IconGlyph">The unicode glyph of the FontAwesome v6 icon
    /// that should be displayed next to the action.</param>
    /// <param name="CloseAfterClick">Whether the menu should be closed after the action is executed.</param>
    /// <param name="Priority">The priority of the entry. Entries with a higher priority
    /// are displayed first.</param>
    public record PopupMenuAction(string Name, Action Action, char IconGlyph, bool CloseAfterClick = true,
        int Priority = default) : PopupMenuEntry(Priority);

    /// <summary>
    /// A heading that can be added to a <see cref="PopupMenu"/>.
    /// </summary>
    /// <param name="Text">The text of the heading.</param>
    /// <param name="Priority">The priority of the entry. Entries with a higher priority
    /// are displayed first.</param>
    public record PopupMenuHeading(string Text, int Priority = default) : PopupMenuEntry(Priority);

    /// <summary>
    /// A sub menu action that can be added to a <see cref="PopupMenu"/>.
    /// </summary>
    /// <param name="Name">The name of the action.</param>
    /// <param name="Action">The action to be executed when the user clicks on the action.</param>
    /// <param name="LeftIconGlyph">The unicode glyph of the FontAwesome v6 icon
    /// that should be displayed left of the <paramref name="Name"/>.</param>
    /// <param name="RightIconGlyph">The unicode glyph of the FontAwesome v6 icon
    /// that should be displayed right of the <paramref name="Name"/>.</param>
    /// <param name="CloseAfterClick">Whether the menu should be closed after the action is executed.</param>
    /// <param name="Priority">The priority of the entry. Entries with a higher priority
    /// are displayed first.</param>
    public record PopupMenuActionDoubleIcon(string Name, Action Action, char LeftIconGlyph, char RightIconGlyph,
        bool CloseAfterClick = true, int Priority = default) : PopupMenuAction(Name, Action, LeftIconGlyph, CloseAfterClick, Priority);
}
