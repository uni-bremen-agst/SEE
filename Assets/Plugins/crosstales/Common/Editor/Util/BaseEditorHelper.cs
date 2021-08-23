﻿#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Crosstales.Common.EditorUtil
{
   /// <summary>Base for various Editor helper functions.</summary>
   public abstract class BaseEditorHelper : Util.BaseHelper
   {
      #region Static variables

      private static readonly System.Type moduleManager = System.Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor.dll");
      private static readonly System.Reflection.MethodInfo isPlatformSupportLoaded = moduleManager.GetMethod("IsPlatformSupportLoaded", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
      private static readonly System.Reflection.MethodInfo getTargetStringFromBuildTarget = moduleManager.GetMethod("GetTargetStringFromBuildTarget", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

      private static Texture2D logo_asset_bwf;
      private static Texture2D logo_asset_dj;
      private static Texture2D logo_asset_fb;
      private static Texture2D logo_asset_oc;
      private static Texture2D logo_asset_radio;
      private static Texture2D logo_asset_rtv;
      private static Texture2D logo_asset_tb;
      private static Texture2D logo_asset_tpb;
      private static Texture2D logo_asset_tps;
      private static Texture2D logo_asset_tr;

      private static Texture2D logo_ct;
      private static Texture2D logo_unity;

      private static Texture2D icon_save;
      private static Texture2D icon_reset;
      private static Texture2D icon_refresh;
      private static Texture2D icon_delete;
      private static Texture2D icon_folder;
      private static Texture2D icon_plus;
      private static Texture2D icon_minus;

      private static Texture2D icon_manual;
      private static Texture2D icon_api;
      private static Texture2D icon_forum;
      private static Texture2D icon_product;

      private static Texture2D icon_check;

      private static Texture2D social_Discord;
      private static Texture2D social_Facebook;
      private static Texture2D social_Twitter;
      private static Texture2D social_Youtube;
      private static Texture2D social_Linkedin;

      private static Texture2D video_promo;
      private static Texture2D video_tutorial;

      private static Texture2D icon_videos;

      private static Texture2D icon_3p_assets;
      private static Texture2D asset_PlayMaker;
      private static Texture2D asset_VolumetricAudio;
      private static Texture2D asset_rocktomate;

      #endregion


      #region Static properties

      public static Texture2D Logo_Asset_BWF => loadImage(ref logo_asset_bwf, "logo_asset_bwf.png");

      public static Texture2D Logo_Asset_DJ => loadImage(ref logo_asset_dj, "logo_asset_dj.png");

      public static Texture2D Logo_Asset_FB => loadImage(ref logo_asset_fb, "logo_asset_fb.png");

      public static Texture2D Logo_Asset_OC => loadImage(ref logo_asset_oc, "logo_asset_oc.png");

      public static Texture2D Logo_Asset_Radio => loadImage(ref logo_asset_radio, "logo_asset_radio.png");

      public static Texture2D Logo_Asset_RTV => loadImage(ref logo_asset_rtv, "logo_asset_rtv.png");

      public static Texture2D Logo_Asset_TB => loadImage(ref logo_asset_tb, "logo_asset_tb.png");

      public static Texture2D Logo_Asset_TPB => loadImage(ref logo_asset_tpb, "logo_asset_tpb.png");

      public static Texture2D Logo_Asset_TPS => loadImage(ref logo_asset_tps, "logo_asset_tps.png");

      public static Texture2D Logo_Asset_TR => loadImage(ref logo_asset_tr, "logo_asset_tr.png");

      public static Texture2D Logo_CT => loadImage(ref logo_ct, "logo_ct.png");

      public static Texture2D Logo_Unity => loadImage(ref logo_unity, "logo_unity.png");

      public static Texture2D Icon_Save => loadImage(ref icon_save, "icon_save.png");

      public static Texture2D Icon_Reset => loadImage(ref icon_reset, "icon_reset.png");

      public static Texture2D Icon_Refresh => loadImage(ref icon_refresh, "icon_refresh.png");

      public static Texture2D Icon_Delete => loadImage(ref icon_delete, "icon_delete.png");

      public static Texture2D Icon_Folder => loadImage(ref icon_folder, "icon_folder.png");

      public static Texture2D Icon_Plus => loadImage(ref icon_plus, "icon_plus.png");

      public static Texture2D Icon_Minus => loadImage(ref icon_minus, "icon_minus.png");

      public static Texture2D Icon_Manual => loadImage(ref icon_manual, "icon_manual.png");

      public static Texture2D Icon_API => loadImage(ref icon_api, "icon_api.png");

      public static Texture2D Icon_Forum => loadImage(ref icon_forum, "icon_forum.png");

      public static Texture2D Icon_Product => loadImage(ref icon_product, "icon_product.png");

      public static Texture2D Icon_Check => loadImage(ref icon_check, "icon_check.png");

      public static Texture2D Social_Discord => loadImage(ref social_Discord, "social_Discord.png");

      public static Texture2D Social_Facebook => loadImage(ref social_Facebook, "social_Facebook.png");

      public static Texture2D Social_Twitter => loadImage(ref social_Twitter, "social_Twitter.png");

      public static Texture2D Social_Youtube => loadImage(ref social_Youtube, "social_Youtube.png");

      public static Texture2D Social_Linkedin => loadImage(ref social_Linkedin, "social_Linkedin.png");

      public static Texture2D Video_Promo => loadImage(ref video_promo, "video_promo.png");

      public static Texture2D Video_Tutorial => loadImage(ref video_tutorial, "video_tutorial.png");

      public static Texture2D Icon_Videos => loadImage(ref icon_videos, "icon_videos.png");

      public static Texture2D Icon_3p_Assets => loadImage(ref icon_3p_assets, "icon_3p_assets.png");

      public static Texture2D Asset_PlayMaker => loadImage(ref asset_PlayMaker, "asset_PlayMaker.png");

      public static Texture2D Asset_VolumetricAudio => loadImage(ref asset_VolumetricAudio, "asset_VolumetricAudio.png");

      public static Texture2D Asset_RockTomate => loadImage(ref asset_rocktomate, "asset_rocktomate.png");

      #endregion


      #region Public methods

      /// <summary>Restart Unity.</summary>
      /// <param name="executeMethod">Executed method after the restart (optional)</param>
      public static void RestartUnity(string executeMethod = "")
      {
         UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

         bool success = false;

         using (System.Diagnostics.Process process = new System.Diagnostics.Process())
         {
            try
            {
               process.StartInfo.CreateNoWindow = true;
               process.StartInfo.UseShellExecute = false;

               string scriptfile;

               switch (Application.platform)
               {
                  case RuntimePlatform.WindowsEditor:
                     scriptfile = $"{System.IO.Path.GetTempPath()}RestartUnity-{System.Guid.NewGuid()}.cmd";

                     System.IO.File.WriteAllText(scriptfile, generateWindowsRestartScript(executeMethod));

                     process.StartInfo.FileName = "cmd.exe";
                     process.StartInfo.Arguments = $"/c start  \"\" \"{scriptfile}\"";
                     break;
                  case RuntimePlatform.OSXEditor:
                     scriptfile = $"{System.IO.Path.GetTempPath()}RestartUnity-{System.Guid.NewGuid()}.sh";

                     System.IO.File.WriteAllText(scriptfile, generateMacRestartScript(executeMethod));

                     process.StartInfo.FileName = "/bin/sh";
                     process.StartInfo.Arguments = $"\"{scriptfile}\" &";
                     break;
                  case RuntimePlatform.LinuxEditor:
                     scriptfile = $"{System.IO.Path.GetTempPath()}RestartUnity-{System.Guid.NewGuid()}.sh";

                     System.IO.File.WriteAllText(scriptfile, generateLinuxRestartScript(executeMethod));

                     process.StartInfo.FileName = "/bin/sh";
                     process.StartInfo.Arguments = $"\"{scriptfile}\" &";
                     break;
                  default:
                     Debug.LogError($"Unsupported Unity Editor: {Application.platform}");
                     return;
               }

               process.Start();

               if (isWindowsEditor)
                  process.WaitForExit(Util.BaseConstants.PROCESS_KILL_TIME);

               success = true;
            }
            catch (System.Exception ex)
            {
               string errorMessage = $"Could restart Unity: {ex}";
               Debug.LogError(errorMessage);
            }
         }

         if (success)
            EditorApplication.Exit(0);
      }

      /// <summary>Shows a separator-UI.</summary>
      /// <param name="space">Space in pixels between the component and the separator line (default: 12, optional).</param>
      public static void SeparatorUI(int space = 12)
      {
         GUILayout.Space(space);
         GUILayout.Box(string.Empty, GUILayout.ExpandWidth(true), GUILayout.Height(1));
      }

      /// <summary>Generates a read-only text field with a label.</summary>
      public static void ReadOnlyTextField(string label, string text)
      {
         EditorGUILayout.BeginHorizontal();
         {
            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - 4));
            EditorGUILayout.SelectableLabel(text, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
         }
         EditorGUILayout.EndHorizontal();
      }

      /// <summary>Refreshes the asset database.</summary>
      /// <param name="options">Asset import options (default: ImportAssetOptions.Default, optional).</param>
      public static void RefreshAssetDatabase(ImportAssetOptions options = ImportAssetOptions.Default)
      {
         if (!Application.isPlaying)
            AssetDatabase.Refresh(options);
      }

      /// <summary>Invokes a public static method on a full qualified class.</summary>
      /// <param name="className">Full qualified name of the class</param>
      /// <param name="methodName">Public static method of the class to execute</param>
      /// <param name="parameters">Parameters for the method (optional)</param>
      public static void InvokeMethod(string className, string methodName, params object[] parameters)
      {
         if (string.IsNullOrEmpty(className))
         {
            Debug.LogWarning("'className' is null or empty; can not execute.");
            return;
         }

         if (string.IsNullOrEmpty(methodName))
         {
            Debug.LogWarning("'methodName' is null or empty; can not execute.");
            return;
         }

         foreach (System.Type type in System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()))
         {
            try
            {
               if (type.FullName?.Equals(className) == true)
                  if (type.IsClass)
                     type.GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Invoke(null, parameters);
            }
            catch (System.Exception ex)
            {
               Debug.LogWarning($"Could not execute method call: {ex}");
            }
         }
      }

      /// <summary>Returns the true if the BuildTarget is installed in Unity.</summary>
      /// <param name="target">BuildTarget to test</param>
      /// <returns>True if the BuildTarget is installed in Unity.</returns>
      public static bool isValidBuildTarget(BuildTarget target)
      {
         return (bool)isPlatformSupportLoaded.Invoke(null, new object[] {(string)getTargetStringFromBuildTarget.Invoke(null, new object[] {target})});
      }

      /*
      public static IEnumerable<BuildTarget> GetAvailableBuildTargets()
      {
          foreach (BuildTarget target in (BuildTarget[])Enum.GetValues(typeof(BuildTarget)))
          {
              BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
              if (BuildPipeline.IsBuildTargetSupported(group, target))
              {
                  yield return target;
              }
          }
      }
      */

      /// <summary>Returns an argument for a name from the command line.</summary>
      /// <param name="name">Name for the argument</param>
      /// <returns>True if the BuildTarget is installed in Unity.</returns>
      public static string getCLIArgument(string name)
      {
         string[] args = System.Environment.GetCommandLineArgs();

         for (int ii = 0; ii < args.Length; ii++)
         {
            if (name.CTEquals(args[ii]) && args.Length > ii + 1)
               return args[ii + 1];
         }

         return null;
      }

      /// <summary>Returns the BuildTarget for a build name, like 'win64'.</summary>
      /// <param name="build">Build name, like 'win64'</param>
      /// <returns>The BuildTarget for a build name.</returns>
      public static BuildTarget getBuildTargetForBuildName(string build)
      {
         if ("win32".CTEquals(build) || "win".CTEquals(build))
            return BuildTarget.StandaloneWindows;

         if ("win64".CTEquals(build))
            return BuildTarget.StandaloneWindows64;

         if (!string.IsNullOrEmpty(build) && build.CTContains("osx"))
            return BuildTarget.StandaloneOSX;
#if UNITY_2019_2_OR_NEWER
         if (!string.IsNullOrEmpty(build) && build.CTContains("linux"))
             return BuildTarget.StandaloneLinux64;
#else
         if ("linux".CTEquals(build))
            return BuildTarget.StandaloneLinux;

         if ("linux64".CTEquals(build))
            return BuildTarget.StandaloneLinux64;

         if ("linuxuniversal".CTEquals(build))
            return BuildTarget.StandaloneLinuxUniversal;
#endif
         if ("android".CTEquals(build))
            return BuildTarget.Android;

         if ("ios".CTEquals(build))
            return BuildTarget.iOS;

         if ("wsaplayer".CTEquals(build) || "WindowsStoreApps".CTEquals(build))
            return BuildTarget.WSAPlayer;

         if ("webgl".CTEquals(build))
            return BuildTarget.WebGL;

         if ("tvOS".CTEquals(build))
            return BuildTarget.tvOS;

         if ("ps4".CTEquals(build))
            return BuildTarget.PS4;

         if ("xboxone".CTEquals(build))
            return BuildTarget.XboxOne;

         if ("switch".CTEquals(build))
            return BuildTarget.Switch;

         Debug.LogWarning($"Build target '{build}' not found! Returning 'StandaloneWindows64'.");
         return BuildTarget.StandaloneWindows64;
      }

      /// <summary>Returns the build name for a BuildTarget.</summary>
      /// <param name="build">BuildTarget for a build name</param>
      /// <returns>The build name for a BuildTarget.</returns>
      public static string getBuildNameFromBuildTarget(BuildTarget build)
      {
         switch (build)
         {
            case BuildTarget.StandaloneWindows:
               return "Win";
            case BuildTarget.StandaloneWindows64:
               return "Win64";
            case BuildTarget.StandaloneOSX:
               return "OSXUniversal";
#if UNITY_2019_2_OR_NEWER
            case BuildTarget.StandaloneLinux64:
              return "Linux64";
#else
            case BuildTarget.StandaloneLinux:
               return "Linux";
            case BuildTarget.StandaloneLinux64:
               return "Linux64";
            case BuildTarget.StandaloneLinuxUniversal:
               return "LinuxUniversal";
#endif
            case BuildTarget.Android:
               return "Android";
            case BuildTarget.iOS:
               return "iOS";
            case BuildTarget.WSAPlayer:
               return "WindowsStoreApps";
            case BuildTarget.WebGL:
               return "WebGL";
            case BuildTarget.tvOS:
               return "tvOS";
            case BuildTarget.PS4:
               return "PS4";
            case BuildTarget.XboxOne:
               return "XboxOne";
            case BuildTarget.Switch:
               return "Switch";
            default:
               Debug.LogWarning($"Build target '{build}' not found! Returning Windows standalone.");
               return "Win64";
         }
      }

      /// <summary>Returns assets for a certain type.</summary>
      /// <returns>List of assets for a certain type.</returns>
      public static System.Collections.Generic.List<T> FindAssetsByType<T>() where T : Object
      {
         string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
         return guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>).Where(asset => asset != null).ToList();
      }

      /// <summary>
      /// Create and return a new asset in a smart location based on the current selection and then select it.
      /// </summary>
      /// <param name="name">Name of the new asset. Do not include the .asset extension.</param>
      /// <param name="showSaveFileBrowser">Shows the save file browser to select a destination for the asset (default: true, optional).</param>
      /// <returns>The new asset.</returns>
      public static T CreateAsset<T>(string name, bool showSaveFileBrowser = true) where T : ScriptableObject
      {
         string path;

         if (showSaveFileBrowser)
         {
            //string classname = nameof(T);
            path = EditorUtility.SaveFilePanelInProject($"Save asset {name}", name, "asset", "");
         }
         else
         {
            path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path.Length == 0)
            {
               // no asset selected, place in asset root
               path = "Assets/" + name + ".asset";
            }
            else if (System.IO.Directory.Exists(path))
            {
               // place in currently selected directory
               path += "/" + name + ".asset";
            }
            else
            {
               // place in current selection's containing directory
               path = System.IO.Path.GetDirectoryName(path) + "/" + name + ".asset";
            }
         }

         if (string.IsNullOrEmpty(path))
            return default;

         T asset = ScriptableObject.CreateInstance<T>();
         AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
         EditorUtility.FocusProjectWindow();
         Selection.activeObject = asset;

         return asset;
      }

      /// <summary>Instantiates a prefab.</summary>
      /// <param name="prefabName">Name of the prefab.</param>
      /// <param name="path">Path to the prefab.</param>
      public static void InstantiatePrefab(string prefabName, string path)
      {
         PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath($"Assets{path}{prefabName}.prefab", typeof(GameObject)));
         UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
      }

      #endregion


      #region Private methods

      private static string generateWindowsRestartScript(string executeMethod)
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         // setup
         sb.AppendLine("@echo off");
         sb.AppendLine("cls");

         // title
         sb.Append("title Restart of ");
         sb.Append(Application.productName);
         sb.AppendLine(" - DO NOT CLOSE THIS WINDOW!");

         // header
         sb.AppendLine("echo ##############################################################################");
         sb.AppendLine("echo #                                                                            #");
         sb.AppendLine("echo #  Common 2021.1.0 - Windows                                                 #");
         sb.AppendLine("echo #  Copyright 2018-2021 by www.crosstales.com                                 #");
         sb.AppendLine("echo #                                                                            #");
         sb.AppendLine("echo #  This script restarts Unity.                                               #");
         sb.AppendLine("echo #  This will take some time, so please be patient and DON'T CLOSE THIS       #");
         sb.AppendLine("echo #  WINDOW before the process is finished!                                    #");
         sb.AppendLine("echo #                                                                            #");
         sb.AppendLine("echo ##############################################################################");
         sb.Append("echo ");
         sb.AppendLine(Application.productName);
         sb.AppendLine("echo.");
         sb.AppendLine("echo.");

         // check if Unity is closed
         sb.AppendLine(":waitloop");
         sb.Append("if not exist \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("Temp\\UnityLockfile\" goto waitloopend");
         sb.AppendLine();
         sb.AppendLine("echo.");
         sb.AppendLine("echo Waiting for Unity to close...");
         sb.AppendLine("timeout /t 3");
         /*
#if UNITY_2018_2_OR_NEWER
         sb.Append("del \"");
         sb.Append(Constants.PATH);
         sb.Append("Temp\\UnityLockfile\" /q");
         sb.AppendLine();
#endif
         */
         sb.AppendLine("goto waitloop");
         sb.AppendLine(":waitloopend");

         // Restart Unity
         sb.AppendLine("echo.");
         sb.AppendLine("echo ##############################################################################");
         sb.AppendLine("echo #  Restarting Unity                                                          #");
         sb.AppendLine("echo ##############################################################################");
         sb.Append("start \"\" \"");
         sb.Append(ValidatePath(EditorApplication.applicationPath, false));
         sb.Append("\" -projectPath \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH.Substring(0, Util.BaseConstants.APPLICATION_PATH.Length - 1));
         sb.Append("\"");

         if (!string.IsNullOrEmpty(executeMethod))
         {
            sb.Append(" -executeMethod ");
            sb.Append(executeMethod);
         }

         sb.AppendLine();
         sb.AppendLine("echo.");

         // check if Unity is started
         sb.AppendLine(":waitloop2");
         sb.Append("if exist \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("Temp\\UnityLockfile\" goto waitloopend2");
         sb.AppendLine();
         sb.AppendLine("echo Waiting for Unity to start...");
         sb.AppendLine("timeout /t 3");
         sb.AppendLine("goto waitloop2");
         sb.AppendLine(":waitloopend2");
         sb.AppendLine("echo.");
         sb.AppendLine("echo Bye!");
         sb.AppendLine("timeout /t 1");
         sb.AppendLine("exit");

         return sb.ToString();
      }

      private static string generateMacRestartScript(string executeMethod)
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         // setup
         sb.AppendLine("#!/bin/bash");
         sb.AppendLine("set +v");
         sb.AppendLine("clear");

         // title
         sb.Append("title='Relaunch of ");
         sb.Append(Application.productName);
         sb.AppendLine(" - DO NOT CLOSE THIS WINDOW!'");
         sb.AppendLine("echo -n -e \"\\033]0;$title\\007\"");

         // header
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  Common 2021.1.0 - macOS                                                   ¦\"");
         sb.AppendLine("echo \"¦  Copyright 2018-2021 by www.crosstales.com                                 ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  This script restarts Unity.                                               ¦\"");
         sb.AppendLine("echo \"¦  This will take some time, so please be patient and DON'T CLOSE THIS       ¦\"");
         sb.AppendLine("echo \"¦  WINDOW before the process is finished!                                    ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.Append("echo \"");
         sb.Append(Application.productName);
         sb.AppendLine("\"");
         sb.AppendLine("echo");
         sb.AppendLine("echo");

         // check if Unity is closed
         sb.Append("while [ -f \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to close...\"");
         sb.AppendLine("  sleep 3");
         /*
#if UNITY_2018_2_OR_NEWER
         sb.Append("  rm \"");
         sb.Append(Constants.PATH);
         sb.Append("Temp/UnityLockfile\"");
         sb.AppendLine();
#endif
         */
         sb.AppendLine("done");

         // Restart Unity
         sb.AppendLine("echo");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦  Restarting Unity                                                          ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.Append("open -a \"");
         sb.Append(EditorApplication.applicationPath);
         sb.Append("\" --args -projectPath \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("\"");

         if (!string.IsNullOrEmpty(executeMethod))
         {
            sb.Append(" -executeMethod ");
            sb.Append(executeMethod);
         }

         sb.AppendLine();

         //check if Unity is started
         sb.AppendLine("echo");
         sb.Append("while [ ! -f \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to start...\"");
         sb.AppendLine("  sleep 3");
         sb.AppendLine("done");
         sb.AppendLine("echo");
         sb.AppendLine("echo \"Bye!\"");
         sb.AppendLine("sleep 1");
         sb.AppendLine("exit");

         return sb.ToString();
      }

      private static string generateLinuxRestartScript(string executeMethod)
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         // setup
         sb.AppendLine("#!/bin/bash");
         sb.AppendLine("set +v");
         sb.AppendLine("clear");

         // title
         sb.Append("title='Relaunch of ");
         sb.Append(Application.productName);
         sb.AppendLine(" - DO NOT CLOSE THIS WINDOW!'");
         sb.AppendLine("echo -n -e \"\\033]0;$title\\007\"");

         // header
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  Common 2021.1.0 - Linux                                                   ¦\"");
         sb.AppendLine("echo \"¦  Copyright 2018-2021 by www.crosstales.com                                 ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  This script restarts Unity.                                               ¦\"");
         sb.AppendLine("echo \"¦  This will take some time, so please be patient and DON'T CLOSE THIS       ¦\"");
         sb.AppendLine("echo \"¦  WINDOW before the process is finished!                                    ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.Append("echo \"");
         sb.Append(Application.productName);
         sb.AppendLine("\"");
         sb.AppendLine("echo");
         sb.AppendLine("echo");

         // check if Unity is closed
         sb.Append("while [ -f \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to close...\"");
         sb.AppendLine("  sleep 3");
         /*
#if UNITY_2018_2_OR_NEWER
         sb.Append("  rm \"");
         sb.Append(Constants.PATH);
         sb.Append("Temp/UnityLockfile\"");
         sb.AppendLine();
#endif
         */
         sb.AppendLine("done");

         // Restart Unity
         sb.AppendLine("echo");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦  Restarting Unity                                                          ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.Append('"');
         sb.Append(EditorApplication.applicationPath);
         sb.Append("\" --args -projectPath \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("\"");

         if (!string.IsNullOrEmpty(executeMethod))
         {
            sb.Append(" -executeMethod ");
            sb.Append(executeMethod);
         }

         sb.Append(" &");
         sb.AppendLine();

         // check if Unity is started
         sb.AppendLine("echo");
         sb.Append("while [ ! -f \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to start...\"");
         sb.AppendLine("  sleep 3");
         sb.AppendLine("done");
         sb.AppendLine("echo");
         sb.AppendLine("echo \"Bye!\"");
         sb.AppendLine("sleep 1");
         sb.AppendLine("exit");

         return sb.ToString();
      }

      private static Texture2D loadImage(ref Texture2D logo, string fileName)
      {
         if (logo == null)
         {
#if CT_DEVELOP
            logo = (Texture2D)AssetDatabase.LoadAssetAtPath($"Assets/Plugins/crosstales/Common/Icons/{fileName}", typeof(Texture2D));
#else
            logo = (Texture2D)EditorGUIUtility.Load($"crosstales/Common/{fileName}");
#endif
            if (logo == null)
               Debug.LogWarning($"Image not found: {fileName}");
         }

         return logo;
      }

      #endregion
   }
}
#endif
// © 2018-2021 crosstales LLC (https://www.crosstales.com)