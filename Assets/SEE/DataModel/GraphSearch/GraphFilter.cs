using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;

namespace SEE.DataModel.GraphSearch
{
    /// <summary>
    /// A configurable filter for graph elements, mainly intended for use with <see cref="GraphSearch"/>.
    /// </summary>
    public record GraphFilter : IGraphModifier
    {
        /// <summary>
        /// A set of toggle attributes of which at least one must be present in a graph element for it to be included.
        /// </summary>
        public readonly ISet<string> IncludeToggleAttributes = new HashSet<string>();

        /// <summary>
        /// A set of toggle attributes of which none must be present in a graph element for it to be included.
        /// </summary>
        public readonly ISet<string> ExcludeToggleAttributes = new HashSet<string>();

        /// <summary>
        /// Elements that should always be excluded.
        /// </summary>
        public readonly ISet<GraphElement> ExcludeElements = new HashSet<GraphElement>();

        /// <summary>
        /// Whether to include nodes.
        /// </summary>
        public bool IncludeNodes = true;

        /// <summary>
        /// Whether to include edges.
        /// </summary>
        public bool IncludeEdges = true;

        /// <summary>
        /// Returns whether the given element should be included in the search results.
        /// </summary>
        /// <param name="element">The element to check.</param>
        /// <typeparam name="T">The type of the element.</typeparam>
        /// <returns>Whether the element should be included.</returns>
        public bool Includes<T>(T element) where T : GraphElement
        {
            return element switch
                {
                    Node => IncludeNodes,
                    Edge => IncludeEdges,
                    _ => false
                }
                && (IncludeToggleAttributes.Count == 0 || IncludeToggleAttributes.Overlaps(element.ToggleAttributes))
                && !ExcludeElements.Contains(element)
                && !ExcludeToggleAttributes.Overlaps(element.ToggleAttributes);
        }

        /// <summary>
        /// Applies the filter to the given elements, that is, returns only those elements that are included.
        /// </summary>
        /// <param name="elements">The elements to filter.</param>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <returns>The filtered elements.</returns>
        public IEnumerable<T> Apply<T>(IEnumerable<T> elements) where T : GraphElement
        {
            return elements.Where(Includes);
        }

        public bool IsActive() => !IncludeNodes || !IncludeEdges
            || IncludeToggleAttributes.Count > 0 || ExcludeToggleAttributes.Count > 0 || ExcludeElements.Count > 0;

        public void Reset()
        {
            IncludeToggleAttributes.Clear();
            ExcludeToggleAttributes.Clear();
            ExcludeElements.Clear();
            IncludeNodes = true;
            IncludeEdges = true;
        }
    }
}
