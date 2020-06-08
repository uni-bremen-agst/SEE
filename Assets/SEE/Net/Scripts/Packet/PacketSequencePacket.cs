using NetworkCommsDotNet.Connections;
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

        internal override bool ExecuteOnServer(Connection connection)
        {
            Assert.IsNotNull(connection);

            bool result = Server.incomingPacketSequenceIDs.TryGetValue(connection, out ulong nextID);
            if (result && id == nextID)
            {
                Server.incomingPacketSequenceIDs[connection] = ++nextID;
                foreach (string serializedPacket in serializedPackets)
                {
                    AbstractPacket packet = PacketSerializer.Deserialize(serializedPacket);
                    packet.ExecuteOnServer(connection);
                }
                return true;
            }

            return false;
        }

        internal override bool ExecuteOnClient(Connection connection)
        {
            Assert.IsNotNull(connection);

            if (id == Client.incomingPacketID)
            {
                Client.incomingPacketID++;
                foreach (string serializedPacket in serializedPackets)
                {
                    AbstractPacket packet = PacketSerializer.Deserialize(serializedPacket);
                    packet.ExecuteOnClient(connection);
                }
                return true;
            }
            return false;
        }
    }

}
