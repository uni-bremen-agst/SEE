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
            public string packet;
        }

        public readonly Dictionary<string, HandlerFunc> handlerFuncDict;
        private List<PendingPacket> pendingMessages = new List<PendingPacket>();

        public PacketHandler(string packetTypePrefix)
        {
            Assert.IsNotNull(packetTypePrefix);
            handlerFuncDict = new Dictionary<string, HandlerFunc>
            {
                { packetTypePrefix + ExecuteCommandPacket.PACKET_TYPE, HandleExecuteCommandPacket },
                { packetTypePrefix + RedoCommandPacket.PACKET_TYPE, HandleRedoCommandPacket},
                { packetTypePrefix + TransformViewPositionPacket.PACKET_TYPE, HandleTransformViewPositionPacket },
                { packetTypePrefix + TransformViewRotationPacket.PACKET_TYPE, HandleTransformViewRotationPacket },
                { packetTypePrefix + TransformViewScalePacket.PACKET_TYPE, HandleTransformViewScalePacket },
                { packetTypePrefix + UndoCommandPacket.PACKET_TYPE, HandleUndoCommandPacket}
            };
        }

        public void Push(PacketHeader packetHeader, Connection connection, string incomingObject)
        {
            lock (pendingMessages)
            {
                pendingMessages.Add(new PendingPacket
                {
                    header = packetHeader,
                    connection = connection,
                    packet = incomingObject
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
                    func(packet.header, packet.connection, packet.packet);
                }
                pendingMessages.Clear();
            }
        }

        protected abstract bool HandleExecuteCommandPacket(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleRedoCommandPacket(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleTransformViewPositionPacket(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleTransformViewRotationPacket(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleTransformViewScalePacket(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleUndoCommandPacket(PacketHeader packetHeader, Connection connection, string data);
    }

}
