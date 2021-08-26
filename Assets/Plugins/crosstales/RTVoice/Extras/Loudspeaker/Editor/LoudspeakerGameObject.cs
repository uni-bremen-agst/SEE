#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class LoudspeakerGameObject
   {
      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/Loudspeaker", false, EditorHelper.GO_ID + 7)]
      private static void AddLoudspeaker()
      {
         EditorHelper.InstantiatePrefab("Loudspeaker", $"{EditorConfig.ASSET_PATH}Extras/Loudspeaker/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)