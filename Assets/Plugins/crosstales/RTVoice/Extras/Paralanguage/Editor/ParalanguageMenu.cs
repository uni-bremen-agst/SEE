#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class ParalanguageMenu
   {
      [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Prefabs/Paralanguage", false, EditorHelper.MENU_ID + 70)]
      private static void AddParalanguage()
      {
         PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath($"Assets{EditorConfig.ASSET_PATH}Extras/Paralanguage/Resources/Prefabs/Paralanguage.prefab", typeof(GameObject)));
         EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)