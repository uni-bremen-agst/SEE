using System;
using System.Collections.Generic;
using System.Threading;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using UnityEngine.Assertions;

namespace SEE.Net
{
    /// <summary>
    /// Handles incoming packets for server and/or client.
    /// </summary>
    public class PacketHandler
    {
        /// <summary>
        /// Contains info of packets that are still pending.
        /// </summary>
        private struct SerializedPendingPacket
        {
            internal PacketHeader PacketHeader;
            internal Connection Connection;
            internal string SerializedPacket;
        }

        /// <summary>
        /// A translated is a pending packet, where the string was deserialized to a
        /// packet.
        /// </summary>
        private struct TranslatedPendingPacket : IComparable<TranslatedPendingPacket>
        {
            internal PacketHeader PacketHeader;
            internal Connection Connection;
            internal PacketSequencePacket Packet;

            public readonly int CompareTo(TranslatedPendingPacket other)
            {
                return Packet.ID.CompareTo(other.Packet.ID);
            }
        }

        /// <summary>
        /// Whether this is a packet handler of a server or a client.
        /// </summary>
        private readonly bool isServer;

        /// <summary>
        /// List of all serialized pending packets.
        /// </summary>
        private readonly List<SerializedPendingPacket> serializedPendingPackets = new List<SerializedPendingPacket>();

        /// <summary>
        /// List of all translated pending packets.
        /// </summary>
        private readonly List<TranslatedPendingPacket> translatedPendingPackets = new List<TranslatedPendingPacket>();

        /// <summary>
        /// Creates a new packet handler for either the server of the client.
        /// </summary>
        /// <param name="isServer">Whether this packet handler handles packets for the
        /// server.</param>
        public PacketHandler(bool isServer)
        {
            this.isServer = isServer;
        }

        /// <summary>
        /// Pushed a serialized packets for handling. Packets arrive via different
        /// threads, which is why they are not yet deserialized and only saved as a
        /// string.
        /// </summary>
        /// <param name="packetHeader">The packet header of the incoming packet.</param>
        /// <param name="connection">The connection of the incoming packet.</param>
        /// <param name="serializedPacket">The serialized packet.</param>
        internal void Push(PacketHeader packetHeader, Connection connection, string serializedPacket)
        {
            lock (serializedPendingPackets)
            {
                serializedPendingPackets.Add(
                    new SerializedPendingPacket
                    {
                        PacketHeader = packetHeader,
                        Connection = connection,
                        SerializedPacket = serializedPacket
                    }
                );
            }
        }

        /// <summary>
        /// Processes pending packets. Packets are translated, sorted by packet id and
        /// then executed. Packets can be executed as server or as client, depending on
        /// <see cref="isServer"/>.
        /// </summary>
        internal void HandlePendingPackets()
        {
            lock (serializedPendingPackets)
            {
                // Some opertations can be executed only by the Unity main thread. That
                // is why we need to make sure, the thread currently executing this code
                // is actually the Unity main thread.
                Assert.AreEqual(Thread.CurrentThread, Network.MainThread);
                foreach (SerializedPendingPacket serializedPendingPacket in serializedPendingPackets)
                {
                    PacketSequencePacket packet = (PacketSequencePacket)PacketSerializer.Deserialize(serializedPendingPacket.SerializedPacket);
                    translatedPendingPackets.Add(
                        new TranslatedPendingPacket
                        {
                            PacketHeader = serializedPendingPacket.PacketHeader,
                            Connection = serializedPendingPacket.Connection,
                            Packet = packet
                        }
                    );
                }
                serializedPendingPackets.Clear();

                translatedPendingPackets.Sort();
                for (int i = 0; i < translatedPendingPackets.Count; i++)
                {
                    TranslatedPendingPacket translatedPendingPacket = translatedPendingPackets[i];
                    bool result = isServer ?
                        translatedPendingPacket.Packet.ExecuteOnServer(translatedPendingPackets[i].Connection) :
                        translatedPendingPacket.Packet.ExecuteOnClient(translatedPendingPackets[i].Connection);
                    if (result)
                    {
                        translatedPendingPackets.RemoveAt(i--);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
