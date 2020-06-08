using NetworkCommsDotNet.Connections;
using SEE.Controls;
using SEE.Game;
using UnityEngine;

namespace SEE.Net
{

    internal class GameStatePacket : AbstractPacket
    {
        public uint[] zoomIDStack;
        public uint[] selectedGameObjectIDs;

        public GameStatePacket()
        {
        }

        public GameStatePacket(GameState gameState)
        {
            zoomIDStack = gameState.zoomIDStack.ToArray();
            selectedGameObjectIDs = gameState.selectedGameObjectIDs.ToArray();
        }

        internal override string Serialize()
        {
            string result = JsonUtility.ToJson(this);
            return result;
        }

        internal override void Deserialize(string serializedPacket)
        {
            GameStatePacket packet = JsonUtility.FromJson<GameStatePacket>(serializedPacket);
            zoomIDStack = packet.zoomIDStack;
            selectedGameObjectIDs = packet.selectedGameObjectIDs;
        }

        internal override bool ExecuteOnServer(Connection connection)
        {
            return true;
        }

        internal override bool ExecuteOnClient(Connection connection)
        {
            GameObject[] gameObjects = new GameObject[zoomIDStack.Length];
            for (int i = 0; i < zoomIDStack.Length; i++)
            {
                gameObjects[zoomIDStack.Length - 1 - i] = InteractableObject.Get(zoomIDStack[i]).gameObject;
            }
            Transformer.SetInitialState(gameObjects);

            foreach (uint id in selectedGameObjectIDs)
            {
                ((HoverableObject)InteractableObject.Get(id)).Hovered(false);
            }
            return true;
        }
    }

}
