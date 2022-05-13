#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class PlatformProviderMenu
   {
      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Prefabs/PlatformProvider", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 160)]
      private static void AddPlatformProvider()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("PlatformProvider", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/PlatformProvider/Resources/Prefabs/");
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Prefabs/PlatformProvider", true)]
      private static bool AddPlatformProviderValidator()
      {
         return !Crosstales.RTVoice.EditorExtension.PlatformProviderEditor.isPrefabInScene;
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)