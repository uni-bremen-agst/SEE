using SEE.Controls.Actions;
using SEE.GO;
using SEE.Utils;
using System;
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
    private LinkedList<Vector3> oldPosition = new LinkedList<Vector3>();

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
    /// <param name="gameObject"></param>
    /// <param name="aState"></param>
    public void SaveObjectForUndo(GameObject gameObject, ActionState.Type aState, float x, float y, float z)
    {
        if (gameObject == null)
        {
            Debug.LogError("null operation");
        }
        if (count == actionStates.Count)
        {
            count++;
        }
        else
        {
            count = actionStates.Count + 1;
        }
        actionStates.AddLast(aState);
        List<GameObject> NodesAndAscendingEdges = new List<GameObject>();
        gameObject.SetVisibility(false, true);

        if (gameObject.TryGetComponent(out NodeRef nodeRef))
        {
            HashSet<string> edgeIDs = Destroyer.GetEdgeIds(nodeRef);
            //Question: Performance in graphs with many edges like SEE?
            foreach (GameObject edge in GameObject.FindGameObjectsWithTag(SEE.DataModel.Tags.Edge))
            {
                if (edge.activeInHierarchy && edgeIDs.Contains(edge.name))
                {
                    edge.SetVisibility(false, true);
                    if (!NodesAndAscendingEdges.Contains(edge))
                    {
                        NodesAndAscendingEdges.Add(edge);
                    }
                }
            }

            if (gameObject.TryGetComponentOrLog(out Collider collider))
            {
                collider.enabled = false;
            }
            NodesAndAscendingEdges.Add(gameObject);
        }
        actionHistory.AddLast(NodesAndAscendingEdges);
        oldPosition.AddLast(new Vector3(x, y, z));
    }

    /// <summary>
    /// 
    /// </summary>
    public Vector3 UndoDeleteOperation()
    {
        Vector3 oldPositionVector = new Vector3();
        if (actionStates == null || actionStates.Count == 0)
        {
            return oldPositionVector;
        }
        // CheckBoundaries(count, actionStates);
        List<GameObject> undo = actionHistory.ElementAt(count - 1);
        oldPositionVector = oldPosition.ElementAt(count - 1);
        foreach (GameObject go in undo)
        {
            go.SetVisibility(true, true);
            if (go.TryGetComponentOrLog(out Collider collider))
            {
                collider.enabled = true;
            }
        }
        if (count > 0)
        {
            count--;
        }

        return oldPositionVector;
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
            Debug.Log(counter + "counter");
            throw new NotSupportedException("Redo function cannot be executed");

        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="actionState"></param>
    private static void InvertActionStateExecute(ActionState.Type actionState)
    {
       // if actionstate == ADdNode
       // {
       //     --> delete
       // }

       // if (ActionState == add)
       // {
       //     --> AddNode();
       // }


    }
}
