#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class AudioFileGeneratorMenu
   {
      [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Prefabs/AudioFileGenerator", false, EditorHelper.MENU_ID + 60)]
      private static void AddAudioFileGenerator()
      {
         EditorHelper.InstantiatePrefab("AudioFileGenerator", $"{EditorConfig.ASSET_PATH}Extras/AudioFileGenerator/Resources/Prefabs/");
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)