#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class SpeechTextGameObject
   {
      [MenuItem("GameObject/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/SpeechText", false, Crosstales.RTVoice.EditorUtil.EditorHelper.GO_ID + 5)]
      private static void AddSpeechText()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("SpeechText", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/SpeechText/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)