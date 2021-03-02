using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class is responsible for saving the deleted objects in a history for the possibility of an undo-operation.
    /// </summary>
    public class ActionHistory : MonoBehaviour
    {

        /// <summary>
        /// A history of all actions of the user for the possibility of an undo. 
        /// </summary>
        public LinkedList<AbstractPlayerAction> ActionHistoryList { get; set; } = new LinkedList<AbstractPlayerAction>();

        /// <summary>
        /// Saves the deleted nodes and/or edges for the possibility of an undo. 
        /// Removes the gameObjects from the graph.
        /// /// Precondition: deletedNodes != null.
        /// </summary>
        /// <param name="deletedNodes">all deleted nodes of the last operation</param>
        /// <param name="oldPositionsOfDeletedNodes">all old positions of the deleted nodes of the last operation</param>
        public void SaveObjectForDeleteUndo(List<GameObject> deletedNodes, List<Vector3> oldPositionsOfDeletedNodes)
        {
            SEECity city = SceneQueries.GetCodeCity(deletedNodes[0].transform)?.gameObject.GetComponent<SEECity>();
            Graph graph = city.LoadedGraph;
            List<GameObject> nodesAndAscendingEdges = new List<GameObject>();
            List<GameObject> edgesToHide = new List<GameObject>();
            
            foreach (GameObject actionHistoryObject in deletedNodes)
            {
                if (actionHistoryObject.TryGetComponent(out NodeRef nodeRef))
                {
                    HashSet<string> edgeIDs = Destroyer.GetEdgeIds(nodeRef);
                    foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                    {
                        if (edge.activeInHierarchy && edgeIDs.Contains(edge.name))
                        {
                            edge.SetVisibility(false, true);

                            if (!nodesAndAscendingEdges.Contains(edge))
                            {
                                edgesToHide.Add(edge);
                            }
                            edge.TryGetComponent(out EdgeRef edgeRef);
                            graph.RemoveEdge(edgeRef.edge);
                        }
                    }

                    nodesAndAscendingEdges.Add(actionHistoryObject);
                }
            }

            List<GameObject> deletedNodesReverse = deletedNodes;
            //For deletion bottom-up
            deletedNodesReverse.Reverse();

            foreach (GameObject deletedNode in deletedNodesReverse)
            {
                if (deletedNode.CompareTag(Tags.Node))
                {
                    deletedNode.TryGetComponent(out NodeRef nodeRef);
                    if (graph.Contains(nodeRef.Value))
                    {
                        graph.RemoveNode(nodeRef.Value);
                    }
                }
                if (deletedNode.CompareTag(Tags.Edge))
                {
                    deletedNode.SetVisibility(false, true);
                    edgesToHide.Add(deletedNode);
                    deletedNode.TryGetComponent(out EdgeRef edgeRef);
                    graph.RemoveEdge(edgeRef.edge);
                }
            }

            oldPositionsOfDeletedNodes.Reverse();
            nodesAndAscendingEdges.Reverse();
            ActionHistoryList.AddLast(new DeleteAction(nodesAndAscendingEdges, oldPositionsOfDeletedNodes, edgesToHide, graph));
        }
    }
}
