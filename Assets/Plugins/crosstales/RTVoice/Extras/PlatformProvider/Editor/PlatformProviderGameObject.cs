#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class PlatformProviderGameObject
   {
      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/PlatformProvider", false, EditorHelper.GO_ID + 9)]
      private static void AddPlatformProvider()
      {
         EditorHelper.InstantiatePrefab("PlatformProvider", $"{EditorConfig.ASSET_PATH}Extras/PlatformProvider/Resources/Prefabs/");
      }

      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/PlatformProvider", true)]
      private static bool AddPlatformProviderValidator()
      {
         return !EditorExtension.PlatformProviderEditor.isPrefabInScene;
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)