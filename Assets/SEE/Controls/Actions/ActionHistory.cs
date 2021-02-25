using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActionHistory : MonoBehaviour
{
    /// <summary>
    /// 
    /// </summary>
    /// 
    public LinkedList<List<GameObject>> deletedNodeHistory = new LinkedList<List<GameObject>>();

    /// <summary>
    /// 
    /// </summary>
    private LinkedList<List<Vector3>> oldPositionHistory = new LinkedList<List<Vector3>>();

    /// <summary>
    /// 
    /// </summary>
    private LinkedList<List<GameObject>> deletedEdgeHistory = new LinkedList<List<GameObject>>();

    /// <summary>
    /// 
    /// </summary>
    private Graph graph;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="deletedNodes"></param>
    /// <param name="oldPositionsOfDeletedNodes"></param>
    public void SaveObjectForUndo(List<GameObject> deletedNodes, List<Vector3> oldPositionsOfDeletedNodes)
    {
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
        deletedNodesReverse.Reverse();
        //deleting all children of node
        foreach (GameObject deletedNode in deletedNodesReverse)
        {
            deletedNode.TryGetComponent(out NodeRef nodeRef);
            graph.RemoveNode(nodeRef.Value);
        }

        deletedEdgeHistory.AddLast(edgesToHide);
        oldPositionsOfDeletedNodes.Reverse();
        nodesAndascendingEdges.Reverse();
        oldPositionHistory.AddLast(oldPositionsOfDeletedNodes);
        deletedNodeHistory.AddLast(nodesAndascendingEdges);
    }

    /// <summary>
    /// 
    /// </summary>
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
        }

        deletedEdgeHistory.RemoveLast();
        deletedNodeHistory.RemoveLast();
        oldPositionHistory.RemoveLast();

        return oldPositionHistory.Last();

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="counter"></param>
    /// <param name="states"></param>
    private static void CheckBoundaries(int counter)
    {
        {

            throw new NotSupportedException("Redo function cannot be executed");

        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="removedObject"></param>
    private static void RemoveForReflexionAnalysis(GameObject removedObject)
    {
        // remove node or edge
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectToAddtoGraphHierarchy"></param>
    private static void Reparent(GameObject objectToAddtoGraphHierarchy)
    {
        //add edge or node - reparenting.
    }

}
