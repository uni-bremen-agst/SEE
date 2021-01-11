namespace SEE.Game.UI
{
    /// <summary>
    /// A menu in which the user can choose one active selection out of a menu.
    /// It is assumed that only one selection can be active at a time.
    /// </summary>
    public class SelectionMenu: Menu<ToggleMenuEntry>
    {
        public override void OnEntrySelected(ToggleMenuEntry entry)
        {
            // Disable all entries except the selected one, this will automatically call DoExitAction()
            foreach (ToggleMenuEntry listEntry in entries)
            {
                listEntry.Active = listEntry == entry;
            }
            // This will ensure that DoAction() is called on entry
            base.OnEntrySelected(entry);
        }
    }
}