using SEE.Command;

namespace SEE.Net.Internal
{

    internal class CommandPacket : Packet
    {
        internal static readonly string PACKET_TYPE = "Command";

        public AbstractCommand command;

        internal CommandPacket(AbstractCommand command) : base(PACKET_TYPE)
        {
            this.command = command;
        }

        internal override string Serialize()
        {
            string result = CommandSerializer.Serialize(command);
            return result;
        }

        internal static CommandPacket Deserialize(string data)
        {
            CommandPacket result = new CommandPacket(CommandSerializer.Deserialize(data));
            return result;
        }
    }

}
