#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class SequencerMenu
   {
      [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Prefabs/Sequencer", false, EditorHelper.MENU_ID + 80)]
      private static void AddSequencer()
      {
         EditorHelper.InstantiatePrefab("Sequencer", $"{EditorConfig.ASSET_PATH}Extras/Sequencer/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)