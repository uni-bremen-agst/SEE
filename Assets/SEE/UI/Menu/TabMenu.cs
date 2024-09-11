using SEE.Utils;
using Sirenix.Utilities;
using UnityEngine;

namespace SEE.UI.Menu
{
    /// <summary>
    /// A tab menu containing <see cref="MenuEntry"/> entries.
    /// </summary>
    public class TabMenu : TabMenu<MenuEntry> { }

    /// <summary>
    /// Extension of the <see cref="SelectionMenu"/> to open a view through a tab button.
    /// </summary>
    /// <typeparam name="T">Menu entry</typeparam>
    public class TabMenu<T> : SelectionMenu<T> where T : MenuEntry
    {
        /// <summary>
        /// Folder for tab menu prefabs.
        /// </summary>
        protected const string TabPrefabFolder = UIPrefabFolder + "TabMenu/";

        /// <summary>
        /// The prefab for the tab menu.
        /// </summary>
        protected override string MenuPrefab => TabPrefabFolder + "TabMenu";

        /// <summary>
        /// The button list is already part of the tab menu prefab.
        /// </summary>
        protected override string EntryListPrefab => null;

        /// <summary>
        /// The button prefab for each entry.
        /// </summary>
        protected override string EntryPrefab => TabPrefabFolder + "TabButton";

        /// <summary>
        /// The path to the content game object.
        /// </summary>
        protected override string ContentPath => "MainContent";

        /// <summary>
        /// The path to the game object containing the buttons.
        /// Starts at the content game object.
        /// </summary>
        protected override string EntryListPath => "TabList/Content";

        /// <summary>
        /// Where to find the game object containing the views.
        /// Starts at the content game object.
        /// </summary>
        protected virtual string ViewListPath => "ViewList";

        /// <summary>
        /// The prefab for the game object containing the views.
        /// Only necessary if no game object is found at <see cref="ViewListPath"/>.
        /// </summary>
        protected virtual string ViewListPrefab => null;

        /// <summary>
        /// The view prefab for each entry.
        /// </summary>
        protected virtual string ViewPrefab => TabPrefabFolder + "TabView";

        /// <summary>
        /// The game object containing the views.
        /// </summary>
        protected GameObject ViewList { get; private set; }

        /// <summary>
        /// Returns a view game object.
        /// Assumes that the entry is part of the menu.
        /// </summary>
        /// <param name="entry">The menu entry.</param>
        /// <returns>The view game object.</returns>
        public virtual GameObject ViewGameObject(T entry) => ViewList.transform.Find(entry.Title).gameObject;

        /// <summary>
        /// Initializes the menu and stores specific parts of the menu.
        /// Creates the view list if necessary.
        /// </summary>
        protected override void StartDesktop()
        {
            base.StartDesktop();
            HideAfterSelection = false;
            // Instantiates the view list if necessary
            if (Content.transform.Find(ViewListPath) == null)
            {
                GameObject go = PrefabInstantiator.InstantiatePrefab(ViewListPrefab, Content.transform, false);
                go.name = ViewListPath.Split('/')[0];
            }
            ViewList = Content.transform.Find(ViewListPath).gameObject;
        }

        /// <summary>
        /// Updates the menu and adds listeners.
        /// </summary>
        protected override void OnStartFinished()
        {
            base.OnStartFinished();
            if (ActiveEntry != null)
            {
                ActivateView(ActiveEntry);
            }
            Entries.ForEach(AddView);

            OnEntryAdded += AddView;
            OnEntrySelected += ActivateView;
            OnEntryUnselected += DeactivateView;
            OnEntryRemoved += RemoveView;
        }

        /// <summary>
        /// Creates the view for an added entry.
        /// </summary>
        /// <param name="entry">The menu entry.</param>
        protected virtual void AddView(T entry)
        {
            GameObject view = PrefabInstantiator.InstantiatePrefab(ViewPrefab, ViewList.transform, false);
            view.name = entry.Title;
            view.SetActive(false);
        }

        /// <summary>
        /// Removes the view game object for a removed menu entry.
        /// Assumes that the entry was contained in the menu.
        /// </summary>
        /// <param name="entry">The menu entry.</param>
        protected virtual void RemoveView(T entry)
        {
            Destroyer.Destroy(ViewGameObject(entry));
        }

        /// <summary>
        /// Activates a view.
        /// Assumes that the entry is contained in the menu.
        /// </summary>
        /// <param name="entry"></param>
        protected virtual void ActivateView(T entry)
        {
            ViewGameObject(entry).SetActive(true);
        }

        /// <summary>
        /// Deactivates a view.
        /// Assumes that the entry is contained in the menu.
        /// </summary>
        /// <param name="entry">The menu entry.</param>
        protected virtual void DeactivateView(T entry)
        {
            ViewGameObject(entry).SetActive(false);
        }
    }
}
