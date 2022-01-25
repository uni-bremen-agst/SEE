using System.Collections.Generic;
using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// Responsible for the desktop UI for menus.
    /// </summary>
    /// <typeparam name="T">the type of entries used. Must be derived from <see cref="MenuEntry"/>.</typeparam>
    public partial class SimpleMenu<T>
    {
        /// <summary>
        /// The path to the prefab for the menu game object.
        /// Will be added as a child to the <see cref="Canvas"/> if it doesn't exist yet.
        /// </summary>
        private const string MENU_PREFAB = "Prefabs/UI/Menu";

        /// <summary>
        /// The path to the prefab for the button.
        /// Will be added for each menu entry in <see cref="entries"/>.
        /// </summary>
        private const string BUTTON_PREFAB = "Prefabs/UI/Button";

        /// <summary>
        /// The path to the prefab for the list game object.
        /// Will be added as a child to the <see cref="MenuGameObject"/>.
        /// </summary>
        private const string LIST_PREFAB = "Prefabs/UI/MenuEntries";

        /// <summary>
        /// The GameObject which contains the actual content of the menu, i.e. its entries.
        /// </summary>
        private GameObject MenuContent;

        /// <summary>
        /// The GameObject which has the <see cref="ModalWindowManager"/> component attached.
        /// </summary>
        private GameObject MenuGameObject;

        /// <summary>
        /// The modal window manager which contains the actual menu.
        /// </summary>
        private ModalWindowManager Manager;

        /// <summary>
        /// UI game object containing the entries as buttons.
        /// </summary>
        private GameObject EntryList;

        /// <summary>
        /// The tooltip in which the description is displayed.
        /// </summary>
        private Tooltip.Tooltip Tooltip;

        /// <summary>
        /// List of all button managers for the buttons used in this menu.
        /// </summary>
        protected readonly List<ButtonManagerBasicWithIcon> ButtonManagers = new List<ButtonManagerBasicWithIcon>();

        protected override void StartDesktop()
        {
            SetUpDesktopWindow();
            SetUpDesktopContent();
        }

        /// <summary>
        /// Sets up the window of the menu. In this case, we use a <see cref="ModalWindowManager"/>, which
        /// uses the given title, description, and icon. If we find managers attached to a game object
        /// whose name equals <see cref="Title"/>, this manager will be re-used. Otherwise a new
        /// <see cref="MenuGameObject"/> will be created and a new <see cref="Manager"/> will be
        /// attached to it.
        /// </summary>
        protected void SetUpDesktopWindow()
        {
            // Find ModalWindowManager with matching name
            ModalWindowManager[] managers = Canvas.GetComponentsInChildren<ModalWindowManager>();
            Manager = managers.FirstOrDefault(component => component.gameObject.name.Equals(Title));

            if (Manager == null)
            {
                // Create it from prefab if it doesn't exist yet
                MenuGameObject = PrefabInstantiator.InstantiatePrefab(MENU_PREFAB, Canvas.transform, false);
                MenuGameObject.name = Title;
                MenuGameObject.TryGetComponentOrLog(out Manager);
            }
            else
            {
                MenuGameObject = Manager.gameObject;
            }
            EnableClosingDesktop(MenuGameObject, allowNoSelection);

            // Set menu properties
            Manager.titleText = Title;
            Manager.descriptionText = Description;
            Manager.icon = Icon;
            HandleHideMenuRegistration();

            // Create tooltip
            Tooltip = gameObject.AddComponent<Tooltip.Tooltip>();

            // Find content GameObject for menu entries.
            MenuContent = MenuGameObject.transform.Find("Main Content/Content Mask/Content")?.gameObject;
            if (MenuContent == null)
            {
                Debug.LogError("Couldn't find required components on MenuGameObject.");
            }
        }

        /// <summary>
        /// Whether the menu should be hidden after the user has made a selection.
        /// </summary>
        private bool hideAfterSelection = true;

        /// <summary>
        /// Declares whether the menu should be hidden (<see cref="ShowMenu(false)"/>) when
        /// the user has made a selection.
        /// The default is to hide the menu after selection.
        /// </summary>
        /// <param name="hide">if true, the menu will be hidden after a selection</param>
        private void HideAfterSelectionDesktop(bool hide)
        {
            /// The <see cref="Manager"/> may exist only when <see cref="SetUpDesktopWindow()"/>
            /// has been called. For this reason, we save the client's wish here and either
            /// fulfill it now or later.
            hideAfterSelection = hide;
            if (Manager)
            {
                HandleHideMenuRegistration();
            }
        }

        /// <summary>
        /// If <see cref="hideAfterSelection"/> is true, <see cref="HideMenu"/> will be
        /// called when a user made a selection. If <see cref="hideAfterSelection"/> is
        /// false, the menu stays open after a selection.
        /// </summary>
        private void HandleHideMenuRegistration()
        {
            if (hideAfterSelection)
            {
                Manager.onConfirm.RemoveListener(HideMenu);
            }
            else
            {
                Manager.onConfirm.AddListener(HideMenu);
            }
        }

        /// <summary>
        /// Equivalent to <see cref="ShowMenu(false)"/>.
        /// </summary>
        protected void HideMenu()
        {
            ShowMenu(false);
        }

        /// <summary>
        /// Sets up the content of the previously created desktop window (<see cref="SetUpDesktopWindow"/>).
        /// In this case, buttons are created for each menu entry and added to the content GameObject.
        /// </summary>
        protected virtual void SetUpDesktopContent()
        {
            EntryList = MenuContent.transform.Find("Menu Entries/Scroll Area/List")?.gameObject;
            if (EntryList == null)
            {
                // Create menu entry list if it doesn't exist yet
                EntryList = PrefabInstantiator.InstantiatePrefab(LIST_PREFAB, MenuContent.transform, false);
                EntryList.name = "Menu Entries";
                // List should actually be the list, not the entry object
                EntryList = EntryList.transform.Find("Scroll Area/List").gameObject;
            }

            // Then, add all entries as buttons
            AddDesktopButtons(Entries);
        }

        /// <summary>
        /// Adds the given <paramref name="buttonEntries"/> as buttons to the Desktop menu.
        /// </summary>
        /// <param name="buttonEntries">The entries to add to the menu.</param>
        protected virtual void AddDesktopButtons(IEnumerable<T> buttonEntries)
        {
            foreach (T entry in buttonEntries)
            {
                GameObject button = PrefabInstantiator.InstantiatePrefab(BUTTON_PREFAB, EntryList.transform, false);
                GameObject text = button.transform.Find("Text").gameObject;
                GameObject icon = button.transform.Find("Icon").gameObject;

                button.name = entry.Title;
                if (!button.TryGetComponentOrLog(out ButtonManagerBasicWithIcon buttonManager) ||
                    !button.TryGetComponentOrLog(out Image buttonImage) ||
                    !text.TryGetComponentOrLog(out TextMeshProUGUI textMeshPro) ||
                    !icon.TryGetComponentOrLog(out Image iconImage) ||
                    !button.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    return;
                }

                buttonManager.buttonText = entry.Title;
                buttonManager.buttonIcon = entry.Icon;
                pointerHelper.EnterEvent.AddListener(() => Tooltip.Show(entry.Description));
                pointerHelper.ExitEvent.AddListener(Tooltip.Hide);
                if (entry.Enabled)
                {
                    buttonManager.clickEvent.AddListener(() => OnEntrySelected(entry));
                    buttonImage.color = entry.EntryColor;
                    textMeshPro.color = entry.EntryColor.IdealTextColor();
                    iconImage.color = entry.EntryColor.IdealTextColor();
                }
                else
                {
                    buttonManager.useRipple = false;
                    buttonImage.color = entry.DisabledColor;
                    textMeshPro.color = entry.DisabledColor.IdealTextColor();
                    iconImage.color = entry.DisabledColor.IdealTextColor();
                }

                ButtonManagers.Add(buttonManager);
            }
        }

        /// <summary>
        /// Destroys the button with the same text as the given <paramref name="entry"/>.
        /// If no such button exists, nothing will happen.
        /// If more than one such button exists, all of them will be removed.
        /// </summary>
        /// <param name="entry">The entry whose button shall be destroyed.</param>
        private void RemoveDesktopButton(T entry)
        {
            IEnumerable<ButtonManagerBasicWithIcon> managers = ButtonManagers?.Where(x => x.buttonText == entry.Title);
            if (managers != null)
            {
                foreach (ButtonManagerBasicWithIcon manager in managers)
                {
                    if (manager)
                    {
                        Destroy(manager.gameObject);
                    }
                    else
                    {
                        Debug.LogWarning("Couldn't remove entry, its button was already destroyed.");
                    }
                }
            }
        }

        protected override void UpdateDesktop()
        {
            if (MenuShown != CurrentMenuShown)
            {
                if (MenuShown)
                {
                    // Move window to the top of the hierarchy (which, confusingly, is actually at the bottom)
                    // so that this menu is rendered over any other potentially existing menu on the UI canvas
                    MenuGameObject.transform.SetAsLastSibling();
                    if (Manager)
                    {
                        Manager.OpenWindow();
                    }
                }
                else
                {
                    if (Manager)
                    {
                        Manager.CloseWindow();
                    }
                }

                CurrentMenuShown = MenuShown;
            }
        }

        /// <summary>
        /// Enables/disables the "Main Content/Buttons" child of <paramref name="menuGameObject"/>.
        /// </summary>
        /// <param name="enable">whether the button for closing should be enabled</param>
        private static void EnableClosingDesktop(GameObject menuGameObject, bool enable)
        {
            const string buttonsPath = "Main Content/Buttons";
            Transform buttons = menuGameObject.transform.Find(buttonsPath);
            if (buttons != null)
            {
                buttons.gameObject.SetActive(enable);
            }
            else
            {
                Debug.LogError($"{menuGameObject.GetFullName()} does not have a child '{buttonsPath}'.\n");
            }
        }
    }
}
