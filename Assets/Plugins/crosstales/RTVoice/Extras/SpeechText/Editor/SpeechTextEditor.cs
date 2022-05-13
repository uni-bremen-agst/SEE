#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Crosstales.RTVoice.EditorExtension
{
   /// <summary>Custom editor for the 'SpeechText'-class.</summary>
   [CustomEditor(typeof(Crosstales.RTVoice.Tool.SpeechText))]
   [CanEditMultipleObjects]
   public class SpeechTextEditor : Editor
   {
      #region Variables

      private Crosstales.RTVoice.Tool.SpeechText script;

      #endregion


      #region Editor methods

      private void OnEnable()
      {
         script = (Crosstales.RTVoice.Tool.SpeechText)target;
      }

      private void OnDisable()
      {
         if (Crosstales.RTVoice.Util.Helper.isEditorMode)
         {
            Speaker.Instance.Silence();
         }
      }

      public override void OnInspectorGUI()
      {
         DrawDefaultInspector();

         Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

         if (script.isActiveAndEnabled)
         {
            if (!string.IsNullOrEmpty(script.Text))
            {
               if (script.GenerateAudioFile && !string.IsNullOrEmpty(script.FileName) || !script.GenerateAudioFile)
               {
                  if (Speaker.Instance.isTTSAvailable && Crosstales.RTVoice.EditorUtil.EditorHelper.isRTVoiceInScene)
                  {
                     GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

                     if (Crosstales.RTVoice.Util.Helper.isEditorMode)
                     {
                        if (Speaker.Instance.isWorkingInEditor)
                        {
                           if (Speaker.Instance.isSpeaking)
                           {
                              if (GUILayout.Button(new GUIContent(" Silence", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Silence, "Silence the active speaker.")))
                              {
                                 script.Silence();
                              }
                           }
                           else
                           {
                              if (GUILayout.Button(new GUIContent(" Speak", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Speak, "Speaks the text with the selected voice and settings.")))
                              {
                                 script.Speak();
                              }
                           }
                        }
                        else
                        {
                           EditorGUILayout.HelpBox("Test-Drive is not supported for current TTS-system inside the Unity Editor.", MessageType.Info);
                        }

                        Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

                        GUILayout.Label("Editor", EditorStyles.boldLabel);

                        if (GUILayout.Button(new GUIContent(" Refresh AssetDatabase", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Refresh, "Refresh the AssetDatabase from the Editor.")))
                           Crosstales.RTVoice.EditorUtil.EditorHelper.RefreshAssetDatabase();
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
                  EditorGUILayout.HelpBox("'File Name' is null or empty! Please enter a valid name (incl. path).", MessageType.Warning);
               }
            }
            else
            {
               EditorGUILayout.HelpBox("Please enter a 'Text'!", MessageType.Warning);
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
// © 2016-2022 crosstales LLC (https://www.crosstales.com)