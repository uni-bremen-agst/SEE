#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class AudioFileGeneratorGameObject
   {
      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/AudioFileGenerator", false, EditorHelper.GO_ID + 2)]
      private static void AddAudioFileGenerator()
      {
         PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath($"Assets{EditorConfig.ASSET_PATH}Extras/AudioFileGenerator/Resources/Prefabs/AudioFileGenerator.prefab", typeof(GameObject)));
         EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)