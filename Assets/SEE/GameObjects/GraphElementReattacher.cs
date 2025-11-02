using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// Allows to re-attach graph elements for game nodes and edges.
    /// </summary>
    internal class GraphElementReattacher
    {
        /// <summary>
        /// Sets the Value of the given <paramref name="graphElementRef"/> to
        /// <paramref name="graphElement"/>.
        ///
        /// N.B.: This is essentially a specification of the Value property of <see cref="NodeRef"/>
        /// or <see cref="EdgeRef"/>, respectively, for write accesses.
        /// </summary>
        /// <typeparam name="GE">subclass of <see cref="GraphElement"/></typeparam>
        /// <typeparam name="R">subclass of <see cref="GraphElementRef"/></typeparam>
        /// <param name="graphElementRef">a reference to a graph element</param>
        /// <param name="graphElement">a graph element becoming the Value of <paramref name="graphElementRef"/></param>
        delegate void SetValue<GE, R>(ref R graphElementRef, GE graphElement) where GE : GraphElement where R : GraphElementRef;

        /// <summary>
        /// Yields the Value of <see cref="GraphElement"/> reference (<see cref="NodeRef"/>
        /// or <see cref="EdgeRef"/>) for the given <paramref name="graphElementRef"/>.
        ///
        /// N.B.: This is essentially a specification of the Value property of <see cref="NodeRef"/>
        /// or <see cref="EdgeRef"/>, respectively, for read accesses.
        /// </summary>
        /// <typeparam name="GE">subclass of <see cref="GraphElement"/></typeparam>
        /// <typeparam name="R">subclass of <see cref="GraphElementRef"/></typeparam>
        /// <param name="graphElementRef">a reference to a graph element</param>
        /// <returns>Value of <paramref name="graphElementRef"/></returns>
        delegate GE GetValue<GE, R>(R graphElementRef) where GE : GraphElement where R : GraphElementRef;

        /// Re-attaches the given <paramref name="graphElement"/> to the given <paramref name="gameObject"/>,
        /// that is, the <see cref="GraphElementRef"/> component of <paramref name="gameObject"/> will refer to
        /// <paramref name="graphElement"/> afterwards. Returns the graph element formerly attached to
        /// <paramref name="gameObject"/> if there was one, or null if there was none.
        /// </summary>
        /// <typeparam name="GE">subclass of <see cref="GraphElement"/></typeparam>
        /// <typeparam name="R">subclass of <see cref="GraphElementRef"/></typeparam>
        /// <param name="gameObject">the game object where the node is to be attached to</param>
        /// <param name="node">the node to be attached</param>
        /// <param name="getValue">yields the Value of a <see cref="GraphElementRef"/></param>
        /// <param name="setValue">sets the Value of a <see cref="GraphElementRef"/></param>
        /// <returns>the graph element formerly attached to <paramref name="gameObject"/> or null</returns>
        private static GE Reattach<GE, R>(GameObject gameObject, GE graphElement, GetValue<GE, R> getValue, SetValue<GE, R> setValue)
            where GE : GraphElement where R : GraphElementRef
        {
            GE formerGraphElement = null;

            if (!gameObject.TryGetComponent(out R graphElementRef))
            {
                // reference should not be null
                Debug.LogError($"Re-used game object for graph element '{graphElement.ID}' does not have a {typeof(R)} attached to it.\n");
                graphElementRef = gameObject.AddComponent<R>();
            }
            else
            {
                formerGraphElement = getValue(graphElementRef);
            }
            setValue(ref graphElementRef, graphElement);
            return formerGraphElement;
        }

        /// <summary>
        /// Re-attaches the given <paramref name="node"/> to the given <paramref name="gameObject"/>,
        /// that is, the <see cref="NodeRef"/> component of <paramref name="gameObject"/> will refer to
        /// <paramref name="node"/> afterwards. Returns the node formerly attached to
        /// <paramref name="gameObject"/> if there was one or null if there was none.
        /// </summary>
        /// <param name="gameObject">the game object where the node is to be attached to</param>
        /// <param name="node">the node to be attached</param>
        /// <returns>the node formerly attached to <paramref name="gameObject"/> or null</returns>
        public static Node ReattachNode(GameObject gameObject, Node node)
        {
            return Reattach(gameObject, node, nr => nr.Value, (ref NodeRef nr, Node n) => { nr.Value = n; });
        }

        /// <summary>
        /// Re-attaches the given <paramref name="edge"/> to the given <paramref name="gameObject"/>,
        /// that is, the <see cref="EdgeRef"/> component of <paramref name="gameObject"/> will refer to
        /// <paramref name="edge"/> afterwards. Returns the edge formerly attached to
        /// <paramref name="gameObject"/> if there was one or null if there was none.
        /// </summary>
        /// <param name="gameObject">the game object where the edge is to be attached to</param>
        /// <param name="edge">the edge to be attached</param>
        /// <returns>the edge formerly attached to <paramref name="gameObject"/> or null</returns>
        public static Edge ReattachEdge(GameObject gameObject, Edge edge)
        {
            return Reattach(gameObject, edge, er => er.Value, (ref EdgeRef er, Edge e) => { er.Value = e; });
        }

        /// <summary>
        /// Re-attaches the given <paramref name="graphElement"/> to the given <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">the game object where the graph element is to be attached</param>
        /// <param name="graphElement">the graph element to be attached (either a <see cref="Node"/>
        /// or an <see cref="Edge"/></param>
        /// <returns>the graph element formerly attached to <paramref name="gameObject"/> or null</returns>
        /// <exception cref="System.ArgumentException">thrown if <paramref name="graphElement"/>
        /// is neither a <see cref="Node"/> nor an <see cref="Edge"/></exception>
        public static GraphElement Reattach(GameObject gameObject, GraphElement graphElement)
        {
            return graphElement switch
            {
                Node node => ReattachNode(gameObject, node),
                Edge edge => ReattachEdge(gameObject, edge),
                _ => throw new System.ArgumentException($"Unsupported graph element type '{graphElement.GetType()}'"),
            };
        }
    }
}
