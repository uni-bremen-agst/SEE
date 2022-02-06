#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class PlatformProviderMenu
   {
      [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Prefabs/PlatformProvider", false, EditorHelper.MENU_ID + 160)]
      private static void AddPlatformProvider()
      {
         EditorHelper.InstantiatePrefab("PlatformProvider", $"{EditorConfig.ASSET_PATH}Extras/PlatformProvider/Resources/Prefabs/");
      }

      [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Prefabs/PlatformProvider", true)]
      private static bool AddPlatformProviderValidator()
      {
         return !EditorExtension.PlatformProviderEditor.isPrefabInScene;
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)