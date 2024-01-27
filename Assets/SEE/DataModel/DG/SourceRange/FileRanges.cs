using UnityEngine;

namespace SEE.DataModel.DG.SourceRange
{
    /// <summary>
    /// A representation of the source-code ranges of a file. This
    /// data structure is used at the top level of the index.
    /// </summary>
    internal class FileRanges
    {
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
            int? sourceLine = node.SourceLine;
            if (sourceLine.HasValue)
            {
                Range descendant = Find(sourceLine.Value);
                if (descendant == null)
                {
                    int? endLine = node.EndLine();
                    if (endLine.HasValue)
                    {
                        Children.Add(new Range(sourceLine.Value, endLine.Value, node));
                    }
                    else
                    {
                        Debug.LogWarning($"{node.ID} does not have an end line. Will be ignored.\n");
                    }
                }
                else
                {
                    descendant.Add(node);
                }
            }
            else
            {
                Debug.LogWarning($"{node.ID} does not have a source line. Will be ignored.\n");
            }
        }

        /// <summary>
        /// Returns the innermost source-code range enclosing the given
        /// source <paramref name="line"/>. If no such range exists,
        /// <c>null</c> is returned.
        /// </summary>
        /// <param name="line">source line to be searched for</param>
        /// <returns>innermost source-code range or <c>null</c></returns>
        public Range Find(int line)
        {
            if (Children.TryGetValue(line, out Range range))
            {
                // We are looking for the innermost range, hence we need to
                // recurse into the Children of the found range.
                Range child = range.Find(line);
                return child ?? range;
            }
            else
            {
                return null;
            }
        }
    }
}
