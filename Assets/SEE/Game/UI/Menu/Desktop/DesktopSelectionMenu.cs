namespace SEE.Game.UI
{
    /// <summary>
    /// Implements Desktop UI for selection menus.
    /// </summary>
    public partial class SelectionMenu
    {
        protected override void StartDesktop()
        {
            base.StartDesktop();
            //TODO: Use horizontal selector
        }

        protected override void UpdateDesktop()
        {
            base.UpdateDesktop();
            // ToggleMenuEntry entry = GetActiveEntry() ?? Entries[0];
            // OnEntrySelected(Entries[(Entries.IndexOf(entry) + 1) % Entries.Count]);
        }
    }
}