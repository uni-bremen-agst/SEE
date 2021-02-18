using SEE.Controls.Actions;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActionHistory : MonoBehaviour
{

    /// <summary>
    /// Start() will register an anonymous delegate of type 
    /// <see cref="ActionState.OnStateChangedFn"/> on the event
    /// <see cref="ActionState.OnStateChanged"/> to be called upon every
    /// change of the action state, where the newly entered state will
    /// be passed as a parameter. The anonymous delegate will compare whether
    /// this state equals <see cref="ThisActionState"/> and if so, execute
    /// what needs to be done for this action here. If that parameter is
    /// different from <see cref="ThisActionState"/>, this action will
    /// put itself to sleep. 
    /// Thus, this action will be executed only if the new state is 
    /// <see cref="ThisActionState"/>.
    /// </summary>
    /// 

    //FIXME: Action State Const definieren - PlayerMenu adden
    // const ActionState.Type ThisActionState = ActionState.Type.Undo;

    private LinkedList<List<GameObject>> actionHistory = new LinkedList<List<GameObject>>();
    private LinkedList<ActionState.Type> actionStates = new LinkedList<ActionState.Type>();
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
    public void SaveObjectForUndo(GameObject actionHistoryObject, ActionState.Type aState)
    {
        if (actionHistoryObject == null)
        {
            Debug.LogError("null operation");
        }

        SEECity city;
        // actionHistoryObject.TryGetComponent<SEECity>(out city);
        city = SceneQueries.GetCodeCity(actionHistoryObject.transform)?.gameObject.GetComponent<SEECity>();
        graph = city.LoadedGraph;


        if (count == actionStates.Count)
        {
            count++;
        }
        else
        {
            count = actionStates.Count + 1;
        }
        actionStates.AddLast(aState);
        List<GameObject> NodesAndascendingEdges = new List<GameObject>();
        actionHistoryObject.SetVisibility(false, true);

        if (actionHistoryObject.TryGetComponent(out NodeRef nodeRef))
        {


            HashSet<String> edgeIDs = Destroyer.GetEdgeIds(nodeRef);
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

            Collider collider;
            if (actionHistoryObject.TryGetComponentOrLog(out collider))
            {
                actionHistoryObject.GetComponent<Collider>().enabled = false;
            }
            NodesAndascendingEdges.Add(actionHistoryObject);
        }
        actionHistory.AddLast(NodesAndascendingEdges);

        List<Edge> incoming = new List<Edge>();
        List<Edge> outgoing = new List<Edge>();

        NodeRef node;
        actionHistoryObject.TryGetComponent(out node);   
        incoming = node.Value.Incomings;
        outgoing = node.Value.Outgoings;

        for(int i = 0; i  < incoming.Count; i ++)
        {
            graph.RemoveEdge(incoming.ElementAt(i));
           
        }

        for (int i = 0; i < outgoing.Count; i++)
        {
            graph.RemoveEdge(outgoing.ElementAt(i));
        }
        graph.RemoveNode(node.Value);

    }

    /// <summary>
    /// 
    /// </summary>
    public void UndoDeleteOperation()
    {

        if (actionStates == null || actionStates.Count == 0)
        {
            return;
        }

        // checkBoundaries(count, actionStates);

        List<GameObject> undo = actionHistory.Last();
        undo.Reverse();
        foreach (GameObject go in undo)
        {

            if (go.TryGetComponent(out NodeRef nodeRef))
            {
                graph.AddNode(nodeRef.Value);
                
            }
  
            EdgeRef edgeReference;

            if (go.TryGetComponent(out  edgeReference))
            {
                graph.AddEdge(edgeReference.edge);
            }

            go.SetVisibility(true, false); 
            
            Collider collider;
            if (go.TryGetComponentOrLog(out collider))
            {
                collider.enabled = true;
            }
        }
        actionHistory.RemoveLast();
    }

    private static void checkBoundaries(int counter, LinkedList<ActionState.Type> states)
    {
        if (counter == 0 || counter > states.Count)
        {

            throw new NotSupportedException("Redo function cannot be executed");

        }
    }

    private static void inverActionStateExecute(ActionState.Type actionState)
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

    private static void removeForReflexionAnalysis(GameObject removedObject)
    {
        // remove node or edge
    }

    private static void reparent(GameObject objectToAddtoGraphHierarchy)
    {
        //add edge or node - reparenting.
    }

}
