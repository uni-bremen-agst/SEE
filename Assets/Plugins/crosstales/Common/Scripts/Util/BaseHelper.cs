using System.Linq;
using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Base for various helper functions.</summary>
#if UNITY_EDITOR
   [UnityEditor.InitializeOnLoad]
#endif
   public abstract class BaseHelper
   {
      #region Variables

      public static readonly System.Globalization.CultureInfo BaseCulture = new System.Globalization.CultureInfo("en-US"); //TODO set with current user locale?

      //protected static readonly Regex cleanStringRegex = new Regex(@"([^a-zA-Z0-9 ]|[ ]{2,})");
      protected static readonly System.Text.RegularExpressions.Regex cleanSpacesRegex = new System.Text.RegularExpressions.Regex(@"\s+");

      protected static readonly System.Text.RegularExpressions.Regex cleanTagsRegex = new System.Text.RegularExpressions.Regex(@"<.*?>");
      //protected static readonly System.Text.RegularExpressions.Regex asciiOnlyRegex = new System.Text.RegularExpressions.Regex(@"[^\u0000-\u00FF]+");

      protected static readonly System.Random rnd = new System.Random();

      protected const string file_prefix = "file://";

      public static bool ApplicationIsPlaying = Application.isPlaying;
      private static string applicationDataPath = Application.dataPath;

      #endregion


      #region Properties

      /// <summary>Checks if an Internet connection is available.</summary>
      /// <returns>True if an Internet connection is available.</returns>
      public static bool isInternetAvailable
      {
         get
         {
#if CT_OC
            if (OnlineCheck.OnlineCheck.Instance == null)
            {
               return Application.internetReachability != NetworkReachability.NotReachable;
            }
            else
            {
               return OnlineCheck.OnlineCheck.Instance.isInternetAvailable;
            }
#else
            return Application.internetReachability != NetworkReachability.NotReachable;
#endif
         }
      }

      /// <summary>Checks if the current platform is Windows.</summary>
      /// <returns>True if the current platform is Windows.</returns>
      public static bool isWindowsPlatform
      {
         get
         {
#if UNITY_STANDALONE_WIN
            return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if the current platform is OSX.</summary>
      /// <returns>True if the current platform is OSX.</returns>
      public static bool isMacOSPlatform
      {
         get
         {
#if UNITY_STANDALONE_OSX
             return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if the current platform is Linux.</summary>
      /// <returns>True if the current platform is Linux.</returns>
      public static bool isLinuxPlatform
      {
         get
         {
#if UNITY_STANDALONE_LINUX
            return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if the current platform is standalone (Windows, macOS or Linux).</summary>
      /// <returns>True if the current platform is standalone (Windows, macOS or Linux).</returns>
      public static bool isStandalonePlatform => isWindowsPlatform || isMacOSPlatform || isLinuxPlatform;

      /// <summary>Checks if the current platform is Android.</summary>
      /// <returns>True if the current platform is Android.</returns>
      public static bool isAndroidPlatform
      {
         get
         {
#if UNITY_ANDROID
            return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if the current platform is iOS.</summary>
      /// <returns>True if the current platform is iOS.</returns>
      public static bool isIOSPlatform
      {
         get
         {
#if UNITY_IOS
            return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if the current platform is tvOS.</summary>
      /// <returns>True if the current platform is tvOS.</returns>
      public static bool isTvOSPlatform
      {
         get
         {
#if UNITY_TVOS
            return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if the current platform is WSA.</summary>
      /// <returns>True if the current platform is WSA.</returns>
      public static bool isWSAPlatform
      {
         get
         {
#if UNITY_WSA || UNITY_XBOXONE
            return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if the current platform is XboxOne.</summary>
      /// <returns>True if the current platform is XboxOne.</returns>
      public static bool isXboxOnePlatform
      {
         get
         {
#if UNITY_XBOXONE
            return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if the current platform is PS4.</summary>
      /// <returns>True if the current platform is PS4.</returns>
      public static bool isPS4Platform
      {
         get
         {
#if UNITY_PS4
            return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if the current platform is WebGL.</summary>
      /// <returns>True if the current platform is WebGL.</returns>
      public static bool isWebGLPlatform
      {
         get
         {
#if UNITY_WEBGL
            return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if the current platform is Web (WebPlayer or WebGL).</summary>
      /// <returns>True if the current platform is Web (WebPlayer or WebGL).</returns>
      public static bool isWebPlatform => isWebGLPlatform;

      /// <summary>Checks if the current platform is Windows-based (Windows standalone, WSA or XboxOne).</summary>
      /// <returns>True if the current platform is Windows-based (Windows standalone, WSA or XboxOne).</returns>
      public static bool isWindowsBasedPlatform => isWindowsPlatform || isWSAPlatform || isXboxOnePlatform;

      /// <summary>Checks if the current platform is WSA-based (WSA or XboxOne).</summary>
      /// <returns>True if the current platform is WSA-based (WSA or XboxOne).</returns>
      public static bool isWSABasedPlatform => isWSAPlatform || isXboxOnePlatform;

      /// <summary>Checks if the current platform is Apple-based (macOS standalone, iOS or tvOS).</summary>
      /// <returns>True if the current platform is Apple-based (macOS standalone, iOS or tvOS).</returns>
      public static bool isAppleBasedPlatform => isMacOSPlatform || isIOSPlatform || isTvOSPlatform;

      /// <summary>Checks if the current platform is iOS-based (iOS or tvOS).</summary>
      /// <returns>True if the current platform is iOS-based (iOS or tvOS).</returns>
      public static bool isIOSBasedPlatform => isIOSPlatform || isTvOSPlatform;

      /// <summary>Checks if the current platform is mobile (Android and iOS).</summary>
      /// <returns>True if the current platform is mobile (Android and iOS).</returns>
      public static bool isMobilePlatform => isAndroidPlatform || isIOSBasedPlatform;

      /// <summary>Checks if we are inside the Editor.</summary>
      /// <returns>True if we are inside the Editor.</returns>
      public static bool isEditor => isWindowsEditor || isMacOSEditor || isLinuxEditor;

      /// <summary>Checks if we are inside the Windows Editor.</summary>
      /// <returns>True if we are inside the Windows Editor.</returns>
      public static bool isWindowsEditor
      {
         get
         {
#if UNITY_EDITOR_WIN
            return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if we are inside the macOS Editor.</summary>
      /// <returns>True if we are inside the macOS Editor.</returns>
      public static bool isMacOSEditor
      {
         get
         {
#if UNITY_EDITOR_OSX
            return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if we are inside the Linux Editor.</summary>
      /// <returns>True if we are inside the Linux Editor.</returns>
      public static bool isLinuxEditor
      {
         get
         {
#if UNITY_EDITOR_LINUX
            return true;
#else
            return false;
#endif
         }
      }

      /// <summary>Checks if we are in Editor mode.</summary>
      /// <returns>True if in Editor mode.</returns>
      public static bool isEditorMode => isEditor && !ApplicationIsPlaying;

      /// <summary>Checks if the current build target uses IL2CPP.</summary>
      /// <returns>True if the current build target uses IL2CPP.</returns>
      public static bool isIL2CPP
      {
         get
         {
#if UNITY_EDITOR
            UnityEditor.BuildTarget target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            UnityEditor.BuildTargetGroup group = UnityEditor.BuildPipeline.GetBuildTargetGroup(target);

            return UnityEditor.PlayerSettings.GetScriptingBackend(group) == UnityEditor.ScriptingImplementation.IL2CPP;
#else
#if ENABLE_IL2CPP
            return true;
#else
            return false;
#endif
#endif
         }
      }

      /// <summary>Returns the current platform.</summary>
      /// <returns>The current platform.</returns>
      public static Model.Enum.Platform CurrentPlatform
      {
         get
         {
            if (isWindowsPlatform)
               return Model.Enum.Platform.Windows;

            if (isMacOSPlatform)
               return Model.Enum.Platform.OSX;

            if (isLinuxPlatform)
               return Model.Enum.Platform.Linux;

            if (isAndroidPlatform)
               return Model.Enum.Platform.Android;

            if (isIOSBasedPlatform)
               return Model.Enum.Platform.IOS;

            if (isWSABasedPlatform)
               return Model.Enum.Platform.WSA;

            return isWebPlatform ? Model.Enum.Platform.Web : Model.Enum.Platform.Unsupported;
         }
      }

      /// <summary>Returns the path to the the "Streaming Assets".</summary>
      /// <returns>The path to the the "Streaming Assets".</returns>
      public static string StreamingAssetsPath
      {
         get
         {
            if (isAndroidPlatform && !isEditor)
               return $"jar:file://{applicationDataPath}!/assets/";

            if (isIOSBasedPlatform && !isEditor)
               return $"{applicationDataPath}/Raw/";

            return $"{applicationDataPath}/StreamingAssets/";
         }
      }

      #endregion


      #region Static block

      static BaseHelper()
      {
         //Debug.Log("Static block");
         initialize();
      }

      [RuntimeInitializeOnLoadMethod]
      private static void initialize()
      {
         //Debug.Log("initialize");
         ApplicationIsPlaying = Application.isPlaying;
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

      /// <summary>Opens the given URL with the file explorer or browser.</summary>
      /// <param name="url">URL to open</param>
      /// <returns>True uf the URL was valid.</returns>
      public static bool OpenURL(string url)
      {
         if (isValidURL(url))
         {
            openURL(url);

            return true;
         }

         Debug.LogWarning($"URL was invalid: {url}");
         return false;
      }

      /// <summary>Creates a string of characters with a given length.</summary>
      /// <param name="replaceChars">Characters to generate the string (if more than one character is used, the generated string will be a randomized result of all characters)</param>
      /// <param name="stringLength">Length of the generated string</param>
      /// <returns>Generated string</returns>
      public static string CreateString(string replaceChars, int stringLength)
      {
         if (replaceChars != null)
         {
            if (replaceChars.Length > 1)
            {
               char[] chars = new char[stringLength];

               for (int ii = 0; ii < stringLength; ii++)
               {
                  chars[ii] = replaceChars[rnd.Next(0, replaceChars.Length)];
               }

               return new string(chars);
            }

            return replaceChars.Length == 1 ? new string(replaceChars[0], stringLength) : string.Empty;
         }

         return string.Empty;
      }

      /// <summary>Determines if an AudioSource has an active clip.</summary>
      /// <param name="source">AudioSource to check.</param>
      /// <returns>True if the AudioSource has an active clip.</returns>
      public static bool hasActiveClip(AudioSource source)
      {
         return source != null && source.clip != null &&
                (source.isPlaying ||
                 source.loop ||
                 (!source.loop && source.timeSamples > 0 && source.timeSamples < source.clip.samples - 256));
      }

#if (!UNITY_WSA && !UNITY_XBOXONE) || UNITY_EDITOR
      /// <summary>HTTPS-certification callback.</summary>
      public static bool RemoteCertificateValidationCallback(object sender,
         System.Security.Cryptography.X509Certificates.X509Certificate certificate,
         System.Security.Cryptography.X509Certificates.X509Chain chain,
         System.Net.Security.SslPolicyErrors sslPolicyErrors)
      {
         bool isOk = true;

         // If there are errors in the certificate chain, look at each error to determine the cause.
         if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
         {
            foreach (System.Security.Cryptography.X509Certificates.X509ChainStatus t in chain.ChainStatus.Where(t =>
               t.Status != System.Security.Cryptography.X509Certificates.X509ChainStatusFlags
                  .RevocationStatusUnknown))
            {
               chain.ChainPolicy.RevocationFlag = System.Security.Cryptography.X509Certificates.X509RevocationFlag.EntireChain;
               chain.ChainPolicy.RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.Online;
               chain.ChainPolicy.UrlRetrievalTimeout = new System.TimeSpan(0, 1, 0);
               chain.ChainPolicy.VerificationFlags = System.Security.Cryptography.X509Certificates.X509VerificationFlags.AllFlags;

               isOk = chain.Build((System.Security.Cryptography.X509Certificates.X509Certificate2)certificate);
            }
         }

         return isOk;
      }
#endif

      /// <summary>Validates a given path and add missing slash.</summary>
      /// <param name="path">Path to validate</param>
      /// <param name="addEndDelimiter">Add delimiter at the end of the path (optional, default: true)</param>
      /// <returns>Valid path</returns>
      public static string ValidatePath(string path, bool addEndDelimiter = true)
      {
         if (!string.IsNullOrEmpty(path))
         {
            if (isValidURL(path))
               return path;

            string pathTemp = path.Trim();
            string result;

            if ((isWindowsBasedPlatform || isWindowsEditor) && !isMacOSEditor && !isLinuxEditor)
            {
               result = pathTemp.Replace('/', '\\');

               if (addEndDelimiter)
               {
                  if (!result.CTEndsWith(BaseConstants.PATH_DELIMITER_WINDOWS))
                  {
                     result += BaseConstants.PATH_DELIMITER_WINDOWS;
                  }
               }
            }
            else
            {
               result = pathTemp.Replace('\\', '/');

               if (addEndDelimiter)
               {
                  if (!result.CTEndsWith(BaseConstants.PATH_DELIMITER_UNIX))
                  {
                     result += BaseConstants.PATH_DELIMITER_UNIX;
                  }
               }
            }

            return string.Join(string.Empty, result.Split(System.IO.Path.GetInvalidPathChars()));
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
            if (isValidURL(path))
               return path;

            string result = ValidatePath(path);

            if (result.CTEndsWith(BaseConstants.PATH_DELIMITER_WINDOWS) ||
                result.CTEndsWith(BaseConstants.PATH_DELIMITER_UNIX))
            {
               result = result.Substring(0, result.Length - 1);
            }

            string fileName;
            if ((isWindowsBasedPlatform || isWindowsEditor) && !isMacOSEditor && !isLinuxEditor)
            {
               fileName = result.Substring(result.CTLastIndexOf(BaseConstants.PATH_DELIMITER_WINDOWS) + 1);
            }
            else
            {
               fileName = result.Substring(result.CTLastIndexOf(BaseConstants.PATH_DELIMITER_UNIX) + 1);
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
      /// Find files inside a path.
      /// </summary>
      /// <param name="path">Path to find the files</param>
      /// <param name="isRecursive">Recursive search (default: false, optional)</param>
      /// <param name="extensions">Extensions for the file search, e.g. "png" (optional)</param>
      /// <returns>Returns array of the found files inside the path (alphabetically ordered). Zero length array when an error occured.</returns>
      public static string[] GetFiles(string path, bool isRecursive = false, params string[] extensions)
      {
         if (isWebPlatform && !isEditor)
         {
            Debug.LogWarning("'GetFiles' is not supported for the current platform!");
         }
         else if (isWSABasedPlatform && !isEditor)
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
            Debug.LogWarning($"'GetFiles' under UWP (WSA) is supported in combination with 'File Browser PRO'. For more, please see: {BaseConstants.ASSET_FB}");
#endif
         }
         else
         {
            if (!string.IsNullOrEmpty(path))
            {
               try
               {
                  string _path = ValidatePath(path);

                  if (extensions == null || extensions.Length == 0 || extensions.Any(extension => extension.Equals("*") || extension.Equals("*.*")))
                  {
#if NET_4_6 || NET_STANDARD_2_0
                     return System.IO.Directory.EnumerateFiles(_path, "*", isRecursive
                        ? System.IO.SearchOption.AllDirectories
                        : System.IO.SearchOption.TopDirectoryOnly).ToArray();
#else
                     return System.IO.Directory.GetFiles(_path, "*",
                        isRecursive
                           ? System.IO.SearchOption.AllDirectories
                           : System.IO.SearchOption.TopDirectoryOnly);
#endif
                  }

                  System.Collections.Generic.List<string> files = new System.Collections.Generic.List<string>();

                  foreach (string extension in extensions)
                  {
                     files.AddRange(System.IO.Directory.EnumerateFiles(_path, $"*.{extension}", isRecursive
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

         return new string[0];
      }

      /// <summary>
      /// Find directories inside.
      /// </summary>
      /// <param name="path">Path to find the directories</param>
      /// <param name="isRecursive">Recursive search (default: false, optional)</param>
      /// <returns>Returns array of the found directories inside the path. Zero length array when an error occured.</returns>
      public static string[] GetDirectories(string path, bool isRecursive = false)
      {
         if (isWebPlatform && !isEditor)
         {
            Debug.LogWarning("'GetDirectories' is not supported for the current platform!");
         }
         else if (isWSABasedPlatform && !isEditor)
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
            Debug.LogWarning($"'GetDirectories' under UWP (WSA) is supported in combination with 'File Browser PRO'. For more, please see: {BaseConstants.ASSET_FB}");
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

         return new string[0];
      }

      /// <summary>
      /// Find all logical drives.
      /// </summary>
      /// <returns>Returns array of the found drives. Zero length array when an error occured.</returns>
      public static string[] GetDrives() //TODO replace with "Util.Helper.GetDrives" in the next version
      {
         if (isWebPlatform && !isEditor)
         {
            Debug.LogWarning("'GetDrives' is not supported for the current platform!");
         }
         else if (isWSABasedPlatform && !isEditor)
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
            Debug.LogWarning($"'GetDrives' under UWP (WSA) is supported in combination with 'File Browser PRO'. For more, please see: {BaseConstants.ASSET_FB}");
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

         return new string[0];
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

      /// <summary>Validates a given file.</summary>
      /// <param name="path">File to validate</param>
      /// <returns>Valid file path</returns>
      public static string ValidURLFromFilePath(string path)
      {
         if (!string.IsNullOrEmpty(path))
         {
            if (!isValidURL(path))
               return BaseConstants.PREFIX_FILE + System.Uri.EscapeUriString(ValidateFile(path).Replace('\\', '/'));

            return System.Uri.EscapeUriString(ValidateFile(path).Replace('\\', '/'));
         }

         return path;
      }

      /// <summary>Cleans a given URL.</summary>
      /// <param name="url">URL to clean</param>
      /// <param name="removeProtocol">Remove the protocol, e.g. http:// (default: true, optional).</param>
      /// <param name="removeWWW">Remove www (default: true, optional).</param>
      /// <param name="removeSlash">Remove slash at the end (default: true, optional)</param>
      /// <returns>Clean URL</returns>
      public static string CleanUrl(string url, bool removeProtocol = true, bool removeWWW = true,
         bool removeSlash = true)
      {
         string result = url?.Trim();

         if (!string.IsNullOrEmpty(url))
         {
            if (removeProtocol)
            {
               result = result.Substring(result.CTIndexOf("//") + 2);
            }

            if (removeWWW)
            {
               result = result.CTReplace("www.", string.Empty);
            }

            if (removeSlash && result.CTEndsWith(BaseConstants.PATH_DELIMITER_UNIX))
            {
               result = result.Substring(0, result.Length - 1);
            }

            /*
               if (urlTemp.StartsWith("http://"))
               {
                   result = urlTemp.Substring(7);
               }
               else if (urlTemp.StartsWith("https://"))
               {
                   result = urlTemp.Substring(8);
               }
               else
               {
                   result = urlTemp;
               }
   
               if (result.StartsWith("www."))
               {
                   result = result.Substring(4);
               }
               */
         }

         return result;
      }

      /// <summary>Cleans a given text from tags.</summary>
      /// <param name="text">Text to clean.</param>
      /// <returns>Clean text without tags.</returns>
      public static string ClearTags(string text)
      {
         return text != null ? cleanTagsRegex.Replace(text, string.Empty).Trim() : null;
      }

      /// <summary>Cleans a given text from multiple spaces.</summary>
      /// <param name="text">Text to clean.</param>
      /// <returns>Clean text without multiple spaces.</returns>
      public static string ClearSpaces(string text)
      {
         return text != null ? cleanSpacesRegex.Replace(text, " ").Trim() : null;
      }

      /// <summary>Cleans a given text from line endings.</summary>
      /// <param name="text">Text to clean.</param>
      /// <returns>Clean text without line endings.</returns>
      public static string ClearLineEndings(string text)
      {
         return text != null ? BaseConstants.REGEX_LINEENDINGS.Replace(text, string.Empty).Trim() : null;
      }

      /// <summary>Split the given text to lines and return it as list.</summary>
      /// <param name="text">Complete text fragment</param>
      /// <param name="ignoreCommentedLines">Ignore commente lines (default: true, optional)</param>
      /// <param name="skipHeaderLines">Number of skipped header lines (default: 0, optional)</param>
      /// <param name="skipFooterLines">Number of skipped footer lines (default: 0, optional)</param>
      /// <returns>Splitted lines as array</returns>
      public static System.Collections.Generic.List<string> SplitStringToLines(string text,
         bool ignoreCommentedLines = true, int skipHeaderLines = 0, int skipFooterLines = 0)
      {
         System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>(100);

         if (string.IsNullOrEmpty(text))
         {
            Debug.LogWarning("Parameter 'text' is null or empty => 'SplitStringToLines()' will return an empty string list.");
         }
         else
         {
            string[] lines = BaseConstants.REGEX_LINEENDINGS.Split(text);

            for (int ii = 0; ii < lines.Length; ii++)
            {
               if (ii + 1 > skipHeaderLines && ii < lines.Length - skipFooterLines)
               {
                  if (!string.IsNullOrEmpty(lines[ii]))
                  {
                     if (ignoreCommentedLines)
                     {
                        if (!lines[ii].CTStartsWith("#")) //valid and not disabled line?
                           result.Add(lines[ii]);
                     }
                     else
                     {
                        result.Add(lines[ii]);
                     }
                  }
               }
            }
         }

         return result;
      }

      /// <summary>Format byte-value to Human-Readable-Form.</summary>
      /// <returns>Formatted byte-value in Human-Readable-Form.</returns>
      public static string FormatBytesToHRF(long bytes)
      {
         string[] sizes = {"B", "KB", "MB", "GB", "TB"};
         double len = bytes;
         int order = 0;
         while (len >= 1024 && order < sizes.Length - 1)
         {
            order++;
            len /= 1024;
         }

         // Adjust the format string to your preferences.
         return $"{len:0.##} {sizes[order]}";
      }

      /// <summary>Format seconds to Human-Readable-Form.</summary>
      /// <returns>Formatted seconds in Human-Readable-Form.</returns>
      public static string FormatSecondsToHourMinSec(double seconds)
      {
         int totalSeconds = (int)seconds;
         int calcSeconds = totalSeconds % 60;

         if (seconds >= 86400)
         {
            int calcDays = totalSeconds / 86400;
            int calcHours = (totalSeconds -= calcDays * 86400) / 3600;
            int calcMinutes = (totalSeconds - calcHours * 3600) / 60;

            return $"{calcDays}d {calcHours}:{addLeadingZero(calcMinutes)}:{addLeadingZero(calcSeconds)}";
         }

         if (seconds >= 3600)
         {
            int calcHours = totalSeconds / 3600;
            int calcMinutes = (totalSeconds - calcHours * 3600) / 60;

            return $"{calcHours}:{addLeadingZero(calcMinutes)}:{addLeadingZero(calcSeconds)}";
         }
         else
         {
            int calcMinutes = totalSeconds / 60;

            return $"{calcMinutes}:{addLeadingZero(calcSeconds)}";
         }
      }

      /// <summary>
      /// Generate nice HSV colors.
      /// Based on https://gist.github.com/rje/6206099
      /// </summary>
      /// <param name="h">Hue</param>
      /// <param name="s">Saturation</param>
      /// <param name="v">Value</param>
      /// <param name="a">Alpha (optional)</param>
      /// <returns>True if the current platform is supported.</returns>
      public static Color HSVToRGB(float h, float s, float v, float a = 1f)
      {
         if (s < BaseConstants.FLOAT_TOLERANCE)
            return new Color(v, v, v, a);

         float _h = h / 60f;
         int sector = Mathf.FloorToInt(_h);
         float fact = _h - sector;
         float p = v * (1f - s);
         float q = v * (1f - s * fact);
         float t = v * (1f - s * (1f - fact));

         switch (sector)
         {
            case 0:
               return new Color(v, t, p, a);
            case 1:
               return new Color(q, v, p, a);
            case 2:
               return new Color(p, v, t, a);
            case 3:
               return new Color(p, q, v, a);
            case 4:
               return new Color(t, p, v, a);
            default:
               return new Color(v, p, q, a);
         }
      }

      /// <summary>Checks if the URL is valid.</summary>
      /// <param name="url">URL to check</param>
      /// <returns>True if the URL is valid.</returns>
      public static bool isValidURL(string url)
      {
         return !string.IsNullOrEmpty(url) &&
                (url.StartsWith(file_prefix, System.StringComparison.OrdinalIgnoreCase) ||
                 url.StartsWith(BaseConstants.PREFIX_HTTP, System.StringComparison.OrdinalIgnoreCase) ||
                 url.StartsWith(BaseConstants.PREFIX_HTTPS, System.StringComparison.OrdinalIgnoreCase));
      }

      /// <summary>Copy or move a directory.</summary>
      /// <param name="sourcePath">Source directory path</param>
      /// <param name="destPath">Destination directory path</param>
      /// <param name="move">Move directory instead of copy (default: false, optional)</param>
      public static void CopyPath(string sourcePath, string destPath, bool move = false)
      {
         if ((isWSABasedPlatform || isWebPlatform) && !isEditor)
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
                        if (BaseConstants.DEV_DEBUG)
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
         if ((isWSABasedPlatform || isWebPlatform) && !isEditor)
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
                        if (BaseConstants.DEV_DEBUG)
                           Debug.LogWarning($"Overwrite destination file: {destFile}");

                        System.IO.File.Delete(destFile);
                     }

                     if (move)
                     {
#if UNITY_STANDALONE || UNITY_EDITOR
                        System.IO.File.Move(sourceFile, destFile);
#else
                        System.IO.File.Copy(sourceFile, destFile);
                        System.IO.File.Delete(destFile);
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
         if (isStandalonePlatform || isEditor)
         {
#if UNITY_STANDALONE || UNITY_EDITOR
            string path;

            if (string.IsNullOrEmpty(file) || file.Equals("."))
            {
               path = ".";
            }
            else if ((isWindowsPlatform || isWindowsEditor) && file.Length < 4)
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
#if ENABLE_IL2CPP && CT_PROC
                  using (CTProcess process = new CTProcess())
                  {
                     process.StartInfo.Arguments = $"\"{path}\"";

                     if (isWindowsPlatform || isWindowsEditor)
                     {
                        process.StartInfo.FileName = "explorer.exe";
                        process.StartInfo.UseCmdExecute = true;
                        process.StartInfo.CreateNoWindow = true;
                     }
                     else if (isMacOSPlatform || isMacOSEditor)
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
                  System.Diagnostics.Process.Start(path);
#endif
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
         if (isStandalonePlatform || isEditor)
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

                     if (isWindowsPlatform || isWindowsEditor)
                     {
                        process.StartInfo.FileName = "explorer.exe";
                        process.StartInfo.UseCmdExecute = true;
                        process.StartInfo.CreateNoWindow = true;
                     }
                     else if (isMacOSPlatform || isMacOSEditor)
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
                     if (isMacOSPlatform || isMacOSEditor)
                     {
                        process.StartInfo.FileName = "open";
                        process.StartInfo.WorkingDirectory =
                           System.IO.Path.GetDirectoryName(file) + BaseConstants.PATH_DELIMITER_UNIX;
                        process.StartInfo.Arguments = $"-t \"{System.IO.Path.GetFileName(file)}\"";
                     }
                     else if (isLinuxPlatform || isLinuxEditor)
                     {
                        process.StartInfo.FileName = "xdg-open";
                        process.StartInfo.WorkingDirectory =
                           System.IO.Path.GetDirectoryName(file) + BaseConstants.PATH_DELIMITER_UNIX;
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

      /// <summary>Returns the IP of a given host name.</summary>
      /// <param name="host">Host name</param>
      /// <returns>IP of a given host name.</returns>
      public static string getIP(string host)
      {
         if (!string.IsNullOrEmpty(host))
         {
#if !UNITY_WSA && !UNITY_WEBGL && !UNITY_XBOXONE
            try
            {
               return System.Net.Dns.GetHostAddresses(host)[0].ToString();
            }
            catch (System.Exception ex)
            {
               Debug.LogWarning($"Could not resolve host '{host}': {ex}");
            }
#else
            Debug.LogWarning("'getIP' doesn't work in WebGL or WSA! Returning original string.");
#endif
         }
         else
         {
            Debug.LogWarning("Host name is null or empty - can't resolve to IP!");
         }

         return host;
      }

      /// <summary>Generates a "Lorem Ipsum" based on various parameters.</summary>
      /// <param name="length">Length of the text</param>
      /// <param name="minSentences">Minimum number of sentences for the text (default: 1, optional)</param>
      /// <param name="maxSentences">Maximal number of sentences for the text (default: int.MaxValue, optional)</param>
      /// <param name="minWords">Minimum number of words per sentence (default: 1, optional)</param>
      /// <param name="maxWords">Maximal number of words per sentence (default: 15, optional)</param>
      /// <returns>"Lorem Ipsum" based on the given parameters.</returns>
      public static string GenerateLoremIpsum(int length, int minSentences = 1, int maxSentences = int.MaxValue, int minWords = 1, int maxWords = 15)
      {
         string[] words =
         {
            "lorem", "ipsum", "dolor", "sit", "amet", "consectetuer", "adipiscing", "elit", "sed", "diam", "nonummy", "nibh", "euismod", "tincidunt", "ut", "laoreet", "dolore", "magna", "aliquam", "erat"
         };

         int numSentences = rnd.Next(maxSentences - minSentences) + minSentences + 1;

         System.Text.StringBuilder result = new System.Text.StringBuilder();

         for (int s = 0; s < numSentences && result.Length <= length; s++)
         {
            int numWords = rnd.Next(maxWords - minWords) + minWords + 1;
            for (int w = 0; w < numWords && result.Length <= length; w++)
            {
               if (w > 0)
                  result.Append(" ");

               result.Append(w == 0 ? words[rnd.Next(words.Length)].CTToTitleCase() : words[rnd.Next(words.Length)]);
            }

            result.Append(". ");
         }

         string text = result.ToString();

         if (length > 0 && text.Length > length)
            text = text.Substring(0, length - 1) + ".";

         return text;
      }

      /// <summary>Converts a SystemLanguage to an ISO639-1 code. Returns "en" if the SystemLanguage could not be converted.</summary>
      /// <param name="language">SystemLanguage to convert.</param>
      /// <returns>"ISO639-1 code for the given SystemLanguage.</returns>
      public static string LanguageToISO639(SystemLanguage language)
      {
         switch (language)
         {
            case SystemLanguage.Afrikaans:
               return "af";
            case SystemLanguage.Arabic:
               return "ar";
            case SystemLanguage.Basque:
               return "eu";
            case SystemLanguage.Belarusian:
               return "be";
            case SystemLanguage.Bulgarian:
               return "bg";
            case SystemLanguage.Catalan:
               return "ca";
            case SystemLanguage.ChineseSimplified:
            case SystemLanguage.ChineseTraditional:
            case SystemLanguage.Chinese:
               return "zh";
            case SystemLanguage.Czech:
               return "cs";
            case SystemLanguage.Danish:
               return "da";
            case SystemLanguage.Dutch:
               return "nl";
            case SystemLanguage.English:
               return "en";
            case SystemLanguage.Estonian:
               return "et";
            case SystemLanguage.Faroese:
               return "fo";
            case SystemLanguage.Finnish:
               return "fi";
            case SystemLanguage.French:
               return "fr";
            case SystemLanguage.German:
               return "de";
            case SystemLanguage.Greek:
               return "el";
            case SystemLanguage.Hebrew:
               return "he";
            case SystemLanguage.Hungarian:
               return "hu";
            case SystemLanguage.Icelandic:
               return "is";
            case SystemLanguage.Indonesian:
               return "id";
            case SystemLanguage.Italian:
               return "it";
            case SystemLanguage.Japanese:
               return "ja";
            case SystemLanguage.Korean:
               return "ko";
            case SystemLanguage.Latvian:
               return "lv";
            case SystemLanguage.Lithuanian:
               return "lt";
            case SystemLanguage.Norwegian:
               return "no";
            case SystemLanguage.Polish:
               return "pl";
            case SystemLanguage.Portuguese:
               return "pt";
            case SystemLanguage.Romanian:
               return "ro";
            case SystemLanguage.Russian:
               return "ru";
            case SystemLanguage.SerboCroatian:
               return "sh";
            case SystemLanguage.Slovak:
               return "sk";
            case SystemLanguage.Slovenian:
               return "sl";
            case SystemLanguage.Spanish:
               return "es";
            case SystemLanguage.Swedish:
               return "sv";
            case SystemLanguage.Thai:
               return "th";
            case SystemLanguage.Turkish:
               return "tr";
            case SystemLanguage.Ukrainian:
               return "uk";
            case SystemLanguage.Vietnamese:
               return "vi";
            default:
               return "en";
         }
      }

      /// <summary>Converts an ISO639-1 code to a SystemLanguage. Returns SystemLanguage.English if the code could not be converted.</summary>
      /// <param name="isoCode">ISO639-1 code to convert.</param>
      /// <returns>"SystemLanguage for the given ISO639-1 code.</returns>
      public static SystemLanguage ISO639ToLanguage(string isoCode)
      {
         if (!string.IsNullOrEmpty(isoCode) && isoCode.Length >= 2)
         {
            string code = isoCode.Substring(0, 2).ToLower();

            switch (code)
            {
               case "af":
                  return SystemLanguage.Afrikaans;
               case "ar":
                  return SystemLanguage.Arabic;
               case "eu":
                  return SystemLanguage.Basque;
               case "be":
                  return SystemLanguage.Belarusian;
               case "bg":
                  return SystemLanguage.Bulgarian;
               case "ca":
                  return SystemLanguage.Catalan;
               case "zh":
                  return SystemLanguage.Chinese;
               case "cs":
                  return SystemLanguage.Czech;
               case "da":
                  return SystemLanguage.Danish;
               case "nl":
                  return SystemLanguage.Dutch;
               case "en":
                  return SystemLanguage.English;
               case "et":
                  return SystemLanguage.Estonian;
               case "fo":
                  return SystemLanguage.Faroese;
               case "fi":
                  return SystemLanguage.Finnish;
               case "fr":
                  return SystemLanguage.French;
               case "de":
                  return SystemLanguage.German;
               case "el":
                  return SystemLanguage.Greek;
               case "he":
                  return SystemLanguage.Hebrew;
               case "hu":
                  return SystemLanguage.Hungarian;
               case "is":
                  return SystemLanguage.Icelandic;
               case "id":
                  return SystemLanguage.Indonesian;
               case "it":
                  return SystemLanguage.Italian;
               case "ja":
                  return SystemLanguage.Japanese;
               case "ko":
                  return SystemLanguage.Korean;
               case "lv":
                  return SystemLanguage.Latvian;
               case "lt":
                  return SystemLanguage.Lithuanian;
               case "no":
                  return SystemLanguage.Norwegian;
               case "pl":
                  return SystemLanguage.Polish;
               case "pt":
                  return SystemLanguage.Portuguese;
               case "ro":
                  return SystemLanguage.Romanian;
               case "ru":
                  return SystemLanguage.Russian;
               case "sh":
                  return SystemLanguage.SerboCroatian;
               case "sk":
                  return SystemLanguage.Slovak;
               case "sl":
                  return SystemLanguage.Slovenian;
               case "es":
                  return SystemLanguage.Spanish;
               case "sv":
                  return SystemLanguage.Swedish;
               case "th":
                  return SystemLanguage.Thai;
               case "tr":
                  return SystemLanguage.Turkish;
               case "uk":
                  return SystemLanguage.Ukrainian;
               case "vi":
                  return SystemLanguage.Vietnamese;
               default:
                  return SystemLanguage.English;
            }
         }

         return SystemLanguage.English;
      }

      #endregion


      #region Private methods

      private static string addLeadingZero(int value)
      {
         return value < 10 ? "0" + value : value.ToString();
      }

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

      private static void openURL(string url)
      {
#if !UNITY_EDITOR && UNITY_WEBGL
         openURLPlugin(url);
#else
         Application.OpenURL(url);
#endif
      }
/*
      private static void openURLJS(string url)
      {
         Application.ExternalEval("window.open('" + url + "');");
      }
*/
#if !UNITY_EDITOR && UNITY_WEBGL
      private static void openURLPlugin(string url)
      {
		   ctOpenWindow(url);
      }

      [System.Runtime.InteropServices.DllImportAttribute("__Internal")]
      private static extern void ctOpenWindow(string url);
#endif

      #endregion

      // StringHelper
      /*
      public static byte[] GetBytesFromText(string text) {
       return new UnicodeEncoding().GetBytes(text);
    }

    public static string GetTextFromBytes(byte[] bytes) {
       return new UnicodeEncoding().GetString(bytes, 0, bytes.Length);
    }

    public static byte[] GetBytesFromBase64(string text) {
       return Convert.FromBase64String(text);
    }

    public static string GetBase64FromBytes(byte[] bytes) {
       return Convert.ToBase64String(bytes);
    }
      */


      // MathHelper
      /*
      public static bool IsInRange(float actValue, float refValue, float range) {
       
       return (actValue >= refValue-range) && (actValue <= refValue+range);
    }


    public static bool IsInRange(int actValue, int refValue, int range) {
       
       return (actValue >= refValue-range) && (actValue <= refValue+range);
    }
      */


      // Add Math3dHelper?


      // Color Helper
      /*
      public static Color HexToColor(string hex)
      {
          if (string.IsNullOrEmpty(hex))
              throw new ArgumentNullException("hex");

          byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
          byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
          byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
          return new Color32(r, g, b, 255);
      }
      */
   }
}
// © 2015-2021 crosstales LLC (https://www.crosstales.com)