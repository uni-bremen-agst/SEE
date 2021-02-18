using SEE.DataModel;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections;
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


        //FIXME: Just For Testing
        private ActionHistory aH;

        private GameObject garbageCan;

        private void Start()
        {
            //FIXME: Just For Testing - AddComponent --> garbage/ trash
            aH = new ActionHistory();

            // An anonymous delegate is registered for the event <see cref="ActionState.OnStateChanged"/>.
            // This delegate will be called from <see cref="ActionState"/> upon every
            // state changed where the passed parameter is the newly entered state.
            ActionState.OnStateChanged += (ActionState.Type newState) =>
            {
                // Is this our action state where we need to do something?
                if (newState == ThisActionState)
                {
                    // The monobehaviour is enabled and Update() will be called by Unity.
                    garbageCan = GameObject.Find("GarbageCan");
                    enabled = true;
                    InteractableObject.LocalAnySelectIn += LocalAnySelectIn;
                    InteractableObject.LocalAnySelectOut += LocalAnySelectOut;
                }
                else
                {
                    // The MonoBehaviour is disabled and Update() no longer be called by Unity.
                    enabled = false;
                    InteractableObject.LocalAnySelectIn -= LocalAnySelectIn;
                    InteractableObject.LocalAnySelectOut -= LocalAnySelectOut;
                }
            };
            enabled = ActionState.Is(ThisActionState);
        }

        private void Update()
        {
            // This script should be disabled, if the action state is not this action's type
            if (!ActionState.Is(ThisActionState))
            {
                // The MonoBehaviour is disabled and Update() no longer be called by Unity.
                enabled = false;
                InteractableObject.LocalAnySelectIn -= LocalAnySelectIn;
                InteractableObject.LocalAnySelectOut -= LocalAnySelectOut;
                return;
            }

            if (selectedObject != null && Input.GetMouseButtonDown(0))
            {
                Assert.IsTrue(selectedObject.HasNodeRef() || selectedObject.HasEdgeRef());
                if (selectedObject.CompareTag(Tags.Edge))
                {
                    // selectedObject.GetComponent<ActionHistory>().saveObjectForUndo(selectedObject, ThisActionState);
                    Destroyer.DestroyGameObject(selectedObject);
                }
                else if (selectedObject.CompareTag(Tags.Node))
                {
                    StartCoroutine(MoveNodeToGarbage(selectedObject));
                }
            }
            //FIXME : undo just For Testing with right mouse button - later it should be connected with an graphical garbabe can
            if (Input.GetMouseButtonDown(1))
            {
                StartCoroutine(RemoveNodeFromGarbage(selectedObject));
            }
        }

        public IEnumerator MoveNodeToGarbage(GameObject deletedNode)
        {
            float tmpx = deletedNode.transform.position.x;
            float tmpy = deletedNode.transform.position.y;
            float tmpz = deletedNode.transform.position.z;
            Tweens.Move(deletedNode, new Vector3(garbageCan.transform.position.x, garbageCan.transform.position.y + 1.4f, garbageCan.transform.position.z), 1f);
            yield return new WaitForSeconds(1.5f);
            Tweens.Move(deletedNode, new Vector3(garbageCan.transform.position.x, garbageCan.transform.position.y, garbageCan.transform.position.z), 1f);
            yield return new WaitForSeconds(1.0f);

            aH.SaveObjectForUndo(deletedNode, ThisActionState,tmpx,tmpy,tmpz);
        }

        public IEnumerator RemoveNodeFromGarbage(GameObject deletedNode)
        {
            Vector3 oldPosition = aH.UndoDeleteOperation();
            Tweens.Move(deletedNode, new Vector3(garbageCan.transform.position.x, garbageCan.transform.position.y + 1.4f, garbageCan.transform.position.z), 1f);
            yield return new WaitForSeconds(1.5f);
            Tweens.Move(deletedNode, oldPosition, 1f);
            yield return new WaitForSeconds(2.0f);
        }

        private void LocalAnySelectIn(InteractableObject interactableObject)
        {
            // FIXME: For an unknown reason, the mouse events in InteractableObject will be
            // triggered twice per frame, which causes this method to be called twice.
            // We need to further investigate this issue.
            // Assert.IsNull(selectedObject);
            selectedObject = interactableObject.gameObject;
        }

        private void LocalAnySelectOut(InteractableObject interactableObject)
        {
            // FIXME: For an unknown reason, the mouse events in InteractableObject will be
            // triggered twice per frame, which causes this method to be called twice.
            // We need to further investigate this issue.
            // Assert.IsTrue(selectedObject == interactableObject.gameObject);
            selectedObject = null;
        }
    }
}
