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

        internal override void ExecuteOnClient()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Block " + id;
            go.AddComponent<BlockID>().id = id;
            go.transform.position = position;
        }

        internal override void ExecuteOnServer()
        {
            id = ++lastID;
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
            foreach (BlockID blockID in Object.FindObjectsOfType<BlockID>())
            {
                if (blockID.id == id)
                {
                    Object.Destroy(blockID.gameObject);
                    return;
                }
            }
        }

        internal override void UndoOnServer()
        {
        }
    }

}
