using SEE.Controls.Actions;
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

    private LinkedList<List<GameObject>> actionHistory = new LinkedList<List<GameObject>>();

    private LinkedList<Vector3> oldPosition = new LinkedList<Vector3>();

    private LinkedList<GameObject> parentCities = new LinkedList<GameObject>();

    private Graph graph;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

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
    public void SaveObjectForUndo(GameObject actionHistoryObject, float x, float y, float z, GameObject gameObjectCity)
    {
        if (actionHistoryObject == null)
        {
            Debug.LogError("null operation");
        }

        SEECity city;
        city = SceneQueries.GetCodeCity(actionHistoryObject.transform)?.gameObject.GetComponent<SEECity>();
        graph = city.LoadedGraph;

        List<GameObject> NodesAndascendingEdges = new List<GameObject>();
        actionHistoryObject.SetVisibility(false, true);

        if (actionHistoryObject.TryGetComponent(out NodeRef nodeRef))
        {

            HashSet<string> edgeIDs = Destroyer.GetEdgeIds(nodeRef);
            foreach (GameObject edge in GameObject.FindGameObjectsWithTag(SEE.DataModel.Tags.Edge))
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
            
            if (actionHistoryObject.TryGetComponentOrLog(out Collider collider))
            {
                actionHistoryObject.GetComponent<Collider>().enabled = false;
            }
            NodesAndascendingEdges.Add(actionHistoryObject);
        }
        actionHistory.AddLast(NodesAndascendingEdges);
        oldPosition.AddLast(new Vector3(x, y, z));

        actionHistoryObject.TryGetComponent(out NodeRef node);

        List<Edge> incoming = node.Value.Incomings;
        List<Edge> outgoing = node.Value.Outgoings;

        for(int i = 0; i  < incoming.Count; i ++)
        {
            graph.RemoveEdge(incoming.ElementAt(i));
        }

        for (int i = 0; i < outgoing.Count; i++)
        {
            graph.RemoveEdge(outgoing.ElementAt(i));
        }

        graph.RemoveNode(node.Value);

        parentCities.AddLast(gameObjectCity);
    }

    /// <summary>
    /// 
    /// </summary>
    public Vector3 UndoDeleteOperation()
    {
        Vector3 oldPositionVector = oldPosition.Last();

        Debug.Log(oldPosition.Count);

        List<GameObject> undo = actionHistory.Last();
        undo.Reverse();

        foreach (GameObject go in undo)
        {
            if (go.TryGetComponent(out NodeRef nodeRef))
            {
                graph.AddNode(nodeRef.Value);
            }

            if (go.TryGetComponent(out EdgeRef edgeReference))
            {
                graph.AddEdge(edgeReference.edge);
            }

            go.SetVisibility(true, false);

            if (go.TryGetComponentOrLog(out Collider collider))
            {
                collider.enabled = true;
            }
        }

        Debug.Log(actionHistory.Last.Value);
        actionHistory.RemoveLast();
        oldPosition.RemoveLast();
        Debug.Log(oldPositionVector);
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
