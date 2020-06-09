using NetworkCommsDotNet.Connections;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// This packet can execute various actions.
    /// </summary>
    internal class ExecuteActionPacket : AbstractPacket
    {
        /// <summary>
        /// The action to be executed.
        /// </summary>
        public AbstractAction action;



        /// <summary>
        /// Empty constructor is necessary for JsonUtility-serialization.
        /// </summary>
        public ExecuteActionPacket()
        {
        }

        /// <summary>
        /// Constructs a packet with given action.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
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

        /// <summary>
        /// Executes the action of this packet as a server, potentially buffers packet on
        /// server and broadcasts packet to all connections.
        /// </summary>
        /// <param name="connection">The connection of this packet.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Executes the action of this packet as a client.
        /// </summary>
        /// <param name="connection">The connecting of the packet.</param>
        /// <returns><code>true</code>.</returns>
        internal override bool ExecuteOnClient(Connection connection)
        {
            Assert.IsNotNull(connection);
            Assert.IsNotNull(action.requesterIPAddress);
            Assert.IsTrue(action.requesterPort != -1);

            action.ExecuteOnClientBase();
            return true;
        }
    }



    /// <summary>
    /// Can undo an action.
    /// </summary>
    internal class UndoActionPacket : AbstractPacket
    {
        /// <summary>
        /// The action to undo.
        /// </summary>
        public AbstractAction action;



        /// <summary>
        /// Empty constructor is necessary for JsonUtility-serialization.
        /// </summary>
        public UndoActionPacket()
        {
        }

        /// <summary>
        /// Constructs a packet with given action.
        /// </summary>
        /// <param name="action">The action to undo.</param>
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

        /// <summary>
        /// Buffers this packet and redos the action of this packet as a server.
        /// </summary>
        /// <param name="connection">The connection of this packet.</param>
        /// <returns><code>true</code>.</returns>
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

        /// <summary>
        /// Undos the action of this packet as a client.
        /// </summary>
        /// <param name="connection">The connection of this packet.</param>
        /// <returns><code>true</code>.</returns>
        internal override bool ExecuteOnClient(Connection connection)
        {
            Assert.IsNotNull(connection);

            action.UndoOnClientBase();
            return true;
        }
    }



    /// <summary>
    /// Can redo an action.
    /// </summary>
    internal class RedoActionPacket : AbstractPacket
    {
        /// <summary>
        /// The action to redo.
        /// </summary>
        public AbstractAction action;



        /// <summary>
        /// Empty constructor is necessary for JsonUtility-serialization.
        /// </summary>
        public RedoActionPacket()
        {
        }

        /// <summary>
        /// Constructs a packet with given action.
        /// </summary>
        /// <param name="action">The action to redo.</param>
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

        /// <summary>
        /// Buffers this packet and redos the action of this packet as a server.
        /// </summary>
        /// <param name="connection">The connection of this packet.</param>
        /// <returns><code>true</code>.</returns>
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

        /// <summary>
        /// Redos the action of this packet as a client.
        /// </summary>
        /// <param name="connection">The connection of this packet.</param>
        /// <returns><code>true</code>.</returns>
        internal override bool ExecuteOnClient(Connection connection)
        {
            Assert.IsNotNull(connection);

            action.RedoOnClientBase();
            return true;
        }
    }

}
