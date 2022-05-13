#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class PlatformProviderGameObject
   {
      [MenuItem("GameObject/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/PlatformProvider", false, Crosstales.RTVoice.EditorUtil.EditorHelper.GO_ID + 9)]
      private static void AddPlatformProvider()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("PlatformProvider", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/PlatformProvider/Resources/Prefabs/");
      }

      [MenuItem("GameObject/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/PlatformProvider", true)]
      private static bool AddPlatformProviderValidator()
      {
         return !Crosstales.RTVoice.EditorExtension.PlatformProviderEditor.isPrefabInScene;
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)