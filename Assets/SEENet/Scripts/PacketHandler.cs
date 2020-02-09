using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SEE.Net.Internal
{

    public abstract class PacketHandler
    {
        private string packetTypePrefix = null;
        protected Stack<Packet> pendingMessages = new Stack<Packet>();

        protected struct Packet
        {
            public PacketHeader header;
            public Connection connection;
            public string data;
        }

        public PacketHandler(string packetTypePrefix)
        {
            this.packetTypePrefix = packetTypePrefix;
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

        // TODO: check if main thread
        // must be called from main thread
        public void HandlePendingPackets()
        {
            lock (pendingMessages)
            {
#if UNITY_EDITOR
                if (!Thread.CurrentThread.Equals(Network.MainThread))
                {
                    throw new ThreadStateException("'HandlePendingPackets' must be called from main thread!");
                }
#endif
                for (int i = 0; i < pendingMessages.Count; i++)
                {
                    Packet packet = pendingMessages.Pop();
                    string packetType = packet.header.PacketType;

                    if (packetType.Equals(packetTypePrefix + GXLPacketData.PACKET_NAME))
                    {
                        HandleGXLPacketData(packet.header, packet.connection, packet.data);
                    }
                    else if (packetType.Equals(packetTypePrefix + InstantiatePacketData.PACKET_NAME))
                    {
                        HandleInstantiatePacketData(packet.header, packet.connection, packet.data);
                    }
                    else if (packetType.Equals(packetTypePrefix + TransformViewPositionPacketData.PACKET_NAME))
                    {
                        HandleTransformViewPositionPacketData(packet.header, packet.connection, packet.data);
                    }
                    else if (packetType.Equals(packetTypePrefix + TransformViewRotationPacketData.PACKET_NAME))
                    {
                        HandleTransformViewRotationPacketData(packet.header, packet.connection, packet.data);
                    }
                    else if (packetType.Equals(packetTypePrefix + TransformViewScalePacketData.PACKET_NAME))
                    {
                        HandleTransformViewScalePacketData(packet.header, packet.connection, packet.data);
                    }
                    else
                    {
                        throw new FormatException("Type of packet it unknown!");
                    }
                }
            }
        }
        protected abstract bool HandleGXLPacketData(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleInstantiatePacketData(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleTransformViewPositionPacketData(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleTransformViewRotationPacketData(PacketHeader packetHeader, Connection connection, string data);
        protected abstract bool HandleTransformViewScalePacketData(PacketHeader packetHeader, Connection connection, string data);
    }

}
