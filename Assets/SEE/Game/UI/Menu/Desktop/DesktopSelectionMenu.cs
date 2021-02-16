using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.UI
{
    /// <summary>
    /// Implements Desktop UI for selection menus.
    /// </summary>
    public partial class SelectionMenu
    {
        /// <summary>
        /// Path to the prefab containing the horizontal selector for the active selection.
        /// </summary>
        private const string SELECTOR_PREFAB = "Prefabs/UI/EntrySelector";

        /// <summary>
        /// Name of the game object created from the EntrySelector prefab.
        /// </summary>
        private const string SELECTOR_NAME = "Entry Selector";

        protected override void SetUpDesktopContent()
        {
            if (MenuGameObject.transform.Find("Main Content").gameObject.TryGetComponentOrLog(out RectTransform rect))
            {
                // Resize menu content to not have as much empty space as is necessary in a normal menu
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y / 2);
            }
            
            // Create selector if it doesn't exist yet
            GameObject selectorGO = MenuContent.transform.Find(SELECTOR_NAME)?.gameObject;
            if (!selectorGO)
            {
                Object selectorPrefab = Resources.Load<GameObject>(SELECTOR_PREFAB);
                selectorGO = Instantiate(selectorPrefab, MenuContent.transform, false) as GameObject;
                UnityEngine.Assertions.Assert.IsNotNull(selectorGO);
                selectorGO.name = SELECTOR_NAME;
            }

            if (!selectorGO.TryGetComponentOrLog(out HorizontalSelector selector))
            {
                return;
            }
            
            // Initialize selector entries
            foreach (ToggleMenuEntry entry in Entries.Where(x => x.Enabled))
            {
                selector.CreateNewItem(entry.Title);
                //TODO: Other entry attributes are not used (color, icon...)
            }

            // Set index to active entry so that it's selected
            selector.index = Entries.IndexOf(Entries.First(x => x.Active));
            selector.SetupSelector();
            
            selector.selectorEvent.AddListener(SelectEntry);
        }
    }
}