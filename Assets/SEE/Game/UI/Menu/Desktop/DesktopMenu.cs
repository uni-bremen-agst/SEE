using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI
{
    /// <summary>
    /// Responsible for the Desktop UI for Menus.
    /// </summary>
    /// <typeparam name="T">the type of entries used. Must be derived from <see cref="MenuEntry"/>.</typeparam>
    public partial class Menu<T>
    {

        /// <summary>
        /// The path to the prefab for the menu game object.
        /// Will be added as a child to the <see cref="Canvas"/> if it doesn't exist yet.
        /// </summary>
        private const string MENU_PREFAB = "Assets/Prefabs/UI/Menu.prefab";

        /// <summary>
        /// The path to the prefab for the menu game object.
        /// Will be added for each menu entry in <see cref="Entries"/>.
        /// </summary>
        private const string BUTTON_PREFAB = "Assets/Prefabs/UI/Button.prefab";
        
        /// <summary>
        /// The menu object which has the <see cref="ModalWindowManager"/> component attached.
        /// </summary>
        protected GameObject MenuGameObject;

        /// <summary>
        /// The modal window manager which contains the actual menu.
        /// </summary>
        protected ModalWindowManager Manager;
        
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
                Object menuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MENU_PREFAB);
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
        }
        
        /// <summary>
        /// Sets up the content of the previously created desktop window (<see cref="SetUpDesktopWindow"/>).
        /// In this case, buttons are created for each menu entry and added to the content GameObject.
        /// </summary>
        private void SetUpDesktopContent()
        {
            // Find content GameObject for menu entries.
            GameObject List = MenuGameObject
                              .transform.Find("Main Content/Content Mask/Content/List View/Scroll Area/List")?.gameObject;
            if (List == null)
            {
                Debug.LogError("Couldn't find required components on MenuGameObject.");
                return;
            }

            // Then, add all entries as buttons
            Object buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BUTTON_PREFAB);
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
                if (entry.Enabled)
                {
                    buttonManager.clickEvent.AddListener(entry.DoAction);
                    buttonImage.color = entry.EntryColor;
                    //TODO: white is used anyway, icon doesn't work
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

                //TODO: description is currently unused
            }
            
        }

        protected override void UpdateDesktop()
        {
            if (MenuShown != CurrentMenuShown)
            {
                // Toggle state when menu state has been changed
                Manager.AnimateWindow();
                CurrentMenuShown = MenuShown;
            }
        }
    }
}