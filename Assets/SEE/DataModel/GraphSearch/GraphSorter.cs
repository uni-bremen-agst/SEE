using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;

namespace SEE.DataModel.GraphSearch
{
    /// <summary>
    /// A configurable sorter for graph elements, mainly intended for use with <see cref="GraphSearch"/>.
    /// </summary>
    public class GraphSorter: IGraphModifier
    {
        /// <summary>
        /// The attributes to sort by along with whether to sort descending, in the order of precedence.
        /// </summary>
        public readonly List<(string attribute, bool descending)> SortAttributes = new();

        public IEnumerable<T> Apply<T>(IEnumerable<T> elements) where T : GraphElement
        {
            return SortAttributes.Count == 0
                ? elements
                // The first `OrderBy` call is to get an IOrderedEnumerable<T> that we can repeatedly pass to `ThenBy`.
                // `OrderBy` is stable, so the order is preserved, as we are passing in a constant key.
                : SortAttributes.Aggregate(elements.OrderBy(_ => 0),
                                           (current, sortAttribute) =>
                                           {
                                              (string attribute, bool descending) = sortAttribute;
                                              return descending
                                                ? current.ThenByDescending(e => GetElementKey(e, attribute))
                                                : current.ThenBy(e => GetElementKey(e, attribute));
                                           });

            object GetElementKey(T element, string attribute)
            {
                return element.TryGetAny(attribute, out object value) ? value : null;
            }
        }

        /// <summary>
        /// Whether the given attribute is sorted descending.
        /// Note that this returns null if the attribute is not sorted at all.
        /// </summary>
        /// <param name="attribute">The attribute to check.</param>
        /// <returns>Whether the attribute is sorted descending, or null if it is not sorted at all.</returns>
        public bool? IsAttributeDescending(string attribute)
        {
            (string attribute, bool descending) result = SortAttributes.FirstOrDefault(a => a.attribute == attribute);
            if (result == default)
            {
                return null;
            }
            else
            {
                return result.descending;
            }
        }


        public bool IsActive() => SortAttributes.Count > 0;

        public void Reset() => SortAttributes.Clear();
    }
}
