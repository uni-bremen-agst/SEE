using System;
using System.Collections.Generic;
using Dissonance.Datastructures;
using Dissonance.Extensions;
using Dissonance.Networking;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Netcode;

namespace Dissonance.Integrations.Unity_NFGO
{
    public class NfgoCommsNetwork
        : BaseCommsNetwork<
            NfgoServer,      // A class which implements BaseServer
            NfgoClient,      // A class which implements BaseClient
            NfgoConn,        // A struct which represents another client in the session
            Unit,            // Nothing
            Unit             // Nothing
        >
    {
        private readonly ConcurrentPool<byte[]> _loopbackBuffers = new ConcurrentPool<byte[]>(8, () => new byte[1024]);
        private readonly List<ArraySegment<byte>> _loopbackQueueToServer = new List<ArraySegment<byte>>();
        private readonly List<ArraySegment<byte>> _loopbackQueueToClient = new List<ArraySegment<byte>>();

        protected override NfgoClient CreateClient(Unit connectionParameters)
        {
            return new NfgoClient(this);
        }

        protected override NfgoServer CreateServer(Unit connectionParameters)
        {
            return new NfgoServer(this);
        }

        protected override void Update()
        {
            // Check if Dissonance is ready
            if (IsInitialized)
            {
                // Check if the network is ready
                var clientActive = NetworkManager.Singleton.IsClient && NetworkManager.Singleton.IsConnectedClient;
                var serverActive = NetworkManager.Singleton.IsServer;
                var networkActive = NetworkManager.Singleton.isActiveAndEnabled && (clientActive || serverActive);

                if (networkActive)
                {
                    // Check what mode the network is in
                    var server = NetworkManager.Singleton.IsServer;
                    var client = NetworkManager.Singleton.IsClient;

                    // Check what mode Dissonance is in and if
                    // they're different then call the correct method
                    if (Mode.IsServerEnabled() != server || Mode.IsClientEnabled() != client)
                    {
                        // Network is server and client, so run as a non-dedicated
                        // host (passing in the correct parameters)
                        if (server && client)
                            RunAsHost(Unit.None, Unit.None);

                        // Network is just a server, so run as a dedicated host
                        else if (server)
                            RunAsDedicatedServer(Unit.None);

                        // Network is just a client, so run as a client
                        else if (client)
                            RunAsClient(Unit.None);
                    }
                }
                else if (Mode != NetworkMode.None)
                {
                    //Network is not active, make sure Dissonance is not active
                    Stop();

                    _loopbackQueueToClient.Clear();
                    _loopbackQueueToServer.Clear();
                }

                //Send looped back packets to client
                if (Client != null)
                {
                    foreach (var item in _loopbackQueueToClient)
                    {
                        if (item.Array != null)
                        {
                            Client.NetworkReceivedPacket(item);
                            _loopbackBuffers.Put(item.Array);
                        }
                    }
                }
                _loopbackQueueToClient.Clear();

                //Send looped back packets to server
                if (Server != null)
                {
                    foreach (var item in _loopbackQueueToServer)
                    {
                        if (item.Array != null)
                        {
                            Server.NetworkReceivedPacket(new NfgoConn(NetworkManager.Singleton.LocalClientId), item);
                            _loopbackBuffers.Put(item.Array);
                        }
                    }
                }
                _loopbackQueueToServer.Clear();
            }

            base.Update();
        }

        internal void SendToServer(ArraySegment<byte> packet, bool reliable, [NotNull] NetworkManager netManager)
        {
            if (packet.Array == null)
                throw new ArgumentException("packet is null");

            if (netManager.IsHost)
            {
                // As we are the host in this scenario we should send the packet directly to the client. Add it to a queue to be delivered to self.
                _loopbackQueueToServer.Add(packet.CopyToSegment(_loopbackBuffers.Get()));
            }
            else
            {
                using var buffer = WritePacket(packet);
                netManager.CustomMessagingManager.SendNamedMessage(
                    "DissonanceToServer",
                    NetworkManager.Singleton.ServerClientId,
                    buffer,
                    reliable ? NetworkDelivery.ReliableSequenced : NetworkDelivery.Unreliable
                );
            }
        }

        internal void SendToClient(ArraySegment<byte> packet, NfgoConn client, bool reliable, [NotNull] NetworkManager netManager)
        {
            if (packet.Array == null)
                throw new ArgumentException("packet is null");

            if (netManager.LocalClientId == client.ClientId)
            {
                // As we are the host in this scenario we should send the packet directly to the client. Add it to a queue to be delivered to self.
                _loopbackQueueToClient.Add(packet.CopyToSegment(_loopbackBuffers.Get()));
            }
            else
            {
                // Drop unreliable packets to clients who are not connected
                if (!reliable && !netManager.ConnectedClients.ContainsKey(client.ClientId))
                    return;

                using var buffer = WritePacket(packet);
                netManager.CustomMessagingManager.SendNamedMessage(
                    "DissonanceToClient",
                    client.ClientId,
                    buffer,
                    reliable ? NetworkDelivery.ReliableSequenced : NetworkDelivery.Unreliable
                );
            }
        }

        private static FastBufferWriter WritePacket(ArraySegment<byte> packet)
        {
            var buffer = new FastBufferWriter(packet.Count + sizeof(uint), Allocator.Temp);
            buffer.WriteValueSafe((uint)packet.Count);
            buffer.WriteBytesSafe(packet.Array, packet.Count, packet.Offset);
            return buffer;
        }

        internal static ArraySegment<byte> ReadPacket(ref FastBufferReader reader, [CanBeNull] ref byte[] buffer)
        {
            reader.ReadValueSafe(out uint length);

            if (buffer == null || buffer.Length < length)
                buffer = new byte[Math.Max(1024, length)];

            for (var i = 0; i < length; i++)
            {
                reader.ReadByteSafe(out var b);
                buffer[i] = b;
            }

            return new ArraySegment<byte>(buffer, 0, (int)length);
        }
    }

    public readonly struct NfgoConn
        : IEquatable<NfgoConn>
    {
        public readonly ulong ClientId;

        public NfgoConn(ulong id)
        {
            ClientId = id;
        }

        public bool Equals(NfgoConn other)
        {
            return ClientId == other.ClientId;
        }

        public override bool Equals(object obj)
        {
            return obj is NfgoConn other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ClientId.GetHashCode();
        }

        public static bool operator ==(NfgoConn left, NfgoConn right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NfgoConn left, NfgoConn right)
        {
            return !left.Equals(right);
        }
    }
}