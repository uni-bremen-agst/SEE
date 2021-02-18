using System.Linq;
using DG.Tweening;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
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
        /// Will be added for each menu entry in <see cref="Entries"/>.
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
            
            // Add tooltip
            TooltipManager = MenuGameObject.GetComponentInChildren<TooltipManager>();
            if (TooltipManager == null)
            {
                // Create new tooltip GameObject
                Object tooltipPrefab = Resources.Load<GameObject>(TOOLTIP_PREFAB);
                GameObject tooltip = Instantiate(tooltipPrefab, Canvas.transform, false) as GameObject;
                UnityEngine.Assertions.Assert.IsNotNull(tooltip);
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
            GameObject List = MenuContent.transform.Find("Menu Entries/Scroll Area/List")?.gameObject;
            if (List == null)
            {
                // Create menu entry list if it doesn't exist yet
                Object listPrefab = Resources.Load<GameObject>(LIST_PREFAB);
                List = Instantiate(listPrefab, MenuContent.transform, false) as GameObject;
                if (List == null)
                {
                    Debug.LogError("Couldn't instantiate List object.");
                    return;
                }
                List.name = "Menu Entries";
                // List should actually be the list, not the entry object
                List = List.transform.Find("Scroll Area/List").gameObject;
            }

            // Then, add all entries as buttons
            Object buttonPrefab = Resources.Load<GameObject>(BUTTON_PREFAB);
            foreach (T entry in Entries)
            {
                GameObject button = Instantiate(buttonPrefab, List.transform, false) as GameObject;
                GameObject text = button?.transform.Find("Text").gameObject;
                GameObject icon = button?.transform.Find("Icon").gameObject;
                if (button == null || text == null || icon == null)
                {
                    Debug.LogError("Couldn't instantiate button for MenuEntry correctly.");
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
                if (entry.Enabled)
                {
                    buttonManager.clickEvent.AddListener(entry.DoAction);
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
            }
        }

        /// <summary>
        /// Displays a tooltip with the given <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The text to display in the tooltip.</param>
        protected void ShowTooltip(string text)
        {
            TooltipManager.allowUpdating = true;
            if (TooltipManager.tooltipObject.TryGetComponentOrLog(out CanvasGroup canvasGroup))
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
                //FIXME Tooltip isn't displayed. I suspect this is because the alpha value of the canvas group can't be changed at runtime.
                DOTween.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, 1f, 0.5f);
            }
        }

        protected override void UpdateDesktop()
        {
            if (MenuShown != CurrentMenuShown)
            {
                // Toggle state when menu state has been changed
                Manager?.AnimateWindow();
                CurrentMenuShown = MenuShown;
            }
        }
    }
}
