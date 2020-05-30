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
    }

}
