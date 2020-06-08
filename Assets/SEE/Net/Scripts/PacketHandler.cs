using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Assertions;

namespace SEE.Net
{

    public class PacketHandler
    {
        private struct SerializedPendingPacket
        {
            internal PacketHeader packetHeader;
            internal Connection connection;
            internal string serializedPacket;
        }

        private struct TranslatedPendingPacket : IComparable<TranslatedPendingPacket>
        {
            internal PacketHeader packetHeader;
            internal Connection connection;
            internal PacketSequencePacket packet;

            public int CompareTo(TranslatedPendingPacket other)
            {
                int result = packet.id.CompareTo(other.packet.id);
                return result;
            }
        }



        private readonly bool isServer;
        private List<SerializedPendingPacket> serializedPendingPackets = new List<SerializedPendingPacket>();
        private List<TranslatedPendingPacket> translatedPendingPackets = new List<TranslatedPendingPacket>();



        public PacketHandler(bool isServer)
        {
            this.isServer = isServer;
        }



        internal void Push(PacketHeader packetHeader, Connection connection, string serializedPacket)
        {
            lock (serializedPendingPackets)
            {
                serializedPendingPackets.Add(
                    new SerializedPendingPacket()
                    {
                        packetHeader = packetHeader,
                        connection = connection,
                        serializedPacket = serializedPacket
                    }
                );
            }
        }

        internal void HandlePendingPackets()
        {
            lock (serializedPendingPackets)
            {
                Assert.AreEqual(Thread.CurrentThread, Network.MainThread);
                foreach (SerializedPendingPacket serializedPendingPacket in serializedPendingPackets)
                {
                    PacketSequencePacket packet = (PacketSequencePacket)PacketSerializer.Deserialize(serializedPendingPacket.serializedPacket);
                    translatedPendingPackets.Add(
                        new TranslatedPendingPacket()
                        {
                            packetHeader = serializedPendingPacket.packetHeader,
                            connection = serializedPendingPacket.connection,
                            packet = packet
                        }
                    );
                }
                serializedPendingPackets.Clear();

                translatedPendingPackets.Sort();
                for (int i = 0; i < translatedPendingPackets.Count; i++)
                {
                    TranslatedPendingPacket translatedPendingPacket = translatedPendingPackets[i];
                    bool result = isServer ?
                        translatedPendingPacket.packet.ExecuteOnServer(translatedPendingPackets[i].connection) :
                        translatedPendingPacket.packet.ExecuteOnClient(translatedPendingPackets[i].connection);
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
