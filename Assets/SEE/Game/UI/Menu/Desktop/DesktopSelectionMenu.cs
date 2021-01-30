using UnityEngine;

namespace SEE.Game.UI
{
    /// <summary>
    /// Implements Desktop UI for selection menus.
    /// </summary>
    public partial class SelectionMenu
    {
        protected override void StartDesktop()
        {
            // TODO: Setup for Desktop UI here (or keep method empty)
        }

        protected override void UpdateDesktop()
        {
            // TODO: Just for testing purposes, pressing space currently cycles through available entries.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleMenuEntry entry = GetActiveEntry() ?? Entries[0];
                OnEntrySelected(Entries[(Entries.IndexOf(entry) + 1) % Entries.Count]);
            }
        }
    }
}