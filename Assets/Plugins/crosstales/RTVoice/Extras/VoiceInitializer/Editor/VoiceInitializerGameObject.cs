#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class VoiceInitializerGameObject
   {
      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/VoiceInitializer", false, EditorHelper.GO_ID + 8)]
      private static void AddVoiceInitializer()
      {
         EditorHelper.InstantiatePrefab("VoiceInitializer", $"{EditorConfig.ASSET_PATH}Extras/VoiceInitializer/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)