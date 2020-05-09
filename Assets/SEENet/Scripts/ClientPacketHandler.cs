using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using SEE.Command;
using UnityEngine;

namespace SEE.Net.Internal
{

    public class ClientPacketHandler : PacketHandler
    {
        public ClientPacketHandler(string packetTypePrefix) : base(packetTypePrefix)
        {
        }

        

        protected override bool HandleExecuteCommandPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            ExecuteCommandPacket packet = ExecuteCommandPacket.Deserialize(data);

            if (packet == null || packet.command == null)
            {
                return false;
            }

            packet.command.ExecuteOnClient();

            return true;
        }

        protected override bool HandleRedoCommandPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            RedoCommandPacket packet = RedoCommandPacket.Deserialize(data);

            if (packet == null)
            {
                return false;
            }

            CommandHistory.RedoOnClient();
            return true;
        }

        protected override bool HandleTransformViewPositionPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewPositionPacket packet = TransformViewPositionPacket.Deserialize(data);
            packet?.transformView?.SetNextPosition(packet.updateTime, packet.position);
            return true;
        }

        protected override bool HandleTransformViewRotationPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewRotationPacket packet = TransformViewRotationPacket.Deserialize(data);
            packet?.transformView?.SetNextRotation(packet.updateTime, packet.rotation);
            return true;
        }

        protected override bool HandleTransformViewScalePacket(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewScalePacket packet = TransformViewScalePacket.Deserialize(data);
            packet?.transformView?.SetNextScale(packet.updateTime, packet.scale);
            return true;
        }

        protected override bool HandleUndoCommandPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            UndoCommandPacket packet = UndoCommandPacket.Deserialize(data);

            if (packet == null)
            {
                return false;
            }

            CommandHistory.UndoOnClient();
            return true;
        }
    }

}
