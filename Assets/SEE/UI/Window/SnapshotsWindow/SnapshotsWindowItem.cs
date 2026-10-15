using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Michsky.UI.ModernUIPack;
using SEE.Extensions;
using SEE.Net.Util;
using SEE.UI.Notification;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Window.SnapshotsWindow
{
    /// <summary>
    /// Represents a snapshot item, which will be displayed in a list in the snapshot window.
    /// </summary>
    public class SnapshotsWindowItem : PlatformDependentComponent
    {
        /// <summary>
        /// The project path to the prefab.
        /// </summary>
        private const string snapshotWindowItemPrefab = "Prefabs/UI/Snapshots/SnapshotWindowItem";

        /// <summary>
        /// The path in the prefab to the download button.
        /// </summary>
        private const string downloadButtonPath = "Foreground/DownloadButton";

        /// <summary>
        /// The path in the prefab to the text component.
        /// </summary>
        private const string textPath = "Foreground/Text";

        /// <summary>
        /// Tooltip content, when the user hovers over the item.
        /// </summary>
        private const string downloadButtonHoverTooltip = "Download Snapshot";

        /// <summary>
        /// The server snapshot that should displayed.
        /// </summary>
        public ServerSnapshot Snapshot;

        /// <summary>
        /// Button to download the snapshot.
        /// </summary>
        private ButtonManagerBasic downloadButton;

        /// <summary>
        /// Event which is called when a snapshot was downloaded.
        /// The path to the snapshot will be passed as an argument.
        /// </summary>
        public UnityEvent<string> SnapshotDownloaded = new();

        /// <summary>
        /// Returns the display name of the snapshot.
        /// Currently, this is a combination of the <see cref="Snapshot.CityName"/>
        /// and <see cref="Snapshot.CreationTime.CreationTime"/>.
        /// </summary>
        private string GetDisplayName => $"{Snapshot.CityName} at {Snapshot.CreationTime.ToUniversalTime()}";

        /// <summary>
        /// GameObject of this item.
        /// </summary>
        [ManagedUI]
        private GameObject item;

        /// <summary>
        /// Initializes the component in a desktop environment.
        /// </summary>
        protected override void StartDesktop()
        {
            if (Snapshot == null)
            {
                Debug.LogError("Snapshot must be set before initialization.");
                return;
            }

            item = PrefabInstantiator.InstantiatePrefab(snapshotWindowItemPrefab, transform, false);
            item.name = GetDisplayName;

            downloadButton = item.transform.Find(downloadButtonPath).gameObject.MustGetComponent<ButtonManagerBasic>();
            downloadButton.hoverEvent.AddListener(() => Tooltip.ActivateWith(downloadButtonHoverTooltip));
            downloadButton.clickEvent.AddListener(() => OnClickDownloadAsync().Forget());

            TextMeshProUGUI textMesh = item.transform.Find(textPath).gameObject.MustGetComponent<TextMeshProUGUI>();
            textMesh.text = GetDisplayName;
        }

        /// <summary>
        /// Is called when the download button is clicked.
        /// </summary>
        /// <returns>An empty task.</returns>
        private async UniTask OnClickDownloadAsync()
        {
            try
            {
                string downloadPath = await DownloadSnapshotAsync();
                SnapshotDownloaded.Invoke(downloadPath);
            }
            catch (Exception e)
            {
                ShowNotification.Error("Can't download snapshot", "An error occurred while downloading the snapshot.");
                Net.Util.Logger.LogException(e);
            }
        }

        /// <summary>
        /// Downloads the snapshot into a tmp file.
        /// </summary>
        /// <returns>The path to the downloaded snapshot zip file.</returns>
        /// <exception cref="IOException">Thrown in case the download fails.</exception>
        private async UniTask<string> DownloadSnapshotAsync()
        {
            string tmpTargetFile = Path.ChangeExtension(Path.GetTempFileName(), ".zip");
            bool success = await BackendSyncUtil.DownloadSnapshotAsync(Snapshot.Id, tmpTargetFile);
            if (!success)
            {
                if (File.Exists(tmpTargetFile))
                {
                    File.Delete(tmpTargetFile);
                }
                throw new IOException("Failed to download snapshot.");
            }
            return tmpTargetFile;
        }
    }
}
