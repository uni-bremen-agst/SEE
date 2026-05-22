using System;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;

namespace SEE.UI.Window.VariablesWindow
{
    /// <summary>
    /// Represents a snapshot item, which will be displayed.
    /// </summary>
    public class SnapshotWindowItem : PlatformDependentComponent
    {
        private const string snapshotWindowItemPrefab = "Prefabs/UI/Snapshots/SnapshotWindowItem";

        /// <summary>
        /// Name
        /// </summary>
        public string CityName;

        public DateTime CreationTime;

        private string GetDisplayName => $"{CityName} at {CreationTime.ToUniversalTime()}";

        [ManagedUI]
        private GameObject item;

        protected override void StartDesktop()
        {

            item = PrefabInstantiator.InstantiatePrefab(snapshotWindowItemPrefab, transform, false);
            item.name = GetDisplayName;

            TextMeshProUGUI textMesh = item.transform.Find("Foreground/Text").gameObject.MustGetComponent<TextMeshProUGUI>();

            textMesh.text = GetDisplayName;
        }
    }
}
