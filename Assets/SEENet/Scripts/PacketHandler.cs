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
        private Stack<PendingPacket> pendingMessages = new Stack<PendingPacket>();

        public PacketHandler(string packetTypePrefix)
        {
            Assert.IsNotNull(packetTypePrefix);
            handlerFuncDict = new Dictionary<string, HandlerFunc>
            {
                { packetTypePrefix + CityBuildingPacket.PACKET_TYPE, HandleCityBuildingPacket },
                { packetTypePrefix + CityNodePacket.PACKET_TYPE, HandleCityNodePacket },
                { packetTypePrefix + InstantiatePacket.PACKET_TYPE, HandleInstantiatePacket },
                { packetTypePrefix + TransformViewPositionPacket.PACKET_TYPE, HandleTransformViewPositionPacket },
                { packetTypePrefix + TransformViewRotationPacket.PACKET_TYPE, HandleTransformViewRotationPacket },
                { packetTypePrefix + TransformViewScalePacket.PACKET_TYPE, HandleTransformViewScalePacket }
            };
        }

        public void Push(PacketHeader packetHeader, Connection connection, string incomingObject)
        {
            lock (pendingMessages)
            {
                pendingMessages.Push(new PendingPacket
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
                while (pendingMessages.Count != 0)
                {
                    PendingPacket packet = pendingMessages.Pop();
                    bool result = handlerFuncDict.TryGetValue(packet.header.PacketType, out HandlerFunc func);
                    Assert.IsTrue(result);
                    func(packet.header, packet.connection, packet.packet);
                }
            }
        }

        protected abstract bool HandleCityBuildingPacket(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleCityNodePacket(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleInstantiatePacket(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleTransformViewPositionPacket(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleTransformViewRotationPacket(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleTransformViewScalePacket(PacketHeader packetHeader, Connection connection, string data);
    }

}
