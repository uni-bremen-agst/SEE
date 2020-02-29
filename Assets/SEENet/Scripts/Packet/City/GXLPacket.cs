namespace SEE.Net.Internal
{

    public class GXLPacket : Packet
    {
        public static readonly string PACKET_TYPE = "GXL";

        public string gxl;

        public GXLPacket(string gxl) : base(PACKET_TYPE)
        {
            this.gxl = gxl;
        }

        public override string Serialize()
        {
            return gxl;
        }

        public static GXLPacket Deserialize(string data)
        {
            return new GXLPacket(data);
        }
    }

}
