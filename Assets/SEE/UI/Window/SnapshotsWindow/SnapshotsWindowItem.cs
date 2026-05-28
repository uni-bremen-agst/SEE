using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Net.Util;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Window.VariablesWindow
{
    /// <summary>
    /// Represents a snapshot item, which will be displayed in a list in the snapshot window.
    /// </summary>
    public class SnapshotWindowItem : PlatformDependentComponent
    {
        private const string snapshotWindowItemPrefab = "Prefabs/UI/Snapshots/SnapshotWindowItem";

        private const string downloadButtonPath = "Foreground/DownloadButton";

        private const string textPath = "Foreground/Text";

        private const string downloadButtonHoverTooltip = "Download Snapshot";

        /// <summary>
        /// The server snapshot that should displayed.
        /// </summary>
        public ServerSnapshot Snapshot;

        /// <summary>
        /// Button to download the snapshot.
        /// </summary>
        private ButtonManagerBasic DownloadButton;

        public UnityEvent<string> SnapshotDownloaded = new();

        /// <summary>
        /// Returns the display name of the snapshot.
        /// Currently, this is a combination of the <see cref="CityName"/> and <see cref="CreationTime"/>
        /// </summary>
        private string GetDisplayName => $"{Snapshot.CityName} at {Snapshot.CreationTime.ToUniversalTime()}";

        /// <summary>
        /// GameObject of this item.
        /// </summary>
        [ManagedUI]
        private GameObject item;

        protected override void StartDesktop()
        {
            item = PrefabInstantiator.InstantiatePrefab(snapshotWindowItemPrefab, transform, false);
            item.name = GetDisplayName;

            DownloadButton = item.transform.Find(downloadButtonPath).gameObject.MustGetComponent<ButtonManagerBasic>();

            DownloadButton.hoverEvent.AddListener(() => Tooltip.ActivateWith(downloadButtonHoverTooltip));
            DownloadButton.clickEvent.AddListener(() => OnClickDownload().Forget());

            TextMeshProUGUI textMesh = item.transform.Find(textPath).gameObject.MustGetComponent<TextMeshProUGUI>();
            textMesh.text = GetDisplayName;
        }

        private async UniTask OnClickDownload()
        {
            string downloadPath = await DownloadSnapshotAsync();
            SnapshotDownloaded.Invoke(downloadPath);
        }

        private async UniTask<string> DownloadSnapshotAsync()
        {
            string tmpTargetFile = Path.GetTempFileName();
            await BackendSyncUtil.DownloadSnapshotAsync(Snapshot.Id, tmpTargetFile);
            return tmpTargetFile;
        }
    }
}
