#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Crosstales.Common.EditorTask
{
   /// <summary>Base-class for all installers.</summary>
   public abstract class BaseInstaller
   {
      #region Variables

      private const string searchTerm = "crosstales";

      #endregion


      #region Public methods

      public static void InstallUI(string assetPath)
      {
         string installerPath = $"{getBasePath(assetPath)}/Common/Extras/";

         installPackage(installerPath, "UI.unitypackage", "CT_UI");
      }

      #endregion


      #region Private methods

      protected static string getBasePath(string assetPath)
      {
         return assetPath.Substring(0, assetPath.LastIndexOf(searchTerm) + searchTerm.Length);
      }

      protected static void installPackage(string installerPath, string package, string compiledefine = null, bool delete = false)
      {
         try
         {
            string packagePath = $"{installerPath}{package}";

            if (System.IO.File.Exists(packagePath))
            {
               AssetDatabase.ImportPackage(packagePath, false);

               if (!string.IsNullOrEmpty(compiledefine))
                  Crosstales.Common.EditorTask.BaseCompileDefines.AddSymbolsToAllTargets(compiledefine);

               if (delete)
                  System.IO.File.Delete(packagePath);
            }
            else
            {
               Debug.LogWarning($"Package '{package}' not found: {packagePath}");
            }
         }
         catch (System.Exception ex)
         {
            Debug.LogError($"Could not successfully import the package '{package}': {ex}");
         }
      }

      #endregion
   }
}
#endif
// © 2022 crosstales LLC (https://www.crosstales.com)