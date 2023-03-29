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
        /// <returns>names of integer attributes</returns>
        public ISet<string> AllIntNodeAttributes()
        {
            return AllAttributes(true, false, ge => ge.AllIntAttributeNames());
        }

        /// <summary>
        /// All names of integer attributes of all edges in the graph.
        /// </summary>
        /// <returns>names of integer attributes</returns>
        public ISet<string> AllIntEdgeAttributes()
        {
            return AllAttributes(false, true, ge => ge.AllIntAttributeNames());
        }

        /// <summary>
        /// All names of integer attributes of all nodes and edges in the graph.
        /// </summary>
        /// <returns>names of integer attributes</returns>
        public ISet<string> AllIntGraphElementAttributes()
        {
            return AllAttributes(true, true, ge => ge.AllIntAttributeNames());
        }

        #endregion

        #region float attributes

        /// <summary>
        /// All names of float attributes of all nodes in the graph.
        /// </summary>
        /// <returns>names of attributes</returns>
        public ISet<string> AllFloatNodeAttributes()
        {
            return AllAttributes(true, false, ge => ge.AllFloatAttributeNames());
        }

        /// <summary>
        /// All names of float attributes of all edges in the graph.
        /// </summary>
        /// <returns>names of attributes</returns>
        public ISet<string> AllFloatEdgeAttributes()
        {
            return AllAttributes(false, true, ge => ge.AllFloatAttributeNames());
        }

        /// <summary>
        /// All names of float attributes of all nodes and edges in the graph.
        /// </summary>
        /// <returns>names of attributes</returns>
        public ISet<string> AllFloatGraphElementAttributes()
        {
            return AllAttributes(true, true, ge => ge.AllFloatAttributeNames());
        }

        #endregion

        #region toggle attributes

        /// <summary>
        /// All names of toggle attributes of all nodes in the graph.
        /// </summary>
        /// <returns>names of toggle attributes</returns>
        public ISet<string> AllToggleNodeAttributes()
        {
            return AllAttributes(true, false, ge => ge.AllToggleAttributeNames());
        }

        /// <summary>
        /// All names of toggle attributes of all edges in the graph.
        /// </summary>
        /// <returns>names of toggle attributes</returns>
        public ISet<string> AllToggleEdgeAttributes()
        {
            return AllAttributes(false, true, ge => ge.AllToggleAttributeNames());
        }

        /// <summary>
        /// All names of toggle attributes of all graph elements in the graph.
        /// </summary>
        /// <returns>names of toggle attributes</returns>
        public ISet<string> AllToggleGraphElementAttributes()
        {
            return AllAttributes(true, true, ge => ge.AllToggleAttributeNames());
        }

        #endregion

        #region string attributes

        /// <summary>
        /// All names of string attributes of all nodes in the graph.
        /// </summary>
        /// <returns>names of string attributes</returns>
        public ISet<string> AllStringNodeAttributes()
        {
            return AllAttributes(true, false, ge => ge.AllStringAttributeNames());
        }

        /// <summary>
        /// All names of string attributes of all edges in the graph.
        /// </summary>
        /// <returns>names of string attributes</returns>
        public ISet<string> AllStringEdgeAttributes()
        {
            return AllAttributes(false, true, ge => ge.AllStringAttributeNames());
        }

        /// <summary>
        /// All names of string attributes of all graph elements in the graph.
        /// </summary>
        /// <returns>names of string attributes</returns>
        public ISet<string> AllStringGraphElementAttributes()
        {
            return AllAttributes(true, true, ge => ge.AllStringAttributeNames());
        }

        #endregion

        /// <summary>
        /// Returns all node attribute names collected via given <paramref name="attributeNames"/>
        /// over all nodes in the graph.
        /// </summary>
        /// <param name="attributeNames">yields the node attribute names to collect</param>
        /// <returns>all node attribute names collected via <paramref name="attributeNames"/></returns>
        private ISet<string> AllAttributes(bool forNodes, bool forEdges, AllAttributeNames attributeNames)
        {
            HashSet<string> result = new();
            if (forNodes)
            {
                ForAll(Nodes().Cast<GraphElement>(), attributeNames, result);
            }
            if (forEdges)
            {
                ForAll(Edges().Cast<GraphElement>(), attributeNames, result);
            }
            return result;

            // Adds all attribute name of all elements to result.
            // The attributes are retrieved from those elements by way of function attributeNames.
            static void ForAll(IEnumerable<GraphElement> elements, AllAttributeNames attributeNames, HashSet<string> result)
            {
                foreach (GraphElement element in elements)
                {
                    foreach (string name in attributeNames(element))
                    {
                        result.Add(name);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the union of <see cref="AllFloatNodeAttributes()"/>
        /// and <see cref="AllIntNodeAttributes()"/>.
        /// </summary>
        /// <returns>names of all numeric (int or float) node attributes</returns>
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
        /// <returns>names of all numeric (int or float) node attributes</returns>
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
        /// <returns>names of all numeric (int or float) node attributes</returns>
        public ISet<string> AllNumericAttributes()
        {
            ISet<string> result = AllNumericNodeAttributes();
            result.UnionWith(AllNumericEdgeAttributes());
            return result;
        }

        /// <summary>
        /// Returns the union of the names of all numeric node attributes of the given <paramref name="graphs"/>.
        /// </summary>
        /// <param name="graphs">graphs for which to yield the metric names</param>
        /// <returns>union of the names of all numeric node attributes</returns>
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
        /// <param name="graphElement">the graph element whose attribute names are to be retrieved</param>
        /// <returns>attribute names of a particular type</returns>
        private delegate ICollection<string> AllAttributeNames(GraphElement graphElement);
    }
}
