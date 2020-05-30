using System;

namespace SEE.Net
{

    internal abstract class AbstractPacket
    {
        internal static readonly string DATE_TIME_FORMAT = "yyyy.MM.dd HH:mm:ss.fffffff";
        private const char DELIM = ';';
        private static readonly char[] DELIMS = new char[] { DELIM };

        protected AbstractPacket()
        {
        }

        internal virtual string Serialize()
        {
            return "";
        }

        internal virtual void Deserialize(string serializedPacket)
        {
        }
    }

    internal static class PacketSerializer
    {
        internal static string Serialize(AbstractPacket packet)
        {
            string result = packet.GetType().ToString() + ';' + packet.Serialize();
            return result;
        }

        internal static AbstractPacket Deserialize(string serializedPacket)
        {
            string[] tokens = serializedPacket.Split(new char[] { ';' }, 2, StringSplitOptions.None);
            Type type = Type.GetType(tokens[0]);
            AbstractPacket result = (AbstractPacket)Activator.CreateInstance(type);
            result.Deserialize(tokens[1]);
            return result;
        }
    }

}
