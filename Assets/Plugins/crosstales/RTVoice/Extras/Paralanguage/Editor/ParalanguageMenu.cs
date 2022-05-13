#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class ParalanguageMenu
   {
      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Prefabs/Paralanguage", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 70)]
      private static void AddParalanguage()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("Paralanguage", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/Paralanguage/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)