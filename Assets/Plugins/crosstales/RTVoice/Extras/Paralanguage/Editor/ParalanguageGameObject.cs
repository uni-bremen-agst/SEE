#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class ParalanguageGameObject
   {
      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/Paralanguage", false, EditorHelper.GO_ID + 3)]
      private static void AddParalanguage()
      {
         EditorHelper.InstantiatePrefab("Paralanguage", $"{EditorConfig.ASSET_PATH}Extras/Paralanguage/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)