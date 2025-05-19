using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// A list of <see cref="FileAuthor"/>s.
    /// </summary>
    [Serializable]
    public class FileAuthorList : List<FileAuthor>
    {
        #region Config I/O
        internal void Save(ConfigWriter writer, string label)
        {
            writer.BeginList(label);
            foreach (FileAuthor author in this)
            {
                author.Save(writer);
            }
            writer.EndList();
        }

        internal void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object list))
            {
                foreach (object item in list as List<object>)
                {
                    // Each item in the list is a dictionary holding the attributes of FileAuthor.
                    Dictionary<string, object> dict = item as Dictionary<string, object>;
                    FileAuthor author = new();
                    if (author.Restore(dict))
                    {
                        Add(author);
                    }
                    else
                    {
                        Debug.LogError($"Entry of {typeof(FileAuthorList)} is incomplete: {author}\n");
                    }
                }
            }
            else
            {
                Debug.LogError($"Entry of {typeof(FileAuthorList)} has no value for {label}\n");
            }
        }
        #endregion Config I/O

        public override string ToString()
        {
            string result = string.Empty;
            foreach (FileAuthor item in this)
            {
                result += item.ToString() + " ";
            }
            return result;
        }
    }
}
