#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class AudioFileGeneratorGameObject
   {
      [MenuItem("GameObject/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/AudioFileGenerator", false, Crosstales.RTVoice.EditorUtil.EditorHelper.GO_ID + 2)]
      private static void AddAudioFileGenerator()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("AudioFileGenerator", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/AudioFileGenerator/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)