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
using UnityEngine.Assertions;

public class ActionHistory : MonoBehaviour
{

    public LinkedList<List<GameObject>> actionHistory = new LinkedList<List<GameObject>>();

    private LinkedList<List<Vector3>> oldPosition = new LinkedList<List<Vector3>>();

    private LinkedList<GameObject> parentCities = new LinkedList<GameObject>();

    private LinkedList<List<GameObject>> allEdges = new LinkedList<List<GameObject>>();

    private Graph graph;

    int count = 0; 
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
        //question to goedecke: when should this case be reached ? Maybe delete it ? At least selected object is inside

        // context given: never to be honest - it is more like a defensive programming habit.
        // there only might be null when the method of deleteAction.cs is moved...
        if (actionHistoryObjects == null)
        {
            Debug.LogError("null operation");
        }

        SEECity city;
        city = SceneQueries.GetCodeCity(actionHistoryObjects[0].transform)?.gameObject.GetComponent<SEECity>();
        graph = city.LoadedGraph;
        List<GameObject> nodesAndascendingEdges = new List<GameObject>();
        List<GameObject> edgesToHide = new List<GameObject>();

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

       

        List<GameObject> tmp = actionHistoryObjects;
        tmp.Reverse();

        //deleting all children (for goedecke: childs) of node
        foreach (GameObject g in tmp)
        {
            g.TryGetComponent(out NodeRef nodeRef);
            graph.RemoveNode(nodeRef.Value);
        }

        city.LoadedGraph = graph; // FIXME: Necessary?
        allEdges.AddLast(edgesToHide);
        oldPositions.Reverse();
        nodesAndascendingEdges.Reverse();
        oldPosition.AddLast(oldPositions);
        actionHistory.AddLast(nodesAndascendingEdges);
    }

    /// <summary>
    /// 
    /// </summary>
    public List<Vector3> UndoDeleteOperation()
    {
        List<Vector3> oldPositionVector = oldPosition.Last();
        List<GameObject> undo = actionHistory.Last(); 

        foreach (GameObject go in undo)
        {
            if (go.TryGetComponent(out NodeRef nodeRef))
            {
                graph.AddNode(nodeRef.Value);
               
            }

            if (go.TryGetComponent(out Collider collider))
            {
                go.GetComponent<Collider>().enabled = true;
            }
        }

        foreach (GameObject edgeTobeShown in allEdges.Last())
        {
            if (edgeTobeShown.TryGetComponent(out EdgeRef edgeReference))
            {
                graph.AddEdge(edgeReference.edge);
                edgeTobeShown.SetVisibility(true, false);
            }
        }

        allEdges.RemoveLast();
        actionHistory.RemoveLast();
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
