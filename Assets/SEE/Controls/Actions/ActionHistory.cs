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
    private int count = 0;

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
    public void SaveObjectForUndo(GameObject actionHistoryObject, ActionState.Type aState, float x, float y, float z, GameObject gameObjectCity)
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

        oldPosition.AddLast(new Vector3(x, y, z));

        graph.RemoveNode(node.Value);

        parentCities.AddLast(gameObjectCity);
    }

    /// <summary>
    /// 
    /// </summary>
    public Vector3 UndoDeleteOperation()
    {
        Vector3 oldPositionVector = new Vector3();

        oldPositionVector = oldPosition.ElementAt(count - 1);

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

        actionHistory.RemoveLast();
        return oldPositionVector;
    }

    public GameObject GetPortalFromGarbageObjects()
    {
        GameObject parent = parentCities.Last();
        parentCities.RemoveLast();
        return parent;
    }

    private static void CheckBoundaries(int counter, LinkedList<ActionState.Type> states)
    {
        if (counter == 0 || counter > states.Count)
        {

            throw new NotSupportedException("Redo function cannot be executed");

        }
    }

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

    private static void RemoveForReflexionAnalysis(GameObject removedObject)
    {
        // remove node or edge
    }

    private static void Reparent(GameObject objectToAddtoGraphHierarchy)
    {
        //add edge or node - reparenting.
    }

}
