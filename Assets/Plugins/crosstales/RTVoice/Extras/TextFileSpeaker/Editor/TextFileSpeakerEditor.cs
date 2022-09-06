#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Crosstales.RTVoice.EditorExtension
{
   /// <summary>Custom editor for the 'TextFileSpeaker'-class.</summary>
   [CustomEditor(typeof(Crosstales.RTVoice.Tool.TextFileSpeaker))]
   [CanEditMultipleObjects]
   public class TextFileSpeakerEditor : Editor
   {
      #region Variables

      private Crosstales.RTVoice.Tool.TextFileSpeaker script;

      #endregion


      #region Editor methods

      private void OnEnable()
      {
         script = (Crosstales.RTVoice.Tool.TextFileSpeaker)target;
      }

      private void OnDisable()
      {
         if (Crosstales.RTVoice.Util.Helper.isEditorMode)
            Speaker.Instance.Silence();
      }

      public override void OnInspectorGUI()
      {
         DrawDefaultInspector();

         Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

         if (script.isActiveAndEnabled)
         {
            if (script.TextFiles?.Length > 0)
            {
               if (script.PlayOnStart && script.PlayAllOnStart)
               {
                  EditorGUILayout.HelpBox("Can't use 'Play On Start' and 'Play All On Start' in combination. Please decide for one approach!", MessageType.Warning);
               }
               else
               {
                  if (Speaker.Instance.isTTSAvailable && Crosstales.RTVoice.EditorUtil.EditorHelper.isRTVoiceInScene)
                  {
                     GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

                     if (Crosstales.RTVoice.Util.Helper.isEditorMode)
                     {
                        if (Speaker.Instance.isWorkingInEditor)
                        {
                           GUILayout.BeginHorizontal();
                           {
                              /*
                              if (GUILayout.Button(new GUIContent(" Previous", EditorHelper.Icon_Previous, "Plays the previous radio station.")))
                              {
                                  script.Previous();
                              }
                              */

                              if (Speaker.Instance.isSpeaking)
                              {
                                 if (GUILayout.Button(new GUIContent(" Silence", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Silence, "Silence the active speaker.")))
                                 {
                                    script.Silence();
                                 }
                              }
                              else
                              {
                                 if (GUILayout.Button(new GUIContent(" Speak", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Speak, "Speaks a random text file with the selected voice and settings.")))
                                 {
                                    script.Speak();
                                 }
                              }

                              /*
                              if (GUILayout.Button(new GUIContent(" Next", EditorHelper.Icon_Next, "Speaks the next text file.")))
                              {
                                  script.Next();
                              }
                              */
                           }
                           GUILayout.EndHorizontal();
                        }
                        else
                        {
                           EditorGUILayout.HelpBox("Test-Drive is not supported for current TTS-system inside the Unity Editor.", MessageType.Info);
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
// © 2016-2022 crosstales LLC (https://www.crosstales.com)