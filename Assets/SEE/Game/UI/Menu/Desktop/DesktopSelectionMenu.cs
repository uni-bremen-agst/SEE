using Michsky.UI.ModernUIPack;
using TMPro;

namespace SEE.Game.UI
{

    /// <summary>
    /// Implements Desktop UI for selection menus.
    /// </summary>
    public partial class SelectionMenu
    {
        
        private ToggleMenuEntry currentSelectedEntry;
        
        protected override void SetUpDesktopContent()
        {
            base.SetUpDesktopContent();
            // Changes in comparison to the base method: 
            // 1. selecting an entry will close the menu
            // 2. the selected entry is highlighted
            foreach (ButtonManagerBasicWithIcon buttonManager in ButtonManagers)
            {
                buttonManager.clickEvent.AddListener(ToggleMenu);
            }
        }

        protected override void UpdateDesktop()
        {
            base.UpdateDesktop();
            // Only change when a different entry has been selected
            if (currentSelectedEntry != GetActiveEntry())
            {
                currentSelectedEntry = GetActiveEntry();
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