using UnityEngine;

namespace SEE.Command
{

    public class CreateBlockCommand : AbstractCommand
    {
        public static int lastID = -1;

        public int id;
        public Vector3 position;

        public CreateBlockCommand(Vector3 position) : base(true)
        {
            this.position = position;
        }

        protected override bool ExecuteOnServer()
        {
            id = ++lastID;
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Block " + id;
            go.transform.position = position;
            Interactable interactable = go.AddComponent<Interactable>();
            interactable.id = id;
            interactable.lastBufferedPosition = go.transform.position;
            return true;
        }

        protected override bool UndoOnServer()
        {
            return true;
        }

        protected override bool UndoOnClient()
        {
            foreach (Interactable interactable in Object.FindObjectsOfType<Interactable>())
            {
                if (interactable.id == id)
                {
                    Object.Destroy(interactable.gameObject);
                    break;
                }
            }
            return true;
        }

        protected override bool RedoOnServer()
        {
            return true;
        }

        protected override bool RedoOnClient()
        {
            foreach (Interactable interactable in Object.FindObjectsOfType<Interactable>())
            {
                if (interactable.id == id)
                {
                    return false;
                }
            }
            bool result = ExecuteOnClient();
            return result;
        }
    }

}
