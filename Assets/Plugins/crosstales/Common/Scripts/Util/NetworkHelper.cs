using System.Linq;
using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Base for various helper functions for networking.</summary>
   public abstract class NetworkHelper
   {
      #region Variables

      protected const string file_prefix = "file://";
#if UNITY_ANDROID
      protected const string content_prefix = "content://";
#endif

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

      /// <summary>Validates a given file.</summary>
      /// <param name="path">File to validate</param>
      /// <returns>Valid file path</returns>
      public static string ValidURLFromFilePath(string path)
      {
         if (!string.IsNullOrEmpty(path))
         {
            if (!isValidURL(path))
               return Crosstales.Common.Util.BaseConstants.PREFIX_FILE + System.Uri.EscapeUriString(Crosstales.Common.Util.FileHelper.ValidateFile(path).Replace('\\', '/'));

            return System.Uri.EscapeUriString(Crosstales.Common.Util.FileHelper.ValidateFile(path).Replace('\\', '/'));
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

            if (removeSlash && result.CTEndsWith(Crosstales.Common.Util.BaseConstants.PATH_DELIMITER_UNIX))
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

      /// <summary>Checks if the URL is valid.</summary>
      /// <param name="url">URL to check</param>
      /// <returns>True if the URL is valid.</returns>
      public static bool isValidURL(string url)
      {
         return !string.IsNullOrEmpty(url) &&
                (url.StartsWith(file_prefix, System.StringComparison.OrdinalIgnoreCase) ||
#if UNITY_ANDROID
                 url.StartsWith(content_prefix, System.StringComparison.OrdinalIgnoreCase) ||
#endif
                 url.StartsWith(Crosstales.Common.Util.BaseConstants.PREFIX_HTTP, System.StringComparison.OrdinalIgnoreCase) ||
                 url.StartsWith(Crosstales.Common.Util.BaseConstants.PREFIX_HTTPS, System.StringComparison.OrdinalIgnoreCase));
      }

      /// <summary>Returns the IP of a given host name.</summary>
      /// <param name="host">Host name</param>
      /// <returns>IP of a given host name.</returns>
      public static string GetIP(string host)
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
            Debug.LogWarning("'GetIP' doesn't work in WebGL or WSA! Returning original string.");
#endif
         }
         else
         {
            Debug.LogWarning("Host name is null or empty - can't resolve to IP!");
         }

         return host;
      }

      #endregion


      #region Private methods

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
   }
}
// © 2015-2022 crosstales LLC (https://www.crosstales.com)