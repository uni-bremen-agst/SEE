using Cysharp.Threading.Tasks;
using Michsky.UI.ModernUIPack;
using SEE.Net.Util;
using SEE.UI.Window.VariablesWindow;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Window.SnapshotWindow
{
    public class SnapshotsWindow : BaseWindow
    {
        private const string snapshotWindowPrefab = "Prefabs/UI/Snapshots/SnapshotsWindow";

        private GameObject items;


        private ButtonManagerBasic RefreshButton;

        protected override void Start()
        {
            Title = "Snapshots";
            base.Start();
        }

        protected override void StartDesktop()
        {
            base.StartDesktop();

            Transform root = PrefabInstantiator.InstantiatePrefab(snapshotWindowPrefab, Window.transform.Find("Content"), false).transform;

            items = root.Find("Content/Items").gameObject;
            foreach (Transform child in items.transform)
            {
                Destroyer.Destroy(child.gameObject);
            }

            Rebuild();
        }

        private async UniTask Rebuild()
        {
            foreach (SnapshotWindowItem child in items.GetComponents<SnapshotWindowItem>())
            {
                Destroyer.Destroy(child);
            }

            foreach (ServerSnapshot snapshot in await BackendSyncUtil.LoadSnapshotsAsync())
            {
                SnapshotWindowItem windowItem = items.AddComponent<SnapshotWindowItem>();

                windowItem.CityName = snapshot.CityName;
                windowItem.CreationTime = snapshot.CreationTime;
            }
        }
        public override void RebuildLayout()
        {
            // throw new System.NotImplementedException();
        }

        public override WindowValues ToValueObject()
        {
            throw new System.NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            throw new System.NotImplementedException();
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            throw new System.NotImplementedException();
        }
    }
}
