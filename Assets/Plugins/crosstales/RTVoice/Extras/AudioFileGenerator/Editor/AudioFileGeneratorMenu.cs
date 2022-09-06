#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class AudioFileGeneratorMenu
   {
      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Prefabs/AudioFileGenerator", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 60)]
      private static void AddAudioFileGenerator()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("AudioFileGenerator", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/AudioFileGenerator/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)