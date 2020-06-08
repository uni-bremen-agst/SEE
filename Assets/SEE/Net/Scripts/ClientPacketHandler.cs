using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using SEE.Controls;
using SEE.Game;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    public class ClientPacketHandler : PacketHandler
    {
        public ClientPacketHandler(string packetTypePrefix) : base(packetTypePrefix)
        {
        }



        internal override bool TryHandlePacketSequence(PacketHeader packetHeader, Connection connection, PacketSequencePacket packetSequence)
        {
            Assert.IsNotNull(connection);
            Assert.IsNotNull(packetSequence);
            Assert.IsTrue(Client.Connection == connection);

            if (packetSequence.id == Client.incomingPacketID)
            {
                Client.incomingPacketID++;
                foreach (string serializedPacket in packetSequence.serializedPackets)
                {
                    AbstractPacket packet = PacketSerializer.Deserialize(serializedPacket);
                    HandlePacket(packetHeader, connection, packet);
                }
                return true;
            }
            return false;
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, ExecuteActionPacket packet)
        {
            if (packet != null && packet.action != null)
            {
                Assert.IsNotNull(packet.action.requesterIPAddress);
                Assert.IsTrue(packet.action.requesterPort != -1);

                packet.action.ExecuteOnClientBase();
            }
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, GameStatePacket packet)
        {
            if (packet != null)
            {
                GameObject[] gameObjects = new GameObject[packet.zoomStack.Length];
                for (int i = 0; i < packet.zoomStack.Length; i++)
                {
                    gameObjects[packet.zoomStack.Length - 1 - i] = InteractableObject.Get(packet.zoomStack[i]).gameObject;
                }
                Transformer.SetInitialState(gameObjects);

                foreach (uint id in packet.selectedGameObjects)
                {
                    ((HoverableObject)InteractableObject.Get(id)).Hovered(false);
                }
            }
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, RedoActionPacket packet)
        {
            if (packet != null)
            {
                packet.action.RedoOnClientBase();
            }
        }

        internal override void HandlePacket(PacketHeader packetHeader, Connection connection, UndoActionPacket packet)
        {
            if (packet != null)
            {
                packet.action.UndoOnClientBase();
            }
        }
    }

}
