#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class TextFileSpeakerMenu
   {
      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Prefabs/TextFileSpeaker", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 100)]
      private static void AddTextFileSpeaker()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("TextFileSpeaker", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/TextFileSpeaker/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)