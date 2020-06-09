using NetworkCommsDotNet.Connections;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// Contains multiple packets, that will be executed one after another in order.
    /// </summary>
    internal class PacketSequencePacket : AbstractPacket
    {
        /// <summary>
        /// The ID of this packet sequence.
        /// </summary>
        public ulong id;

        /// <summary>
        /// The serialized packets, this packet contains.
        /// </summary>
        public string[] serializedPackets;

        /// <summary>
        /// Empty constructor is necessary for JsonUtility-serialization.
        /// </summary>
        public PacketSequencePacket()
        {
        }

        /// <summary>
        /// Constructs a packet sequence with given ID and given ordered serialized
        /// packets.
        /// </summary>
        /// <param name="id">The ID of the packet sequence.</param>
        /// <param name="serializedPackets">The ordered serialized packets.</param>
        public PacketSequencePacket(ulong id, string[] serializedPackets)
        {
            Assert.IsNotNull(serializedPackets);

            this.id = id;
            this.serializedPackets = serializedPackets;
        }

        /// <summary>
        /// Serializes the packet sequence into a string.
        /// </summary>
        /// <returns>The serialized packet sequence as a string.</returns>
        internal override string Serialize()
        {
            string result = JsonUtility.ToJson(this);
            return result;
        }

        /// <summary>
        /// Deserializes the packet sequence from a string.
        /// </summary>
        /// <returns>The deserialized packet sequence.</returns>
        internal override void Deserialize(string serializedPacket)
        {
            PacketSequencePacket packet = JsonUtility.FromJson<PacketSequencePacket>(serializedPacket);
            id = packet.id;
            serializedPackets = packet.serializedPackets;
        }

        /// <summary>
        /// Executes every packet in this packet sequence one after another as a server.
        /// </summary>
        /// <param name="connection">The connecting of the packet.</param>
        /// <returns><code>true</code> if each of the packets could be executed, <code>false</code>
        /// otherwise.</returns>
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

        /// <summary>
        /// Executes every packet in this packet sequence one after another as a client.
        /// </summary>
        /// <param name="connection">The connecting of the packet.</param>
        /// <returns><code>true</code> if each of the packets could be executed, <code>false</code>
        /// otherwise.</returns>
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
