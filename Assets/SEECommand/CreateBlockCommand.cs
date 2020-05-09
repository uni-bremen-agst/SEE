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

        internal override void ExecuteOnServer()
        {
            id = ++lastID;
        }

        internal override KeyValuePair<GameObject[], GameObject[]> ExecuteOnClient()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Block " + id;
            go.transform.position = position;
            Interactable interactable = go.AddComponent<Interactable>();
            interactable.id = id;

            GameObject[] originalGameObjects = new GameObject[] { null };
            GameObject[] copiedAndModifiedGameObjects = new GameObject[] { go };
            KeyValuePair<GameObject[], GameObject[]> result = new KeyValuePair<GameObject[], GameObject[]>(originalGameObjects, copiedAndModifiedGameObjects);
            return result;
        }
    }

}
