using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.GraphProviders.VCS
{

    /// <summary>
    /// Represents a mapping between a <see cref="FileAuthor"/> and a list of aliases
    /// of associated <see cref="FileAuthor"/>. This can be used in grouping equivalent
    /// author identities.
    ///
    /// The key is the represenative of the grouped <see cref="FileAuthor"/>s,
    /// while the values are the aliases of the respective <see cref="FileAuthor"/>.
    /// </summary>
    [Serializable]
    public class AuthorMapping : Dictionary<FileAuthor, FileAuthorList>
    {
        #region Config I/O

        /// <summary>
        /// The label of the <see cref="FileAuthor"/> key in the configuration file.
        /// </summary>
        private const string authorLabel = "Author";

        /// <summary>
        /// The label of the aliases of <see cref="FileAuthor"/> (values) in the configuration file.
        /// </summary>
        private const string aliasesLabel = "Aliases";

        internal void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object list))
            {
                foreach (object item in list as List<object>)
                {
                    // Each item in the list is a dictionary holding the pair of (author, aliases).
                    Dictionary<string, object> dict = item as Dictionary<string, object>;
                    // author
                    FileAuthor author = new();
                    if (!author.Restore(dict, authorLabel))
                    {
                        Debug.LogError($"Entry of {typeof(AuthorMapping)} has no value for {authorLabel}\n");
                        continue;
                    }
                    // aliases
                    FileAuthorList aliases = new();
                    aliases.Restore(dict, aliasesLabel);
                    this[author] = aliases;
                }

                Debug.Log(this);
            }
        }

        internal void Save(ConfigWriter writer, string label)
        {
            writer.BeginList(label);
            foreach (var item in this)
            {
                writer.BeginGroup();
                item.Key.Save(writer, authorLabel);
                item.Value.Save(writer, aliasesLabel);
                writer.EndGroup();
            }
            writer.EndList();
        }

        #endregion Config I/O

        public override string ToString()
        {
            string result = string.Empty;
            foreach (var item in this)
            {
                result += item.Key.ToString() + " => " + item.Value.ToString() + "\n";
            }
            return result;
        }
    }
}
