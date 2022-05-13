#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class LoudspeakerGameObject
   {
      [MenuItem("GameObject/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Loudspeaker", false, Crosstales.RTVoice.EditorUtil.EditorHelper.GO_ID + 7)]
      private static void AddLoudspeaker()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("Loudspeaker", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/Loudspeaker/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)