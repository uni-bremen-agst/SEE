using System.Collections.Generic;
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

        internal override void ExecuteOnServer()
        {
        }

        internal override KeyValuePair<GameObject[], GameObject[]> ExecuteOnClient()
        {
            foreach (Interactable interactable in Object.FindObjectsOfType<Interactable>())
            {
                if (interactable.id == id)
                {
                    GameObject go = null;
                    if (buffer)
                    {
                        go = Object.Instantiate(interactable.gameObject);
                        interactable.gameObject.transform.position = originalPosition;
                    }
                    else
                    {
                        go = interactable.gameObject;
                    }
                    go.transform.position = newPosition;

                    GameObject[] originalGameObjects = new GameObject[] { interactable.gameObject };
                    GameObject[] copiedAndModifiedGameObjects = new GameObject[] { go };
                    KeyValuePair<GameObject[], GameObject[]> result = new KeyValuePair<GameObject[], GameObject[]>(originalGameObjects, copiedAndModifiedGameObjects);
                    return result;
                }
            }

            Assertions.InvalidCodePath("Currently, only existing objects should be attempted to be moved!");
            return new KeyValuePair<GameObject[], GameObject[]>();
        }
    }

}
