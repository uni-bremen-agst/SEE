using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance.Editor.Windows
{
    /// <summary>
    /// Get some anonymised metadata about the current editor
    /// </summary>
    internal class EditorMetadata
    {
        /// <summary>
        /// Get an anonymous unique ID for this user
        /// </summary>
        /// <returns></returns>
        [NotNull] private static string UserId()
        {
            var parts = string.Format("{0}={1}", Environment.MachineName, Environment.UserName);

            using (var sha512 = SHA1.Create())
                return Convert.ToBase64String(sha512.ComputeHash(Encoding.UTF8.GetBytes(parts))).Replace("+", "").Replace("/", "");
        }

        /// <summary>
        /// Get the version of the editor
        /// </summary>
        /// <returns></returns>
        [NotNull] private static string UnityVersion()
        {
            return Uri.EscapeUriString(Application.unityVersion);
        }

        [NotNull] internal static string GetQueryString([NotNull] string utmMedium, [CanBeNull] IEnumerable<KeyValuePair<string, string>> parts = null)
        {
            var qStringParts = new[] {
                new { k = "uid", v = UserId() },
                new { k = "uve", v = UnityVersion() },
                new { k = "dve", v = Uri.EscapeUriString(DissonanceComms.Version.ToString()) },
                new { k = "utm_source", v = "unity_editor" },
                new { k = "utm_medium", v = utmMedium },
                new { k = "utm_campaign", v = "unity_editor_dissonance" },
            };

            var extensions = (parts ?? new KeyValuePair<string, string>[0])
                .Select(a => new { k = a.Key, v = a.Value })
                .Concat(qStringParts)
                .Select(a => string.Format("{0}={1}", a.k, a.v))
                .ToArray();

            return "?" + string.Join("&", extensions.ToArray());
        }
    }
}
