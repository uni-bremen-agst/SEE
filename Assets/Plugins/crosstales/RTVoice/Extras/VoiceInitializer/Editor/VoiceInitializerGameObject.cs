#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class VoiceInitializerGameObject
   {
      [MenuItem("GameObject/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/VoiceInitializer", false, Crosstales.RTVoice.EditorUtil.EditorHelper.GO_ID + 8)]
      private static void AddVoiceInitializer()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("VoiceInitializer", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/VoiceInitializer/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)