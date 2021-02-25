using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class is responsible for saving the deleted objects in a history for the possibility of an undo-operation.
    /// </summary>
    public class ActionHistory : MonoBehaviour
    {
        /// <summary>
        /// A history of all deleted nodes
        /// </summary>
        public LinkedList<List<GameObject>> deletedNodeHistory = new LinkedList<List<GameObject>>();

        /// <summary>
        /// A history of the old positions of the deleted nodes
        /// </summary>
        private LinkedList<List<Vector3>> oldPositionHistory = new LinkedList<List<Vector3>>();

        /// <summary>
        /// A history of all deleted edges
        /// </summary>
        private LinkedList<List<GameObject>> deletedEdgeHistory = new LinkedList<List<GameObject>>();

        /// <summary>
        /// The graph of the node to be deleted
        /// </summary>
        private Graph graph;

        /// <summary>
        /// Saves the deleted nodes and/or edges for the possibility of an undo. 
        /// Removes the gameObjects from the graph.
        /// </summary>
        /// <param name="deletedNodes"> all deleted nodes of the last operation</param>
        /// <param name="oldPositionsOfDeletedNodes">all old positions of the deleted nodes of the last operation</param>
        public void SaveObjectForUndo(List<GameObject> deletedNodes, List<Vector3> oldPositionsOfDeletedNodes)
        {
            //FIXME FOR GOEDECKE: param deletedNodes -> deleted Objects..? dunno cause of outsourcing of edges in other function in future - if -> in docs and param

            SEECity city = SceneQueries.GetCodeCity(deletedNodes[0].transform)?.gameObject.GetComponent<SEECity>();
            graph = city.LoadedGraph;
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

                    if (actionHistoryObject.TryGetComponent(out Collider collider))
                    {
                        actionHistoryObject.GetComponent<Collider>().enabled = false;
                    }

                    nodesAndascendingEdges.Add(actionHistoryObject);
                }
            }

            List<GameObject> deletedNodesReverse = deletedNodes;
            //For deletion bottom-up
            deletedNodesReverse.Reverse();

            foreach (GameObject deletedNode in deletedNodesReverse)
            {
                if (deletedNode.tag == Tags.Node)
                {
                    deletedNode.TryGetComponent(out NodeRef nodeRef);
                    graph.RemoveNode(nodeRef.Value);
                }
                if (deletedNode.tag == Tags.Edge)
                {

                    deletedNode.SetVisibility(false, true);
                    edgesToHide.Add(deletedNode);
                    deletedNode.TryGetComponent(out EdgeRef edgeRef);
                    graph.RemoveEdge(edgeRef.edge);
                    if (deletedNode.TryGetComponent(out Collider collider))
                    {
                        deletedNode.GetComponent<Collider>().enabled = false;
                    }
                }
            }

            oldPositionsOfDeletedNodes.Reverse();
            nodesAndascendingEdges.Reverse();
            deletedEdgeHistory.AddLast(edgesToHide);
            oldPositionHistory.AddLast(oldPositionsOfDeletedNodes);
            deletedNodeHistory.AddLast(nodesAndascendingEdges);
        }

        /// <summary>
        /// Gets the last operation in history and undoes it. 
        /// </summary>
        /// <returns>the positions of the gameObjects where they has to be moved after undo again</returns>
        public List<Vector3> UndoDeleteOperation()
        {
            foreach (GameObject node in deletedNodeHistory.Last())
            {
                if (node.TryGetComponent(out NodeRef nodeRef))
                {
                    graph.AddNode(nodeRef.Value);
                }
                if (node.TryGetComponent(out Collider collider))
                {
                    node.GetComponent<Collider>().enabled = true;
                }
            }

            foreach (GameObject edge in deletedEdgeHistory.Last())
            {
                if (edge.TryGetComponent(out EdgeRef edgeReference))
                {
                    graph.AddEdge(edgeReference.edge);
                    edge.SetVisibility(true, false);
                }
                if (edge.TryGetComponent(out Collider collider))
                {
                    edge.GetComponent<Collider>().enabled = true;
                }
            }

            List<Vector3> oldPosition = oldPositionHistory.Last();
            deletedEdgeHistory.RemoveLast();
            deletedNodeHistory.RemoveLast();
            oldPositionHistory.RemoveLast();

            return oldPosition;
        }

    }
}
