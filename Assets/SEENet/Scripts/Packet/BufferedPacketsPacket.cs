using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    internal class BufferedPacketsPacket : AbstractPacket
    {
        public string[] packetDatas;



        public BufferedPacketsPacket()
        {
        }

        public BufferedPacketsPacket(string[] packetDatas)
        {
            Assert.IsNotNull(packetDatas);
            this.packetDatas = packetDatas;
        }



        internal override string Serialize()
        {
            string result = JsonUtility.ToJson(this);
            return result;
        }

        internal override void Deserialize(string serializedPacket)
        {
            BufferedPacketsPacket deserializedPacket = JsonUtility.FromJson<BufferedPacketsPacket>(serializedPacket);
            packetDatas = deserializedPacket.packetDatas;
        }
    }

}
