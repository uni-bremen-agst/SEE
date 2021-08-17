#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Crosstales.RTVoice.EditorUtil
{
   /// <summary>Editor helper class.</summary>
   public abstract class EditorHelper : Common.EditorUtil.BaseEditorHelper
   {
      #region Static variables

      /// <summary>Start index inside the "GameObject"-menu.</summary>
      public const int GO_ID = 35;

      /// <summary>Start index inside the "Tools"-menu.</summary>
      //public const int MENU_ID = 2000;
      public const int MENU_ID = 11820; // 1, R = 18, T = 20

      private static Texture2D logo_asset;
      private static Texture2D logo_asset_small;

      private static Texture2D icon_speak;
      private static Texture2D icon_silence;
      private static Texture2D icon_next;
      private static Texture2D icon_previous;

      private static Texture2D store_AdventureCreator;
      private static Texture2D store_CinemaDirector;
      private static Texture2D store_DialogueSystem;
      private static Texture2D store_LDC;
      private static Texture2D store_LipSync;
      private static Texture2D store_Naninovel;
      private static Texture2D store_NPC_Chat;
      private static Texture2D store_QuestSystem;
      private static Texture2D store_SALSA;
      private static Texture2D store_SLATE;
      private static Texture2D store_Amplitude;
      private static Texture2D store_Klattersynth;
      private static Texture2D store_WebGL;
      private static Texture2D store_Google;

      #endregion


      #region Static properties

      public static Texture2D Logo_Asset => loadImage(ref logo_asset, "logo_asset_pro.png");

      public static Texture2D Logo_Asset_Small => loadImage(ref logo_asset_small, "logo_asset_small_pro.png");

      public static Texture2D Icon_Speak => loadImage(ref icon_speak, "icon_speak.png");

      public static Texture2D Icon_Silence => loadImage(ref icon_silence, "icon_silence.png");

      public static Texture2D Icon_Next => loadImage(ref icon_next, "icon_next.png");

      public static Texture2D Icon_Previous => loadImage(ref icon_previous, "icon_previous.png");

      public static Texture2D Store_AdventureCreator => loadImage(ref store_AdventureCreator, "Store_AdventureCreator.png");

      public static Texture2D Store_CinemaDirector => loadImage(ref store_CinemaDirector, "Store_CinemaDirector.png");

      public static Texture2D Store_DialogueSystem => loadImage(ref store_DialogueSystem, "Store_DialogueSystem.png");

      public static Texture2D Store_LDC => loadImage(ref store_LDC, "Store_LDC.png");

      public static Texture2D Store_LipSync => loadImage(ref store_LipSync, "Store_LipSync.png");

      public static Texture2D Store_Naninovel => loadImage(ref store_Naninovel, "Store_Naninovel.png");
      
      public static Texture2D Store_NPC_Chat => loadImage(ref store_NPC_Chat, "Store_NPC_Chat.png");

      public static Texture2D Store_QuestSystem => loadImage(ref store_QuestSystem, "Store_QuestSystem.png");

      public static Texture2D Store_SALSA => loadImage(ref store_SALSA, "Store_SALSA.png");

      public static Texture2D Store_SLATE => loadImage(ref store_SLATE, "Store_SLATE.png");

      public static Texture2D Store_Amplitude => loadImage(ref store_Amplitude, "Store_Amplitude.png");

      public static Texture2D Store_Klattersynth => loadImage(ref store_Klattersynth, "Store_Klattersynth.png");

      public static Texture2D Store_WebGL => loadImage(ref store_WebGL, "Store_WebGL.png");

      public static Texture2D Store_Google => loadImage(ref store_Google, "Store_Google.png");

      #endregion


      #region Static methods

      /// <summary>Shows an "RT-Voice unavailable"-UI.</summary>
      public static void RTVUnavailable()
      {
         EditorGUILayout.HelpBox("RT-Voice not available!", MessageType.Warning);

         EditorGUILayout.HelpBox($"Did you add the '{Util.Constants.RTVOICE_SCENE_OBJECT_NAME}'-prefab to the scene?", MessageType.Info);

         GUILayout.Space(8);

         if (GUILayout.Button(new GUIContent($"Add {Util.Constants.RTVOICE_SCENE_OBJECT_NAME}", Icon_Plus, $"Add the '{Util.Constants.RTVOICE_SCENE_OBJECT_NAME}'-prefab to the current scene.")))
         {
            InstantiatePrefab(Util.Constants.RTVOICE_SCENE_OBJECT_NAME);
         }
      }

      /// <summary>Shows the "no voices found"-UI.</summary>
      public static void NoVoicesUI()
      {
         if (isRTVoiceInScene)
         {
            if (Speaker.Instance.isPlatformSupported && !Speaker.Instance.isWorkingInPlaymode)
            {
               EditorGUILayout.HelpBox("The current TTS only works in builds!", MessageType.Error);
            }
            else if (!Speaker.Instance.isPlatformSupported)
            {
               EditorGUILayout.HelpBox("The current platform is not supported by the active voice provider. Please use MaryTTS or a custom provider (e.g. Klattersynth).", MessageType.Error);
            }
            else
            {
               if (Speaker.Instance.hasVoicesInEditor)
                  EditorGUILayout.HelpBox("TTS with the current settings is not possible!", MessageType.Error);
            }
         }
         else
         {
            EditorGUILayout.HelpBox("Could not load voices!", MessageType.Warning);

            EditorGUILayout.HelpBox("Did you add the '" + Util.Constants.RTVOICE_SCENE_OBJECT_NAME + "'-prefab to the scene?", MessageType.Info);

            GUILayout.Space(8);

            if (GUILayout.Button(new GUIContent(" Add RTVoice", Icon_Plus, "Add the '" + Util.Constants.RTVOICE_SCENE_OBJECT_NAME + "'-prefab to the current scene.")))
            {
               InstantiatePrefab(Util.Constants.RTVOICE_SCENE_OBJECT_NAME);
            }
         }
      }

      /// <summary>Instantiates a prefab.</summary>
      /// <param name="prefabName">Name of the prefab.</param>
      public static void InstantiatePrefab(string prefabName)
      {
         InstantiatePrefab(prefabName, EditorConfig.PREFAB_PATH);
      }

      /// <summary>Checks if the "RTVoice"-prefab is in the scene.</summary>
      /// <returns>True if the "RTVoice"-prefab is in the scene.</returns>
      public static bool isRTVoiceInScene => GameObject.FindObjectOfType(typeof(Speaker)) != null; //GameObject.Find(Util.Constants.RTVOICE_SCENE_OBJECT_NAME) != null;

      /// <summary>Checks if the "GlobalCache"-prefab is in the scene.</summary>
      /// <returns>True if the "GlobalCache"-prefab is in the scene.</returns>
      public static bool isGlobalCacheInScene => GameObject.FindObjectOfType(typeof(GlobalCache)) != null; //GameObject.Find(Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME) != null;

      /// <summary>Shows a banner for "Online Check".</summary>
      public static void BannerOC()
      {
#if !CT_OC
         if (Util.Constants.SHOW_OC_BANNER)
         {
            GUILayout.BeginHorizontal();
            {
               EditorGUILayout.HelpBox("'Online Check' is not installed!" + System.Environment.NewLine + "For reliable Internet availability tests, please install or get it from the Unity AssetStore.", MessageType.Info);

               GUILayout.BeginVertical(GUILayout.Width(32));
               {
                  GUILayout.Space(4);

                  if (GUILayout.Button(new GUIContent(string.Empty, Logo_Asset_OC, "Visit Online Check in the Unity AssetStore")))
                     Util.Helper.OpenURL(Util.Constants.ASSET_OC);
               }
               GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
         }
#endif
      }

      /// <summary>Loads an image as Texture2D from 'Editor Default Resources'.</summary>
      /// <param name="logo">Logo to load.</param>
      /// <param name="fileName">Name of the image.</param>
      /// <returns>Image as Texture2D from 'Editor Default Resources'.</returns>
      private static Texture2D loadImage(ref Texture2D logo, string fileName)
      {
         if (logo == null)
         {
#if CT_DEVELOP
            logo = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + EditorConfig.ASSET_PATH + "Icons/" + fileName, typeof(Texture2D));
#else
                logo = (Texture2D)EditorGUIUtility.Load("crosstales/RTVoice/" + fileName);
#endif

            if (logo == null)
            {
               Debug.LogWarning("Image not found: " + fileName);
            }
         }

         return logo;
      }

      #endregion
   }
}
#endif
// © 2016-2021 crosstales LLC (https://www.crosstales.com)