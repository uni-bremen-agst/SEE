using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to hide/show the currently selected game object (edge or node).
    /// </summary>
    public class HideAction : MonoBehaviour
    {
        private readonly ActionStateType ThisActionState = ActionStateType.Hide;

        /// <summary>
        /// The currently selected object (a node or edge).
        /// </summary>
        private GameObject selectedObject;

        // Start is called before the first frame update
        void Start()
        {
            ActionState.OnStateChanged += newState =>
            {
                if (Equals(newState, ThisActionState))
                {
                    // The monobehaviour is enabled and Update() will be called by Unity.
                    enabled = true;
                }
                else
                {
                    // The monobehaviour is diabled and Update() no longer be called by Unity.
                    enabled = false;
                }
            };
            enabled = ActionState.Is(ThisActionState);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}