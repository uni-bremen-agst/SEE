using SEE.Command;

namespace SEE.Net.Internal
{

    internal class ExecuteCommandPacket : AbstractPacket
    {
        internal static readonly string PACKET_TYPE = "ExecuteCommand";

        public AbstractCommand command;

        internal ExecuteCommandPacket(AbstractCommand command) : base(PACKET_TYPE)
        {
            this.command = command;
        }

        internal override string Serialize()
        {
            string result = CommandSerializer.Serialize(command);
            return result;
        }

        internal static ExecuteCommandPacket Deserialize(string data)
        {
            ExecuteCommandPacket result = new ExecuteCommandPacket(CommandSerializer.Deserialize(data));
            return result;
        }
    }

    internal class RedoCommandPacket : AbstractPacket
    {
        internal static readonly string PACKET_TYPE = "RedoCommand";

        internal RedoCommandPacket() : base(PACKET_TYPE)
        {
        }

        internal override string Serialize()
        {
            string result = "";
            return result;
        }

        internal static RedoCommandPacket Deserialize(string data)
        {
            RedoCommandPacket p = new RedoCommandPacket();
            return p;
        }
    }

    internal class UndoCommandPacket : AbstractPacket
    {
        internal static readonly string PACKET_TYPE = "UndoCommand";

        internal UndoCommandPacket() : base(PACKET_TYPE)
        {
        }

        internal override string Serialize()
        {
            string result = "";
            return result;
        }

        internal static UndoCommandPacket Deserialize(string data)
        {
            UndoCommandPacket p = new UndoCommandPacket();
            return p;
        }
    }

}
