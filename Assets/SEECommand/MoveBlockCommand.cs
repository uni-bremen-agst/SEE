using UnityEngine;

namespace SEE.Command
{

    public class MoveBlockCommand : AbstractCommand
    {
        public int id;
        public Vector3 originalPosition;
        public Vector3 newPosition;

        public MoveBlockCommand(GameObject block, Vector3 originalPosition, Vector3 newPosition, bool buffer) : base(buffer)
        {
            id = block.GetComponent<Interactable>().id;
            this.originalPosition = originalPosition;
            this.newPosition = newPosition;
        }

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            foreach (Interactable interactable in Object.FindObjectsOfType<Interactable>())
            {
                if (interactable.id == id)
                {
                    interactable.transform.position = newPosition;
                    return;
                }
            }

            Assertions.InvalidCodePath("Only existing objects can be moved!");
        }

        protected override void UndoOnServer()
        {
        }

        protected override void UndoOnClient()
        {
            foreach (Interactable interactable in Object.FindObjectsOfType<Interactable>())
            {
                if (interactable.id == id)
                {
                    if (interactable.transform.position == newPosition)
                    {
                        interactable.transform.position = originalPosition;
                    }
                    return;
                }
            }

            Assertions.InvalidCodePath("Only movement of existing objects can be undone!");
        }

        protected override void RedoOnServer()
        {
        }

        protected override void RedoOnClient()
        {
        }
    }

}
