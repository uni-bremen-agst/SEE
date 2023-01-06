using SEE.Utils;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Game.UI.Menu
{
    public class TabMenu : TabMenu<ToggleMenuEntry> {}

    public class TabMenu<T> : SelectionMenu<T> where T : ToggleMenuEntry
    {
        // TODO: The Prefabs don't exist yet
        protected virtual string ViewPrefab => UI_PREFAB_FOLDER + "View";
        protected override string MenuPrefab => UI_PREFAB_FOLDER + "TabMenu";
        protected override string EntryPrefab => UI_PREFAB_FOLDER + "TabButton";

        protected virtual string ViewListPrefab => UI_PREFAB_FOLDER + "TabList";
        
        protected virtual string ViewListPath => "ViewList/Content";

        protected GameObject ViewList { get; private set; }

        public virtual GameObject ViewGameObject(T entry)
        {
            return ViewList.transform.Find(entry.Title).gameObject;
        }
        
        protected override void StartDesktop()
        {
            base.StartDesktop();
            if (Content.transform.Find(ViewListPath) == null)
            {
                GameObject go = PrefabInstantiator.InstantiatePrefab(ViewListPrefab, Content.transform, false);
                go.name = ViewListPath.Split('/')[0];
            }
            ViewList = Content.transform.Find(ViewListPath).gameObject;
        }

        protected override void OnStartFinished()
        {
            base.OnStartFinished();
            if (ActiveEntry != null) ActivateView(ActiveEntry);
            Entries.ForEach(AddView);
            
            OnEntryAdded += AddView;
            OnEntrySelected += ActivateView;
            OnEntryUnselected += DeactivateView;
        }

        protected virtual void AddView(T entry)
        {
            GameObject view = PrefabInstantiator.InstantiatePrefab(ViewPrefab, ViewList.transform, false);
            view.name = entry.Title;
            view.SetActive(false);
        }

        protected virtual void RemoveView(T entry)
        {
            Destroy(ViewGameObject(entry));
        }

        protected virtual void ActivateView(T entry)
        {
            ViewGameObject(entry).SetActive(true);
        }

        protected virtual void DeactivateView(T entry)
        {
            ViewGameObject(entry).SetActive(false);
        }
    }
}
