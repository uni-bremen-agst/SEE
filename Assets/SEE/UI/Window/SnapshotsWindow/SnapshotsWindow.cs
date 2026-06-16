using System;
using System.Linq;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using Michsky.UI.ModernUIPack;
using SEE.Game.City;
using SEE.GO;
using SEE.Net.Util;
using SEE.UI.Notification;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Window.SnapshotsWindow
{
    /// <summary>
    /// This window will display a list of all available snapshots of the server.
    /// </summary>
    public class SnapshotsWindow : BaseWindow
    {
        /// <summary>
        /// Project path of the prefab.
        /// </summary>
        private const string snapshotWindowPrefab = "Prefabs/UI/Snapshots/SnapshotsWindow";

        /// <summary>
        /// Tooltip content, when the user hovers over the refresh button (<see cref="refreshButton"/>)
        /// </summary>
        private const string refreshButtonTooltipText = "Reload snapshots from server";

        /// <summary>
        /// The path in the prefab to the list of snapshots.
        /// </summary>
        private const string snapshotListPath = "Content/Items";

        /// <summary>
        /// The path in the prefab to the refresh button.
        /// </summary>
        private const string refreshButtonPath = "Refresh";

        /// <summary>
        /// The list view in which all snapshot entries will be shown.
        /// </summary>
        private GameObject items;

        /// <summary>
        /// The refresh button, to reload the snapshots from the server.
        /// </summary>
        private ButtonManagerBasic refreshButton;

        /// <summary>
        /// Is called when the component is mounted and initializes it.
        /// </summary>
        protected override void Start()
        {
            Title = "Snapshots";
            base.Start();
        }

        /// <summary>
        /// Initializes the component in a desktop environment.
        /// </summary>
        protected override void StartDesktop()
        {
            base.StartDesktop();

            Transform root = PrefabInstantiator.InstantiatePrefab(snapshotWindowPrefab, Window.transform.Find("Content"), false).transform;
            items = root.Find(snapshotListPath).gameObject;
            refreshButton = root.Find(refreshButtonPath).gameObject.MustGetComponent<ButtonManagerBasic>();
            refreshButton.clickEvent.AddListener(() => RebuildAsync().Forget());
            foreach (Transform child in items.transform)
            {
                Destroyer.Destroy(child.gameObject);
            }

            refreshButton.hoverEvent.AddListener(() => Tooltip.ActivateWith(refreshButtonTooltipText));

            RebuildAsync().Forget();
        }

        /// <summary>
        /// Rebuilds the item list of snapshots.
        /// </summary>
        /// <returns>An empty task.</returns>
        private async UniTask RebuildAsync()
        {
            Debug.Log("Loading snapshots from server");
            foreach (SnapshotsWindowItem child in items.GetComponents<SnapshotsWindowItem>())
            {
                Destroyer.Destroy(child);
            }

            try
            {
                foreach (ServerSnapshot snapshot in await BackendSyncUtil.LoadSnapshotsAsync())
                {
                    SnapshotsWindowItem windowItem = items.AddComponent<SnapshotsWindowItem>();
                    windowItem.Snapshot = snapshot;
                    windowItem.SnapshotDownloaded.AddListener((path) =>
                    {
                        SEECity city = FindObjectsByType<SEECity>(FindObjectsSortMode.None).FirstOrDefault(x => x.gameObject.name == snapshot.CityName);
                        if (city == null)
                        {
                            Debug.LogError($"City with name: {snapshot.CityName} can not be found");
                            return;
                        }
                        city.LoadServerSnapshotAsync(path).Forget();
                    });
                }
            }
            catch (HttpRequestException e)
            {
                ShowNotification.Error("Error fetching snapshots", $"Snapshots can't be fetched from the server: {e.Message}.");
                Net.Util.Logger.LogException(e);
            }
            catch (TimeoutException e)
            {
                ShowNotification.Error("Error fetching snapshots", "Snapshots can't be fetched from the server due to a timeout.");
                Net.Util.Logger.LogException(e);
            }
            catch (OperationCanceledException)
            {
                // Nothing to be done.
            }
            catch (InvalidOperationException e)
            {
                Net.Util.Logger.LogException(e);
                throw;
            }
        }

        /// <summary>
        /// Will be called when the window layout changes.
        /// </summary>
        public override void RebuildLayout()
        {
            // Intentionally left empty - nothing to do, when the window is resized.
        }

        /// <summary>
        /// Converts the window data into <see cref="WindowValues"/>
        /// </summary>
        /// <returns>The resulting <see cref="WindowValues"/> object.</returns>
        public override WindowValues ToValueObject()
        {
            return new WindowValues(Title, gameObject.name);
        }

        /// <summary>
        /// Updates the current window with the data from a <see cref="WindowValues"/> network object.
        /// </summary>
        /// <param name="valueObject">The <see cref="WindowValues"/> to update.</param>
        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            Title = valueObject.Title;
        }

        /// <summary>
        /// Initializes the window from a <see cref="WindowValues"/> object.
        /// </summary>
        /// <param name="valueObject">The <see cref="WindowValues"/> to initialize from.</param>
        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            Title = valueObject.Title;
        }
    }
}
