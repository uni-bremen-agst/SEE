using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class is responsible for saving the deleted objects in a history for the possibility of an undo operation.
    /// </summary>
    public class ActionHistory : MonoBehaviour
    {
        /// <summary>
        /// A history of all actions of the user for the possibility of an undo. 
        /// </summary>
        public List<AbstractPlayerAction> ActionHistoryList { get; set; } = new List<AbstractPlayerAction>();

        public int Pointer { get; set; } = -1;

        public void Update()
        {
            if (ActionHistoryList.Count != 0)
            {
                ActionHistoryList.Last().Update();
            }
            if (Pointer > -2)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    if (Input.GetKeyDown(KeyCode.Z))
                    {
                        Pointer++;
                        Debug.Log("POINTER" + Pointer);
                        ActionHistoryList[Pointer].Redo();
                        Debug.Log("Redo" + Pointer);
                    }
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    ActionHistoryList[Pointer].Undo();
                    Pointer--;
                    Debug.Log("Undo" + Pointer);
                }
            }
        }

        public void Start()
        {

        }

        /// <summary>
        /// Calls the undo of the last executed PlayerAction
        /// </summary>
        public void Undo()
        {
            ActionHistoryList[Pointer].Undo();
        }

        /// <summary>
        /// Calls the redo of the last executed PlayerAction
        /// </summary>
        public void Redo()
        {
            ActionHistoryList[Pointer].Redo();
        }

    }
}
