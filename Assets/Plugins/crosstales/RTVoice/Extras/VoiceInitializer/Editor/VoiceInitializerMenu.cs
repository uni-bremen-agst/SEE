#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class VoiceInitializerMenu
   {
      [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Prefabs/VoiceInitializer", false, EditorHelper.MENU_ID + 140)]
      private static void AddVoiceInitializer()
      {
         EditorHelper.InstantiatePrefab("VoiceInitializer", $"{EditorConfig.ASSET_PATH}Extras/VoiceInitializer/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)