using UnityEngine;

namespace SEE.DataModel.DG.GraphIndex
{
    /// <summary>
    /// A representation of the source-code ranges of a file. This
    /// data structure is used at the top level of the index.
    /// </summary>
    internal class FileRanges
    {
        /// <summary>
        /// If true, a warning will be logged in <see cref="Add(Node)"/> if a node does
        /// not have a source-code range.
        /// </summary>
        public static bool ReportMissingSourceRange = true;

        /// <summary>
        /// The children sorted by the SourceLine.
        /// </summary>
        public readonly SortedRanges Children = new();

        /// <summary>
        /// Adds the source-code range of the given <paramref name="node"/>
        /// to the list of children.
        /// </summary>
        /// <param name="node">node whose source-code range is to be added</param>
        public void Add(Node node)
        {
            Range range = node.SourceRange;
            if (range != null)
            {
                SourceRange descendant = Find(range.StartLine);
                if (descendant == null)
                {
                    Children.Add(new SourceRange(range, node));
                }
                else
                {
                    descendant.Add(node);
                }
            }
            else if (ReportMissingSourceRange)
            {
                Debug.LogWarning($"{node.ID} does not have a source range. Will be ignored.\n");
            }
        }

        /// <summary>
        /// Returns the innermost source-code range enclosing the given
        /// source <paramref name="line"/>. If no such range exists,
        /// <c>null</c> is returned.
        /// </summary>
        /// <param name="line">source line to be searched for</param>
        /// <returns>innermost source-code range or <c>null</c></returns>
        public SourceRange Find(int line)
        {
            if (Children.TryGetValue(line, out SourceRange range))
            {
                // We are looking for the innermost range, hence we need to
                // recurse into the Children of the found range.
                SourceRange child = range.Find(line);
                return child ?? range;
            }
            else
            {
                return null;
            }
        }
    }
}
