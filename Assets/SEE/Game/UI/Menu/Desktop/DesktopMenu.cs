using System.Collections.Generic;
using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// Responsible for the desktop UI for menus.
    /// </summary>
    /// <typeparam name="T">the type of entries used. Must be derived from <see cref="MenuEntry"/>.</typeparam>
    public partial class Menu<T>
    {
        /// <summary>
        /// The path to the prefab for the menu game object.
        /// Will be added as a child to the <see cref="Canvas"/> if it doesn't exist yet.
        /// </summary>
        private const string MENU_PREFAB = "Prefabs/UI/Menu";

        /// <summary>
        /// The path to the prefab for the menu game object.
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
        protected GameObject MenuContent;
        
        /// <summary>
        /// The GameObject which has the <see cref="ModalWindowManager"/> component attached.
        /// </summary>
        protected GameObject MenuGameObject;

        /// <summary>
        /// The modal window manager which contains the actual menu.
        /// </summary>
        protected ModalWindowManager Manager;

        /// <summary>
        /// UI game object containing the entries as buttons.
        /// </summary>
        protected GameObject EntryList;

        /// <summary>
        /// The tooltip in which the description is displayed.
        /// </summary>
        protected Tooltip.Tooltip Tooltip;

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
        /// uses the given title, description, and icon.
        /// </summary>
        protected void SetUpDesktopWindow()
        {
            // Find ModalWindowManager with matching name
            ModalWindowManager[] managers = Canvas.GetComponentsInChildren<ModalWindowManager>();
            Manager = managers.FirstOrDefault(component => component.gameObject.name.Equals(Title));
            if (Manager == null)
            {
                // Create it from prefab if it doesn't exist yet
                Object menuPrefab = Resources.Load<GameObject>(MENU_PREFAB);
                MenuGameObject = Instantiate(menuPrefab, Canvas.transform, false) as GameObject;
                Assert.IsNotNull(MenuGameObject);
                MenuGameObject.name = Title;
                MenuGameObject.TryGetComponentOrLog(out Manager);
            }
            else
            {
                MenuGameObject = Manager.gameObject;
            }

            // Set menu properties
            Manager.titleText = Title;
            Manager.descriptionText = Description;
            Manager.icon = Icon;
            Manager.onConfirm.AddListener(() => ShowMenu(false));

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
        /// Sets up the content of the previously created desktop window (<see cref="SetUpDesktopWindow"/>).
        /// In this case, buttons are created for each menu entry and added to the content GameObject.
        /// </summary>
        protected virtual void SetUpDesktopContent()
        {
            EntryList = MenuContent.transform.Find("Menu Entries/Scroll Area/List")?.gameObject;
            if (EntryList == null)
            {
                // Create menu entry list if it doesn't exist yet
                Object listPrefab = Resources.Load<GameObject>(LIST_PREFAB);
                EntryList = Instantiate(listPrefab, MenuContent.transform, false) as GameObject;
                if (EntryList == null)
                {
                    Debug.LogError("Couldn't instantiate List object.");
                    return;
                }
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
            Object buttonPrefab = Resources.Load<GameObject>(BUTTON_PREFAB);
            foreach (T entry in buttonEntries)
            {
                GameObject button = Instantiate(buttonPrefab, EntryList.transform, false) as GameObject;
                Assert.IsNotNull(button);
                GameObject text = button.transform.Find("Text").gameObject;
                GameObject icon = button.transform.Find("Icon").gameObject;

                button.name = entry.Title;
                if (!button.TryGetComponentOrLog(out ButtonManagerBasicWithIcon buttonManager) ||
                    !button.TryGetComponentOrLog(out Image buttonImage) ||
                    !text.TryGetComponentOrLog(out TextMeshProUGUI textMeshPro) ||
                    !icon.TryGetComponentOrLog(out Image iconImage) ||
                    !button.TryGetComponentOrLog(out EventTrigger triggerComponent))
                {
                    return;
                }

                buttonManager.buttonText = entry.Title;
                buttonManager.buttonIcon = entry.Icon;
                buttonManager.hoverEvent.AddListener(() => Tooltip.Show(entry.Description));
                if (triggerComponent.triggers.Count != 1)
                {
                    Debug.LogError("The 'Event Trigger' component may only contain one trigger for the "
                                   + "'PointerExit' event, not more and not fewer.\n");
                    return;
                }
                EventTrigger.Entry trigger = triggerComponent.triggers.Single();
                trigger.callback.AddListener(_ => Tooltip.Hide());
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
        /// If more than one such button exists, an InvalidOperationException will be thrown.
        /// </summary>
        /// <param name="entry">The entry whose button shall be destroyed.</param>
        /// <exception cref="System.InvalidOperationException">If more than one button with the same text as
        /// <paramref name="entry"/> exists.</exception>
        protected void RemoveDesktopButton(T entry)
        {
            Destroy(ButtonManagers.SingleOrDefault(x => x.buttonText == entry.Title)?.gameObject);
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
                    Manager?.OpenWindow();
                }
                else
                {
                    Manager?.CloseWindow();
                }

                CurrentMenuShown = MenuShown;
            }
        }
    }
}
