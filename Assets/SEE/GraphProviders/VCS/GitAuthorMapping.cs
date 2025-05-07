using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// Represents a mapping between a <see cref="GitFileAuthor"/> and a list of aliases or associated GitFileAuthors.
    /// This can be used in grouping similar or equivalent author identities and simplifying the graph.
    /// </summary>
    [Serializable]
    public class GitAuthorMapping : Dictionary<GitFileAuthor, List<GitFileAuthor>> { }
}
