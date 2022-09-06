#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class SequencerMenu
   {
      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Prefabs/Sequencer", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 80)]
      private static void AddSequencer()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("Sequencer", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/Sequencer/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)