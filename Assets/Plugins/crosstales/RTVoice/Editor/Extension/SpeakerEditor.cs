#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorExtension
{
   /// <summary>Custom editor for the 'Speaker'-class.</summary>
   [InitializeOnLoad]
   [CustomEditor(typeof(Speaker))]
   public class SpeakerEditor : Editor
   {
      #region Variables

      private string text = "Hello world!";
      private int voiceIndex;
      private float rate = 1f;
      private float pitch = 1f;
      private float volume = 1f;
      private Speaker script;

      private bool showVoices;

      private Object customProvider;
      private bool customMode;

      private bool eSpeakMode;
      private Model.Enum.ESpeakModifiers eSpeakModifier;
      private string eSpeakApp;
      private string eSpeakData;

      private string androidEngine;

      private bool autoClearTags;

      private bool caching;

      //private bool windowsLegacy;
      //private bool wsaNative;
      private bool silenceOnDisable;
      private bool silenceOnFocusLost;
      private bool handleFocus;
      private bool dontDestroy;

      #endregion


      #region Static constructor

      static SpeakerEditor()
      {
         EditorApplication.hierarchyWindowItemOnGUI += hierarchyItemCB;
      }

      #endregion


      #region Editor methods

      public void OnEnable()
      {
         script = (Speaker)target;
      }

      public void OnDisable()
      {
         if (Util.Helper.isEditorMode)
            script.Silence();
      }

      public override bool RequiresConstantRepaint()
      {
         return true;
      }

      public override void OnInspectorGUI()
      {
         EditorHelper.BannerOC();

         if (script.enforcedStandaloneTTS)
         {
            EditorGUILayout.HelpBox("Standalone TTS is used for development. The TTS on the current build target may have other voices and features.", MessageType.Warning);
         }

         if (script.Voices?.Count == 0)
         {
            if (script.isPlatformSupported && !script.isWorkingInPlaymode)
            {
               EditorGUILayout.HelpBox("The current TTS only works in builds!", MessageType.Error);
            }
            else if (!script.isPlatformSupported)
            {
               EditorGUILayout.HelpBox("The current platform is not supported by the active voice provider. Please use MaryTTS or a custom provider (e.g. Klattersynth).", MessageType.Error);
            }
            else
            {
               if (script.hasVoicesInEditor)
                  EditorGUILayout.HelpBox("TTS with the current settings is not possible!", MessageType.Error);
            }
         }

         if (Util.Helper.isIL2CPP && !script.isIL2CPPSupported)
         {
            GUILayout.Space(6);
            EditorGUILayout.HelpBox("IL2CPP is not supported by the current voice provider. Please use Mono, MaryTTS or a custom provider (e.g. Klattersynth).", MessageType.Error);
         }

         serializedObject.Update();

         GUILayout.Label("Custom Provider", EditorStyles.boldLabel);

         customMode = EditorGUILayout.BeginToggleGroup(new GUIContent("Active", "Enables or disables the custom provider (default: false)."), script.CustomMode);
         if (customMode != script.CustomMode)
         {
            serializedObject.FindProperty("customMode").boolValue = customMode;
            serializedObject.ApplyModifiedProperties();

            voiceIndex = 0;

            script.ReloadProvider();
         }

         EditorGUI.indentLevel++;

         customProvider = EditorGUILayout.ObjectField("Custom Provider", script.CustomProvider, typeof(Provider.BaseCustomVoiceProvider), true);
         if (customProvider != script.CustomProvider)
         {
            serializedObject.FindProperty("customProvider").objectReferenceValue = customProvider;
            serializedObject.ApplyModifiedProperties();

            voiceIndex = 0;

            script.ReloadProvider();
         }

         EditorGUI.indentLevel--;
         EditorGUILayout.EndToggleGroup();

         if (customMode)
         {
            if (script.CustomProvider == null)
            {
               EditorGUILayout.HelpBox("'Custom Provider' is null! Please add a valid provider.", MessageType.Warning);
            }
            else
            {
               if (!script.CustomProvider.isPlatformSupported)
               {
                  EditorGUILayout.HelpBox("'Custom Provider' does not support the current platform!", MessageType.Warning);
               }
            }
         }

         GUILayout.Space(8);
         GUILayout.Label("eSpeak Settings", EditorStyles.boldLabel);

         eSpeakMode = EditorGUILayout.BeginToggleGroup(new GUIContent("Active", "Enable or disable eSpeak for standalone platforms (default: false)."), script.ESpeakMode);
         if (eSpeakMode != script.ESpeakMode)
         {
            serializedObject.FindProperty("eSpeakMode").boolValue = eSpeakMode;
            serializedObject.ApplyModifiedProperties();

            voiceIndex = 0;

            script.ReloadProvider();
         }

         EditorGUI.indentLevel++;

         eSpeakApp = EditorGUILayout.TextField(new GUIContent("Application", "eSpeak application name/path (default: 'espeak')."), script.ESpeakApplication);
         if (!eSpeakApp.Equals(script.ESpeakApplication))
         {
            serializedObject.FindProperty("eSpeakApplication").stringValue = eSpeakApp;
            serializedObject.ApplyModifiedProperties();

            //script.ReloadProvider();
         }

         eSpeakData = EditorGUILayout.TextField(new GUIContent("Data Path", "eSpeak application data path (default: empty)."), script.ESpeakDataPath);
         if (!eSpeakData.Equals(script.ESpeakDataPath))
         {
            serializedObject.FindProperty("eSpeakDataPath").stringValue = eSpeakData;
            serializedObject.ApplyModifiedProperties();

            //script.ReloadProvider();
         }

         eSpeakModifier = (Model.Enum.ESpeakModifiers)EditorGUILayout.EnumPopup(new GUIContent("Modifier", "Active modifier for all eSpeak voices (default: none, m1-m6 = male, f1-f4 = female)."), script.ESpeakModifier);
         if (eSpeakModifier != script.ESpeakModifier)
         {
            serializedObject.FindProperty("eSpeakModifier").enumValueIndex = (int)eSpeakModifier;
            serializedObject.ApplyModifiedProperties();
         }

         EditorGUI.indentLevel--;
         EditorGUILayout.EndToggleGroup();

         if (eSpeakMode && !Provider.VoiceProviderLinux.isSupported)
         {
            EditorGUILayout.HelpBox("'eSpeak' is not supported on the current platform!", MessageType.Warning);
         }

         GUILayout.Space(8);
         GUILayout.Label("Android Settings", EditorStyles.boldLabel);
         androidEngine = EditorGUILayout.TextField(new GUIContent("Engine", "Active speech engine under Android (default: empty)."), script.AndroidEngine);
         if (!androidEngine.Equals(script.AndroidEngine))
         {
            serializedObject.FindProperty("androidEngine").stringValue = androidEngine;
            serializedObject.ApplyModifiedProperties();

            //script.ReloadProvider();
         }

         GUILayout.Space(8);
         GUILayout.Label("Advanced Settings", EditorStyles.boldLabel);

         autoClearTags = EditorGUILayout.Toggle(new GUIContent("Auto Clear Tags", "Automatically clear tags from speeches depending on the capabilities of the current TTS-system (default: false)."), script.AutoClearTags);
         if (autoClearTags != script.AutoClearTags)
         {
            serializedObject.FindProperty("autoClearTags").boolValue = autoClearTags;
            serializedObject.ApplyModifiedProperties();
         }

         caching = EditorGUILayout.Toggle(new GUIContent("Caching", "Enable or disable the caching of generated speeches (default: true)."), script.Caching);
         if (caching != script.Caching)
         {
            serializedObject.FindProperty("caching").boolValue = caching;
            serializedObject.ApplyModifiedProperties();
         }

         GUILayout.Space(8);
         GUILayout.Label("Behaviour Settings", EditorStyles.boldLabel);

         silenceOnDisable = EditorGUILayout.Toggle(new GUIContent("Silence On Disable", "Silence any speeches if this component gets disabled (default: false)."), script.SilenceOnDisable);
         if (silenceOnDisable != script.SilenceOnDisable)
         {
            serializedObject.FindProperty("silenceOnDisable").boolValue = silenceOnDisable;
            serializedObject.ApplyModifiedProperties();
         }

         silenceOnFocusLost = EditorGUILayout.Toggle(new GUIContent("Silence On Focus Lost", "Silence any speeches if the application loses the focus (default: true)."), script.SilenceOnFocusLost);
         if (silenceOnFocusLost != script.SilenceOnFocusLost)
         {
            serializedObject.FindProperty("silenceOnFocusLost").boolValue = silenceOnFocusLost;
            serializedObject.ApplyModifiedProperties();
         }

         handleFocus = EditorGUILayout.Toggle(new GUIContent("Handle Focus", "Starts and stops the Speaker depending on the focus and running state (default: true)."), script.HandleFocus);
         if (handleFocus != script.HandleFocus)
         {
            serializedObject.FindProperty("handleFocus").boolValue = handleFocus;
            serializedObject.ApplyModifiedProperties();
         }

         dontDestroy = EditorGUILayout.Toggle(new GUIContent("Dont Destroy", "Don't destroy gameobject during scene switches (default: true)."), script.DontDestroy);
         if (dontDestroy != script.DontDestroy)
         {
            serializedObject.FindProperty("dontDestroy").boolValue = dontDestroy;
            serializedObject.ApplyModifiedProperties();
         }

         SerializedProperty onReady = serializedObject.FindProperty("OnReady");
         EditorGUILayout.PropertyField(onReady);

         SerializedProperty onSpeakStarted = serializedObject.FindProperty("OnSpeakStarted");
         EditorGUILayout.PropertyField(onSpeakStarted);

         SerializedProperty onSpeakCompleted = serializedObject.FindProperty("OnSpeakCompleted");
         EditorGUILayout.PropertyField(onSpeakCompleted);

         SerializedProperty onProviderChanged = serializedObject.FindProperty("OnProviderChanged");
         EditorGUILayout.PropertyField(onProviderChanged);

         SerializedProperty onError = serializedObject.FindProperty("OnError");
         EditorGUILayout.PropertyField(onError);

         EditorHelper.SeparatorUI();

         if (script.isActiveAndEnabled)
         {
            GUILayout.Label("Data", EditorStyles.boldLabel);

            showVoices = EditorGUILayout.Foldout(showVoices, "Voices (" + script.Voices.Count + ")");
            if (showVoices)
            {
               EditorGUI.indentLevel++;

               foreach (string voice in script.Voices.CTToString())
               {
                  EditorGUILayout.SelectableLabel(voice, GUILayout.Height(16), GUILayout.ExpandHeight(false));
               }

               EditorGUI.indentLevel--;
            }

            if (GUILayout.Button(new GUIContent(" Reload", EditorHelper.Icon_Refresh, "Reload the provider.")))
            {
               script.ReloadProvider();
            }

            EditorHelper.SeparatorUI();

            GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

            if (script.Voices.Count > 0)
            {
               //EditorHelper.SeparatorUI();

               //GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

               if (Util.Helper.isEditorMode)
               {
                  if (script.isWorkingInEditor)
                  {
                     GUI.enabled = !script.isSpeaking;

                     text = EditorGUILayout.TextField("Text: ", text);
                     voiceIndex = EditorGUILayout.Popup("Voice", voiceIndex, script.Voices.CTToString().ToArray());
                     rate = EditorGUILayout.Slider("Rate", rate, 0f, 3f);

                     if (Util.Helper.isWindowsPlatform)
                     {
                        pitch = EditorGUILayout.Slider("Pitch", pitch, 0f, 2f);

                        volume = EditorGUILayout.Slider("Volume", volume, 0f, 1f);
                     }

                     GUI.enabled = true;

                     GUILayout.Space(8);

                     if (script.isSpeaking)
                     {
                        if (GUILayout.Button(new GUIContent(" Silence", EditorHelper.Icon_Silence, "Silence all active speakers.")))
                        {
                           script.Silence();
                        }
                     }
                     else
                     {
                        if (GUILayout.Button(new GUIContent(" Speak", EditorHelper.Icon_Speak, "Speaks the text with the selected voice and settings.")))
                        {
                           //script.SpeakNative("You have selected " + script.Voices[voiceIndex].Name, script.Voices[voiceIndex], rate, pitch, volume);
                           script.SpeakNative(text, script.Voices[voiceIndex], rate, pitch, volume);
                        }
                     }
                  }
                  else
                  {
                     EditorGUILayout.HelpBox("Test-Drive is not supported for the current TTS-system.", MessageType.Info);
                  }
               }
               else
               {
                  EditorGUILayout.HelpBox("Disabled in Play-mode!", MessageType.Info);
               }
            }
            else
            {
               if (Util.Helper.isEditorMode)
               {
                  if (!script.isWorkingInEditor)
                  {
                     EditorGUILayout.HelpBox("Test-Drive is not supported for the current TTS-system.", MessageType.Info);
                  }
               }
               else
               {
                  EditorGUILayout.HelpBox("Disabled in Play-mode!", MessageType.Info);
               }
            }

            /*
            else
            {
                if (script.isPlatformSupported && !script.isWorkingInPlaymode)
                {
                    EditorGUILayout.HelpBox("The current TTS only works in builds!", MessageType.Error);
                }
                else if (!script.isPlatformSupported)
                {
                    EditorGUILayout.HelpBox("The current platform is not supported by the active voice provider. Please use MaryTTS or a custom provider (e.g. Klattersynth).", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("TTS with the current settings is not possible!", MessageType.Error);
                }
            }
            */

            EditorHelper.SeparatorUI();

            GUILayout.Label("Information", EditorStyles.boldLabel);

            GUILayout.Label("Speech Count:\t" + script.SpeechCount);
            GUILayout.Label("Total Speeches:\t" + Util.Context.NumberOfSpeeches);
#if UNITY_2019_1_OR_NEWER
            GUILayout.Label("Total Files:\t" + Util.Context.NumberOfAudioFiles);
#else
            GUILayout.Label("Total Audio Files:\t" + Util.Context.NumberOfAudioFiles);
#endif

            /*
            if (script.Caching)
            {
               GUILayout.Space(12);
               GUILayout.Label($"Cached Speeches:\t{Util.Helper.FormatBytesToHRF(GlobalCache.Instance.CurrentClipCacheSize)}/{Util.Helper.FormatBytesToHRF(GlobalCache.Instance.ClipCacheSize)} ({GlobalCache.Instance.Clips.Count})");
               GUILayout.Label($"Cache Efficiency:\t{Util.Context.CacheEfficiency:P}");
            }
            */
         }
         else
         {
            EditorGUILayout.HelpBox("Script is disabled!", MessageType.Info);
         }

         serializedObject.ApplyModifiedProperties();
      }

      #endregion


      #region Private methods

      private static void hierarchyItemCB(int instanceID, Rect selectionRect)
      {
         if (EditorConfig.HIERARCHY_ICON)
         {
            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (go != null && go.GetComponent<Speaker>())
            {
               Rect r = new Rect(selectionRect);
               r.x = r.width - 4;

               //Debug.Log("HierarchyItemCB: " + r);

               GUI.Label(r, EditorHelper.Logo_Asset_Small);
            }
         }
      }

      #endregion
   }
}
#endif
// © 2016-2021 crosstales LLC (https://www.crosstales.com)