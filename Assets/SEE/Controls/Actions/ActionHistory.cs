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
        public LinkedList<AbstractPlayerAction> ActionHistoryList { get; set; } = new LinkedList<AbstractPlayerAction>();

        public void Update()
        {
            if (ActionHistoryList.Count != 0)
            {
                ActionHistoryList.Last().Update();
                Debug.Log(ActionHistoryList.Last.Value);
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
            ActionHistoryList.Last().Undo();
            ActionHistoryList.RemoveLast();
        }

        /// <summary>
        /// Calls the redo of the last executed PlayerAction
        /// </summary>
        public void Redo()
        {
            AbstractPlayerAction playerAction = ActionHistoryList.Last();
            ActionHistoryList.AddLast(playerAction);
            ActionHistoryList.Last().Redo();
        }

    }
}
