#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Crosstales.RTVoice.EditorUtil
{
   /// <summary>Editor configuration for the asset.</summary>
   [InitializeOnLoad]
   public static class EditorConfig
   {
      #region Variables

      /// <summary>Enable or disable update-checks for the asset.</summary>
      public static bool UPDATE_CHECK = Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_UPDATE_CHECK;

      /// <summary>Enable or disable adding compile define "CT_RTV" for the asset.</summary>
      public static bool COMPILE_DEFINES = Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_COMPILE_DEFINES;

      /// <summary>Automatically load and add the prefabs to the scene.</summary>
      public static bool PREFAB_AUTOLOAD = Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_PREFAB_AUTOLOAD;

      /// <summary>Enable or disable the icon in the hierarchy.</summary>
      public static bool HIERARCHY_ICON = Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_HIERARCHY_ICON;

      /// <summary>Is the configuration loaded?</summary>
      public static bool isLoaded;

      private static string assetPath;
      private const string idPath = "Documentation/id/";
      private static readonly string idName = Crosstales.RTVoice.EditorUtil.EditorConstants.ASSET_UID + ".txt";

      #endregion


      #region Constructor

      static EditorConfig()
      {
         if (!isLoaded)
         {
            Load();
         }
      }

      #endregion


      #region Properties

      /// <summary>Returns the path to the asset inside the Unity project.</summary>
      /// <returns>The path to the asset inside the Unity project.</returns>
      public static string ASSET_PATH
      {
         get
         {
            if (assetPath == null)
            {
               initAssetPath();
            }

            return assetPath;
         }
      }

      /// <summary>Returns the path of the prefabs.</summary>
      /// <returns>The path of the prefabs.</returns>
      public static string PREFAB_PATH => ASSET_PATH + Crosstales.RTVoice.EditorUtil.EditorConstants.PREFAB_SUBPATH;

      #endregion


      #region Public static methods

      /// <summary>Resets all changeable variables to their default value.</summary>
      public static void Reset()
      {
         UPDATE_CHECK = Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_UPDATE_CHECK;
         COMPILE_DEFINES = Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_COMPILE_DEFINES;
         PREFAB_AUTOLOAD = Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_PREFAB_AUTOLOAD;
         HIERARCHY_ICON = Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_HIERARCHY_ICON;
      }

      /// <summary>Loads all changeable variables.</summary>
      public static void Load()
      {
         initAssetPath();

         if (Crosstales.Common.Util.CTPlayerPrefs.HasKey(Crosstales.RTVoice.EditorUtil.EditorConstants.KEY_UPDATE_CHECK))
            UPDATE_CHECK = Crosstales.Common.Util.CTPlayerPrefs.GetBool(Crosstales.RTVoice.EditorUtil.EditorConstants.KEY_UPDATE_CHECK);

         if (Crosstales.Common.Util.CTPlayerPrefs.HasKey(Crosstales.RTVoice.EditorUtil.EditorConstants.KEY_COMPILE_DEFINES))
            COMPILE_DEFINES = Crosstales.Common.Util.CTPlayerPrefs.GetBool(Crosstales.RTVoice.EditorUtil.EditorConstants.KEY_COMPILE_DEFINES);

         if (Crosstales.Common.Util.CTPlayerPrefs.HasKey(Crosstales.RTVoice.EditorUtil.EditorConstants.KEY_PREFAB_AUTOLOAD))
            PREFAB_AUTOLOAD = Crosstales.Common.Util.CTPlayerPrefs.GetBool(Crosstales.RTVoice.EditorUtil.EditorConstants.KEY_PREFAB_AUTOLOAD);

         if (Crosstales.Common.Util.CTPlayerPrefs.HasKey(Crosstales.RTVoice.EditorUtil.EditorConstants.KEY_HIERARCHY_ICON))
            HIERARCHY_ICON = Crosstales.Common.Util.CTPlayerPrefs.GetBool(Crosstales.RTVoice.EditorUtil.EditorConstants.KEY_HIERARCHY_ICON);

         isLoaded = true;
      }

      /// <summary>Saves all changeable variables.</summary>
      public static void Save()
      {
         Crosstales.Common.Util.CTPlayerPrefs.SetBool(Crosstales.RTVoice.EditorUtil.EditorConstants.KEY_UPDATE_CHECK, UPDATE_CHECK);
         Crosstales.Common.Util.CTPlayerPrefs.SetBool(Crosstales.RTVoice.EditorUtil.EditorConstants.KEY_COMPILE_DEFINES, COMPILE_DEFINES);
         Crosstales.Common.Util.CTPlayerPrefs.SetBool(Crosstales.RTVoice.EditorUtil.EditorConstants.KEY_PREFAB_AUTOLOAD, PREFAB_AUTOLOAD);
         Crosstales.Common.Util.CTPlayerPrefs.SetBool(Crosstales.RTVoice.EditorUtil.EditorConstants.KEY_HIERARCHY_ICON, HIERARCHY_ICON);

         Crosstales.Common.Util.CTPlayerPrefs.Save();
      }

      #endregion

      private static void initAssetPath()
      {
         if (assetPath == null)
         {
            try
            {
               if (System.IO.File.Exists(Application.dataPath + Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_ASSET_PATH + idPath + idName))
               {
                  assetPath = Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_ASSET_PATH;
               }
               else
               {
                  string[] files = System.IO.Directory.GetFiles(Application.dataPath, idName, System.IO.SearchOption.AllDirectories);

                  if (files.Length > 0)
                  {
                     string name = files[0].Substring(Application.dataPath.Length);
                     assetPath = name.Substring(0, name.Length - idPath.Length - idName.Length).Replace("\\", "/");
                  }
                  else
                  {
                     Debug.LogWarning("Could not locate the asset! File not found: " + idName);
                     assetPath = Crosstales.RTVoice.EditorUtil.EditorConstants.DEFAULT_ASSET_PATH;
                  }
               }

               //Debug.LogWarning("PATH : " + assetPath);
               Crosstales.Common.Util.CTPlayerPrefs.SetString(Crosstales.RTVoice.Util.Constants.KEY_ASSET_PATH, assetPath);
               Crosstales.Common.Util.CTPlayerPrefs.Save();
            }
            catch (System.Exception ex)
            {
               Debug.LogWarning("Could not locate asset: " + ex);
            }
         }
      }
   }
}
#endif
// © 2017-2022 crosstales LLC (https://www.crosstales.com)