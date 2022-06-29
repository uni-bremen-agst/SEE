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

      //public static readonly System.Globalization.CultureInfo BaseCulture = new System.Globalization.CultureInfo("en-US");
      public static readonly System.Globalization.CultureInfo BaseCulture = new System.Globalization.CultureInfo(LanguageToISO639(Application.systemLanguage));

      protected static readonly System.Random rnd = new System.Random();

      public static bool ApplicationIsPlaying = Application.isPlaying;

      private static string[] args;

      private static int androidAPILevel = 0;

      #endregion


      #region Properties

      #region Platforms

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

      #endregion

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
      public static Crosstales.Common.Model.Enum.Platform CurrentPlatform
      {
         get
         {
            if (isWindowsPlatform)
               return Crosstales.Common.Model.Enum.Platform.Windows;

            if (isMacOSPlatform)
               return Crosstales.Common.Model.Enum.Platform.OSX;

            if (isLinuxPlatform)
               return Crosstales.Common.Model.Enum.Platform.Linux;

            if (isAndroidPlatform)
               return Crosstales.Common.Model.Enum.Platform.Android;

            if (isIOSBasedPlatform)
               return Crosstales.Common.Model.Enum.Platform.IOS;

            if (isWSABasedPlatform)
               return Crosstales.Common.Model.Enum.Platform.WSA;

            return isWebPlatform ? Crosstales.Common.Model.Enum.Platform.Web : Crosstales.Common.Model.Enum.Platform.Unsupported;
         }
      }

      /// <summary>Returns the Android API level of the current device (Android only)".</summary>
      /// <returns>The Android API level of the current device.</returns>
      public static int AndroidAPILevel
      {
         get
         {
#if UNITY_ANDROID
            if (androidAPILevel == 0)
               androidAPILevel = int.Parse(SystemInfo.operatingSystem.Substring(SystemInfo.operatingSystem.IndexOf("-") + 1, 3));
#endif
            return androidAPILevel;
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
            string[] lines = Crosstales.Common.Util.BaseConstants.REGEX_LINEENDINGS.Split(text);

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
/*
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
*/

      /// <summary>Format byte-value to Human-Readable-Form.</summary>
      /// <param name="bytes">Value in bytes</param>
      /// <param name="useSI">Use SI-system (default: false, optional)</param>
      /// <returns>Formatted byte-value in Human-Readable-Form.</returns>
      public static string FormatBytesToHRF(long bytes, bool useSI = false)
      {
         const string ci = "kMGTPE";
         int index = 0;

         if (useSI)
         {
            if (-1000 < bytes && bytes < 1000)
               return bytes + " B";

            while (bytes <= -999_950 || bytes >= 999_950)
            {
               bytes /= 1000;
               index++;
            }

            return $"{(bytes / 1000f):N2} {ci[index]}B";
         }

         long absB = bytes == long.MinValue ? long.MaxValue : System.Math.Abs(bytes);
         if (absB < 1024)
            return bytes + " B";

         long value = absB;

         for (int i = 40; i >= 0 && absB > 0xfffccccccccccccL >> i; i -= 10)
         {
            value >>= 10;
            index++;
         }

         value *= System.Math.Sign(bytes);

         return $"{(value / 1024f):N2} {ci[index]}iB";
      }

      /// <summary>Format seconds to Human-Readable-Form.</summary>
      /// <param name="seconds">Value in seconds</param>
      /// <returns>Formatted seconds in Human-Readable-Form.</returns>
      public static string FormatSecondsToHRF(double seconds)
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
         if (s < Crosstales.Common.Util.BaseConstants.FLOAT_TOLERANCE)
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

/*
      /// <summary>Generates a string of all latin latin characters (ABC...xyz).</summary>
      /// <returns>"String of all latin latin characters.</returns>
      public static string GenerateLatinABC()
      {
         return GenerateLatinUppercaseABC() + GenerateLatinLowercaseABC();
      }

      /// <summary>Generates a string of all latin latin characters in uppercase (ABC...XYZ).</summary>
      /// <returns>"String of all latin latin characters in uppercase.</returns>
      public static string GenerateLatinUppercaseABC()
      {
         System.Text.StringBuilder result = new System.Text.StringBuilder();

         for (int ii = 65; ii <= 90; ii++)
         {
            result.Append((char)ii);
         }

         return result.ToString();
      }

      /// <summary>Generates a string of all latin latin characters in lowercase (abc...xyz).</summary>
      /// <returns>"String of all latin latin characters in lowercase.</returns>
      public static string GenerateLatinLowercaseABC()
      {
         System.Text.StringBuilder result = new System.Text.StringBuilder();

         for (int ii = 97; ii <= 122; ii++)
         {
            result.Append((char)ii);
         }

         return result.ToString();
      }
*/
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

      /// <summary>Invokes a public static method on a full qualified class.</summary>
      /// <param name="className">Full qualified name of the class</param>
      /// <param name="methodName">Public static method of the class to execute</param>
      /// <param name="parameters">Parameters for the method (optional)</param>
      public static object InvokeMethod(string className, string methodName, params object[] parameters)
      {
         if (string.IsNullOrEmpty(className))
         {
            Debug.LogWarning("'className' is null or empty; can not execute.");
            return null;
         }

         if (string.IsNullOrEmpty(methodName))
         {
            Debug.LogWarning("'methodName' is null or empty; can not execute.");
            return null;
         }

         foreach (System.Type type in System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()))
         {
            try
            {
               if (type.FullName?.Equals(className) == true)
                  if (type.IsClass)
                     return type.GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Invoke(null, parameters);
            }
            catch (System.Exception ex)
            {
               Debug.LogWarning($"Could not execute method call: {ex}");
            }
         }

         return null;
      }

      /// <summary>Returns an argument for a name from the url or command line.</summary>
      /// <param name="name">Name for the argument</param>
      /// <returns>Argument for a name from the url or command line.</returns>
      public static string GetArgument(string name)
      {
         if (!string.IsNullOrEmpty(name))
         {
            string[] args = GetArguments();

            for (int ii = 0; ii < args.Length; ii++)
            {
               if (name.CTEquals(args[ii]) && args.Length > ii + 1)
                  return args[ii + 1];
            }
         }

         return null;
      }

      /// <summary>Returns all arguments from the url or command line.</summary>
      /// <returns>Arguments from the url or command line.</returns>
      public static string[] GetArguments()
      {
         if (args == null)
         {
#if UNITY_WEBGL && !UNITY_EDITOR
//#if (UNITY_WEBGL || UNITY_ANDROID) && !UNITY_EDITOR
            // url with parameters syntax : http://example.com?arg1=value1&arg2=value2
            string parameters = Application.absoluteURL.Substring(Application.absoluteURL.IndexOf("?") + 1);

            //Debug.Log("URL parameters: " + parameters);
            args = parameters.Split(new char[] { '&', '=' });
#else
            args = System.Environment.GetCommandLineArgs();
#endif
         }

         return args;
      }

      #endregion


      #region Private methods

      private static string addLeadingZero(int value)
      {
         return value < 10 ? "0" + value : value.ToString();
      }

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
// © 2015-2022 crosstales LLC (https://www.crosstales.com)