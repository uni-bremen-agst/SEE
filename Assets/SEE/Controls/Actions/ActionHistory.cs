using SEE.Controls.Actions;
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

    public LinkedList<List<GameObject>> actionHistory = new LinkedList<List<GameObject>>();

    private LinkedList<List<Vector3>> oldPosition = new LinkedList<List<Vector3>>();

    private LinkedList<GameObject> parentCities = new LinkedList<GameObject>();

    private Graph graph;

    List<GameObject> childsOfParent = new List<GameObject>();

    public List<GameObject> ChildsOfParent { get => childsOfParent; set => childsOfParent = value; }

    /// <summary>
    /// Saves all childs of the graph-parent object
    /// </summary>
    /// <param name="parent"></param>
    /// <returns>List of all childs of a parent</returns>
    public List<GameObject> GetAllChildNodesAsGameObject(GameObject parent)
    {
        List<GameObject> childsOfThisParent = new List<GameObject>();
        if (!childsOfParent.Contains(parent))
        {
            childsOfParent.Add(parent);
        }
        int gameNodeCount = childsOfParent.Count;

        foreach (Transform child in parent.transform)
        {
            if (child.gameObject.CompareTag(Tags.Node))
            {
                if (!childsOfParent.Contains(child.gameObject))
                {
                    childsOfParent.Add(child.gameObject);
                }
                childsOfThisParent.Add(child.gameObject);
            }

        }
        if (childsOfParent.Count == gameNodeCount)
        {
            return childsOfParent;
        }
        else
        {
            foreach (GameObject childs in childsOfThisParent)
            {
                GetAllChildNodesAsGameObject(childs);
            }

            return childsOfParent;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="actionHistoryObject"></param>
    /// <param name="aState"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="gameObjectCity"></param>
    public void SaveObjectForUndo(List<GameObject> actionHistoryObjects, List<Vector3> oldPositions)
    {
        if (actionHistoryObjects == null)
        {
            Debug.LogError("null operation");
        }


        foreach (GameObject g in actionHistoryObjects)
        {
            Debug.Log(g.name + "im actionHistOb");

        } 
        SEECity city;
        actionHistoryObjects[0].TryGetComponent(out NodeRef nodeRef2);
        city = SceneQueries.GetCodeCity(actionHistoryObjects[0].transform)?.gameObject.GetComponent<SEECity>();
        graph = city.LoadedGraph;

        List<GameObject> NodesAndascendingEdges = new List<GameObject>();

        foreach (GameObject actionHistoryObject in actionHistoryObjects)
        {
            if (actionHistoryObject.TryGetComponent(out NodeRef nodeRef))
            {

                HashSet<string> edgeIDs = Destroyer.GetEdgeIds(nodeRef);
                foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                {
                    if (edge.activeInHierarchy && edgeIDs.Contains(edge.name))
                    {
                        edge.SetVisibility(false, true);
                        if (NodesAndascendingEdges.Contains(edge) == false)
                        {
                            NodesAndascendingEdges.Add(edge);
                        }
                    }
                }

                if (actionHistoryObject.TryGetComponent(out Collider collider))
                {
                    actionHistoryObject.GetComponent<Collider>().enabled = false;
                }
                if (!NodesAndascendingEdges.Contains(actionHistoryObject)) {
                NodesAndascendingEdges.Add(actionHistoryObject);
                }
            }

            foreach (GameObject go in actionHistoryObjects)
            {
                go.TryGetComponent(out NodeRef node);
                //  Debug.Log("name des GO´s : " + go.name);
                List<Edge> incoming = node.Value.Incomings;
                List<Edge> outgoing = node.Value.Outgoings;

                for (int i = 0; i < incoming.Count; i++)
                {
                    // FIXME: DOESNT WORK BECAUSE MULTIPLE ADDING OF SOME EDGES IF THEY ARE INCOMING AND OUTGOING
                    //  graph.RemoveEdge(incoming.ElementAt(i));
                    //  Debug.Log(incoming);
                }

                for (int i = 0; i < outgoing.Count; i++)
                {
                    // FIXME: DOESNT WORK BECAUSE MULTIPLE ADDING OF SOME EDGES IF THEY ARE INCOMING AND OUTGOING
                    //graph.RemoveEdge(outgoing.ElementAt(i));
                    // Debug.Log(outgoing);
                }

            }
        }
        List<GameObject> tmp = actionHistoryObjects;
        tmp.Reverse();
        foreach (GameObject g in tmp)
        {
            g.TryGetComponent(out NodeRef nodeRef);
            graph.RemoveNode(nodeRef.Value);
            Debug.Log("Removed " + g.name + " from graph");
        }


        // FIXME(Mr. Frenzel): justNodes currently 
        oldPosition.AddLast(oldPositions);
        actionHistory.AddLast(NodesAndascendingEdges);
        // parentCities.AddLast(gameObjectCity);
    }

    /// <summary>
    /// 
    /// </summary>
    public List<Vector3> UndoDeleteOperation()
    {
        List<Vector3> oldPositionVector = oldPosition.Last();
        List<GameObject> undo = actionHistory.Last();
        undo.Reverse();

        foreach (List<GameObject> goList in actionHistory)
        {

            foreach (GameObject go in goList)
            {
                Debug.Log("golistenName" + go.name);
            }
        }


        foreach (GameObject go in undo)
        {
            if (go.TryGetComponent(out NodeRef nodeRef))
            {
                graph.AddNode(nodeRef.Value);
            }

            if (go.TryGetComponent(out EdgeRef edgeReference))
            {
                // graph.AddEdge(edgeReference.edge);
                go.SetVisibility(true, false);
            }

            if (go.TryGetComponentOrLog(out Collider collider))
            {
                collider.enabled = true;
            }
        }
        actionHistory.RemoveLast();

        
        Debug.Log(actionHistory.Count + "count der ActHistList");
        oldPosition.RemoveLast();
        return oldPositionVector;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public GameObject GetPortalFromGarbageObjects()
    {
        GameObject parent = parentCities.Last();
        parentCities.RemoveLast();
        return parent;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="counter"></param>
    /// <param name="states"></param>
    private static void CheckBoundaries(int counter, LinkedList<ActionState.Type> states)
    {
        if (counter == 0 || counter > states.Count)
        {

            throw new NotSupportedException("Redo function cannot be executed");

        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="actionState"></param>
    private static void InverActionStateExecute(ActionState.Type actionState)
    {
        // if actionstate == ADdNode 
        // {
        //  -->delete
        //  }

        //  if(ActionState == add)
        //     {
        //    --> AddNode();
        //   }
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

    public LinkedList<List<GameObject>> GetActionHistory()
    {
        return actionHistory;
    }
}
