#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class LoudspeakerMenu
   {
      [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Prefabs/Loudspeaker", false, EditorHelper.MENU_ID + 120)]
      private static void AddLoudspeaker()
      {
         EditorHelper.InstantiatePrefab("Loudspeaker", $"{EditorConfig.ASSET_PATH}Extras/Loudspeaker/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)