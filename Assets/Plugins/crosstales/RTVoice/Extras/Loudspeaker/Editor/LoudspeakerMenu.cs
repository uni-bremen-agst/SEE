#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class LoudspeakerMenu
   {
      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Prefabs/Loudspeaker", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 120)]
      private static void AddLoudspeaker()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("Loudspeaker", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/Loudspeaker/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)