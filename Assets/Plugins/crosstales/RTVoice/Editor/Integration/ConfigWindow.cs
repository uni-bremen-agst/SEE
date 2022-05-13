#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

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

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Configuration...", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 1)]
      public static void ShowWindow()
      {
         GetWindow(typeof(ConfigWindow));
      }

      public static void ShowWindow(int tab)
      {
         ConfigWindow window = GetWindow(typeof(ConfigWindow)) as ConfigWindow;
         if (window != null) window.tab = tab;
      }

      private void OnEnable()
      {
         titleContent = new GUIContent(Crosstales.RTVoice.Util.Constants.ASSET_NAME_SHORT, Crosstales.RTVoice.EditorUtil.EditorHelper.Logo_Asset_Small);

         OnStopPlayback += silence;
      }

      private void OnDisable()
      {
         //Speaker.Instance.Silence();

         OnStopPlayback -= silence;
      }

      private void OnGUI()
      {
         tab = GUILayout.Toolbar(tab, new[] { "Config", "Prefabs", "TD", "Help", "About" });

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

               Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

               GUILayout.BeginHorizontal();
               {
                  if (GUILayout.Button(new GUIContent(" Save", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Save, "Saves the configuration settings for this project.")))
                  {
                     save();
                  }

                  if (GUILayout.Button(new GUIContent(" Reset", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Reset, "Resets the configuration settings for this project.")))
                  {
                     if (EditorUtility.DisplayDialog("Reset configuration?", "Reset the configuration of " + Util.Constants.ASSET_NAME + "?", "Yes", "No"))
                     {
                        Crosstales.RTVoice.Util.Config.Reset();
                        Crosstales.RTVoice.EditorUtil.EditorConfig.Reset();
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

      private void OnInspectorUpdate()
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
         //Crosstales.RTVoice.EditorUtil.EditorHelper.BannerOC();

         scrollPosPrefabs = EditorGUILayout.BeginScrollView(scrollPosPrefabs, false, false);
         {
            //GUILayout.Space(8);
            GUILayout.Label("Available Prefabs", EditorStyles.boldLabel);

            GUILayout.Space(6);
            //Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI (6);

            GUI.enabled = !Crosstales.RTVoice.EditorUtil.EditorHelper.isRTVoiceInScene;

            GUILayout.Label(Crosstales.RTVoice.Util.Constants.RTVOICE_SCENE_OBJECT_NAME);

            if (GUILayout.Button(new GUIContent(" Add", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Plus, "Adds the '" + Crosstales.RTVoice.Util.Constants.RTVOICE_SCENE_OBJECT_NAME + "'-prefab to the scene.")))
               Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab(Crosstales.RTVoice.Util.Constants.RTVOICE_SCENE_OBJECT_NAME);

            GUI.enabled = true;

            Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

            GUI.enabled = !Crosstales.RTVoice.EditorUtil.EditorHelper.isGlobalCacheInScene;

            GUILayout.Label(Crosstales.RTVoice.Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME);

            if (GUILayout.Button(new GUIContent(" Add", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Plus, "Adds a '" + Crosstales.RTVoice.Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME + "'-prefab to the scene.")))
               Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab(Crosstales.RTVoice.Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME);

            GUI.enabled = true;

            Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

            GUILayout.Label("AudioFileGenerator");

            if (GUILayout.Button(new GUIContent(" Add", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Plus, "Adds a 'AudioFileGenerator'-prefab to the scene.")))
               Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("AudioFileGenerator", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/AudioFileGenerator/Resources/Prefabs/");

            GUILayout.Space(6);

            GUILayout.Label("Paralanguage");

            if (GUILayout.Button(new GUIContent(" Add", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Plus, "Adds a 'Paralanguage'-prefab to the scene.")))
               Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("Paralanguage", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/Paralanguage/Resources/Prefabs/");

            GUILayout.Space(6);

            GUILayout.Label("Sequencer");

            if (GUILayout.Button(new GUIContent(" Add", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Plus, "Adds a 'Sequencer'-prefab to the scene.")))
               Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("Sequencer", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/Sequencer/Resources/Prefabs/");

            GUILayout.Space(6);

            GUILayout.Label("SpeechText");

            if (GUILayout.Button(new GUIContent(" Add", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Plus, "Adds a 'SpeechText'-prefab to the scene.")))
               Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("SpeechText", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/SpeechText/Resources/Prefabs/");

            GUILayout.Space(6);

            GUILayout.Label("TextFileSpeaker");

            if (GUILayout.Button(new GUIContent(" Add", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Plus, "Adds a 'TextFileSpeaker'-prefab to the scene.")))
               Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("TextFileSpeaker", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/TextFileSpeaker/Resources/Prefabs/");

            Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

            GUILayout.Label("Loudspeaker");

            if (GUILayout.Button(new GUIContent(" Add", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Plus, "Adds a 'Loudspeaker'-prefab to the scene.")))
               Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("Loudspeaker", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/Loudspeaker/Resources/Prefabs/");

            Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

            GUILayout.Label("VoiceInitializer");

            if (GUILayout.Button(new GUIContent(" Add", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Plus, "Adds a 'VoiceInitializer'-prefab to the scene.")))
               Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("VoiceInitializer", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/VoiceInitializer/Resources/Prefabs/");

            Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

            GUILayout.Label("PlatformProvider");

            if (GUILayout.Button(new GUIContent(" Add", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Plus, "Adds a 'PlatformProvider'-prefab to the scene.")))
               Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("PlatformProvider", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/PlatformProvider/Resources/Prefabs/");

            Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

            GUILayout.Label("MaryTTS");

            if (GUILayout.Button(new GUIContent(" Add", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Plus, "Adds a 'MaryTTS'-prefab to the scene.")))
               Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("MaryTTS", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/MaryTTS/Resources/Prefabs/");

            GUILayout.Space(6);

            GUILayout.Label("SAPI Unity");

            if (GUILayout.Button(new GUIContent(" Add", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Plus, "Adds a 'SAPI Unity'-prefab to the scene.")))
               Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab("SAPI Unity", $"{Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH}Extras/SAPI Unity/Resources/Prefabs/");

            GUILayout.Space(6);
         }
         EditorGUILayout.EndScrollView();
      }

      private void showTestDrive()
      {
         //Crosstales.RTVoice.EditorUtil.EditorHelper.BannerOC();

         GUILayout.Space(3);
         GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

         if (Crosstales.RTVoice.Util.Helper.isEditorMode)
         {
            if (Crosstales.RTVoice.EditorUtil.EditorHelper.isRTVoiceInScene)
            {
               if (Speaker.Instance != null && Speaker.Instance.isWorkingInEditor)
               {
                  if (Speaker.Instance.Voices.Count > 0 && Crosstales.RTVoice.EditorUtil.EditorHelper.isRTVoiceInScene)
                  {
                     scrollPosTD = EditorGUILayout.BeginScrollView(scrollPosTD, false, false);
                     {
                        if (Speaker.Instance.isWorkingInEditor)
                        {
                           GUI.enabled = !Speaker.Instance.isSpeaking;

                           text = EditorGUILayout.TextField("Text: ", text);
                           voiceIndex = EditorGUILayout.Popup("Voice", voiceIndex, Speaker.Instance.Voices.CTToString().ToArray());
                           rate = EditorGUILayout.Slider("Rate", rate, 0f, 3f);

                           if (Crosstales.RTVoice.Util.Helper.isWindowsPlatform)
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


                     Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

                     if (Speaker.Instance.isSpeaking)
                     {
                        if (GUILayout.Button(new GUIContent(" Silence", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Silence, "Silence all active speakers.")))
                        {
                           silence();
                        }
                     }
                     else
                     {
                        if (GUILayout.Button(new GUIContent(" Speak", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Speak, "Speaks the text with the selected voice and settings.")))
                        {
                           Speaker.Instance.SpeakNative(text, Speaker.Instance.Voices[voiceIndex], rate, pitch, volume);
                           silenced = false;
                        }
                     }

                     GUILayout.Space(6);
                  }
                  else
                  {
                     Crosstales.RTVoice.EditorUtil.EditorHelper.NoVoicesUI();
                  }
               }
               else
               {
                  EditorGUILayout.HelpBox("Test-Drive is not supported for the current TTS-system.", MessageType.Info);
               }
            }
            else
            {
               Crosstales.RTVoice.EditorUtil.EditorHelper.RTVUnavailable();
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
// © 2016-2022 crosstales LLC (https://www.crosstales.com)