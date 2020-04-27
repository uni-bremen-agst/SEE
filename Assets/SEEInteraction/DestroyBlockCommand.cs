using UnityEngine;

namespace SEE.Command
{

    public class DestroyBlockCommand : AbstractCommand
    {
        public int id;

        // for undo
        private Vector3 undoPosition;

        public DestroyBlockCommand(int id) : base(true)
        {
            this.id = id;
        }

        internal override void ExecuteOnClient()
        {
            foreach (BlockID blockID in Object.FindObjectsOfType<BlockID>())
            {
                if (blockID.id == id)
                {
                    undoPosition = blockID.transform.position;
                    Object.Destroy(blockID.gameObject);
                    return;
                }
            }
        }

        internal override void ExecuteOnServer()
        {
        }

        internal override void RedoOnClient()
        {
            ExecuteOnClient();
        }

        internal override void RedoOnServer()
        {
        }

        internal override void UndoOnClient()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Block " + id;
            go.AddComponent<BlockID>().id = id;
            go.transform.position = undoPosition;
        }

        internal override void UndoOnServer()
        {
        }
    }

}
