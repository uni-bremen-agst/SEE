using System.Collections.Generic;
using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

namespace SEE.Game.UI.Menu
{
    public class SimpleMenu : PlatformDependentComponent
    {
        /// <summary>
        /// Prefab for the menu.
        /// </summary>
        protected virtual string MenuPrefab => UI_PREFAB_FOLDER + "Menu";
        /// <summary>
        /// Sprite for the icon.
        /// </summary>
        protected virtual string IconSprite => "Materials/ModernUIPack/Settings";

        /// <summary>
        /// The title of this menu.
        /// </summary>
        private string title;
        /// <summary>
        /// The title of this menu.
        /// </summary>
        public string Title
        {
            get => title;
            set
            {
                title = value;
                OnTitleChanged?.Invoke();
            }
        }
    
        /// <summary>
        /// The description of this menu.
        /// </summary>
        private string description;
        /// <summary>
        /// The description of this menu.
        /// </summary>
        public string Description
        {
            get => description;
            set
            {
                description = value;
                OnDescriptionChanged?.Invoke();
            }
        }
    
        /// <summary>
        /// The icon of this menu.
        /// </summary>
        private Sprite icon;
        /// <summary>
        /// The icon of this menu.
        /// </summary>
        public Sprite Icon
        {
            get => icon;
            set
            {
                icon = value;
                OnIconChanged?.Invoke();
            }
        }
    
        /// <summary>
        /// The keyword listener.
        /// </summary>
        protected KeywordInput KeywordListener;
        
        /// <summary>
        /// The keyword to close the menu.
        /// </summary>
        private string closeMenuKeyword = "close menu";
        /// <summary>
        /// The keyword to close the menu.
        /// </summary>
        public string CloseMenuKeyword
        {
            get => closeMenuKeyword;
            set
            {
                closeMenuKeyword = value;
                OnCloseMenuCommandChanged?.Invoke();
            }
        }

        /// <summary>
        /// Whether the menu is shown.
        /// </summary>
        private bool showMenu;
        /// <summary>
        /// Whether the menu is shown.
        /// </summary>
        public bool ShowMenu
        {
            get => showMenu;
            set
            {
                showMenu = value;
                OnShowMenuChanged?.Invoke();
            }
        }

        /// <summary>
        /// The parent of the menu.
        /// Uses <see cref="Canvas"/> by default.
        /// </summary>
        private Transform parent;

        /// <summary>
        /// The parent of the menu.
        /// Uses <see cref="Canvas"/> by default.
        /// </summary>
        protected Transform Parent
        {
            get => parent != null ? parent : Canvas.transform;
            set
            {
                parent = value;
                OnParentChanged?.Invoke();
            }
        }
    
        /// <summary>
        /// Toggles the menu.
        /// </summary>
        /// <see cref="ShowMenu"/>
        public void ToggleMenu()
        {
            ShowMenu = !ShowMenu;
        }

        /// <summary>
        /// The menu game object.
        /// </summary>
        protected GameObject Menu { get; private set; }
        /// <summary>
        /// The menu manager.
        /// </summary>
        protected ModalWindowManager MenuManager { get; private set; }
        /// <summary>
        /// The menu tooltip.
        /// </summary>
        protected Tooltip.Tooltip MenuTooltip { get; private set; }
    
        /// <summary>
        /// Updates the keyword listener.
        /// </summary>
        protected virtual void UpdateKeywordListener()
        {
            // stops if already listening
            if (KeywordListener != null)
            {
                KeywordListener.Unregister(HandleKeyword);
                KeywordListener.Dispose();
                KeywordListener = null;
            }
            if (!ShowMenu) return;
            // starts listening
            KeywordListener = new KeywordInput(GetKeywords().ToArray());
            KeywordListener.Register(HandleKeyword);
            KeywordListener.Start();
        }
    
