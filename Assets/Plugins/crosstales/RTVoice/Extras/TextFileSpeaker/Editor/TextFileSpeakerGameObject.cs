#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class TextFileSpeakerGameObject
   {
      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/TextFileSpeaker", false, EditorHelper.GO_ID + 6)]
      private static void AddTextFileSpeaker()
      {
         PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath($"Assets{EditorConfig.ASSET_PATH}Extras/TextFileSpeaker/Resources/Prefabs/TextFileSpeaker.prefab", typeof(GameObject)));
         EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)