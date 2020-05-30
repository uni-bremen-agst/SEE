using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    internal class PacketSequencePacket : AbstractPacket
    {
        public ulong id;
        public string[] serializedPackets;

        public PacketSequencePacket()
        {
        }

        public PacketSequencePacket(ulong id, string[] serializedPackets)
        {
            Assert.IsNotNull(serializedPackets);

            this.id = id;
            this.serializedPackets = serializedPackets;
        }

        internal override string Serialize()
        {
            string result = JsonUtility.ToJson(this);
            return result;
        }

        internal override void Deserialize(string serializedPacket)
        {
            PacketSequencePacket packet = JsonUtility.FromJson<PacketSequencePacket>(serializedPacket);
            id = packet.id;
            serializedPackets = packet.serializedPackets;
        }
    }

}
