#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class SequencerGameObject
   {
      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/Sequencer", false, EditorHelper.GO_ID + 4)]
      private static void AddSequencer()
      {
         PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath($"Assets{EditorConfig.ASSET_PATH}Extras/Sequencer/Resources/Prefabs/Sequencer.prefab", typeof(GameObject)));
         EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)