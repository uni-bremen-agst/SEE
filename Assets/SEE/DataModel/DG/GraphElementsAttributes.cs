using System.Collections.Generic;
using System.Linq;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// This part of <see cref="Graph"/> provides the interface to all node and edge attribute names.
    /// </summary>
    public partial class Graph
    {
        #region integer attributes

        /// <summary>
        /// All names of integer attributes of all nodes in the graph.
        /// </summary>
        /// <returns>Names of integer attributes.</returns>
        public ISet<string> AllIntNodeAttributes()
        {
            return AllAttributes(forNodes: true, forEdges: false, ge => ge.AllIntAttributeNames());
        }

        /// <summary>
        /// All names of integer attributes of all edges in the graph.
        /// </summary>
        /// <returns>Names of integer attributes.</returns>
        public ISet<string> AllIntEdgeAttributes()
        {
            return AllAttributes(forNodes: false, forEdges: true, ge => ge.AllIntAttributeNames());
        }

        /// <summary>
        /// All names of integer attributes of all nodes and edges in the graph.
        /// </summary>
        /// <returns>Names of integer attributes.</returns>
        public ISet<string> AllIntGraphElementAttributes()
        {
            return AllAttributes(forNodes: true, forEdges: true, ge => ge.AllIntAttributeNames());
        }

        #endregion

        #region float attributes

        /// <summary>
        /// All names of float attributes of all nodes in the graph.
        /// </summary>
        /// <returns>Names of attributes.</returns>
        public ISet<string> AllFloatNodeAttributes()
        {
            return AllAttributes(forNodes: true, forEdges: false, ge => ge.AllFloatAttributeNames());
        }

        /// <summary>
        /// All names of float attributes of all edges in the graph.
        /// </summary>
        /// <returns>Names of attributes.</returns>
        public ISet<string> AllFloatEdgeAttributes()
        {
            return AllAttributes(forNodes: false, forEdges: true, ge => ge.AllFloatAttributeNames());
        }

        /// <summary>
        /// All names of float attributes of all nodes and edges in the graph.
        /// </summary>
        /// <returns>Names of attributes.</returns>
        public ISet<string> AllFloatGraphElementAttributes()
        {
            return AllAttributes(forNodes: true, forEdges: true, ge => ge.AllFloatAttributeNames());
        }

        #endregion

        #region toggle attributes

        /// <summary>
        /// All names of toggle attributes of all nodes in the graph.
        /// </summary>
        /// <returns>Names of toggle attributes.</returns>
        public ISet<string> AllToggleNodeAttributes()
        {
            return AllAttributes(forNodes: true, forEdges: false, ge => ge.AllToggleAttributeNames());
        }

        /// <summary>
        /// All names of toggle attributes of all edges in the graph.
        /// </summary>
        /// <returns>Names of toggle attributes.</returns>
        public ISet<string> AllToggleEdgeAttributes()
        {
            return AllAttributes(forNodes: false, forEdges: true, ge => ge.AllToggleAttributeNames());
        }

        /// <summary>
        /// All names of toggle attributes of all graph elements in the graph.
        /// </summary>
        /// <returns>Names of toggle attributes.</returns>
        public ISet<string> AllToggleGraphElementAttributes()
        {
            return AllAttributes(forNodes: true, forEdges: true, ge => ge.AllToggleAttributeNames());
        }

        #endregion

        #region string attributes

        /// <summary>
        /// All names of string attributes of all nodes in the graph.
        /// </summary>
        /// <returns>Names of string attributes.</returns>
        public ISet<string> AllStringNodeAttributes()
        {
            return AllAttributes(forNodes: true, forEdges: false, ge => ge.AllStringAttributeNames());
        }

        /// <summary>
        /// All names of string attributes of all edges in the graph.
        /// </summary>
        /// <returns>Names of string attributes.</returns>
        public ISet<string> AllStringEdgeAttributes()
        {
            return AllAttributes(forNodes: false, forEdges: true, ge => ge.AllStringAttributeNames());
        }

        /// <summary>
        /// All names of string attributes of all graph elements in the graph.
        /// </summary>
        /// <returns>Names of string attributes.</returns>
        public ISet<string> AllStringGraphElementAttributes()
        {
            return AllAttributes(forNodes: true, forEdges: true, ge => ge.AllStringAttributeNames());
        }

        #endregion

        /// <summary>
        /// Returns all node attribute names collected via given <paramref name="attributeNames"/>
        /// over all nodes in the graph.
        /// </summary>
        /// <param name="attributeNames">Yields the node attribute names to collect.</param>
        /// <returns>All node attribute names collected via <paramref name="attributeNames"/>.</returns>
        private ISet<string> AllAttributes(bool forNodes, bool forEdges, AllAttributeNames attributeNames)
        {
            return Elements().Where(x => (forNodes && x is Node) || (forEdges && x is Edge))
                             .SelectMany(x => attributeNames(x)).ToHashSet();

        }

        /// <summary>
        /// Returns the union of <see cref="AllFloatNodeAttributes()"/>
        /// and <see cref="AllIntNodeAttributes()"/>.
        /// </summary>
        /// <returns>Names of all numeric (int or float) node attributes.</returns>
        public ISet<string> AllNumericNodeAttributes()
        {
            ISet<string> result = AllIntNodeAttributes();
            result.UnionWith(AllFloatNodeAttributes());
            return result;
        }

        /// <summary>
        /// Returns the union of <see cref="AllFloatEdgeAttributes()"/>
        /// and <see cref="AllIntEdgeAttributes()"/>.
        /// </summary>
        /// <returns>Names of all numeric (int or float) node attributes.</returns>
        public ISet<string> AllNumericEdgeAttributes()
        {
            ISet<string> result = AllIntEdgeAttributes();
            result.UnionWith(AllFloatEdgeAttributes());
            return result;
        }

        /// <summary>
        /// Returns the union of <see cref="AllNumericNodeAttributes()"/>
        /// and <see cref="AllNumericEdgeAttributes()"/>.
        /// </summary>
        /// <returns>Names of all numeric (int or float) attributes.</returns>
        public ISet<string> AllNumericAttributes()
        {
            ISet<string> result = AllNumericNodeAttributes();
            result.UnionWith(AllNumericEdgeAttributes());
            return result;
        }

        /// <summary>
        /// Returns the union of <see cref="AllStringNodeAttributes()"/>
        /// and <see cref="AllStringEdgeAttributes()"/>.
        /// </summary>
        /// <returns>Names of all string attributes.</returns>
        public ISet<string> AllStringAttributes()
        {
            ISet<string> result = AllStringNodeAttributes();
            result.UnionWith(AllStringEdgeAttributes());
            return result;
        }

        /// <summary>
        /// Returns the union of the names of all numeric node attributes of the given <paramref name="graphs"/>.
        /// </summary>
        /// <param name="graphs">Graphs for which to yield the metric names.</param>
        /// <returns>Union of the names of all numeric node attributes.</returns>
        internal static ISet<string> AllNodeMetrics(ICollection<Graph> graphs)
        {
            HashSet<string> result = new();
            foreach (Graph graph in graphs)
            {
                result.UnionWith(graph.AllNumericNodeAttributes());
            }
            return result;
        }

        /// <summary>
        /// Returns the attribute names of given <paramref name="graphElement"/>.
        /// </summary>
        /// <param name="graphElement">The graph element whose attribute names are to be retrieved.</param>
        /// <returns>Attribute names of a particular type.</returns>
        private delegate ICollection<string> AllAttributeNames(GraphElement graphElement);
    }
}
