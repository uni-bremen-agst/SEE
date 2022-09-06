#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Crosstales.RTVoice.EditorExtension
{
   /// <summary>Custom editor for the 'SpeechText'-class.</summary>
   [CustomEditor(typeof(Crosstales.RTVoice.Tool.AudioFileGenerator))]
   [CanEditMultipleObjects]
   public class AudioFileGeneratorEditor : Editor
   {
      #region Variables

      private Crosstales.RTVoice.Tool.AudioFileGenerator script;

      #endregion


      #region Editor methods

      private void OnEnable()
      {
         script = (Crosstales.RTVoice.Tool.AudioFileGenerator)target;
      }

      public override void OnInspectorGUI()
      {
         DrawDefaultInspector();

         Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

         if (script.isActiveAndEnabled)
         {
            if (script.TextFiles?.Length > 0)
            {
               if (Speaker.Instance.isTTSAvailable && Crosstales.RTVoice.EditorUtil.EditorHelper.isRTVoiceInScene)
               {
                  if (Crosstales.RTVoice.Util.Helper.isEditorMode)
                  {
                     GUILayout.Label("Generate Audio Files", EditorStyles.boldLabel);

                     GUILayout.BeginHorizontal();
                     {
                        if (Speaker.Instance.isWorkingInEditor)
                        {
                           if (GUILayout.Button(new GUIContent(" Generate", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Speak, "Generates the speeches from the text files.")))
                              script.Generate();
                        }
                        else
                        {
                           EditorGUILayout.HelpBox("Generate is not supported for current TTS-system inside the Unity Editor.", MessageType.Info);
                        }
                     }
                     GUILayout.EndHorizontal();

                     Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

                     GUILayout.Label("Editor", EditorStyles.boldLabel);

                     if (GUILayout.Button(new GUIContent(" Refresh AssetDatabase", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Refresh, "Refresh the AssetDatabase from the Editor.")))
                     {
                        Crosstales.RTVoice.EditorUtil.EditorHelper.RefreshAssetDatabase();
                     }
                  }
                  else
                  {
                     EditorGUILayout.HelpBox("Disabled in Play-mode!", MessageType.Info);
                  }
               }
               else
               {
                  Crosstales.RTVoice.EditorUtil.EditorHelper.NoVoicesUI();
               }
            }
            else
            {
               EditorGUILayout.HelpBox("Please add an entry to 'Text Files'!", MessageType.Warning);
            }
         }
         else
         {
            EditorGUILayout.HelpBox("Script is disabled!", MessageType.Info);
         }
      }

      #endregion
   }
}
#endif
// © 2017-2022 crosstales LLC (https://www.crosstales.com)