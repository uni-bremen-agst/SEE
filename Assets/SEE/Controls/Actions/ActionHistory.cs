using SEE.Controls.Actions;
using SEE.DataModel.DG;
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
    public void saveObjectForUndo(GameObject gameObject, ActionState.Type aState)
    {
        if (gameObject == null)
        {
            Debug.LogError("null operation");
        }
    
        
        if (count == actionStates.Count) {
            count++;
                } else
        {
            count = actionStates.Count + 1;
        }
        actionStates.AddLast(aState);
        List<GameObject> NodesAndascendingEdges = new List<GameObject>();
        gameObject.SetVisibility(false, true); 

            if(gameObject.TryGetComponent(out NodeRef nodeRef))
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
            if (gameObject.TryGetComponentOrLog(out collider)) {
                gameObject.GetComponent<Collider>().enabled = false;
            }
           NodesAndascendingEdges.Add(gameObject);
        }
        actionHistory.AddLast(NodesAndascendingEdges);
    }

    /// <summary>
    /// 
    /// </summary>
    public void UndoDeleteOperation()
    {
        Debug.Log(count + "counter");
        if (actionStates == null || actionStates.Count == 0)
        {
            return;
        }

        // checkBoundaries(count, actionStates);

        List<GameObject> undo = actionHistory.ElementAt(count -1);

        foreach (GameObject go in undo)
        {
            go.SetVisibility(true, true);
            Collider collider;
            if(go.TryGetComponentOrLog(out collider))
            { 
                collider.enabled = true;
            }
        }
        if (count > 0)
        {
            count--;
        }
    }

    private static void checkBoundaries(int counter, LinkedList<ActionState.Type> states)
    {
        if (counter == 0 || counter > states.Count )
        {
            Debug.Log(counter + "counter");
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
}
