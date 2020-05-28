using UnityEngine;

namespace SEE.Controls
{

    public class MoveBlockAction : AbstractAction
    {
        public int id;
        public Vector3 originalPosition;
        public Vector3 newPosition;



        public MoveBlockAction(GameObject block, Vector3 originalPosition, Vector3 newPosition, bool buffer) : base(buffer)
        {
            id = block.GetComponent<ExampleInteractable>().id;
            this.originalPosition = originalPosition;
            this.newPosition = newPosition;
        }



        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            foreach (ExampleInteractable interactable in Object.FindObjectsOfType<ExampleInteractable>())
            {
                if (interactable.id == id)
                {
                    if (!buffer)
                    {
                        interactable.lastBufferedPosition = originalPosition;
                    }

                    if (interactable.lastBufferedPosition == originalPosition)
                    {
                        interactable.transform.position = newPosition;
                        if (buffer)
                        {
                            interactable.lastBufferedPosition = newPosition;
                        }
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }

        protected override bool UndoOnServer()
        {
            return true;
        }

        protected override bool UndoOnClient()
        {
            foreach (ExampleInteractable interactable in Object.FindObjectsOfType<ExampleInteractable>())
            {
                if (interactable.id == id)
                {
                    if (interactable.lastBufferedPosition == newPosition)
                    {
                        interactable.transform.position = originalPosition;
                        interactable.lastBufferedPosition = originalPosition;
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }

        protected override bool RedoOnServer()
        {
            return true;
        }

        protected override bool RedoOnClient()
        {
            bool result = ExecuteOnClient();
            return result;
        }
    }

}
