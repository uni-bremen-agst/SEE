#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class SpeechTextMenu
   {
      [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Prefabs/SpeechText", false, EditorHelper.MENU_ID + 90)]
      private static void AddSpeechText()
      {
         EditorHelper.InstantiatePrefab("SpeechText", $"{EditorConfig.ASSET_PATH}Extras/SpeechText/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)