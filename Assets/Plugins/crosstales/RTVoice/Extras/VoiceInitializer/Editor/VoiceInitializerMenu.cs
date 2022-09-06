#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class VoiceInitializerMenu
   {
      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Prefabs/VoiceInitializer", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 140)]
      private static void AddVoiceInitializer()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("VoiceInitializer", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/VoiceInitializer/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)