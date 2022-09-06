#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class ParalanguageGameObject
   {
      [MenuItem("GameObject/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Paralanguage", false, Crosstales.RTVoice.EditorUtil.EditorHelper.GO_ID + 3)]
      private static void AddParalanguage()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("Paralanguage", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/Paralanguage/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)