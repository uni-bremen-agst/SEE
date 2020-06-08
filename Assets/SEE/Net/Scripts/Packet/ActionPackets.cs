using NetworkCommsDotNet.Connections;
using UnityEngine.Assertions;

namespace SEE.Net
{

    internal class ExecuteActionPacket : AbstractPacket
    {
        public AbstractAction action;



        public ExecuteActionPacket()
        {
        }

        public ExecuteActionPacket(AbstractAction action)
        {
            this.action = action;
        }



        internal override string Serialize()
        {
            string result = ActionSerializer.Serialize(action);
            return result;
        }

        internal override void Deserialize(string serializedPacket)
        {
            ExecuteActionPacket deserializedPacket = new ExecuteActionPacket(ActionSerializer.Deserialize(serializedPacket));
            action = deserializedPacket.action;
        }

        internal override bool ExecuteOnServer(Connection connection)
        {
            Assert.IsNotNull(connection);

            action.ExecuteOnServerBase();

            if (action.buffer)
            {
                Server.BufferPacket(this);
            }

            foreach (Connection c in Server.Connections)
            {
                Network.SubmitPacket(c, this);
            }
            return true;
        }

        internal override bool ExecuteOnClient(Connection connection)
        {
            Assert.IsNotNull(connection);
            Assert.IsNotNull(action.requesterIPAddress);
            Assert.IsTrue(action.requesterPort != -1);

            action.ExecuteOnClientBase();
            return true;
        }
    }



    internal class UndoActionPacket : AbstractPacket
    {
        public AbstractAction action;



        public UndoActionPacket()
        {
        }

        public UndoActionPacket(AbstractAction action)
        {
            this.action = action;
        }



        internal override string Serialize()
        {
            string result = action.index.ToString();
            return result;
        }

        internal override void Deserialize(string serializedPacket)
        {
            int index = int.Parse(serializedPacket);
            UndoActionPacket deserializedPacket = new UndoActionPacket(ActionHistory.actions[index]);
            action = deserializedPacket.action;
        }

        internal override bool ExecuteOnServer(Connection connection)
        {
            Assert.IsNotNull(connection);

            Server.BufferPacket(this);
            action.UndoOnServerBase();

            foreach (Connection co in Server.Connections)
            {
                Network.SubmitPacket(co, this);
            }
            return true;
        }

        internal override bool ExecuteOnClient(Connection connection)
        {
            Assert.IsNotNull(connection);

            action.UndoOnClientBase();
            return true;
        }
    }



    internal class RedoActionPacket : AbstractPacket
    {
        public AbstractAction action;



        public RedoActionPacket()
        {
        }

        public RedoActionPacket(AbstractAction action)
        {
            this.action = action;
        }



        internal override string Serialize()
        {
            string result = action.index.ToString();
            return result;
        }

        internal override void Deserialize(string serializedPacket)
        {
            int index = int.Parse(serializedPacket);
            RedoActionPacket deserializedPacket = new RedoActionPacket(ActionHistory.actions[index]);
            action = deserializedPacket.action;
        }

        internal override bool ExecuteOnServer(Connection connection)
        {
            Assert.IsNotNull(connection);

            Server.BufferPacket(this);
            action.RedoOnServerBase();

            foreach (Connection co in Server.Connections)
            {
                Network.SubmitPacket(co, this);
            }
            return true;
        }

        internal override bool ExecuteOnClient(Connection connection)
        {
            Assert.IsNotNull(connection);

            action.RedoOnClientBase();
            return true;
        }
    }

}
