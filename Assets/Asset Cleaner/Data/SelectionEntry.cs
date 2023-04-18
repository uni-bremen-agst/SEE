using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Asset_Cleaner {
    [Serializable]
    class SelectionEntry {
        public bool IsGuids;
        public string[] Guids;

        public Object[] SceneObjects;

        public bool Valid() {
            if (IsGuids) {
                foreach (var guid in Guids) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path))
                        return true;
                }

                return false;
            }

            foreach (var sceneObject in SceneObjects)
                if (sceneObject)
                    return true;

            return false;
        }
    }
}