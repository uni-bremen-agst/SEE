using SEE.Utils.Config;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// A dictionary for inclusive and exclusive path globbings.
    ///
    /// If the value of a key is true, the key is considered an inclusion pattern;
    /// otherwise an exclusion pattern.
    ///
    /// A path matches the globbing if it matches at least one inclusion pattern and
    /// does not match any exclusion pattern.
    /// </summary>
    public class Globbing : Dictionary<string, bool>
    {
        #region Config IO

        /// <summary>
        /// The label for a key (the path) in the configuration file.
        /// </summary>
        private const string pathLabel = "Path";

        /// <summary>
        /// The label for a key (the path) in the configuration file.
        /// </summary>
        private const string isIncludeLabel = "IsInclude";

        /// <summary>
        /// Saves the attributes of this <see cref="Globbing"/> using <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to save the attributes.</param>
        /// <param name="label">The label under which this <see cref="Globbing"/> is to be saved.</param>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginList(label);
            foreach (KeyValuePair<string, bool> pattern in this)
            {
                writer.BeginGroup();
                writer.Save(pattern.Key, pathLabel);
                writer.Save(pattern.Value, isIncludeLabel);
                writer.EndGroup();
            }
            writer.EndList();
        }

        /// <summary>
        /// Restores the attributes of this <see cref="Globbing"/> from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">The attributes to restore from.</param>
        /// <param name="label">The label under which to look up the values in <paramref name="attributes"/>.</param>
        /// <returns>True if <paramref name="attributes"/> contained a <see cref="Globbing"/>
        /// for the given <paramref name="label"/>.</returns>
        public bool Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object list))
            {
                foreach (object item in list as List<object>)
                {
                    // Each item in the list is a dictionary holding the pair of (string, bool).
                    Dictionary<string, object> dict = item as Dictionary<string, object>;
                    // string path
                    string path = null;
                    if (!ConfigIO.Restore(dict, pathLabel, ref path))
                    {
                        Debug.LogError($"Entry of {typeof(Globbing)} has no value for {pathLabel}\n");
                        continue;
                    }
                    // bool isInclude
                    bool isInclude = false;
                    if (!ConfigIO.Restore(dict, isIncludeLabel, ref isInclude))
                    {
                        Debug.LogError($"Entry of {typeof(Globbing)} has no value for {isIncludeLabel}\n");
                        continue;
                    }
                    this[path] = isInclude;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion Config IO
    }
}
