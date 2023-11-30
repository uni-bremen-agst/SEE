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

namespace SEE.Controls.Actions
{
    public class ShowDiffAction : AbstractPlayerAction
    {
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

                
           


            }

            return false;
        }

        // Start is called before the first frame update
        void Start()
        {
        //Open Window
        }

        void ShowDiffGraph(LaidOutGraph current, LaidOutGraph next) {
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
    }

}
