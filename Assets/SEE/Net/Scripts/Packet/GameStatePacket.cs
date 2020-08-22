using NetworkCommsDotNet.Connections;
using SEE.Controls;
using SEE.Game;
using UnityEngine;

namespace SEE.Net
{

    /// <summary>
    /// Used for sending the <see cref="GameState"/> to a newly connected client.
    /// </summary>
    internal class GameStatePacket : AbstractPacket
    {
        // <see cref="GameState"/>
        public uint[] zoomIDStack;

        // <see cref="GameState"/>
        public uint[] selectedGameObjectIDs;

        /// <summary>
        /// Empty constructor is necessary for JsonUtility-serialization.
        /// </summary>
        public GameStatePacket()
        {
        }

        /// <summary>
        /// Constructs a packet with given game state.
        /// </summary>
        /// <param name="gameState">The game state.</param>
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

        /// <summary>
        /// Initializes the game with given game state.
        /// </summary>
        /// <param name="connection">The connection of the packet.</param>
        /// <returns></returns>
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
                Outline.Create(InteractableObject.Get(id).gameObject);
            }
            return true;
        }
    }

}
