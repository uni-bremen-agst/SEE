using UnityEngine;

namespace Asset_Cleaner {
    class WindowData {
        public bool ExpandFiles;
        public bool ExpandScenes;
        public Vector2 ScrollPos;
        public CleanerStyleAsset.Style Style;
        public GUIContent SceneFoldout;
        public PrevClick Click;
        public AufWindow Window;
        public FindModeEnum FindFrom;
    }
}