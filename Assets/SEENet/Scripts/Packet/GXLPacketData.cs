namespace SEE.Net.Internal
{

    public class GXLPacketData : PacketData
    {
        public static readonly string PACKET_NAME = "GXL";

        public string gxl;

        public GXLPacketData(string gxl)
        {
            this.gxl = gxl;
        }

        public override string Serialize()
        {
            return gxl;
        }

        public static GXLPacketData Deserialize(string data)
        {
            return new GXLPacketData(data);
        }
    }

}
