using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace SEE.Game.UI
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
        /// The path to the prefab for the tooltip game object.
        /// Will be added as a child to the <see cref="Canvas"/>.
        /// </summary>
        private const string TOOLTIP_PREFAB = "Prefabs/UI/Tooltip";

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
        /// The tooltip manager, which can (as its name implies) control tooltips.
        /// Note that this manager controls a single tooltip whose text can be changed. If multiple tooltips
        /// are needed, more GameObjects with TooltipManagers need to be created.
        /// </summary>
        protected TooltipManager TooltipManager;

        /// <summary>
        /// UI game object containing the entries as buttons.
        /// </summary>
        protected GameObject EntryList;

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
                UnityEngine.Assertions.Assert.IsNotNull(MenuGameObject);
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
            
            // Add tooltip
            TooltipManager = MenuGameObject.GetComponentInChildren<TooltipManager>();
            if (TooltipManager == null)
            {
                // Create new tooltip GameObject
                Object tooltipPrefab = Resources.Load<GameObject>(TOOLTIP_PREFAB);
                GameObject tooltip = Instantiate(tooltipPrefab, Canvas.transform, false) as GameObject;
                Assert.IsNotNull(tooltip);
                tooltip.TryGetComponentOrLog(out TooltipManager);
            }
            
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
        /// Adds the given <paramref name="entries"/> as buttons to the Desktop menu.
        /// </summary>
        /// <param name="entries">The entries to add to the menu.</param>
        protected virtual void AddDesktopButtons(IEnumerable<T> entries)
        {
            Object buttonPrefab = Resources.Load<GameObject>(BUTTON_PREFAB);
            foreach (T entry in entries)
            {
                GameObject button = Instantiate(buttonPrefab, EntryList.transform, false) as GameObject;
                Assert.IsNotNull(button);
                GameObject text = button.transform.Find("Text").gameObject;
                GameObject icon = button.transform.Find("Icon").gameObject;
                if (button == null || text == null || icon == null)
                {
                    Debug.LogError("Couldn't instantiate button for MenuEntry correctly.\n");
                    return;
                }

                button.name = entry.Title;
                if (!button.TryGetComponentOrLog(out ButtonManagerBasicWithIcon buttonManager) ||
                    !button.TryGetComponentOrLog(out Image buttonImage) ||
                    !text.TryGetComponentOrLog(out TextMeshProUGUI textMeshPro) ||
                    !icon.TryGetComponentOrLog(out Image iconImage))
                {
                    return;
                }

                buttonManager.buttonText = entry.Title;
                buttonManager.buttonIcon = entry.Icon;
                buttonManager.hoverEvent.AddListener(() => ShowTooltip(entry.Description));
                buttonManager.hoverExitEvent.AddListener(() => HideTooltip());
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
        /// <exception cref="InvalidOperationException">If more than one button with the same text as
        /// <paramref name="entry"/> exists.</exception>
        protected void RemoveDesktopButton(T entry)
        {
            Destroy(ButtonManagers.SingleOrDefault(x => x.buttonText == entry.Title)?.gameObject);
        }

        /// <summary>
        /// Displays a tooltip with the given <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The text to display in the tooltip.</param>
        protected void ShowTooltip(string text)
        {
            TooltipManager.allowUpdating = true;
            // tooltipObject only has 1 child, and will never have more than that
            if (TooltipManager.tooltipObject.gameObject.transform.GetChild(0).gameObject.TryGetComponentOrLog(out CanvasGroup canvasGroup))
            {
                // Change text
                TextMeshProUGUI[] texts = TooltipManager.tooltipContent.GetComponentsInChildren<TextMeshProUGUI>();
                TextMeshProUGUI textComp = texts.FirstOrDefault(x => x.name == "Description");
                if (textComp == null)
                {
                    Debug.LogError("Couldn't find Description text component for tooltip.");
                    return;
                }
                textComp.text = text;
                
                // Fade in 
                DOTween.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, 1f, 0.5f);
            }
        }

        /// <summary>
        /// Hides the tooltip previously displayed
        /// </summary>
        protected void HideTooltip()
        {
            TooltipManager.allowUpdating = true;
            if (TooltipManager.tooltipObject.gameObject.transform.GetChild(0).gameObject.TryGetComponentOrLog(out CanvasGroup canvasGroup))
            {
                // Fade out
                DOTween.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, 0f, 0.5f);
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
