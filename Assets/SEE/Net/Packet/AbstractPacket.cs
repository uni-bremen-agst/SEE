using System;
using NetworkCommsDotNet.Connections;

namespace SEE.Net
{

    /// <summary>
    /// The abstract packet for all packets, that can be sent via network.
    /// </summary>
    internal abstract class AbstractPacket
    {

        /// <summary>
        /// Constructs an empty abstract packet.
        /// </summary>
        protected AbstractPacket()
        {
        }

        /// <summary>
        /// Serializes this packet into an empty packet.
        /// </summary>
        /// <returns></returns>
        internal virtual string Serialize()
        {
            return "";
        }

        /// <summary>
        /// Deserializes this packet into an empty packet.
        /// </summary>
        /// <param name="serializedPacket"></param>
        internal virtual void Deserialize(string serializedPacket)
        {
        }

        /// <summary>
        /// Executes this packet as a server.
        /// </summary>
        /// <param name="connection">The connection of this packet.</param>
        /// <returns><code>true</code> if the packet could be executed, <code>false</code> otherwise.</returns>
        internal abstract bool ExecuteOnServer(Connection connection);

        /// <summary>
        /// Executes this packet as a client.
        /// </summary>
        /// <param name="connection">The connection of this packet.</param>
        /// <returns><code>true</code> if the packet could be executed, <code>false</code> otherwise.</returns>
        internal abstract bool ExecuteOnClient(Connection connection);
    }

    /// <summary>
    /// Serializes and deserializes packets to and from strings.
    /// </summary>
    internal static class PacketSerializer
    {
        /// <summary>
        /// Serializes given packet into a string.
        /// </summary>
        /// <param name="packet">The packet to be serialized.</param>
        /// <returns>The serialized packet as a string.</returns>
        internal static string Serialize(AbstractPacket packet)
        {
            string result = packet.GetType().ToString() + ';' + packet.Serialize();
            return result;
        }

        /// <summary>
        /// Deserializes given string into packet.
        /// </summary>
        /// <param name="serializedPacket">Serializes packet as string.</param>
        /// <returns>the deserialized packet.</returns>
        internal static AbstractPacket Deserialize(string serializedPacket)
        {
            string[] tokens = serializedPacket.Split(new[] { ';' }, 2, StringSplitOptions.None);
            Type type = Type.GetType(tokens[0]);
            AbstractPacket result = (AbstractPacket)Activator.CreateInstance(type);
            result.Deserialize(tokens[1]);
            return result;
        }
    }

}
