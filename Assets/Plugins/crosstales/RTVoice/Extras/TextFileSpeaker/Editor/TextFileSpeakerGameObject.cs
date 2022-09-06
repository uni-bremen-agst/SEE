#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class TextFileSpeakerGameObject
   {
      [MenuItem("GameObject/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/TextFileSpeaker", false, Crosstales.RTVoice.EditorUtil.EditorHelper.GO_ID + 6)]
      private static void AddTextFileSpeaker()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("TextFileSpeaker", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/TextFileSpeaker/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)