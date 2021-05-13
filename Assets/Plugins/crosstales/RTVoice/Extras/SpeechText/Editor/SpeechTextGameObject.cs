#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class SpeechTextGameObject
   {
      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/SpeechText", false, EditorHelper.GO_ID + 5)]
      private static void AddSpeechText()
      {
         PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath($"Assets{EditorConfig.ASSET_PATH}Extras/SpeechText/Resources/Prefabs/SpeechText.prefab", typeof(GameObject)));
         EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)