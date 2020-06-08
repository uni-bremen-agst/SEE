using NetworkCommsDotNet.Connections;
using SEE.Controls;
using SEE.Game;
using UnityEngine;

namespace SEE.Net
{

    internal class GameStatePacket : AbstractPacket
    {
        public uint[] zoomStack;
        public uint[] selectedGameObjects;

        public GameStatePacket()
        {

        }

        public GameStatePacket(uint[] zoomStack, uint[] selectedGameObjects)
        {
            this.zoomStack = zoomStack;
            this.selectedGameObjects = selectedGameObjects;
        }

        internal override string Serialize()
        {
            string result = JsonUtility.ToJson(this);
            return result;
        }

        internal override void Deserialize(string serializedPacket)
        {
            GameStatePacket packet = JsonUtility.FromJson<GameStatePacket>(serializedPacket);
            zoomStack = packet.zoomStack;
            selectedGameObjects = packet.selectedGameObjects;
        }

        internal override bool ExecuteOnServer(Connection connection)
        {
            return true;
        }

        internal override bool ExecuteOnClient(Connection connection)
        {
            GameObject[] gameObjects = new GameObject[zoomStack.Length];
            for (int i = 0; i < zoomStack.Length; i++)
            {
                gameObjects[zoomStack.Length - 1 - i] = InteractableObject.Get(zoomStack[i]).gameObject;
            }
            Transformer.SetInitialState(gameObjects);

            foreach (uint id in selectedGameObjects)
            {
                ((HoverableObject)InteractableObject.Get(id)).Hovered(false);
            }
            return true;
        }
    }

}
