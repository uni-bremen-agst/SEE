using Cysharp.Threading.Tasks;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Net.Util;
using SEE.UI.Window.VariablesWindow;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Window.SnapshotWindow
{
    public class SnapshotsWindow : BaseWindow
    {
        private const string snapshotWindowPrefab = "Prefabs/UI/Snapshots/SnapshotsWindow";

        /// <summary>
        /// Tooltip content, when the user hovers over the refresh button (<see cref="RefreshButton"/>)
        /// </summary>
        private const string refreshButtonTooltipText = "Reload snapshots from server";
        private GameObject items;

        private ButtonManagerBasic RefreshButton;

        public UnityEvent<string> SnapshotDownloaded = new();

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

            RefreshButton = root.Find("Refresh").gameObject.MustGetComponent<ButtonManagerBasic>();

            RefreshButton.clickEvent.AddListener(() => Rebuild().Forget());
            foreach (Transform child in items.transform)
            {
                Destroyer.Destroy(child.gameObject);
            }

            RefreshButton.hoverEvent.AddListener(() => Tooltip.ActivateWith(refreshButtonTooltipText));

            Rebuild().Forget();
        }

        private async UniTask Rebuild()
        {
            Debug.Log("Loading snapshots from server");
            foreach (SnapshotWindowItem child in items.GetComponents<SnapshotWindowItem>())
            {
                Destroyer.Destroy(child);
            }

            foreach (ServerSnapshot snapshot in await BackendSyncUtil.LoadSnapshotsAsync())
            {
                SnapshotWindowItem windowItem = items.AddComponent<SnapshotWindowItem>();
                windowItem.Snapshot = snapshot;
                windowItem.SnapshotDownloaded.AddListener((path) =>
                {
                    SnapshotDownloaded.Invoke(path);
                });
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
