using UnityEditor;

namespace Asset_Cleaner {
    class ProcessAllAssets : AssetPostprocessor {
        static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            if (!AufCtx.InitStarted) return;
            if (!Globals<BacklinkStore>.Value.Initialized) return;

            var store = Globals<BacklinkStore>.Value;
            if (store == null) {
                return;
            }

            var length = movedAssets.Length;
            for (var i = 0; i < length; i++)
                store.Replace (movedFromAssetPaths[i], movedAssets[i]);

            foreach (var path in deletedAssets)
                store.Remove (path);

            foreach (var path in importedAssets)
                store.RebuildFor (path, true);

            var config = Globals<Config>.Value;
            if (config.UpdateUnusedAssetsOnDemand) {
                config.PendingUpdateUnusedAssets = true;
            }
            else {
                ForceUpdateUnusedAssets ();
            }
        }

        public static void ForceUpdateUnusedAssets () {
            Globals<Config>.Value.PendingUpdateUnusedAssets = false;
            var store = Globals<BacklinkStore>.Value;
            store.UpdateUnusedAssets ();
        }
    }
}