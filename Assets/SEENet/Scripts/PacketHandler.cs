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
        private struct PendingPacket
        {
            public PacketHeader header;
            public Connection connection;
            public string serializedPacket;
        }



        protected string packetTypePrefix;
        private List<PendingPacket> pendingMessages = new List<PendingPacket>();



        public PacketHandler(string packetTypePrefix)
        {
            Assert.IsNotNull(packetTypePrefix);

            this.packetTypePrefix = packetTypePrefix;
        }



        internal void Push(PacketHeader packetHeader, Connection connection, string serializedPacket)
        {
            lock (pendingMessages)
            {
                pendingMessages.Add(new PendingPacket
                {
                    header = packetHeader,
                    connection = connection,
                    serializedPacket = serializedPacket
                });
            }
        }

        internal void HandlePendingPackets()
        {
            lock (pendingMessages)
            {
                Assert.AreEqual(Thread.CurrentThread, Network.MainThread);
                for (int i = 0; i < pendingMessages.Count; i++)
                {
                    PendingPacket pendingPacket = pendingMessages[i];
                    AbstractPacket packet = PacketSerializer.Deserialize(pendingPacket.serializedPacket);
                    HandlePacket(pendingPacket.header, pendingPacket.connection, packet);
                }
                pendingMessages.Clear();
            }
        }

        internal void HandlePacket(PacketHeader packetHeader, Connection connection, AbstractPacket packet)
        {
            if (packet.GetType() == typeof(BufferedPacketsPacket))
            {
                HandlePacket(packetHeader, connection, (BufferedPacketsPacket)packet);
            }
            else if (packet.GetType() == typeof(ExecuteCommandPacket))
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

        internal abstract void HandlePacket(PacketHeader packetHeader, Connection connection, BufferedPacketsPacket packet);
        internal abstract void HandlePacket(PacketHeader packetHeader, Connection connection, ExecuteCommandPacket packet);
        internal abstract void HandlePacket(PacketHeader packetHeader, Connection connection, RedoCommandPacket packet);
        internal abstract void HandlePacket(PacketHeader packetHeader, Connection connection, UndoCommandPacket packet);
    }

}
