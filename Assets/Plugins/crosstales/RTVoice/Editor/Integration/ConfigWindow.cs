#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor window extension.</summary>
   [InitializeOnLoad]
   public class ConfigWindow : ConfigBase
   {
      #region Variables

      private int tab;
      private int lastTab;
      private string text = "Test all your voices with different texts and settings.";
      private int voiceIndex;
      private float rate = 1f;
      private float pitch = 1f;
      private float volume = 1f;
      private bool silenced = true;

      private Vector2 scrollPosPrefabs;
      private Vector2 scrollPosTD;

      public delegate void StopPlayback();

      public static event StopPlayback OnStopPlayback;

      #endregion


      #region Static constructor

      static ConfigWindow()
      {
         EditorApplication.update += onEditorUpdate;
      }

      #endregion


      #region EditorWindow methods

      [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/Configuration...", false, EditorHelper.MENU_ID + 1)]
      public static void ShowWindow()
      {
         GetWindow(typeof(ConfigWindow));
      }

      public static void ShowWindow(int tab)
      {
         ConfigWindow window = GetWindow(typeof(ConfigWindow)) as ConfigWindow;
         if (window != null) window.tab = tab;
      }

      public void OnEnable()
      {
         titleContent = new GUIContent(Util.Constants.ASSET_NAME_SHORT, EditorHelper.Logo_Asset_Small);

         OnStopPlayback += silence;
      }

      public void OnDisable()
      {
         //Speaker.Instance.Silence();

         OnStopPlayback -= silence;
      }

      public void OnGUI()
      {
         tab = GUILayout.Toolbar(tab, new[] {"Config", "Prefabs", "TD", "Help", "About"});

         if (tab != lastTab)
         {
            lastTab = tab;
            GUI.FocusControl(null);
         }

         switch (tab)
         {
            case 0:
            {
               showConfiguration();

               EditorHelper.SeparatorUI();

               GUILayout.BeginHorizontal();
               {
                  if (GUILayout.Button(new GUIContent(" Save", EditorHelper.Icon_Save, "Saves the configuration settings for this project.")))
                  {
                     save();
                  }

                  if (GUILayout.Button(new GUIContent(" Reset", EditorHelper.Icon_Reset, "Resets the configuration settings for this project.")))
                  {
                     if (EditorUtility.DisplayDialog("Reset configuration?", "Reset the configuration of " + Util.Constants.ASSET_NAME + "?", "Yes", "No"))
                     {
                        Util.Config.Reset();
                        EditorConfig.Reset();
                        save();
                     }
                  }
               }
               GUILayout.EndHorizontal();

               GUILayout.Space(6);
               break;
            }
            case 1:
               showPrefabs();
               break;
            case 2:
               showTestDrive();
               break;
            case 3:
               showHelp();
               break;
            default:
               showAbout();
               break;
         }
      }

      public void OnInspectorUpdate()
      {
         Repaint();
      }

      #endregion


      #region Private methods

      private static void onEditorUpdate()
      {
         if (EditorApplication.isCompiling || EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer)
         {
            onStopPlayback();
         }
      }

      private static void onStopPlayback()
      {
         OnStopPlayback?.Invoke();
      }

      private void silence()
      {
         if (!silenced)
         {
            Speaker.Instance.Silence();
            silenced = true;
         }
      }

      private void showPrefabs()
      {
         EditorHelper.BannerOC();

         scrollPosPrefabs = EditorGUILayout.BeginScrollView(scrollPosPrefabs, false, false);
         {
            //GUILayout.Space(8);
            GUILayout.Label("Available Prefabs", EditorStyles.boldLabel);

            GUILayout.Space(6);
            //EditorHelper.SeparatorUI (6);

            GUI.enabled = !EditorHelper.isRTVoiceInScene;

            GUILayout.Label(Util.Constants.RTVOICE_SCENE_OBJECT_NAME);

            if (GUILayout.Button(new GUIContent(" Add", EditorHelper.Icon_Plus, "Adds the '" + Util.Constants.RTVOICE_SCENE_OBJECT_NAME + "'-prefab to the scene.")))
               EditorHelper.InstantiatePrefab(Util.Constants.RTVOICE_SCENE_OBJECT_NAME);

            GUI.enabled = true;

            EditorHelper.SeparatorUI();

            GUI.enabled = !EditorHelper.isGlobalCacheInScene;

            GUILayout.Label(Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME);

            if (GUILayout.Button(new GUIContent(" Add", EditorHelper.Icon_Plus, "Adds a '" + Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME + "'-prefab to the scene.")))
               EditorHelper.InstantiatePrefab(Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME);

            GUI.enabled = true;

            EditorHelper.SeparatorUI();

            GUILayout.Label("AudioFileGenerator");

            if (GUILayout.Button(new GUIContent(" Add", EditorHelper.Icon_Plus, "Adds a 'AudioFileGenerator'-prefab to the scene.")))
               EditorHelper.InstantiatePrefab("AudioFileGenerator", $"{EditorConfig.ASSET_PATH}Extras/AudioFileGenerator/Resources/Prefabs/");

            GUILayout.Space(6);

            GUILayout.Label("Paralanguage");

            if (GUILayout.Button(new GUIContent(" Add", EditorHelper.Icon_Plus, "Adds a 'Paralanguage'-prefab to the scene.")))
               EditorHelper.InstantiatePrefab("Paralanguage", $"{EditorConfig.ASSET_PATH}Extras/Paralanguage/Resources/Prefabs/");

            GUILayout.Space(6);

            GUILayout.Label("Sequencer");

            if (GUILayout.Button(new GUIContent(" Add", EditorHelper.Icon_Plus, "Adds a 'Sequencer'-prefab to the scene.")))
               EditorHelper.InstantiatePrefab("Sequencer", $"{EditorConfig.ASSET_PATH}Extras/Sequencer/Resources/Prefabs/");

            GUILayout.Space(6);

            GUILayout.Label("SpeechText");

            if (GUILayout.Button(new GUIContent(" Add", EditorHelper.Icon_Plus, "Adds a 'SpeechText'-prefab to the scene.")))
               EditorHelper.InstantiatePrefab("SpeechText", $"{EditorConfig.ASSET_PATH}Extras/SpeechText/Resources/Prefabs/");

            GUILayout.Space(6);

            GUILayout.Label("TextFileSpeaker");

            if (GUILayout.Button(new GUIContent(" Add", EditorHelper.Icon_Plus, "Adds a 'TextFileSpeaker'-prefab to the scene.")))
               EditorHelper.InstantiatePrefab("TextFileSpeaker", $"{EditorConfig.ASSET_PATH}Extras/TextFileSpeaker/Resources/Prefabs/");

            EditorHelper.SeparatorUI();

            GUILayout.Label("Loudspeaker");

            if (GUILayout.Button(new GUIContent(" Add", EditorHelper.Icon_Plus, "Adds a 'Loudspeaker'-prefab to the scene.")))
               EditorHelper.InstantiatePrefab("Loudspeaker", $"{EditorConfig.ASSET_PATH}Extras/Loudspeaker/Resources/Prefabs/");

            EditorHelper.SeparatorUI();

            GUILayout.Label("VoiceInitializer");

            if (GUILayout.Button(new GUIContent(" Add", EditorHelper.Icon_Plus, "Adds a 'VoiceInitializer'-prefab to the scene.")))
               EditorHelper.InstantiatePrefab("VoiceInitializer", $"{EditorConfig.ASSET_PATH}Extras/VoiceInitializer/Resources/Prefabs/");

            EditorHelper.SeparatorUI();

            GUILayout.Label("MaryTTS");

            if (GUILayout.Button(new GUIContent(" Add", EditorHelper.Icon_Plus, "Adds a 'MaryTTS'-prefab to the scene.")))
               EditorHelper.InstantiatePrefab("MaryTTS", $"{EditorConfig.ASSET_PATH}Extras/MaryTTS/Resources/Prefabs/");

            GUILayout.Space(6);

            GUILayout.Label("SAPI Unity");

            if (GUILayout.Button(new GUIContent(" Add", EditorHelper.Icon_Plus, "Adds a 'SAPI Unity'-prefab to the scene.")))
               EditorHelper.InstantiatePrefab("SAPI Unity", $"{EditorConfig.ASSET_PATH}Extras/SAPI Unity/Resources/Prefabs/");

            GUILayout.Space(6);
         }
         EditorGUILayout.EndScrollView();
      }

      private void showTestDrive()
      {
         EditorHelper.BannerOC();

         GUILayout.Space(3);
         GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

         if (Util.Helper.isEditorMode)
         {
            if (EditorHelper.isRTVoiceInScene)
            {
               if (Speaker.Instance != null && Speaker.Instance.isWorkingInEditor)
               {
                  if (Speaker.Instance.Voices.Count > 0 && EditorHelper.isRTVoiceInScene)
                  {
                     scrollPosTD = EditorGUILayout.BeginScrollView(scrollPosTD, false, false);
                     {
                        if (Speaker.Instance.isWorkingInEditor)
                        {
                           GUI.enabled = !Speaker.Instance.isSpeaking;

                           text = EditorGUILayout.TextField("Text: ", text);
                           voiceIndex = EditorGUILayout.Popup("Voice", voiceIndex, Speaker.Instance.Voices.CTToString().ToArray());
                           rate = EditorGUILayout.Slider("Rate", rate, 0f, 3f);

                           if (Util.Helper.isWindowsPlatform)
                           {
                              pitch = EditorGUILayout.Slider("Pitch", pitch, 0f, 2f);

                              volume = EditorGUILayout.Slider("Volume", volume, 0f, 1f);
                           }

                           GUI.enabled = true;
                        }
                        else
                        {
                           EditorGUILayout.HelpBox("Test-Drive is not supported for the current TTS-system.", MessageType.Info);
                        }
                     }
                     EditorGUILayout.EndScrollView();


                     EditorHelper.SeparatorUI();

                     if (Speaker.Instance.isSpeaking)
                     {
                        if (GUILayout.Button(new GUIContent(" Silence", EditorHelper.Icon_Silence, "Silence all active speakers.")))
                        {
                           silence();
                        }
                     }
                     else
                     {
                        if (GUILayout.Button(new GUIContent(" Speak", EditorHelper.Icon_Speak, "Speaks the text with the selected voice and settings.")))
                        {
                           Speaker.Instance.SpeakNative(text, Speaker.Instance.Voices[voiceIndex], rate, pitch, volume);
                           silenced = false;
                        }
                     }

                     GUILayout.Space(6);
                  }
                  else
                  {
                     EditorHelper.NoVoicesUI();
                  }
               }
               else
               {
                  EditorGUILayout.HelpBox("Test-Drive is not supported for the current TTS-system.", MessageType.Info);
               }
            }
            else
            {
               EditorHelper.RTVUnavailable();
            }
         }
         else
         {
            EditorGUILayout.HelpBox("Disabled in Play-mode!", MessageType.Info);
         }
      }

      #endregion
   }
}
#endif
// © 2016-2021 crosstales LLC (https://www.crosstales.com)