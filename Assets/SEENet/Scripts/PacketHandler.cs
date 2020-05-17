using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    using HandlerFunc = Func<PacketHeader, Connection, string, bool>;

    public abstract class PacketHandler
    {
        private struct PendingPacket
        {
            public PacketHeader header;
            public Connection connection;
            public string serializedPacket;
        }



        public readonly Dictionary<string, HandlerFunc> handlerFuncDict;
        protected string packetTypePrefix;
        private List<PendingPacket> pendingMessages = new List<PendingPacket>();



        public PacketHandler(string packetTypePrefix)
        {
            Assert.IsNotNull(packetTypePrefix);

            this.packetTypePrefix = packetTypePrefix;
            handlerFuncDict = new Dictionary<string, HandlerFunc>
            {
                { packetTypePrefix + BufferedPacketsPacket.PACKET_TYPE, HandleBufferedPacketsPacket },
                { packetTypePrefix + ExecuteCommandPacket.PACKET_TYPE, HandleExecuteCommandPacket },
                { packetTypePrefix + RedoCommandPacket.PACKET_TYPE, HandleRedoCommandPacket},
                { packetTypePrefix + UndoCommandPacket.PACKET_TYPE, HandleUndoCommandPacket}
            };
        }



        public void Push(PacketHeader packetHeader, Connection connection, string serializedPacket)
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

        public void HandlePendingPackets()
        {
            lock (pendingMessages)
            {
                Assert.AreEqual(Thread.CurrentThread, Network.MainThread);
                for (int i = 0; i < pendingMessages.Count; i++)
                {
                    PendingPacket packet = pendingMessages[i];
                    bool result = handlerFuncDict.TryGetValue(packet.header.PacketType, out HandlerFunc func);
                    Assert.IsTrue(result);
                    func(packet.header, packet.connection, packet.serializedPacket);
                }
                pendingMessages.Clear();
            }
        }

        protected abstract bool HandleBufferedPacketsPacket(PacketHeader packetHeader, Connection connection, string data);

        protected abstract bool HandleExecuteCommandPacket(PacketHeader packetHeader, Connection connection, string data);

        protected abstract bool HandleRedoCommandPacket(PacketHeader packetHeader, Connection connection, string data);

        protected abstract bool HandleUndoCommandPacket(PacketHeader packetHeader, Connection connection, string data);
    }

}
