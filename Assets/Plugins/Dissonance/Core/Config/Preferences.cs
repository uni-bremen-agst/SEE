using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dissonance.Config
{
    internal static class Preferences
    {
        /// <summary>
        /// Get a user preferences
        /// </summary>
        /// <typeparam name="T">Type of the preference</typeparam>
        /// <param name="key">Key to get the preference by</param>
        /// <param name="output">field to store the preference in (with the default value already in it)</param>
        /// <param name="get">Given the string and the current value, get the new value</param>
        /// <param name="log">Logger instance</param>
        public static void Get<T>(string key, ref T output, Func<string, T, T> get, Log log)
        {
#if !NCRUNCH
            if (PlayerPrefs.HasKey(key))
            {
                output = get(key, output);
                log.Debug("Loaded Pref {0} = {1}", key, output);
            }
#endif
        }

        /// <summary>
        /// Set a user preference
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Type of the preference</param>
        /// <param name="field">Field to set the value into</param>
        /// <param name="value">New value of the preferences</param>
        /// <param name="save">Saves the given value with the given key</param>
        /// <param name="log">Optional Logger</param>
        /// <param name="equality"></param>
        /// <param name="setAtRuntime">If this is not true calling this method while the game is running will throw</param>
        public static void Set<T>(string key, ref T field, T value, Action<string, T> save, Log log, IEqualityComparer<T> equality = null, bool setAtRuntime = true)
        {
            if (!setAtRuntime && Application.isPlaying)
                throw log.CreatePossibleBugException(string.Format("Attempted to set pref '{0}' but this cannot be set at runtime", key), "28579FE7-72D7-4516-BF04-BE96B11BB0C7");

            if (equality == null)
                equality = EqualityComparer<T>.Default;

            //No need to do anything if the value is unchanged
            if (equality.Equals(field, value))
                return;
            field = value;

#if !NCRUNCH
            save(key, value);
            log.Info("Saved Pref {0} = {1}", key, value);

            PlayerPrefs.Save();
#endif
        }

        internal static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, Convert.ToInt32(value));
        }

        internal static bool GetBool(string key, bool defaultValue)
        {
            return Convert.ToBoolean(PlayerPrefs.GetInt(key, Convert.ToInt32(defaultValue)));
        }
    }
}
