#if UNITY_EDITOR && UNITY_STANDALONE_OSX || CT_DEVELOP
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;

namespace Crosstales.Common.Util
{
   /// <summary>Post processor for macOS.</summary>
   public static class CTPMacOSPostProcessor
   {
      private const string id = "com.crosstales.procstart";

      private const bool rewriteBundle = true; //change it to false if the bundle should not be changed

      [PostProcessBuildAttribute(1)]
      public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
      {
         if (BaseHelper.isMacOSPlatform)
         {
            //remove all meta-files
            string[] files = FileHelper.GetFiles(pathToBuiltProject, true, "meta");

            try
            {
               foreach (string file in files)
               {
                  //Debug.Log(file);
                  System.IO.File.Delete(file);
               }
            }
            catch (System.Exception ex)
            {
               Debug.Log($"Could not delete files: {ex}");
            }

            if (rewriteBundle)
            {
               //rewrite Info.plist
               files = FileHelper.GetFiles(pathToBuiltProject, true, "plist");

               try
               {
                  foreach (string file in files)
                  {
                     string content = System.IO.File.ReadAllText(file);

                     if (content.Contains(id))
                     {
                        content = content.Replace(id, $"{id}.{System.DateTime.Now:yyyyMMddHHmmss}");
                        System.IO.File.WriteAllText(file, content);
                     }
                  }
               }
               catch (System.Exception ex)
               {
                  Debug.Log($"Could not rewrite 'Info.plist' files: {ex}");
               }
               //UnityEditor.OSXStandalone.MacOSCodeSigning.CodeSignAppBundle("/path/to/bundle.bundle"); //TODO add for Unity > 2018?
            }
         }
      }
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)