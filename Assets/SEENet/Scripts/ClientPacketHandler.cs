using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using SEE.Command;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    using HandlerFunc = Func<PacketHeader, Connection, string, bool>;

    public class ClientPacketHandler : PacketHandler
    {
        public ClientPacketHandler(string packetTypePrefix) : base(packetTypePrefix)
        {
        }



        protected override bool HandleBufferedPacketsPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            BufferedPacketsPacket packet = BufferedPacketsPacket.Deserialize(data);

            if (packet == null || packet.packetTypes == null || packet.packetDatas == null)
            {
                return false;
            }

            for (int i = 0; i < packet.packetTypes.Length; i++)
            {
                bool result = handlerFuncDict.TryGetValue(packetTypePrefix + packet.packetTypes[i], out HandlerFunc func);
                Assert.IsTrue(result);
                func(null, null, packet.packetDatas[i]);
            }
            return true;
        }

        protected override bool HandleExecuteCommandPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            ExecuteCommandPacket packet = ExecuteCommandPacket.Deserialize(data);

            if (packet == null || packet.command == null)
            {
                return false;
            }

            Assert.IsNotNull(packet.command.requesterIPAddress);
            Assert.IsTrue(packet.command.requesterPort != -1);

            KeyValuePair<GameObject[], GameObject[]> result = packet.command.ExecuteOnClient();
            IPEndPoint stateOwner = new IPEndPoint(IPAddress.Parse(packet.command.requesterIPAddress), packet.command.requesterPort);
            if (packet.command.buffer)
            {
                CommandHistory.OnExecute(stateOwner, result.Key, result.Value);
            }

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
