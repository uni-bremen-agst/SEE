using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    internal class BufferedPacketsPacket : AbstractPacket
    {
        internal static readonly string PACKET_TYPE = "BufferedPackets";

        public string[] packetTypes;
        public string[] packetDatas;

        public BufferedPacketsPacket(string[] packetTypes, string[] packetDatas) : base(PACKET_TYPE)
        {
            Assert.IsNotNull(packetTypes);
            Assert.IsNotNull(packetDatas);

            this.packetTypes = packetTypes;
            this.packetDatas = packetDatas;
        }

        internal override string Serialize()
        {
            string result = JsonUtility.ToJson(this);
            return result;
        }

        internal static BufferedPacketsPacket Deserialize(string data)
        {
            BufferedPacketsPacket result = JsonUtility.FromJson<BufferedPacketsPacket>(data);
            return result;
        }
    }

}
