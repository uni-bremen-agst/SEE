using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#pragma warning disable 649

namespace Asset_Cleaner {
    class CleanerStyleAsset : ScriptableObject {
        [Serializable]
        public class Style {
            public Color RedHighlight = new Color(1, 0, 0, 1f);

            public GUIContent Lock;
            public GUIStyle LockBtn = new GUIStyle();
            public GUIStyle SampleBtn = new GUIStyle();

            public GUIContent Unlock;
            public GUIStyle UnlockBtn = new GUIStyle();

            public GUIContent RemoveFile;
            public GUIContent RemoveScene;

            public GUIStyle RowMainAssetBtn = new GUIStyle();
            public GUIStyle RemoveUnusedBtn = new GUIStyle();

            public GUIStyle CurrentBtn = new GUIStyle();

            public GUIContent ArrowL;
            public GUIContent ArrowR;
            public GUIStyle ArrowBtn = new GUIStyle();

            public float SceneIndent1 = 20f;
            public float SceneIndent2 = 20f;
            public GUIStyle ProjectViewCounterLabel;

            public GUIContent MultiSelect;

            public static bool TryFindSelf(out Style value) {
                const string typeName = nameof(CleanerStyleAsset);

                var guids = AssetDatabase.FindAssets($"t:{typeName}");
                if (!guids.Any()) {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                }

                Asr.IsTrue(guids.Length > 0, $"No '{typeName}' assets found");
                var res = guids.Select(AssetDatabase.GUIDToAssetPath).Select(t => (CleanerStyleAsset) AssetDatabase.LoadAssetAtPath(t, typeof(CleanerStyleAsset))).FirstOrDefault();
                if (res == null) {
                    value = default;
                    return false;
                }

                value = EditorGUIUtility.isProSkin ? res.Pro : res.Personal;
                return value != null;
            }
        }
#pragma warning disable 0649
        public Style Pro;
        public Style Personal;
#pragma warning restore

        [CustomEditor(typeof(CleanerStyleAsset))]
        class Editor : UnityEditor.Editor {
            public override void OnInspectorGUI() {
#if false
                     if (GUILayout.Button("Update Btn backgrounds")) {
                    var targ = (CleanerStyleAsset) target; 
                    Set(targ.Pro);
                }
#endif
                EditorGUI.BeginChangeCheck();
                base.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }

#if false
            static void Set(Style style) {
                var st = style;
                var sample = st.SampleBtn;

                foreach (var btn in new[] {
                    st.LockBtn,
                    st.UnlockBtn,
                    st.RowMainAssetBtn,
                    st.RemoveUnusedBtn,
                    st.CurrentBtn,
                    st.ArrowBtn,
                }) {
                    btn.normal = sample.normal;
                    btn.hover = sample.hover;
                    btn.active = sample.active;
                    btn.focused = sample.focused;
                    btn.onNormal = sample.onNormal;
                    btn.onHover = sample.onHover;
                    btn.onActive = sample.onActive;
                    btn.onFocused = sample.onFocused;
                }
            }
#endif
        }
    }
}