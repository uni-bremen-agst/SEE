using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using TMPro;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// Implements Desktop UI for selection menus.
    /// </summary>
    public partial class SelectionMenu
    {
        /// <summary>
        /// The entry which is visually marked as selected.
        /// May be different from <see cref="GetActiveEntry"/>:
        /// In this case, the visuals will be updated accordingly
        /// and this variable will be set to <see cref="GetActiveEntry"/>.
        /// </summary>
        private ToggleMenuEntry currentSelectedEntry;

        protected override void AddDesktopButtons(IEnumerable<ToggleMenuEntry> menuEntries)
        {
            base.AddDesktopButtons(menuEntries);
            // Changes in comparison to the base method:
            // 1. selecting an entry will close the menu
            // 2. the selected entry is highlighted
            foreach (ButtonManagerBasicWithIcon buttonManager in ButtonManagers)
            {
                buttonManager.clickEvent.AddListener(HideMenu);
            }
        }

        protected override void UpdateDesktop()
        {
            base.UpdateDesktop();
            // Only change when a different entry has been selected
            if (currentSelectedEntry != ActiveEntry)
            {
                currentSelectedEntry = ActiveEntry;
                foreach (ButtonManagerBasicWithIcon manager in ButtonManagers)
                {
                    // Indicate selected button by typesetting it [LIKE THIS]
                    if (manager.buttonText.Equals(currentSelectedEntry.Title))
                    {
                        manager.buttonText = $"[{currentSelectedEntry.Title}]";
                        manager.normalText.fontStyle = FontStyles.UpperCase;
                    }
                    else
                    {
                        manager.buttonText = manager.buttonText.TrimStart('[').TrimEnd(']');
                        manager.normalText.fontStyle = 0;
                    }
                    manager.normalText.text = manager.buttonText;
                }
            }
        }
    }
}