using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Evolution;
using Sirenix.Utilities;
using SEE.Layout;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    public class ShowDiffAction : AbstractPlayerAction
    {
        private static SEEBranchCity seeBranchCity = new();

        /// <summary>
        /// The manager of the game objects created for the city.
        /// This attribute will be set in the setter of the attribute CityEvolution because it
        /// depends upon the graphRenderer, which in turn depends upon the city, which is set by
        /// this setter.
        /// </summary>
        private ObjectManager objectManager;

        private Graph loadedGraph;

        private Graph nextGraph;

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Node"/> from different graphs.
        /// </summary>
        private static readonly NodeEqualityComparer nodeEqualityComparer = new();

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Edge"/> from different graphs.
        /// </summary>
        private static readonly EdgeEqualityComparer edgeEqualityComparer = new();

        /// <summary>
        /// The city (graph + layout) currently shown.
        /// </summary>
        private LaidOutGraph currentCity;  // not serialized by Unity

        /// <summary>
        /// The city (graph + layout) to be shown next.
        /// </summary>
        private LaidOutGraph nextCity;

        /// <summary>
        /// Manager object which takes care of the player selection menu and window space dictionary for us.
        /// </summary>
        private WindowSpaceManager spaceManager;
        public override ActionStateType GetActionStateType() => ActionStateTypes.ShowDiff;

        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>();
        }

        public override IReversibleAction NewInstance() => CreateReversibleAction();


        public static IReversibleAction CreateReversibleAction() => new ShowDiffAction();

        public override bool Update()
        {
            // Only allow local player to open new code windows
            if (spaceManager.CurrentPlayer == WindowSpaceManager.LocalPlayer
                && SEEInput.Select()
                && Raycasting.RaycastGraphElement(out RaycastHit hit, out GraphElementRef graphElementRef) != HitGraphElement.None)
            {
                // Show diff
                // If nothing is selected, there's nothing more we need to do
                if (graphElementRef == null)
                {
                    return false;
                }
                GraphElement graphElement;
                if (graphElementRef is NodeRef nodeRef)
                {
                    graphElement = nodeRef.Value;
                }
                else if (graphElementRef is EdgeRef edgeRef)
                {
                    graphElement = edgeRef.Value;
                }
                else
                {
                    Debug.LogError("Neither node nor edge.\n");
                    return false;
                }

                loadedGraph = seeBranchCity.LoadedGraph;
                nextGraph = seeBranchCity.NextGraph;

                currentCity = new LaidOutGraph(loadedGraph, null, null);
                nextCity = new LaidOutGraph(nextGraph, null, null);

                CalcDiffGraph(currentCity, nextCity);
            }

            return false;
        }

        // Start is called before the first frame update
        void Start()
        {
        //Open Window
        }

        void CalcDiffGraph(LaidOutGraph current, LaidOutGraph next) {
            Graph oldGraph = current?.Graph;
            Graph newGraph = next?.Graph;

            // Node comparison.
            newGraph.Diff(oldGraph,
                          g => g.Nodes(),
                          (g, id) => g.GetNode(id),
                          GraphExtensions.AttributeDiff(newGraph, oldGraph),
                          nodeEqualityComparer,
                          out addedNodes,
                          out removedNodes,
                          out changedNodes,
                          out equalNodes);

            // Edge comparison.
            newGraph.Diff(oldGraph,
                          g => g.Edges(),
                          (g, id) => g.GetEdge(id),
                          GraphExtensions.AttributeDiff(newGraph, oldGraph),
                          edgeEqualityComparer,
                          out addedEdges,
                          out removedEdges,
                          out changedEdges,
                          out equalEdges);

        }

        private void DeleteDiffGraph()
        {
            int deletedGraphElements = removedNodes.Count + removedEdges.Count;
            if (deletedGraphElements > 0)
            {
                // Remove those edges.
                removedEdges.ForEach(edge =>
                {
                    objectManager.RemoveEdge(edge, out GameObject gameObjectEdge);
                    Destroyer.Destroy(gameObjectEdge);
                });

                // Remove those nodes.
                removedNodes.ForEach(node =>
                {
                    objectManager.RemoveNode(node, out GameObject gameObjectNode);
                    Destroyer.Destroy(gameObjectNode);
                });
            }
        }

        private void MoveDiffGraph(LaidOutGraph next)
        {

        }

        private void AdjustExistingGraph()
        {
            // Even the equal nodes need adjustments because the layout could have
            // changed their dimensions. The treemap layout, for instance, may do that.
            int changedElements = equalNodes.Count + changedNodes.Count;
            if (changedElements > 0)
            {
                //equalNodes.ForEach(AdjustExistingNode);
                //changedNodes.ForEach(AdjustExistingNode);
            }
        }

        private void AddNewNodes()
        {
            addedNodes.ForEach(AddNode);
        }

        private void AddNode(Node node)
        {
            Assert.IsNotNull(node);
            ILayoutNode layoutNode = NextLayoutToBeShown[node.ID];
            // The game node representing the graphNode if there is any; null if there is none
            Node formerGraphNode = objectManager.GetNode(node, out GameObject gameNode);
            Assert.IsTrue(gameNode.HasNodeRef());
            // Assert.IsNull(formerGraphNode);
            if (formerGraphNode != null)
            {
                Debug.LogError($"A graph node for {formerGraphNode.ID} was expected not to exist.\n");
            }

            Add(gameNode, layoutNode);

            void Add(GameObject gameNode, ILayoutNode layoutNode)
            {
                // A new node has no layout applied to it yet.
                Vector3 initialPosition = layoutNode.CenterPosition;
                gameNode.transform.position = initialPosition;

                gameNode.SetAbsoluteScale(layoutNode.AbsoluteScale, animate: false);

                // The node is new. Hence, it has no parent yet. It must be contained
                // in a code city though; otherwise the NodeOperator would not work.
                //gameNode.transform.SetParent(gameObject.transform);



            }
        }


        /// <summary>
        /// Set of added nodes from the current to the next graph.
        /// They are contained in the next graph.
        /// </summary>
        private ISet<Node> addedNodes;
        /// <summary>
        /// Set of removed nodes from the current to the next graph.
        /// They are contained in the current graph.
        /// </summary>
        private ISet<Node> removedNodes;
        /// <summary>
        /// Set of changed nodes from the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is a node in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Node> changedNodes;
        /// <summary>
        /// Set of equal nodes (i.e., nodes without any changes) from
        /// the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is a node in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Node> equalNodes;

        /// <summary>
        /// Set of added edges from the current to the next graph.
        /// They are contained in the next graph.
        /// </summary>
        private ISet<Edge> addedEdges;
        /// <summary>
        /// Set of removed edges from the current to the next graph.
        /// They are contained in the current graph.
        /// </summary>
        private ISet<Edge> removedEdges;
        /// <summary>
        /// Set of changed edges from the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is an edge in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Edge> changedEdges;
        /// <summary>
        /// Set of equal edges (i.e., edges without any changes) from
        /// the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is an edge in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Edge> equalEdges;

        /// <summary>
        /// The layout of <see cref="nextCity"/>. The layout is a mapping of the graph
        /// nodes' IDs onto their <see cref="ILayoutNode"/>.
        /// </summary>
        private Dictionary<string, ILayoutNode> NextLayoutToBeShown => nextCity?.Layout;

    }

}

