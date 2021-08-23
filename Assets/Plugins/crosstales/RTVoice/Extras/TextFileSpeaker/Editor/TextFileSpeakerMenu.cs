#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class TextFileSpeakerMenu
   {
      [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Prefabs/TextFileSpeaker", false, EditorHelper.MENU_ID + 100)]
      private static void AddTextFileSpeaker()
      {
         EditorHelper.InstantiatePrefab("TextFileSpeaker", $"{EditorConfig.ASSET_PATH}Extras/TextFileSpeaker/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)