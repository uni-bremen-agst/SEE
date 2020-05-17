using SEE.Command;

namespace SEE.Net.Internal
{

    internal class ExecuteCommandPacket : AbstractPacket
    {
        public AbstractCommand command;



        public ExecuteCommandPacket()
        {
        }

        public ExecuteCommandPacket(AbstractCommand command)
        {
            this.command = command;
        }



        internal override string Serialize()
        {
            string result = CommandSerializer.Serialize(command);
            return result;
        }

        internal override void Deserialize(string serializedPacket)
        {
            ExecuteCommandPacket deserializedPacket = new ExecuteCommandPacket(CommandSerializer.Deserialize(serializedPacket));
            command = deserializedPacket.command;
        }
    }



    internal class UndoCommandPacket : AbstractPacket
    {
        public AbstractCommand command;



        public UndoCommandPacket()
        {
        }

        public UndoCommandPacket(AbstractCommand command)
        {
            this.command = command;
        }



        internal override string Serialize()
        {
            string result = CommandSerializer.Serialize(command);
            return result;
        }

        internal override void Deserialize(string serializedPacket)
        {
            UndoCommandPacket deserializedPacket = new UndoCommandPacket(CommandSerializer.Deserialize(serializedPacket));
            command = deserializedPacket.command;
        }
    }



    internal class RedoCommandPacket : AbstractPacket
    {
        public AbstractCommand command;



        public RedoCommandPacket()
        {
        }

        public RedoCommandPacket(AbstractCommand command)
        {
            this.command = command;
        }



        internal override string Serialize()
        {
            string result = CommandSerializer.Serialize(command);
            return result;
        }

        internal override void Deserialize(string serializedPacket)
        {
            RedoCommandPacket deserializedPacket = new RedoCommandPacket(CommandSerializer.Deserialize(serializedPacket));
            command = deserializedPacket.command;
        }
    }

}
