using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    public abstract class PacketHandler
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



        protected string packetTypePrefix;
        private List<SerializedPendingPacket> serializedPendingPackets = new List<SerializedPendingPacket>();
        private List<TranslatedPendingPacket> translatedPendingPackets = new List<TranslatedPendingPacket>();



        public PacketHandler(string packetTypePrefix)
        {
            Assert.IsNotNull(packetTypePrefix);

            this.packetTypePrefix = packetTypePrefix;
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
                    bool result = TryHandlePacketSequence(translatedPendingPacket.packetHeader, translatedPendingPacket.connection, translatedPendingPacket.packet);
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

        internal void HandlePacket(PacketHeader packetHeader, Connection connection, AbstractPacket packet)
        {
            if (packet.GetType() == typeof(ExecuteCommandPacket))
            {
                HandlePacket(packetHeader, connection, (ExecuteCommandPacket)packet);
            }
            else if (packet.GetType() == typeof(RedoCommandPacket))
            {
                HandlePacket(packetHeader, connection, (RedoCommandPacket)packet);
            }
            else if (packet.GetType() == typeof(UndoCommandPacket))
            {
                HandlePacket(packetHeader, connection, (UndoCommandPacket)packet);
            };
        }

        internal abstract bool TryHandlePacketSequence(PacketHeader packetHeader, Connection connection, PacketSequencePacket packetSequence);
        internal abstract void HandlePacket(PacketHeader packetHeader, Connection connection, ExecuteCommandPacket packet);
        internal abstract void HandlePacket(PacketHeader packetHeader, Connection connection, RedoCommandPacket packet);
        internal abstract void HandlePacket(PacketHeader packetHeader, Connection connection, UndoCommandPacket packet);
    }

}
