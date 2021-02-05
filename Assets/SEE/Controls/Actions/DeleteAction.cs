using SEE.DataModel;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to delete the currently selected game object (edge or node).
    /// </summary>
    class DeleteAction : MonoBehaviour
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
        const ActionState.Type ThisActionState = ActionState.Type.Delete;

        /// <summary>
        /// The currently selected object (a node or edge).
        /// </summary>
        private GameObject selectedObject;

        private void Start()
        {
            // An anonymous delegate is registered for the event <see cref="ActionState.OnStateChanged"/>.
            // This delegate will be called from <see cref="ActionState"/> upon every
            // state changed where the passed parameter is the newly entered state.
            ActionState.OnStateChanged += (ActionState.Type newState) =>
            {
                // Is this our action state where we need to do something?
                if (newState == ThisActionState)
                {
                    // The monobehaviour is enabled and Update() will be called by Unity.
                    enabled = true;
                    InteractableObject.LocalAnySelectIn += LocalAnySelectIn;
                    InteractableObject.LocalAnySelectOut += LocalAnySelectOut;
                }
                else
                {
                    // The monobehaviour is diabled and Update() no longer be called by Unity.
                    enabled = false;
                    InteractableObject.LocalAnySelectIn -= LocalAnySelectIn;
                    InteractableObject.LocalAnySelectOut -= LocalAnySelectOut;
                }
            };
            enabled = ActionState.Is(ThisActionState);
        }

        private void Update()
        {
            Assert.IsTrue(ActionState.Is(ThisActionState));
            if (selectedObject != null) // Input.GetMouseButtonDown(0) && 
            {
                Assert.IsTrue(selectedObject.HasNodeRef() || selectedObject.HasEdgeRef());
                if (selectedObject.CompareTag(Tags.Edge))
                {
                    Destroyer.DestroyGameObject(selectedObject);
                }
                else if (selectedObject.CompareTag(Tags.Node))
                {
                    Destroyer.DestroyGameObjectWithChildren(selectedObject);
                }
            }
        }

        private void LocalAnySelectIn(InteractableObject interactableObject)
        {
            Assert.IsNull(selectedObject);
            selectedObject = interactableObject.gameObject;
        }

        private void LocalAnySelectOut(InteractableObject interactableObject)
        {
            Assert.IsTrue(selectedObject == interactableObject.gameObject);
            selectedObject = null;
        }
    }
}
