using UnityEngine;

namespace SEE.Net
{

    internal class GameStatePacket : AbstractPacket
    {
        public uint[] zoomStack;

        public GameStatePacket()
        {

        }

        public GameStatePacket(uint[] zoomStack)
        {
            this.zoomStack = zoomStack;
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
        }
    }

}
