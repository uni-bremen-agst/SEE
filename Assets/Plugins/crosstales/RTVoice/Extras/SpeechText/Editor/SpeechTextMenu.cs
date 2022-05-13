#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class SpeechTextMenu
   {
      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Prefabs/SpeechText", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 90)]
      private static void AddSpeechText()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("SpeechText", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/SpeechText/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)