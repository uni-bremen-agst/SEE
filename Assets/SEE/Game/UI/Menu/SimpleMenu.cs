using System;
using Michsky.UI.ModernUIPack;
using SEE.Utils;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// A simple list menu containing <see cref="MenuEntry"/> items.
    /// </summary>
    public class SimpleMenu : SimpleMenu<MenuEntry> { }

    /// <summary>
    /// A simple list menu which instantiates prefabs for each menu entry.
    /// The hierarchy of the prefabs and which prefabs are used are customizable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleMenu<T> : ListMenu<T> where T : MenuEntry
    {
        /// <summary>
        /// Prefab for the menu.
        /// </summary>
        protected virtual string MenuPrefab => UI_PREFAB_FOLDER + "Menu";
        /// <summary>
        /// Prefab for the list containing the menu entries.
        /// Only used if nothing is found at <see cref="EntryListPath"/>.
        /// </summary>
        protected virtual string EntryListPrefab => UI_PREFAB_FOLDER + "MenuEntries";
        /// <summary>
        /// Prefab for each menu entry.
        /// </summary>
        protected virtual string EntryPrefab => UI_PREFAB_FOLDER + "Button";
        /// <summary>
        /// Sprite for the icon.
        /// </summary>
        protected virtual string IconSprite => "Materials/ModernUIPack/Settings";

        /// <summary>
        /// The path to the game object containing the icon and the title.
        /// </summary>
        protected virtual string IconTitlePath => "Main Content/Icon Title Mask/Content";
        /// <summary>
        /// Path to the close button game object.
        /// </summary>
        protected virtual string CloseButtonPath => "Main Content/Buttons";
        /// <summary>
        /// Path to the content game object.
        /// Contains the entry list.
        /// </summary>
        protected virtual string ContentPath => "Main Content/Content Mask/Content";
        /// <summary>
        /// Path to the entry list game object.
        /// Is child of the content game object.
        /// </summary>
        protected virtual string EntryListPath => "Menu Entries/Scroll Area/List";
        
        /// <summary>
        /// The menu manager.
        /// </summary>
        protected ModalWindowManager MenuManager { get; private set; }
        /// <summary>
        /// The game object containing the icon and the title.
        /// </summary>
        protected GameObject IconTitle { get; private set; }
        /// <summary>
        /// The close button game object.
        /// </summary>
        protected GameObject CloseButton { get; private set; }
        /// <summary>
        /// The content game object.
        /// </summary>
        protected GameObject Content { get; private set; }
        /// <summary>
        /// The entry list game object.
        /// </summary>
        protected GameObject EntryList { get; private set; }
        
        /// <summary>
        /// Returns the game object corresponding to a menu entry.
        /// Assumes that the menu contains the entry.
        /// </summary>
        /// <param name="entry">The menu entry.</param>
        /// <returns>The game object of the entry.</returns>
        public override GameObject EntryGameObject(T entry) => EntryList.transform.Find(entry.Title).gameObject;
        
        /// <summary>
        /// The menu tooltip.
        /// </summary>
        protected Tooltip.Tooltip MenuTooltip { get; private set; }

        /// <summary>
        /// Initializes the menu and stores specific parts of the menu.
        /// Creates the entry list if necessary.
        /// </summary>
        protected override void StartDesktop()
        {
            // instantiates the menu
            Menu = PrefabInstantiator.InstantiatePrefab(MenuPrefab, Canvas.transform, false);
            MenuManager = Menu.GetComponent<ModalWindowManager>();
            
            // stores specific parts of the menu
            IconTitle = Menu.transform.Find(IconTitlePath).gameObject;
            CloseButton = Menu.transform.Find(CloseButtonPath).gameObject;
            Content = Menu.transform.Find(ContentPath).gameObject;
            // instantiates the entry list if necessary
            if (!Content.transform.Find(EntryListPath))
            {
                GameObject go = PrefabInstantiator.InstantiatePrefab(EntryListPrefab, Content.transform, false);
                // uses the start of EntryListPath as the name
                go.name = EntryListPath.Split('/')[0];
            }
            EntryList = Content.transform.Find(EntryListPath).gameObject;

            // creates the tooltip
            MenuTooltip = Menu.AddComponent<Tooltip.Tooltip>();
        }

        /// <summary>
        /// <see cref="StartDesktop"/>
        /// </summary>
        protected override void StartVR() => StartDesktop();
        
        /// <summary>
        /// <see cref="StartDesktop"/>
        /// </summary>
        protected override void StartTouchGamepad() => StartDesktop();

        /// <summary>
        /// Loads the sprite.
        /// </summary>
        private void Awake()
        {
            Icon = Resources.Load<Sprite>(IconSprite);
        }

        /// <summary>
        /// Updates the menu and adds listeners for updating the menu.
        /// </summary>
        protected override void OnStartFinished()
        {
            base.OnStartFinished();
            // updates the menu
            if (Title != null) UpdateTitle();
            if (Description != null) UpdateDescription();
            if (Icon != null) UpdateIcon();
            if (ShowMenu) UpdateShowMenu();
            Entries.ForEach(AddButton);
            // adds listeners for updating the menu
            OnTitleChanged += UpdateTitle;
            OnDescriptionChanged += UpdateDescription;
            OnIconChanged += UpdateIcon;
            OnShowMenuChanged += UpdateShowMenu;
            OnEntryAdded += AddButton;
            OnEntryRemoved += DestroyButton;
        }

        /// <summary>
        /// Updates the title.
        /// </summary>
        protected virtual void UpdateTitle()
        {
            Menu.name = Title;
            MenuManager.titleText = Title;
            UpdateLayout();
        }

        /// <summary>
        /// Updates the description.
        /// </summary>
        protected virtual void UpdateDescription()
        {
            MenuManager.descriptionText = Description;
            UpdateLayout();
        }

        /// <summary>
        /// Updates the icon.
        /// </summary>
        protected virtual void UpdateIcon()
        {
            MenuManager.icon = Icon;
            UpdateLayout();
        }

        /// <summary>
        /// Updates whether the menu is shown.
        /// </summary>
        protected virtual void UpdateShowMenu()
        {
            if (ShowMenu)
            {
                Menu.transform.SetAsLastSibling();
                MenuManager.OpenWindow();
                MenuTooltip.enabled = true;
            }
            else
            {
                MenuManager.CloseWindow();
                MenuTooltip.enabled = false;
            }
        }

        /// <summary>
        /// Instantiates the button for a menu entry.
        /// It is assumed that the menu contains the entry.
        /// </summary>
        /// <param name="entry">The added menu entry.</param>
        protected virtual void AddButton(T entry)
        {
            Debug.Log($"Add Button {entry.Title}");
            GameObject button =
                PrefabInstantiator.InstantiatePrefab(EntryPrefab, EntryList.transform, false);

            // title and icon
            button.name = entry.Title;
            ButtonManagerBasicWithIcon manager = button.GetComponent<ButtonManagerBasicWithIcon>();
            manager.buttonText = entry.Title;
            manager.buttonIcon = entry.Icon;
            
            // hover listeners
            PointerHelper pointerHelper = button.GetComponent<PointerHelper>();
            pointerHelper.EnterEvent.AddListener(() => MenuTooltip.Show(entry.Description));
            pointerHelper.ExitEvent.AddListener(MenuTooltip.Hide);

            // select listener or show that button is disabled
            if (entry.Enabled) 
                manager.clickEvent.AddListener(() => SelectEntry(entry));
            else 
                manager.useRipple = false;
            
            // colors
            Color color = entry.Enabled ? entry.EntryColor : entry.DisabledColor;
            button.GetComponent<Image>().color = color;
            Color textColor = color.IdealTextColor();
            button.transform.Find("Text").gameObject.GetComponent<TextMeshProUGUI>().color = textColor;
            button.transform.Find("Icon").gameObject.GetComponent<Image>().color = textColor;
        }

        /// <summary>
        /// Destroys the button for a removed menu entry.
        /// It is assumed that the menu contained the entry.
        /// </summary>
        /// <param name="entry">The menu entry.</param>
        protected virtual void DestroyButton(T entry)
        {
            Destroy(EntryGameObject(entry));
        }

        /// <summary>
        /// Updates the menu layout.
        /// </summary>
        protected virtual void UpdateLayout()
        {
            MenuManager.UpdateUI();
            LayoutRebuilder.ForceRebuildLayoutImmediate(IconTitle.transform as RectTransform);
        }
    }
}
