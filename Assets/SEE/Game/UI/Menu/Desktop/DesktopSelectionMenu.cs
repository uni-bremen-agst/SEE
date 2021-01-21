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
            // Finished desktop menu should behave similarly to the old
            // circular menu, but with improved visuals. For reference, look at file "CircularMenu.cs"
            // Something that should be thought about later is whether to really check for the space press here --
            // after all, there may be multiple selection menus. Instead it may be better to call the ShowMenu() method
            // in the component calling MenuFactory.CreateModeMenu() (because there will only be one mode menu.)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                MenuShown = !MenuShown;
                Debug.Log($"Menu should no{(MenuShown ? "w" : "t")} be shown.");
                OnEntrySelected(Entries[(Entries.IndexOf(GetActiveEntry()) + 1) % Entries.Count]);
            }
        }
    }
}