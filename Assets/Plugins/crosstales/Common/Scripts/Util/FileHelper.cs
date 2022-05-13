using System.Linq;
using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Various helper functions for the file system.</summary>
#if UNITY_EDITOR
   [UnityEditor.InitializeOnLoad]
#endif
   public abstract class FileHelper
   {
      #region Variables

      private static string applicationDataPath = Application.dataPath;

      #endregion


      #region Properties

      /// <summary>Returns the path to the the "Streaming Assets".</summary>
      /// <returns>The path to the the "Streaming Assets".</returns>
      public static string StreamingAssetsPath
      {
         get
         {
            if (Crosstales.Common.Util.BaseHelper.isAndroidPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
               return $"jar:file://{applicationDataPath}!/assets/";

            if (Crosstales.Common.Util.BaseHelper.isIOSBasedPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
               return $"{applicationDataPath}/Raw/";

            return $"{applicationDataPath}/StreamingAssets/";
         }
      }

      #endregion


      #region Static block

      static FileHelper()
      {
         //Debug.Log("Static block");
         initialize();
      }

      [RuntimeInitializeOnLoadMethod]
      private static void initialize()
      {
         //Debug.Log("initialize");
         applicationDataPath = Application.dataPath;
/*
         if (!isEditorMode)
         {
            GameObject go = new GameObject("_HelperCT");
            go.AddComponent<HelperCT>();
            GameObject.DontDestroyOnLoad(go);
         }
*/
      }

      #endregion


      #region Public methods

      /// <summary>Validates a given path and add missing slash.</summary>
      /// <param name="path">Path to validate</param>
      /// <param name="addEndDelimiter">Add delimiter at the end of the path (optional, default: true)</param>
      /// <param name="preserveFile">Preserves a given file in the path (optional, default: true)</param>
      /// <returns>Valid path</returns>
      public static string ValidatePath(string path, bool addEndDelimiter = true, bool preserveFile = true)
      {
         if (!string.IsNullOrEmpty(path))
         {
            if (Crosstales.Common.Util.NetworkHelper.isValidURL(path))
               return path;

            string pathTemp = !preserveFile && System.IO.File.Exists(path.Trim()) ? System.IO.Path.GetDirectoryName(path.Trim()) : path.Trim();

            string result;

            if ((Crosstales.Common.Util.BaseHelper.isWindowsBasedPlatform || Crosstales.Common.Util.BaseHelper.isWindowsEditor) && !Crosstales.Common.Util.BaseHelper.isMacOSEditor && !Crosstales.Common.Util.BaseHelper.isLinuxEditor)
            {
               result = pathTemp.Replace('/', '\\');

               if (addEndDelimiter)
               {
                  if (!result.CTEndsWith(Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_WINDOWS))
                  {
                     result += Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_WINDOWS;
                  }
               }
            }
            else
            {
               result = pathTemp.Replace('\\', '/');

               if (addEndDelimiter)
               {
                  if (!result.CTEndsWith(Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_UNIX))
                  {
                     result += Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_UNIX;
                  }
               }
            }

            return string.Join(string.Empty, result.Split(System.IO.Path.GetInvalidPathChars()));
            //return result;
         }

         return path;
      }

      /// <summary>Validates a given file.</summary>
      /// <param name="path">File to validate</param>
      /// <returns>Valid file path</returns>
      public static string ValidateFile(string path)
      {
         if (!string.IsNullOrEmpty(path))
         {
            if (Crosstales.Common.Util.NetworkHelper.isValidURL(path))
               return path;

            string result = ValidatePath(path);

            if (result.CTEndsWith(Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_WINDOWS) ||
                result.CTEndsWith(Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_UNIX))
            {
               result = result.Substring(0, result.Length - 1);
            }

            string fileName;
            if ((Crosstales.Common.Util.BaseHelper.isWindowsBasedPlatform || Crosstales.Common.Util.BaseHelper.isWindowsEditor) && !Crosstales.Common.Util.BaseHelper.isMacOSEditor && !Crosstales.Common.Util.BaseHelper.isLinuxEditor)
            {
               fileName = result.Substring(result.CTLastIndexOf(Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_WINDOWS) + 1);
            }
            else
            {
               fileName = result.Substring(result.CTLastIndexOf(Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_UNIX) + 1);
            }

            string newName =
               string.Join(string.Empty,
                  fileName.Split(System.IO.Path
                     .GetInvalidFileNameChars())); //.Replace(BaseConstants.PATH_DELIMITER_WINDOWS, string.Empty).Replace(BaseConstants.PATH_DELIMITER_UNIX, string.Empty);

            return result.Substring(0, result.Length - fileName.Length) + newName;
         }

         return path;
      }

      /// <summary>
      /// Checks a given path for invalid characters
      /// </summary>
      /// <param name="path">Path to check for invalid characters</param>
      /// <returns>Returns true if the path contains invalid chars, otherwise it's false.</returns>
      public static bool PathHasInvalidChars(string path)
      {
         return !string.IsNullOrEmpty(path) && path.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0;
      }

      /// <summary>
      /// Checks a given file for invalid characters
      /// </summary>
      /// <param name="file">File to check for invalid characters</param>
      /// <returns>Returns true if the file contains invalid chars, otherwise it's false.</returns>
      public static bool FileHasInvalidChars(string file)
      {
         return !string.IsNullOrEmpty(file) && file.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0;
      }

      /// <summary>
      /// Find files inside a path.
      /// </summary>
      /// <param name="path">Path to find the files</param>
      /// <param name="isRecursive">Recursive search (default: false, optional)</param>
      /// <param name="filenames">Filenames for the file search, e.g. "Image.png" (optional)</param>
      /// <returns>Returns array of the found files inside the path (alphabetically ordered). Zero length array when an error occured.</returns>
      public static string[] GetFilesForName(string path, bool isRecursive = false, params string[] filenames)
      {
         if (Crosstales.Common.Util.BaseHelper.isWebPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'GetFilesForName' is not supported for the current platform!");
         }
         else
         {
            if (!string.IsNullOrEmpty(path))
            {
               if (Crosstales.Common.Util.BaseHelper.isWSABasedPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
               {
#if CT_FB_PRO
#if (UNITY_WSA || UNITY_XBOXONE) && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
                  Crosstales.FB.FileBrowserWSAImpl fbWsa = new Crosstales.FB.FileBrowserWSAImpl();
                  fbWsa.isBusy = true;
                  UnityEngine.WSA.Application.InvokeOnUIThread(() => { fbWsa.GetFilesForName(path, isRecursive, extensions); }, false);

                  do
                  {
                    //wait
                  } while (fbWsa.isBusy);

                  return fbWsa.Selection.ToArray();
#endif
#else
                  Debug.LogWarning($"'GetFilesForName' under UWP (WSA) is supported in combination with 'File Browser PRO'. For more, please see: {Crosstales.Common.Util.BaseConstants.ASSET_FB}");
#endif
               }
               else
               {
                  try
                  {
                     string _path = ValidatePath(path);

                     if (filenames == null || filenames.Length == 0 || filenames.Any(extension => extension.Equals("*") || extension.Equals("*.*")))
                     {
                        return System.IO.Directory.EnumerateFiles(_path, "*", isRecursive
                           ? System.IO.SearchOption.AllDirectories
                           : System.IO.SearchOption.TopDirectoryOnly).ToArray();
                     }

                     System.Collections.Generic.List<string> files = new System.Collections.Generic.List<string>();

                     foreach (string extension in filenames)
                     {
                        files.AddRange(System.IO.Directory.EnumerateFiles(_path, $"{extension}", isRecursive
                           ? System.IO.SearchOption.AllDirectories
                           : System.IO.SearchOption.TopDirectoryOnly));
                     }

                     return files.OrderBy(q => q).ToArray();
                  }
                  catch (System.Exception ex)
                  {
                     Debug.LogWarning($"Could not scan the path for files: {ex}");
                  }
               }
            }
         }

         return System.Array.Empty<string>();
      }

      /// <summary>
      /// Find files inside a path.
      /// </summary>
      /// <param name="path">Path to find the files</param>
      /// <param name="isRecursive">Recursive search (default: false, optional)</param>
      /// <param name="extensions">Extensions for the file search, e.g. "png" (optional)</param>
      /// <returns>Returns array of the found files inside the path (alphabetically ordered). Zero length array when an error occured.</returns>
      public static string[] GetFiles(string path, bool isRecursive = false, params string[] extensions)
      {
         if (Crosstales.Common.Util.BaseHelper.isWSABasedPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
#if CT_FB_PRO
#if (UNITY_WSA || UNITY_XBOXONE) && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
            Crosstales.FB.FileBrowserWSAImpl fbWsa = new Crosstales.FB.FileBrowserWSAImpl();
            fbWsa.isBusy = true;
            UnityEngine.WSA.Application.InvokeOnUIThread(() => { fbWsa.GetFiles(path, isRecursive, extensions); }, false);

            do
            {
              //wait
            } while (fbWsa.isBusy);

            return fbWsa.Selection.ToArray();
#endif
#else
            Debug.LogWarning($"'GetFiles' under UWP (WSA) is supported in combination with 'File Browser PRO'. For more, please see: {Crosstales.Common.Util.BaseConstants.ASSET_FB}");
            return System.Array.Empty<string>();
#endif
         }

         if (extensions?.Length > 0)
         {
            string[] wildcardExt = new string[extensions.Length];

            for (int ii = 0; ii < extensions.Length; ii++)
            {
               wildcardExt[ii] = $"*.{extensions[ii]}";
            }

            return GetFilesForName(path, isRecursive, wildcardExt);
         }

         return GetFilesForName(path, isRecursive, extensions);
      }

      /// <summary>
      /// Find directories inside.
      /// </summary>
      /// <param name="path">Path to find the directories</param>
      /// <param name="isRecursive">Recursive search (default: false, optional)</param>
      /// <returns>Returns array of the found directories inside the path. Zero length array when an error occured.</returns>
      public static string[] GetDirectories(string path, bool isRecursive = false)
      {
         if (Crosstales.Common.Util.BaseHelper.isWebPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'GetDirectories' is not supported for the current platform!");
         }
         else if (Crosstales.Common.Util.BaseHelper.isWSABasedPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
#if CT_FB_PRO
#if (UNITY_WSA || UNITY_XBOXONE) && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
            Crosstales.FB.FileBrowserWSAImpl fbWsa = new Crosstales.FB.FileBrowserWSAImpl();
            fbWsa.isBusy = true;
            UnityEngine.WSA.Application.InvokeOnUIThread(() => { fbWsa.GetDirectories(path, isRecursive); }, false);

            do
            {
              //wait
            } while (fbWsa.isBusy);

            return fbWsa.Selection.ToArray();
#endif
#else
            Debug.LogWarning($"'GetDirectories' under UWP (WSA) is supported in combination with 'File Browser PRO'. For more, please see: {Crosstales.Common.Util.BaseConstants.ASSET_FB}");
#endif
         }
         else
         {
            if (!string.IsNullOrEmpty(path))
            {
               try
               {
                  string _path = ValidatePath(path);
#if NET_4_6 || NET_STANDARD_2_0
                  return System.IO.Directory.EnumerateDirectories(_path, "*", isRecursive
                     ? System.IO.SearchOption.AllDirectories
                     : System.IO.SearchOption.TopDirectoryOnly).ToArray();
#else
                  return System.IO.Directory.GetDirectories(_path, "*",
                     isRecursive
                        ? System.IO.SearchOption.AllDirectories
                        : System.IO.SearchOption.TopDirectoryOnly);
#endif
               }
               catch (System.Exception ex)
               {
                  Debug.LogWarning($"Could not scan the path for directories: {ex}");
               }
            }
         }

         return System.Array.Empty<string>();
      }

      /// <summary>
      /// Find all logical drives.
      /// </summary>
      /// <returns>Returns array of the found drives. Zero length array when an error occured.</returns>
      public static string[] GetDrives() //TODO replace with "Util.Helper.GetDrives" in the next version
      {
         if (Crosstales.Common.Util.BaseHelper.isWebPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'GetDrives' is not supported for the current platform!");
         }
         else if (Crosstales.Common.Util.BaseHelper.isWSABasedPlatform && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
#if CT_FB_PRO
#if (UNITY_WSA || UNITY_XBOXONE) && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
            Crosstales.FB.FileBrowserWSAImpl fbWsa = new Crosstales.FB.FileBrowserWSAImpl();
            fbWsa.isBusy = true;
            UnityEngine.WSA.Application.InvokeOnUIThread(() => { fbWsa.GetDrives(); }, false);

            do
            {
              //wait
            } while (fbWsa.isBusy);

            return fbWsa.Selection.ToArray();
#endif
#else
            Debug.LogWarning($"'GetDrives' under UWP (WSA) is supported in combination with 'File Browser PRO'. For more, please see: {Crosstales.Common.Util.BaseConstants.ASSET_FB}");
#endif
         }
         else
         {
#if (!UNITY_WSA && !UNITY_XBOXONE) || UNITY_EDITOR
            try
            {
               return System.IO.Directory.GetLogicalDrives();
            }
            catch (System.Exception ex)
            {
               Debug.LogWarning($"Could not scan the path for directories: {ex}");
            }
#endif
         }

         return System.Array.Empty<string>();
      }

      /*
      /// <summary>Validates a given path and add missing slash.</summary>
      /// <param name="path">Path to validate</param>
      /// <returns>Valid path</returns>
      public static string ValidPath(string path)
      {
          if (!string.IsNullOrEmpty(path))
          {
              string pathTemp = path.Trim();
              string result = null;

              if (isWindowsPlatform)
              {
                  result = pathTemp.Replace('/', '\\');

                  if (!result.EndsWith(BaseConstants.PATH_DELIMITER_WINDOWS))
                  {
                      result += BaseConstants.PATH_DELIMITER_WINDOWS;
                  }
              }
              else
              {
                  result = pathTemp.Replace('\\', '/');

                  if (!result.EndsWith(BaseConstants.PATH_DELIMITER_UNIX))
                  {
                      result += BaseConstants.PATH_DELIMITER_UNIX;
                  }
              }

              return result;
          }

          return path;
      }

      /// <summary>Validates a given file.</summary>
      /// <param name="path">File to validate</param>
      /// <returns>Valid file path</returns>
      public static string ValidFilePath(string path)
      {
          if (!string.IsNullOrEmpty(path))
          {

              string result = ValidPath(path);

              if (result.EndsWith(BaseConstants.PATH_DELIMITER_WINDOWS) || result.EndsWith(BaseConstants.PATH_DELIMITER_UNIX))
              {
                  result = result.Substring(0, result.Length - 1);
              }

              return result;
          }

          return path;
      }
      */

      /// <summary>Copy or move a directory.</summary>
      /// <param name="sourcePath">Source directory path</param>
      /// <param name="destPath">Destination directory path</param>
      /// <param name="move">Move directory instead of copy (default: false, optional)</param>
      public static void CopyPath(string sourcePath, string destPath, bool move = false)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'CopyPath' is not supported for the current platform!");
         }
         else
         {
            if (!string.IsNullOrEmpty(destPath))
            {
               try
               {
                  if (!System.IO.Directory.Exists(sourcePath))
                  {
                     Debug.LogError($"Source directory does not exists: {sourcePath}");
                  }
                  else
                  {
                     //System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destPath));

                     if (System.IO.Directory.Exists(destPath))
                     {
                        if (Crosstales.Common.Util.BaseConstants.DEV_DEBUG)
                           Debug.LogWarning($"Overwrite destination directory: {destPath}");

                        System.IO.Directory.Delete(destPath, true);
                     }

                     if (move)
                     {
                        System.IO.Directory.Move(sourcePath, destPath);
                     }
                     else
                     {
                        copyAll(new System.IO.DirectoryInfo(sourcePath), new System.IO.DirectoryInfo(destPath));
                     }
                  }
               }
               catch (System.Exception ex)
               {
                  Debug.LogError($"Could not {(move ? "move" : "copy")} directory: {ex}");
               }
            }
         }
      }

      /// <summary>Copy or move a file.</summary>
      /// <param name="sourceFile">Source file path</param>
      /// <param name="destFile">Destination file path</param>
      /// <param name="move">Move file instead of copy (default: false, optional)</param>
      public static void CopyFile(string sourceFile, string destFile, bool move = false)
      {
         if ((Crosstales.Common.Util.BaseHelper.isWSABasedPlatform || Crosstales.Common.Util.BaseHelper.isWebPlatform) && !Crosstales.Common.Util.BaseHelper.isEditor)
         {
            Debug.LogWarning("'CopyFile' is not supported for the current platform!");
         }
         else
         {
            if (!string.IsNullOrEmpty(destFile))
            {
               try
               {
                  if (!System.IO.File.Exists(sourceFile))
                  {
                     Debug.LogError($"Source file does not exists: {sourceFile}");
                  }
                  else
                  {
                     System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destFile));

                     if (System.IO.File.Exists(destFile))
                     {
                        if (Crosstales.Common.Util.BaseConstants.DEV_DEBUG)
                           Debug.LogWarning($"Overwrite destination file: {destFile}");

                        System.IO.File.Delete(destFile);
                     }

                     if (move)
                     {
#if UNITY_STANDALONE || UNITY_EDITOR
                        System.IO.File.Move(sourceFile, destFile);
#else
                        System.IO.File.Copy(sourceFile, destFile);
                        System.IO.File.Delete(sourceFile);
#endif
                     }
                     else
                     {
                        System.IO.File.Copy(sourceFile, destFile);
                     }
                  }
               }
               catch (System.Exception ex)
               {
                  Debug.LogError($"Could not {(move ? "move" : "copy")} file: {ex}");
               }
            }
         }
      }

      /// <summary>
      /// Shows the location of a path (or file) in OS file explorer.
      /// NOTE: only works on standalone platforms
      /// </summary>
      public static void ShowPath(string path)
      {
         ShowFile(path);
      }

      /// <summary>
      /// Shows the location of a file (or path) in OS file explorer.
      /// NOTE: only works on standalone platforms
      /// </summary>
      public static void ShowFile(string file)
      {
         if (Crosstales.Common.Util.BaseHelper.isStandalonePlatform || Crosstales.Common.Util.BaseHelper.isEditor)
         {
#if UNITY_STANDALONE || UNITY_EDITOR
            string path;

            if (string.IsNullOrEmpty(file) || file.Equals("."))
            {
               path = ".";
            }
            else if ((Crosstales.Common.Util.BaseHelper.isWindowsPlatform || Crosstales.Common.Util.BaseHelper.isWindowsEditor) && file.Length < 4)
            {
               path = file; //root directory
            }
            else
            {
               path = ValidatePath(System.IO.Path.GetDirectoryName(file));
            }

            try
            {
               if (System.IO.Directory.Exists(path))
               {
#if (ENABLE_IL2CPP && CT_PROC) || (CT_DEVELOP && CT_PROC)
                  using (Crosstales.Common.Util.CTProcess process = new Crosstales.Common.Util.CTProcess())
#else
                  using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                  //using (CTProcess process = new CTProcess())
#endif
                  {
                     process.StartInfo.Arguments = $"\"{path}\"";

                     if (Crosstales.Common.Util.BaseHelper.isWindowsPlatform || Crosstales.Common.Util.BaseHelper.isWindowsEditor)
                     {
                        process.StartInfo.FileName = "explorer.exe";
#if (ENABLE_IL2CPP && CT_PROC) || (CT_DEVELOP && CT_PROC)
                        process.StartInfo.UseCmdExecute = true;
#endif
                        process.StartInfo.CreateNoWindow = true;
                     }
                     else if (Crosstales.Common.Util.BaseHelper.isMacOSPlatform || Crosstales.Common.Util.BaseHelper.isMacOSEditor)
                     {
                        process.StartInfo.FileName = "open";
                     }
                     else
                     {
                        process.StartInfo.FileName = "xdg-open";
                     }

                     process.Start();
                  }
               }
               else
               {
                  Debug.LogWarning($"Path to file doesn't exist: {path}");
               }
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not show file location: {ex}");
            }
#endif
         }
         else
         {
            Debug.LogWarning("'ShowFileLocation' is not supported on the current platform!");
         }
      }

      /// <summary>
      /// Opens a file with the OS default application.
      /// NOTE: only works for standalone platforms
      /// </summary>
      /// <param name="file">File path</param>
      public static void OpenFile(string file)
      {
         if (Crosstales.Common.Util.BaseHelper.isStandalonePlatform || Crosstales.Common.Util.BaseHelper.isEditor)
         {
            try
            {
#if UNITY_STANDALONE || UNITY_EDITOR
               if (System.IO.File.Exists(file))
               {
#if ENABLE_IL2CPP && CT_PROC
                  using (CTProcess process = new CTProcess())
                  {
                     process.StartInfo.Arguments = $"\"{file}\"";

                     if (Crosstales.Common.Util.BaseHelper.isWindowsPlatform || Crosstales.Common.Util.BaseHelper.isWindowsEditor)
                     {
                        process.StartInfo.FileName = "explorer.exe";
                        process.StartInfo.UseCmdExecute = true;
                        process.StartInfo.CreateNoWindow = true;
                     }
                     else if (Crosstales.Common.Util.BaseHelper.isMacOSPlatform || Crosstales.Common.Util.BaseHelper.isMacOSEditor)
                     {
                        process.StartInfo.FileName = "open";
                     }
                     else
                     {
                        process.StartInfo.FileName = "xdg-open";
                     }

                     process.Start();
                  }
#else
                  using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                  {
                     if (Crosstales.Common.Util.BaseHelper.isMacOSPlatform || Crosstales.Common.Util.BaseHelper.isMacOSEditor)
                     {
                        process.StartInfo.FileName = "open";
                        process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(file) + Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_UNIX;
                        process.StartInfo.Arguments = $"-t \"{System.IO.Path.GetFileName(file)}\"";
                     }
                     else if (Crosstales.Common.Util.BaseHelper.isLinuxPlatform || Crosstales.Common.Util.BaseHelper.isLinuxEditor)
                     {
                        process.StartInfo.FileName = "xdg-open";
                        process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(file) + Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_UNIX;
                        process.StartInfo.Arguments = System.IO.Path.GetFileName(file);
                     }
                     else
                     {
                        process.StartInfo.FileName = file;
                     }

                     process.Start();
                  }
#endif
               }
               else
               {
                  Debug.LogWarning($"File doesn't exist: {file}");
               }
#endif
            }
            catch (System.Exception ex)
            {
               Debug.LogError($"Could not open file: {ex}");
            }
         }
         else
         {
            Debug.LogWarning("'OpenFile' is not supported on the current platform!");
         }
      }

      #endregion


      #region Private methods

      private static void copyAll(System.IO.DirectoryInfo source, System.IO.DirectoryInfo target)
      {
         System.IO.Directory.CreateDirectory(target.FullName);

         foreach (System.IO.FileInfo fi in source.GetFiles())
         {
            fi.CopyTo(System.IO.Path.Combine(target.FullName, fi.Name), true);
         }

         // Copy each subdirectory using recursion.
         foreach (System.IO.DirectoryInfo sourceSubDir in source.GetDirectories())
         {
            System.IO.DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(sourceSubDir.Name);
            copyAll(sourceSubDir, nextTargetSubDir);
         }
      }

      #endregion
   }
}
// © 2015-2022 crosstales LLC (https://www.crosstales.com)