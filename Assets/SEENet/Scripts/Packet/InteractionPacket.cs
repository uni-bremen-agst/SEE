using SEE.Interact;

namespace SEE.Net.Internal
{

    internal class InteractionPacket : Packet
    {
        internal static readonly string PACKET_TYPE = "Interaction";

        public AbstractInteraction interaction;

        internal InteractionPacket(AbstractInteraction interaction) : base(PACKET_TYPE)
        {
            this.interaction = interaction;
        }

        internal override string Serialize()
        {
            string result = InteractionSerializer.Serialize(interaction);
            return result;
        }

        internal static InteractionPacket Deserialize(string data)
        {
            InteractionPacket result = new InteractionPacket(InteractionSerializer.Deserialize(data));
            return result;
        }
    }

}
