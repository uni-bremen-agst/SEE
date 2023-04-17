using Leopotam.Ecs;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Asset_Cleaner {
    class RequestRepaintEvt { }

    class SysRepaintWindow : IEcsRunSystem, IEcsInitSystem {
        EcsFilter<RequestRepaintEvt> Repaint = null;

        public void Init() {
            var wd = Globals<WindowData>.Value;
            wd.SceneFoldout = new GUIContent(AssetPreview.GetMiniTypeThumbnail(typeof(SceneAsset)));
            wd.ExpandScenes = true;
            wd.ExpandFiles = true;
            wd.ScrollPos = Vector2.zero;
        }

        public void Run() {
            var wd = Globals<WindowData>.Value;

            if (Repaint.IsEmpty()) return;
            wd.Window.Repaint();
            InternalEditorUtility.RepaintAllViews();
            Repaint.AllDestroy();
        }
    }
}