using Michsky.UI.ModernUIPack;
using SEE.Utils;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// Desktop implementation of SimpleListMenu.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public partial class SimpleListMenu<T> where T : MenuEntry
    {
        /// <summary>
        /// Prefab for the list containing the menu entries.
        /// Can be <code>null</code> if a game object can be found at <see cref="EntryListPath"/>.
        /// </summary>
        protected virtual string EntryListPrefab => UI_PREFAB_FOLDER + "MenuEntries";
        /// <summary>
        /// Prefab for each menu entry.
        /// Required components: ButtonManagerBasicWithIcon, PointerHelper and Image.
        /// </summary>
        protected virtual string EntryPrefab => UI_PREFAB_FOLDER + "Button";

        /// <summary>
        /// Path to the content game object.
        /// Contains the entry list.
        /// </summary>
        protected virtual string ContentPath => "Main Content/Content Mask/Content";
        /// <summary>
        /// Path to the entry list game object.
        /// Starts at the <see cref="Content"/> game object. (<seealso cref="ContentPath"/>)
        /// </summary>
        protected virtual string EntryListPath => "Menu Entries/Scroll Area/List";

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
        public GameObject EntryGameObject(T entry) => EntryList.transform.Find(entry.Title).gameObject;

        /// <summary>
        /// Initializes the menu.
        /// Creates the entry list if necessary.
        /// </summary>
        protected override void StartDesktop()
        {
            base.StartDesktop();
            // gets the content game object
            Content = Menu.transform.Find(ContentPath).gameObject;
            // whether the entry list is already contained in the menu
            if (!Content.transform.Find(EntryListPath))
            {
                // instantiates the entry list
                GameObject go = PrefabInstantiator.InstantiatePrefab(EntryListPrefab, Content.transform, false);
                // uses the start of EntryListPath as the name
                go.name = EntryListPath.Split('/')[0];
            }
            EntryList = Content.transform.Find(EntryListPath).gameObject;
        }

        /// <summary>
        /// Updates the menu and adds listeners for updating the menu.
        /// </summary>
        protected override void OnStartFinished()
        {
            base.OnStartFinished();
            // updates the menu
            UpdateCloseButton();
            Entries.ForEach(AddButton);
            // adds listeners for updating the menu
            OnAllowNoSelectionChanged += UpdateKeywordListener;
            OnEntryAdded += _ => UpdateKeywordListener();
            OnEntryRemoved += _ => UpdateKeywordListener();
            OnAllowNoSelectionChanged += UpdateCloseButton;
            OnEntryAdded += AddButton;
            OnEntryRemoved += DestroyButton;
            OnEntrySelected += _ =>
            {
                if (HideAfterSelection) ShowMenu = false;
            };
        }

        /// <summary>
        /// Instantiates the button for a menu entry.
        /// It is assumed that the menu contains the entry.
        /// </summary>
        /// <param name="entry">The added menu entry.</param>
        protected virtual void AddButton(T entry)
        {
            GameObject button =
                PrefabInstantiator.InstantiatePrefab(EntryPrefab, EntryList.transform, false);

            // title and icon
            button.name = entry.Title;
            ButtonManagerBasicWithIcon buttonManager = button.GetComponent<ButtonManagerBasicWithIcon>();
            buttonManager.buttonText = entry.Title;
            buttonManager.buttonIcon = entry.Icon;

            // hover listeners
            PointerHelper pointerHelper = button.GetComponent<PointerHelper>();
            pointerHelper.EnterEvent.AddListener(() => MenuTooltip.Show(entry.Description));
            pointerHelper.ExitEvent.AddListener(MenuTooltip.Hide);

            // adds clickEvent listener or show that button is disabled
            if (entry.Enabled)
                buttonManager.clickEvent.AddListener(() => SelectEntry(entry));
            else
                buttonManager.useRipple = false;

            // colors
            Color color = entry.Enabled ? entry.EntryColor : entry.DisabledColor;
            button.GetComponent<Image>().color = color;
            Color textColor = color.IdealTextColor();
            buttonManager.normalText.color = textColor;
            buttonManager.normalImage.color = textColor;
        }

        /// <summary>
        /// Destroys the button for a removed menu entry.
        /// It is assumed that the menu contained the entry.
        /// </summary>
        /// <param name="entry">The menu entry.</param>
        protected virtual void DestroyButton(T entry)
        {
            Destroyer.Destroy(EntryGameObject(entry));
        }

        /// <summary>
        /// Updates whether the close button is active.
        /// Only visible if <see cref="AllowNoSelection"/> is true.
        /// </summary>
        protected virtual void UpdateCloseButton()
        {
            if (MenuManager.confirmButton != null) MenuManager.confirmButton.gameObject.SetActive(AllowNoSelection);
        }
    }
}
