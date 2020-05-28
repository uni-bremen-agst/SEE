using UnityEngine;

namespace SEE.Controls
{

    public class CreateBlockAction : AbstractAction
    {
        public static int lastID = -1;

        public int id;
        public Vector3 position;



        public CreateBlockAction(Vector3 position) : base(true)
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
            ExampleInteractable interactable = go.AddComponent<ExampleInteractable>();
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
            foreach (ExampleInteractable interactable in Object.FindObjectsOfType<ExampleInteractable>())
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
            foreach (ExampleInteractable interactable in Object.FindObjectsOfType<ExampleInteractable>())
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
