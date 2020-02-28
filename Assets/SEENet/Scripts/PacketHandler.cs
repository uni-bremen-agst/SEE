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
        protected struct Packet
        {
            public PacketHeader header;
            public Connection connection;
            public string data;
        }

        public readonly Dictionary<string, HandlerFunc> handlerFuncDict;
        private Stack<Packet> pendingMessages = new Stack<Packet>();

        public PacketHandler(string packetTypePrefix)
        {
            Assert.IsNotNull(packetTypePrefix);
            handlerFuncDict = new Dictionary<string, HandlerFunc>
            {
                { packetTypePrefix + BuildingPacketData.PACKET_NAME, HandleBuildingPacketData },
                { packetTypePrefix + BuildingsPacketData.PACKET_NAME, HandleBuildingsPacketData },
                { packetTypePrefix + GXLPacketData.PACKET_NAME, HandleGXLPacketData },
                { packetTypePrefix + InstantiatePacketData.PACKET_NAME, HandleInstantiatePacketData },
                { packetTypePrefix + TransformViewPositionPacketData.PACKET_NAME, HandleTransformViewPositionPacketData },
                { packetTypePrefix + TransformViewRotationPacketData.PACKET_NAME, HandleTransformViewRotationPacketData },
                { packetTypePrefix + TransformViewScalePacketData.PACKET_NAME, HandleTransformViewScalePacketData }
            };
        }

        public void Push(PacketHeader packetHeader, Connection connection, string incomingObject)
        {
            lock (pendingMessages)
            {
                pendingMessages.Push(new Packet
                {
                    header = packetHeader,
                    connection = connection,
                    data = incomingObject
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
                    Packet packet = pendingMessages.Pop();
                    bool result = handlerFuncDict.TryGetValue(packet.header.PacketType, out HandlerFunc func);
                    Assert.IsTrue(result);
                    func(packet.header, packet.connection, packet.data);
                }
            }
        }
        protected abstract bool HandleBuildingPacketData(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleBuildingsPacketData(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleGXLPacketData(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleInstantiatePacketData(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleTransformViewPositionPacketData(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleTransformViewRotationPacketData(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleTransformViewScalePacketData(PacketHeader packetHeader, Connection connection, string data);
    }

}
