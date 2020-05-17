using System.Collections.Generic;
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

        protected override void ExecuteOnServer()
        {
            id = ++lastID;
        }

        protected override void ExecuteOnClient()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Block " + id;
            go.transform.position = position;
            Interactable interactable = go.AddComponent<Interactable>();
            interactable.id = id;
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
                    Object.Destroy(interactable.gameObject);
                }
            }
        }

        protected override void RedoOnServer()
        {
        }

        protected override void RedoOnClient()
        {
            ExecuteOnClient();
        }
    }

}
