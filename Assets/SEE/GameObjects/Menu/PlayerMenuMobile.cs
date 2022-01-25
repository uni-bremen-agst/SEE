using System.Collections.Generic;
using System.Linq;
using SEE.Controls.Actions;
using SEE.Game.UI.Menu;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.GO.Menu
{
    /// <summary>
    /// Implements the behaviour of the in-game player menu, in which action states can be selected.
    /// </summary>
    public class PlayerMenuMobile : MonoBehaviour
    {
        /// <summary>
        /// The UI object representing the  mobile menu the user chooses the action state from.
        /// </summary>
        private SimpleMenu ModeMenu;

        /// <summary>
        /// This creates and returns the mobile menu, with which you can select the active game mode.
        ///
        /// Available modes can be found in <see cref="MobileActionStateType"/>.
        /// </summary>
        /// <returns>the newly created menu component.</returns>
        private static SimpleMenu CreateMenu(GameObject attachTo = null)
        {

            // Note: A ?? expression can't be used here, or Unity's overloaded null-check will be overridden.
            GameObject modeMenuGO = attachTo ? attachTo : new GameObject { name = "Mode Menu" };

            // IMPORTANT NOTE: Because an ActionStateType value will be used as an index into
            // the following field of menu entries, the rank of an entry in this field of entry
            // must correspond to the ActionStateType value. If this is not the case, we will
            // run into an endless recursion.

            MobileActionStateType firstType = MobileActionStateType.AllTypes.First();
            List<MenuEntry> entries = MobileActionStateType.AllTypes.Select(ToMenuEntry).ToList();

            // Initial state will be the first action state type
            GlobalActionHistory.ExecuteMoblie(firstType);

            SimpleMenu menu = modeMenuGO.AddComponent<SimpleMenu>();
            menu.Title = "Mobile Menu";
            menu.Description = "Please select the mode you want to activate.";
            menu.AddEntries(entries);

            return menu;

            // Constructs a toggle menu entry for the mode menu from the given action state type.
            MenuEntry ToMenuEntry(MobileActionStateType type) =>
                new MenuEntry(
                    action: () => GlobalActionHistory.ExecuteMoblie(type), title: type.Name,
                    description: type.Description, entryColor: type.Color,
                    icon: Resources.Load<Sprite>(type.IconPath));

        }


        private void Start()
        {
            ModeMenu = CreateMenu(gameObject);
        }

    }
}
