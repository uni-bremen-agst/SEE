using JetBrains.Annotations;

namespace Dissonance.Networking
{
    internal struct TextPacket
    {
        public readonly ushort Sender;
        public readonly ChannelType RecipientType;
        public readonly ushort Recipient;
        [CanBeNull] public readonly string Text;

        public TextPacket(ushort sender, ChannelType recipientType, ushort recipient, [CanBeNull] string text)
        {
            Sender = sender;
            RecipientType = recipientType;
            Recipient = recipient;
            Text = text;
        }
    }
}