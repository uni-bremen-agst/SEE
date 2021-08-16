#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class SpeechTextGameObject
   {
      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/SpeechText", false, EditorHelper.GO_ID + 5)]
      private static void AddSpeechText()
      {
         EditorHelper.InstantiatePrefab("SpeechText", $"{EditorConfig.ASSET_PATH}Extras/SpeechText/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)