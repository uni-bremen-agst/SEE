using Leopotam.Ecs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Asset_Cleaner {
    class SceneToClose : IEcsAutoReset {
        public Scene Scene;
        public int SelectionId;
        public bool ForceClose;

        public void Reset() {
            ForceClose = default;
            Scene = default;
            SelectionId = default;
        }
    }

    class SysSceneCleanup : IEcsRunSystem, IEcsDestroySystem {
        EcsFilter<SceneToClose> ScenesToClose = default;

        public void Run() {
            if (ScenesToClose.IsEmpty()) return;

            var selectionId = Globals<PersistentUndoRedoState>.Value.Id;

            foreach (var i in ScenesToClose.Out(out var g1, out var entities)) {
                var s = g1[i].Scene;
                if (g1[i].SelectionId == selectionId && !g1[i].ForceClose) continue;
                if (Selection.activeGameObject && Selection.activeGameObject.scene == s) continue;
                if (s.isLoaded) EditorSceneManager.CloseScene(s, removeScene: true);
                entities[i].Destroy();
            }
        }

        // close scenes on window close
        public void Destroy() {
            foreach (var i in ScenesToClose.Out(out var g1, out _)) {
                var s = g1[i].Scene;
                if (s.isLoaded) EditorSceneManager.CloseScene(s, removeScene: true);
            }
        }
    }
}