using Cysharp.Threading.Tasks;
using Michsky.UI.ModernUIPack;
using SEE.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Menu
{
    /// <summary>
    /// Desktop implementation of <see cref="SimpleMenu"/>.
    /// </summary>
    public partial class SimpleMenu<T>
    {
        /// <summary>
        /// Prefab for the menu.
        /// Requires a ModalWindowManager component.
        /// </summary>
        protected virtual string MenuPrefab => UIPrefabFolder + "Menu";

        /// <summary>
        /// Sprite for the icon.
        /// </summary>
        protected virtual string IconSprite => "Materials/ModernUIPack/Settings";

        /// <summary>
        /// The menu game object.
        /// </summary>
        protected GameObject Menu { get; private set; }

        /// <summary>
        /// The menu manager.
        /// </summary>
        protected ModalWindowManager MenuManager { get; private set; }

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
            OnCloseMenuCommandChanged += UpdateKeywordListener;
            MenuManager.confirmButton.onClick.AddListener(() => ShowMenu = false);
        }

        /// <summary>
        /// Updates the component for the current platform.
        /// Destroys this component if the corresponding menu has been destroyed or was not properly initialized.
        /// </summary>
        protected override void Update()
        {
            // destroys the component without a menu
            if (Menu == null)
            {
                Destroyer.Destroy(this);
                return;
            }
            base.Update();
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
                Menu.SetActive(true);
                Menu.transform.SetAsLastSibling();
                MenuManager.OpenWindow();
            }
            else
            {
                MenuManager.CloseWindow();
                Tooltip.Deactivate();

                // We cannot disable the menu immediately, otherwise the animation will be cut short.
                DisableSoon().Forget();

                async UniTaskVoid DisableSoon()
                {
                    await UniTask.Delay(1000);
                    if (Menu != null && !ShowMenu)
                    {
                        Menu.SetActive(false);
                    }
                }
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
    }
}
