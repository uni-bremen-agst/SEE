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
        private LinkedList<AbstractPlayerAction> actionHistory = new LinkedList<AbstractPlayerAction>();

        public LinkedList<AbstractPlayerAction> GetActionHistory { get => actionHistory; set => actionHistory = value; }

        /// <summary>
        /// Saves the deleted nodes and/or edges for the possibility of an undo. 
        /// Removes the gameObjects from the graph.
        /// </summary>
        /// <param name="deletedNodes"> all deleted nodes of the last operation</param>
        /// <param name="oldPositionsOfDeletedNodes">all old positions of the deleted nodes of the last operation</param>
        public void SaveObjectForDeleteUndo(List<GameObject> deletedNodes, List<Vector3> oldPositionsOfDeletedNodes)
        {
            SEECity city = SceneQueries.GetCodeCity(deletedNodes[0].transform)?.gameObject.GetComponent<SEECity>();
            Graph graph = city.LoadedGraph;
            List<GameObject> nodesAndascendingEdges = new List<GameObject>();
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

                            if (nodesAndascendingEdges.Contains(edge) == false)
                            {
                                edgesToHide.Add(edge);
                            }
                            edge.TryGetComponent(out EdgeRef edgeRef);
                            graph.RemoveEdge(edgeRef.edge);
                        }
                    }

                    nodesAndascendingEdges.Add(actionHistoryObject);
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
            nodesAndascendingEdges.Reverse();
            actionHistory.AddLast(new DeleteAction(nodesAndascendingEdges, oldPositionsOfDeletedNodes, edgesToHide, graph));
        }

    }
}
