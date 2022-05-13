#if UNITY_EDITOR
namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Base class for editor windows.</summary>
   public abstract class ConfigBase : UnityEditor.EditorWindow
   {
      #region Variables

      private static string updateText = Crosstales.RTVoice.EditorTask.UpdateCheck.TEXT_NOT_CHECKED;
      private static Crosstales.RTVoice.EditorTask.UpdateStatus updateStatus = Crosstales.RTVoice.EditorTask.UpdateStatus.NOT_CHECKED;

      private System.Threading.Thread worker;

      private UnityEngine.Vector2 scrollPosConfig;
      private UnityEngine.Vector2 scrollPosHelp;
      private UnityEngine.Vector2 scrollPosAboutUpdate;
      private UnityEngine.Vector2 scrollPosAboutReadme;
      private UnityEngine.Vector2 scrollPosAboutVersions;

      private static string readme;
      private static string versions;
      private static string ssml;
      private static string emotionml;

      private bool enforceStandaloneTTS;

      private int aboutTab;

      //private static readonly System.Random rnd = new System.Random();
      //private readonly int adRnd1 = rnd.Next(0, 8);

      private const int maxChars = 16000;

      #endregion


      #region Protected methods

      protected void showConfiguration()
      {
         //Crosstales.RTVoice.EditorUtil.EditorHelper.BannerOC();

         UnityEngine.GUI.skin.label.wordWrap = true;

         scrollPosConfig = UnityEditor.EditorGUILayout.BeginScrollView(scrollPosConfig, false, false);
         {
            UnityEngine.GUILayout.Label("General Settings", UnityEditor.EditorStyles.boldLabel);

            //Util.Config.AUDIOFILE_PATH = EditorGUILayout.TextField(new GUIContent("Audio Path", "Path to the generated audio files (default: '" + Util.Constants.DEFAULT_AUDIOFILE_PATH + "')."), Util.Config.AUDIOFILE_PATH);
            UnityEngine.GUILayout.Label("Audio Path", UnityEditor.EditorStyles.centeredGreyMiniLabel);
            //EditorGUI.indentLevel++;
            UnityEditor.EditorGUILayout.BeginHorizontal();
            {
               UnityEditor.EditorGUILayout.SelectableLabel(Crosstales.RTVoice.Util.Config.AUDIOFILE_PATH);
               //GUILayout.Label(Util.Config.AUDIOFILE_PATH);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" Select", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Folder, "Select path for the audio files")))
               {
                  string path = UnityEditor.EditorUtility.OpenFolderPanel("Select path for the audio files", Crosstales.RTVoice.Util.Config.AUDIOFILE_PATH, string.Empty);

                  if (!string.IsNullOrEmpty(path))
                  {
                     Crosstales.RTVoice.Util.Config.AUDIOFILE_PATH = path + "/";
                  }
               }
            }
            UnityEditor.EditorGUILayout.EndHorizontal();
            //EditorGUI.indentLevel--;

            Crosstales.RTVoice.Util.Config.AUDIOFILE_AUTOMATIC_DELETE = UnityEditor.EditorGUILayout.Toggle(new UnityEngine.GUIContent("Audio Auto-Delete", "Enable or disable auto-delete of the generated audio files (default: " + Crosstales.RTVoice.Util.Constants.DEFAULT_AUDIOFILE_AUTOMATIC_DELETE + ")."), Crosstales.RTVoice.Util.Config.AUDIOFILE_AUTOMATIC_DELETE);

            Crosstales.RTVoice.EditorUtil.EditorConfig.PREFAB_AUTOLOAD = UnityEditor.EditorGUILayout.Toggle(new UnityEngine.GUIContent("Prefab Auto-Load", "Enable or disable auto-loading of the prefabs to the scene (default: " + Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_PREFAB_AUTOLOAD + ")."), Crosstales.RTVoice.EditorUtil.EditorConfig.PREFAB_AUTOLOAD);

            Crosstales.RTVoice.Util.Config.DEBUG = UnityEditor.EditorGUILayout.Toggle(new UnityEngine.GUIContent("Debug", "Enable or disable debug logs (default: " + Crosstales.RTVoice.Util.Constants.DEFAULT_DEBUG + ")."), Crosstales.RTVoice.Util.Config.DEBUG);

            Crosstales.RTVoice.EditorUtil.EditorConfig.UPDATE_CHECK = UnityEditor.EditorGUILayout.Toggle(new UnityEngine.GUIContent("Update Check", "Enable or disable the update-checks for the asset (default: " + Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_UPDATE_CHECK + ")"), Crosstales.RTVoice.EditorUtil.EditorConfig.UPDATE_CHECK);

            Crosstales.RTVoice.EditorUtil.EditorConfig.COMPILE_DEFINES = UnityEditor.EditorGUILayout.Toggle(new UnityEngine.GUIContent("Compile Defines", "Enable or disable adding compile define 'CT_RTV' for the asset (default: " + Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_COMPILE_DEFINES + ")"), Crosstales.RTVoice.EditorUtil.EditorConfig.COMPILE_DEFINES);

            Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

            UnityEngine.GUILayout.Label("Speaker", UnityEditor.EditorStyles.boldLabel);
            Crosstales.RTVoice.EditorUtil.EditorConfig.HIERARCHY_ICON = UnityEditor.EditorGUILayout.Toggle(new UnityEngine.GUIContent("Show Hierarchy Icon", "Show hierarchy icon (default: " + Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_HIERARCHY_ICON + ")."), Crosstales.RTVoice.EditorUtil.EditorConfig.HIERARCHY_ICON);

            Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();
            UnityEngine.GUILayout.Label("Development Settings", UnityEditor.EditorStyles.boldLabel);
            enforceStandaloneTTS = UnityEditor.EditorGUILayout.Toggle(new UnityEngine.GUIContent("Enforce Standalone TTS", "Enforce standalone TTS for development (default: " + Crosstales.RTVoice.Util.Constants.DEFAULT_ENFORCE_STANDALONE_TTS + ")."), Crosstales.RTVoice.Util.Config.ENFORCE_STANDALONE_TTS);
            if (enforceStandaloneTTS != Crosstales.RTVoice.Util.Config.ENFORCE_STANDALONE_TTS)
            {
               Crosstales.RTVoice.Util.Config.ENFORCE_STANDALONE_TTS = enforceStandaloneTTS;
               Speaker.Instance.ReloadProvider();
            }

/*
            Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();
            GUILayout.Label("Windows Settings", EditorStyles.boldLabel);
            Util.Config.ENFORCE_32BIT_WINDOWS = EditorGUILayout.Toggle(new GUIContent("Enforce 32bit Voices", "Enforce 32bit versions of voices under Windows (default: " + Util.Constants.DEFAULT_ENFORCE_32BIT_WINDOWS + ")."), Util.Config.ENFORCE_32BIT_WINDOWS);
*/
         }
         UnityEditor.EditorGUILayout.EndScrollView();
      }


      protected void showHelp()
      {
         //Crosstales.RTVoice.EditorUtil.EditorHelper.BannerOC();

         scrollPosHelp = UnityEditor.EditorGUILayout.BeginScrollView(scrollPosHelp, false, false);
         {
            UnityEngine.GUILayout.Label("Resources", UnityEditor.EditorStyles.boldLabel);

            //GUILayout.Space(8);

            UnityEngine.GUILayout.BeginHorizontal();
            {
               UnityEngine.GUILayout.BeginVertical();
               {
                  if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" Manual", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Manual, "Show the manual.")))
                     Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_MANUAL_URL);

                  UnityEngine.GUILayout.Space(6);

                  if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" Forum", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Forum, "Visit the forum page.")))
                     Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_FORUM_URL);
               }
               UnityEngine.GUILayout.EndVertical();

               UnityEngine.GUILayout.BeginVertical();
               {
                  if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" API", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_API, "Show the API.")))
                     Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_API_URL);

                  UnityEngine.GUILayout.Space(6);

                  if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" Product", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Product, "Visit the product page.")))
                     Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_WEB_URL);
               }
               UnityEngine.GUILayout.EndVertical();
            }
            UnityEngine.GUILayout.EndHorizontal();

            Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

            UnityEngine.GUILayout.Label("Videos", UnityEditor.EditorStyles.boldLabel);

            UnityEngine.GUILayout.BeginHorizontal();
            {
               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" Promo", Crosstales.RTVoice.EditorUtil.EditorHelper.Video_Promo, "View the promotion video on 'Youtube'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_VIDEO_PROMO);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" Tutorial", Crosstales.RTVoice.EditorUtil.EditorHelper.Video_Tutorial, "View the tutorial video on 'Youtube'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_VIDEO_TUTORIAL);
            }
            UnityEngine.GUILayout.EndHorizontal();

            UnityEngine.GUILayout.Space(6);

            if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" All Videos", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Videos, "Visit our 'Youtube'-channel for more videos.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_SOCIAL_YOUTUBE);

            Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

            UnityEngine.GUILayout.Label("3rd Party Assets", UnityEditor.EditorStyles.boldLabel);

            UnityEngine.GUILayout.BeginHorizontal();
            {
               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_AdventureCreator, "More information about 'Adventure Creator'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_ADVENTURE_CREATOR);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_Amplitude, "More information about 'Amplitude'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_AMPLITUDE);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_CinemaDirector, "More information about 'Cinema Director'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_CINEMA_DIRECTOR);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_DialogueSystem, "More information about 'Dialogue System'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_DIALOGUE_SYSTEM);
            }
            UnityEngine.GUILayout.EndHorizontal();

            UnityEngine.GUILayout.BeginHorizontal();
            {
               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_Google, "More information about 'Google Cloud Text To Speech'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_GOOGLE);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_Klattersynth, "More information about 'Klattersynth'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_KLATTERSYNTH);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_LipSync, "More information about 'LipSync'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_LIPSYNC);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_LDC, "More information about 'Localized Dialogs'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_LOCALIZED_DIALOGS);
            }
            UnityEngine.GUILayout.EndHorizontal();

            UnityEngine.GUILayout.BeginHorizontal();
            {
               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_Naninovel, "More information about 'Naninovel'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_NANINOVEL);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_NPC_Chat, "More information about 'NPC Chat'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_NPC_CHAT);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Asset_PlayMaker, "More information about 'PlayMaker'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_PLAYMAKER);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_QuestSystem, "More information about 'Quest System'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_QUEST_SYSTEM);
            }
            UnityEngine.GUILayout.EndHorizontal();

            UnityEngine.GUILayout.BeginHorizontal();
            {
               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_SALSA, "More information about 'SALSA'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_SALSA);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_SLATE, "More information about 'SLATE'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_SLATE);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Asset_VolumetricAudio, "More information about 'Volumetric Audio'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_VOLUMETRIC_AUDIO);

               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Store_WebGL, "More information about 'WebGL Speech Synthesis'.")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_WEBGL);

               /*
               //CT Ads
               switch (adRnd1)
               {
                  case 0:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Logo_Asset_BWF, "More information about 'Bad Word Filter'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_BWF);

                     break;
                  }
                  case 1:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Logo_Asset_DJ, "More information about 'DJ'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_DJ);

                     break;
                  }
                  case 2:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Logo_Asset_FB, "More information about 'File Browser'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_FB);

                     break;
                  }
                  case 3:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Logo_Asset_Radio, "More information about 'Radio'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_RADIO);

                     break;
                  }
                  case 4:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Logo_Asset_TB, "More information about 'Turbo Backup'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_TB);

                     break;
                  }
                  case 5:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Logo_Asset_TPS, "More information about 'Turbo Switch'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_TPS);

                     break;
                  }
                  case 6:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Logo_Asset_TPB, "More information about 'Turbo Builder'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_TPB);

                     break;
                  }
                  case 7:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Logo_Asset_OC, "More information about 'Online Check'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_OC);


                     break;
                  }
                  default:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Logo_Asset_TR, "More information about 'True Random'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_TR);

                     break;
                  }
               }
               */
            }
            UnityEngine.GUILayout.EndHorizontal();

            UnityEngine.GUILayout.Space(6);

            if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" All Supported Assets", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_3p_Assets, "More information about the all supported assets.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_URL);
         }
         UnityEditor.EditorGUILayout.EndScrollView();

         UnityEngine.GUILayout.Space(6);
      }

      protected void showAbout()
      {
         //Crosstales.RTVoice.EditorUtil.EditorHelper.BannerOC();

         UnityEngine.GUILayout.Space(3);
         UnityEngine.GUILayout.Label(Crosstales.RTVoice.Util.Constants.ASSET_NAME, UnityEditor.EditorStyles.boldLabel);

         UnityEngine.GUILayout.BeginHorizontal();
         {
            UnityEngine.GUILayout.BeginVertical(UnityEngine.GUILayout.Width(60));
            {
               UnityEngine.GUILayout.Label("Version:");

               UnityEngine.GUILayout.Space(12);

               UnityEngine.GUILayout.Label("Web:");

               UnityEngine.GUILayout.Space(2);

               UnityEngine.GUILayout.Label("Email:");
            }
            UnityEngine.GUILayout.EndVertical();

            UnityEngine.GUILayout.BeginVertical(UnityEngine.GUILayout.Width(170));
            {
               UnityEngine.GUILayout.Space(0);

               UnityEngine.GUILayout.Label(Crosstales.RTVoice.Util.Constants.ASSET_VERSION);

               UnityEngine.GUILayout.Space(12);

               UnityEditor.EditorGUILayout.SelectableLabel(Crosstales.RTVoice.Util.Constants.ASSET_AUTHOR_URL, UnityEngine.GUILayout.Height(16), UnityEngine.GUILayout.ExpandHeight(false));

               UnityEngine.GUILayout.Space(2);

               UnityEditor.EditorGUILayout.SelectableLabel(Crosstales.RTVoice.Util.Constants.ASSET_CONTACT, UnityEngine.GUILayout.Height(16), UnityEngine.GUILayout.ExpandHeight(false));
            }
            UnityEngine.GUILayout.EndVertical();

            UnityEngine.GUILayout.BeginVertical(UnityEngine.GUILayout.ExpandWidth(true));
            {
               //GUILayout.Space(0);
            }
            UnityEngine.GUILayout.EndVertical();

            UnityEngine.GUILayout.BeginVertical(UnityEngine.GUILayout.Width(64));
            {
               if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Logo_Asset, "Visit asset website")))
                  Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.EditorUtil.EditorConstants.ASSET_URL);
            }
            UnityEngine.GUILayout.EndVertical();
         }
         UnityEngine.GUILayout.EndHorizontal();

         UnityEngine.GUILayout.Label("© 2015-2022 by " + Crosstales.RTVoice.Util.Constants.ASSET_AUTHOR);

         Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

         UnityEngine.GUILayout.BeginHorizontal();
         {
            if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" AssetStore", Crosstales.RTVoice.EditorUtil.EditorHelper.Logo_Unity, "Visit the 'Unity AssetStore' website.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_CT_URL);

            if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" " + Crosstales.RTVoice.Util.Constants.ASSET_AUTHOR, Crosstales.RTVoice.EditorUtil.EditorHelper.Logo_CT, "Visit the '" + Crosstales.RTVoice.Util.Constants.ASSET_AUTHOR + "' website.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_AUTHOR_URL);
         }
         UnityEngine.GUILayout.EndHorizontal();

         Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

         aboutTab = UnityEngine.GUILayout.Toolbar(aboutTab, new[] { "Readme", "Versions", "SSML", "EML", "Update" });

         switch (aboutTab)
         {
            case 4:
            {
               scrollPosAboutUpdate = UnityEditor.EditorGUILayout.BeginScrollView(scrollPosAboutUpdate, false, false);
               {
                  UnityEngine.Color fgColor = UnityEngine.GUI.color;

                  UnityEngine.GUI.color = UnityEngine.Color.yellow;

                  switch (updateStatus)
                  {
                     case Crosstales.RTVoice.EditorTask.UpdateStatus.NO_UPDATE:
                        UnityEngine.GUI.color = UnityEngine.Color.green;
                        UnityEngine.GUILayout.Label(updateText);
                        break;
                     case Crosstales.RTVoice.EditorTask.UpdateStatus.UPDATE:
                     {
                        UnityEngine.GUILayout.Label(updateText);

                        if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" Download", "Visit the 'Unity AssetStore' to download the latest version.")))
                        {
                           UnityEditorInternal.AssetStore.Open("content/" + Crosstales.RTVoice.EditorUtil.EditorConstants.ASSET_ID);
                        }

                        break;
                     }
                     case Crosstales.RTVoice.EditorTask.UpdateStatus.UPDATE_VERSION:
                     {
                        UnityEngine.GUILayout.Label(updateText);

                        if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" Upgrade", "Upgrade to the newer version in the 'Unity AssetStore'")))
                           Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_CT_URL);

                        break;
                     }
                     case Crosstales.RTVoice.EditorTask.UpdateStatus.DEPRECATED:
                     {
                        UnityEngine.GUILayout.Label(updateText);

                        if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(" More Information", "Visit the 'crosstales'-site for more information.")))
                           Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_AUTHOR_URL);

                        break;
                     }
                     default:
                        UnityEngine.GUI.color = UnityEngine.Color.cyan;
                        UnityEngine.GUILayout.Label(updateText);
                        break;
                  }

                  UnityEngine.GUI.color = fgColor;
               }
               UnityEditor.EditorGUILayout.EndScrollView();

               if (updateStatus == Crosstales.RTVoice.EditorTask.UpdateStatus.NOT_CHECKED || updateStatus == Crosstales.RTVoice.EditorTask.UpdateStatus.NO_UPDATE)
               {
                  bool isChecking = !(worker == null || worker?.IsAlive == false);

                  UnityEngine.GUI.enabled = Crosstales.Common.Util.NetworkHelper.isInternetAvailable && !isChecking;

                  if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(isChecking ? "Checking... Please wait." : " Check For Update", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Check, "Checks for available updates of " + Crosstales.RTVoice.Util.Constants.ASSET_NAME)))
                  {
                     worker = new System.Threading.Thread(() => Crosstales.RTVoice.EditorTask.UpdateCheck.UpdateCheckForEditor(out updateText, out updateStatus));
                     worker.Start();
                  }

                  UnityEngine.GUI.enabled = true;
               }

               break;
            }
            case 0:
            {
               if (readme == null)
               {
                  string path = UnityEngine.Application.dataPath + Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH + "README.txt";

                  try
                  {
                     readme = verifyTextLength(System.IO.File.ReadAllText(path));
                  }
                  catch (System.Exception)
                  {
                     readme = "README not found: " + path;
                  }
               }

               scrollPosAboutReadme = UnityEditor.EditorGUILayout.BeginScrollView(scrollPosAboutReadme, false, false);
               {
                  UnityEngine.GUILayout.Label(readme);
               }
               UnityEditor.EditorGUILayout.EndScrollView();
               break;
            }
            case 1:
            {
               if (versions == null)
               {
                  string path = UnityEngine.Application.dataPath + Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH + "Documentation/VERSIONS.txt";

                  try
                  {
                     versions = verifyTextLength(System.IO.File.ReadAllText(path));
                  }
                  catch (System.Exception)
                  {
                     versions = "VERSIONS not found: " + path;
                  }
               }

               scrollPosAboutVersions = UnityEditor.EditorGUILayout.BeginScrollView(scrollPosAboutVersions, false, false);
               {
                  UnityEngine.GUILayout.Label(versions);
               }

               UnityEditor.EditorGUILayout.EndScrollView();
               break;
            }
            case 2:
            {
               if (ssml == null)
               {
                  string path = UnityEngine.Application.dataPath + Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH + "Documentation/SSML.txt";

                  try
                  {
                     ssml = verifyTextLength(System.IO.File.ReadAllText(path));
                  }
                  catch (System.Exception)
                  {
                     ssml = "SSML not found: " + path;
                  }
               }

               scrollPosAboutVersions = UnityEditor.EditorGUILayout.BeginScrollView(scrollPosAboutVersions, false, false);
               {
                  UnityEngine.GUILayout.Label(ssml);
               }

               UnityEditor.EditorGUILayout.EndScrollView();
               break;
            }
            default:
            {
               if (emotionml == null)
               {
                  string path = UnityEngine.Application.dataPath + Crosstales.RTVoice.EditorUtil.EditorConfig.ASSET_PATH + "Documentation/EMOTIONML.txt";

                  try
                  {
                     emotionml = verifyTextLength(System.IO.File.ReadAllText(path));
                  }
                  catch (System.Exception)
                  {
                     emotionml = "EmotionML not found: " + path;
                  }
               }

               scrollPosAboutVersions = UnityEditor.EditorGUILayout.BeginScrollView(scrollPosAboutVersions, false, false);
               {
                  UnityEngine.GUILayout.Label(emotionml);
               }

               UnityEditor.EditorGUILayout.EndScrollView();
               break;
            }
         }

         Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

         UnityEngine.GUILayout.BeginHorizontal();
         {
            if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Social_Discord, "Communicate with us via 'Discord'.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_SOCIAL_DISCORD);

            if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Social_Facebook, "Follow us on 'Facebook'.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_SOCIAL_FACEBOOK);

            if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Social_Twitter, "Follow us on 'Twitter'.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_SOCIAL_TWITTER);

            if (UnityEngine.GUILayout.Button(new UnityEngine.GUIContent(string.Empty, Crosstales.RTVoice.EditorUtil.EditorHelper.Social_Linkedin, "Follow us on 'LinkedIn'.")))
               Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_SOCIAL_LINKEDIN);
         }
         UnityEngine.GUILayout.EndHorizontal();

         UnityEngine.GUILayout.Space(6);
      }

      private static string verifyTextLength(string text)
      {
         string result = text;

         if (text.Length > maxChars)
         {
            result = text.Substring(0, maxChars) + "..." + System.Environment.NewLine + "<--- Content truncated --->";
         }

         return result;
      }

      protected static void save()
      {
         Crosstales.RTVoice.Util.Config.Save();
         Crosstales.RTVoice.EditorUtil.EditorConfig.Save();

         if (Crosstales.RTVoice.Util.Config.DEBUG)
            UnityEngine.Debug.Log("Config data saved");
      }

      #endregion
   }
}
#endif
// © 2016-2022 crosstales LLC (https://www.crosstales.com)