        /// <summary>
        /// The keywords the menu should listen to.
        /// </summary>
        /// <returns>titles the menu should listen to</returns>
        protected virtual IEnumerable<string> GetKeywords()
        {
            return Enumerable.Empty<string>().Append(CloseMenuKeyword);
        }
    
        /// <summary>
        /// Triggers when a keyword was recognized.
        /// Executes a special action depending on the keyword.
        /// </summary>
        /// <param name="args">The phrase recognized.</param>
        /// <see cref="GetKeywords"/>
        /// <see cref="CloseMenuKeyword"/>
        protected virtual void HandleKeyword(PhraseRecognizedEventArgs args)
        {
            if (args.text == CloseMenuKeyword)
            {
                ShowMenu = false;
            }
            OnKeywordRecognized?.Invoke(args.text);
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
            }
            else
            {
                MenuManager.CloseWindow();
                MenuTooltip.Hide();
            }
        }

        /// <summary>
        /// Updates the menu layout.
        /// </summary>
        protected virtual void UpdateLayout()
        {
            MenuManager.UpdateUI();
            LayoutRebuilder.ForceRebuildLayoutImmediate(Menu.transform as RectTransform);
        }

        /// <summary>
        /// Initializes the menu.
        /// </summary>
        protected override void StartDesktop()
        {
            // instantiates the menu
            Menu = PrefabInstantiator.InstantiatePrefab(MenuPrefab, Parent, false);
            Menu.name = Title;
            MenuManager = Menu.GetComponent<ModalWindowManager>();
            
            // sets the icon
            Icon = Resources.Load<Sprite>(IconSprite);
        
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
        /// Updates the menu and adds listeners to events.
        /// </summary>
        protected override void OnStartFinished()
        {
            base.OnStartFinished();
            // updates the menu
            UpdateKeywordListener();
            UpdateTitle();
            UpdateDescription();
            UpdateIcon();
            UpdateShowMenu();
            // adds listeners for updating the menu
            OnTitleChanged += UpdateTitle;
            OnDescriptionChanged += UpdateDescription;
            OnIconChanged += UpdateIcon;
            OnShowMenuChanged += UpdateShowMenu;
            OnShowMenuChanged += UpdateKeywordListener;
            OnCloseMenuCommandChanged += UpdateKeywordListener; ;
        }

        /// <summary>
        /// Updates the component for the current platform.
        /// Destroys the component if the corresponding menu has been destroyed or was not properly initialized.
        /// </summary>
        protected override void Update()

        {
            // destroys the component without a menu
            if (Menu == null)
            {
                Destroy(this);
                return;
            }
            base.Update();
        }

        /// <summary>
        /// Destroying the component also destroys the menu.
        /// </summary>
        private void OnDestroy()
        {
            if (Menu != null) Destroy(Menu);
        }
    
        /// <summary>
        /// Triggers when <see cref="Title"/> was changed.
        /// </summary>
        public event UnityAction OnTitleChanged;

        /// <summary>
        /// Triggers when <see cref="Description"/> was changed.
        /// </summary>
        public event UnityAction OnDescriptionChanged;
        
        /// <summary>
        /// Triggers when <see cref="Icon"/> was changed.
        /// </summary>
        public event UnityAction OnIconChanged;

        /// <summary>
        /// Triggers when <see cref="ShowMenu"/> was changed.
        /// </summary>
        public event UnityAction OnShowMenuChanged;
    
        /// <summary>
        /// Triggers when <see cref="CloseMenuKeyword"/> was changed
        /// </summary>
        public event UnityAction OnCloseMenuCommandChanged;
    
        /// <summary>
        /// Triggers when <see cref="Parent"/> was changed.
        /// </summary>
        public event UnityAction OnParentChanged;
    
        /// <summary>
        /// Triggers when a keyword was recognized by the listener. (<see cref="HandleKeyword"/>)
        /// </summary>
        public event UnityAction<string> OnKeywordRecognized;
    }
}
