using System.Linq;
using System.Net;
using NetworkCommsDotNet.Connections;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// This packet can execute various actions.
    /// </summary>
    internal sealed class ExecuteActionPacket : AbstractPacket
    {
        /// <summary>
        /// The action to be executed.
        /// </summary>
        public AbstractNetAction action;

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
        public ExecuteActionPacket(AbstractNetAction action)
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
        /// <returns><code>true</code>.</returns>
        internal override bool ExecuteOnServer(Connection connection)
        {
            Assert.IsNotNull(connection);

            action.ExecuteOnServerBase();

            IPEndPoint[] recipients = action.GetRecipients();
            if (recipients == null)
            {
                foreach (Connection c in Server.Connections)
                {
                    Network.SubmitPacket(c, this);
                }
            }
            else
            {
                foreach (Connection c in Server.Connections)
                {
                    // FIXME do we need a hashmap for IPEndPoint => connection or is this approach sufficient?
                    if (recipients.Any(recipient => c.ConnectionInfo.RemoteEndPoint.Equals(recipient)))
                    {
                        Network.SubmitPacket(c, this);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Executes the action of this packet as a client.
        /// </summary>
        /// <param name="connection">The connection of the packet.</param>
        /// <returns><c>true</c>.</returns>
        internal override bool ExecuteOnClient(Connection connection)
        {
            Assert.IsNotNull(connection);
            Assert.IsNotNull(action.RequesterIPAddress);
            Assert.IsTrue(action.RequesterPort != -1);

            action.ExecuteOnClientBase();
            return true;
        }
    }

}
