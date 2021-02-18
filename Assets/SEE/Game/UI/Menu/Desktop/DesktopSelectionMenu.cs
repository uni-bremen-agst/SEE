using Michsky.UI.ModernUIPack;

namespace SEE.Game.UI
{
    /// <summary>
    /// Implements Desktop UI for selection menus.
    /// </summary>
    public partial class SelectionMenu
    {
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
            //TODO: For the active element, add an animation/outline/gradient showcasing it
            base.UpdateDesktop();
        }
    }
}