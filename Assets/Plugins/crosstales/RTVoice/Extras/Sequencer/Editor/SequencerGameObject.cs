#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class SequencerGameObject
   {
      [MenuItem("GameObject/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Sequencer", false, Crosstales.RTVoice.EditorUtil.EditorHelper.GO_ID + 4)]
      private static void AddSequencer()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("Sequencer", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/Sequencer/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)