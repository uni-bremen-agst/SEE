using System.Linq;
using Michsky.UI.ModernUIPack;
using MoreLinq;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Menu
{
    /// <summary>
    /// Desktop implementation of <see cref="SimpleListMenu"/>.
    /// </summary>
    /// <typeparam name="T">the type of <see cref="MenuEntry"/></typeparam>
    public partial class SimpleListMenu<T> where T : MenuEntry
    {
        /// <summary>
        /// Prefab for the list containing the menu entries.
        /// Can be null if a game object can be found at <see cref="EntryListPath"/>.
        /// </summary>
        protected virtual string EntryListPrefab => $"{UIPrefabFolder}MenuEntries";

        /// <summary>
        /// Prefab for each menu entry.
        /// Required components: ButtonManagerBasic, PointerHelper and Image.
        /// </summary>
        protected virtual string EntryPrefab => $"{UIPrefabFolder}Button";

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
        public GameObject Content { get; private set; }

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
        protected GameObject EntryGameObject(T entry) => EntryList.transform.Cast<Transform>().FirstOrDefault(x => x.name == entry.Title)?.gameObject;

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
                if (HideAfterSelection)
                {
                    ShowMenu = false;
                }
            };
        }

        /// <summary>
        /// Instantiates the button for a menu entry.
        /// It is assumed that the menu contains the entry.
        /// </summary>
        /// <param name="entry">The added menu entry.</param>
        protected virtual void AddButton(T entry)
        {
            GameObject button = PrefabInstantiator.InstantiatePrefab(EntryPrefab, EntryList.transform, false);

            // title and icon
            button.name = entry.Title;
            ButtonManagerBasic buttonManager = button.MustGetComponent<ButtonManagerBasic>();
            TextMeshProUGUI iconText = button.transform.Find("Icon").gameObject.MustGetComponent<TextMeshProUGUI>();
            buttonManager.buttonText = entry.Title;
            iconText.text = entry.Icon.ToString();

            // hover listeners
            PointerHelper pointerHelper = button.MustGetComponent<PointerHelper>();
            if (entry.Description != null)
            {
                pointerHelper.EnterEvent.AddListener(_ => Tooltip.ActivateWith(entry.Description));
                pointerHelper.ExitEvent.AddListener(_ => Tooltip.Deactivate());
            }

            // adds clickEvent listener or show that button is disabled
            if (entry.Enabled)
            {
                buttonManager.clickEvent.AddListener(() => SelectEntry(entry));
            }
            else
            {
                buttonManager.useRipple = false;
            }

            // colors
            Color color = entry.Enabled ? entry.EntryColor : entry.DisabledColor;
            button.MustGetComponent<Image>().color = color;
            Color textColor = color.IdealTextColor();
            buttonManager.normalText.color = textColor;
            iconText.color = textColor;
        }

        /// <summary>
        /// Destroys the button for a removed menu entry.
        /// It is assumed that the menu contained the entry.
        /// </summary>
        /// <param name="entry">The menu entry.</param>
        protected virtual void DestroyButton(T entry)
        {
            if (entry == null)
            {
                return;
            }
            Destroyer.Destroy(EntryGameObject(entry));
        }

        /// <summary>
        /// Updates whether the close button is active.
        /// Only visible if <see cref="AllowNoSelection"/> is true.
        /// </summary>
        protected virtual void UpdateCloseButton()
        {
            if (MenuManager.confirmButton != null)
            {
                MenuManager.confirmButton.gameObject.SetActive(AllowNoSelection);
            }
        }
    }
}
