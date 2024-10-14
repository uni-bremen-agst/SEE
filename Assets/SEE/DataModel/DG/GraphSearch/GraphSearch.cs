using System;
using System.Collections.Generic;
using System.Linq;
using FuzzySharp;

namespace SEE.DataModel.DG.GraphSearch
{
    /// <summary>
    /// Allows searching for nodes by their source name.
    /// The graph associated to this search may be dynamic – that is, when the graph changes
    /// (for example, when a node is added), the search index will be updated accordingly.
    /// Searches are fuzzy, i.e., they will return results even if the query does not match the
    /// element's name exactly.
    ///
    /// To perform a search on the associated graph, call <see cref="Search"/>.
    /// </summary>
    public class GraphSearch : IObserver<ChangeEvent>
    {
        /// <summary>
        /// A mapping from names to a list of nodes with that name.
        /// Is constructed in the constructor in order not to have to descend into the graph every
        /// time a search is executed.
        /// </summary>
        private readonly IDictionary<string, List<Node>> elements;

        /// <summary>
        /// The graph to be searched.
        /// </summary>
        public readonly Graph Graph;

        /// <summary>
        /// The filter that is applied to the graph elements when they are searched.
        /// </summary>
        public GraphFilter Filter { get; } = new();

        /// <summary>
        /// The sorter that is applied to the graph elements when they are searched.
        /// </summary>
        public GraphSorter Sorter { get; } = new();

        /// <summary>
        /// Returns all graph modifiers that shall be applied to the search results.
        /// </summary>
        private IEnumerable<IGraphModifier> Modifiers => new IGraphModifier[] { Filter, Sorter };

        /// <summary>
        /// Creates a new instance of <see cref="GraphSearch"/> for the given <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph to be searched.</param>
        public GraphSearch(Graph graph)
        {
            Graph = graph;
            elements = graph.Nodes().GroupBy(ElementToString).ToDictionary(g => g.Key, g => g.ToList());
            graph.Subscribe(this);
        }

        /// <summary>
        /// Performs a fuzzy search for the given <paramref name="query"/> in the graph,
        /// by comparing it to the source name of the nodes.
        /// Case will be ignored, and the query may be a substring of the source name (this is a fuzzy search).
        /// </summary>
        /// <param name="query">The query to be searched for.</param>
        /// <returns>A list of nodes which match the query.</returns>
        public IEnumerable<Node> Search(string query, int limit = 10, int cutoff = 40)
        {
            IEnumerable<(int Score, Node Element)> results = Process.ExtractTop(FilterString(query), elements.Keys, limit: limit, cutoff: cutoff)
                                                                    .SelectMany(x => Modifiers.ApplyAll(elements[x.Value]).Select(element => (x.Score, element)));
            if (!Sorter.IsActive())
            {
                // If we don't sort by any custom attribute, we sort by the fuzzy score.
                results = results.OrderByDescending(x => x.Score);
            }
            return results.Select(x => x.Element);
        }

        /// <summary>
        /// Removes zero-width-spaces from the given <paramref name="input"/>, as well as whitespace at the
        /// beginning and end, and converts the string to lowercase.
        /// </summary>
        /// <param name="input">The string which shall be filtered.</param>
        /// <returns>The filtered string.</returns>
        public static string FilterString(string input)
        {
            const string zeroWidthSpace = "\u200B";
            return input.Trim().Replace(zeroWidthSpace, string.Empty).ToLowerInvariant();
        }

        /// <summary>
        /// Adds the given <paramref name="element"/> to the <see cref="elements"/> dictionary.
        /// </summary>
        /// <param name="element">The element to be added.</param>
        private void AddElement(Node element)
        {
            string elementString = ElementToString(element);
            if (elements.TryGetValue(elementString, out List<Node> list))
            {
                list.Add(element);
            }
            else
            {
                elements.Add(elementString, new List<Node> { element });
            }
        }

        /// <summary>
        /// Removes the given <paramref name="element"/> from the <see cref="elements"/> dictionary.
        /// </summary>
        /// <param name="element">The element to be removed.</param>
        private void RemoveElement(Node element)
        {
            string elementString = ElementToString(element);
            if (elements.TryGetValue(elementString, out List<Node> list))
            {
                list.Remove(element);
                if (list.Count == 0)
                {
                    elements.Remove(elementString);
                }
            }
        }

        /// <summary>
        /// Converts the given <paramref name="element"/> to a searchable string.
        /// </summary>
        /// <param name="element">The element to be converted.</param>
        /// <returns>The string representation of the element.</returns>
        private static string ElementToString(Node element)
        {
            return element.SourceName?.ToLowerInvariant() ?? string.Empty;
        }

        /// <summary>
        /// Called when no more events will be fired from the graph.
        /// </summary>
        public void OnCompleted()
        {
            // Nothing to be done.
        }

        /// <summary>
        /// Called when an error occurs in the graph.
        /// </summary>
        /// <param name="error">The error which occurred.</param>
        public void OnError(Exception error)
        {
            throw error;
        }

        /// <summary>
        /// Called when a new event is fired from the graph.
        /// </summary>
        /// <param name="changeEvent">The event which was fired.</param>
        public void OnNext(ChangeEvent changeEvent)
        {
            // We want to update our mapping of names to nodes whenever a node is added or removed.
            switch (changeEvent)
            {
                case NodeEvent { Change: ChangeType.Addition } nodeEvent:
                    AddElement(nodeEvent.Node);
                    break;
                case NodeEvent { Change: ChangeType.Removal } nodeEvent:
                    RemoveElement(nodeEvent.Node);
                    break;
                case IAttributeEvent { AttributeName: Node.SourceNameAttribute, Attributable: Node node }:
                    // If the source name of a node changes, we need to update the mapping.
                    RemoveElement(node);
                    AddElement(node);
                    break;
            }
        }
    }
}
