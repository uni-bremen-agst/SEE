using static SEE.Utils.CRDT;

namespace SEE.Utils
{
    public static class ICRDT
    {


        private static CRDT crdt = new CRDT(1);//TODO wie bekomme ich die SiteID hier richtig?


        public static void RemoteAddChar(char c, Identifier[] position, Identifier[] prePosition)
        {
            crdt.RemoteAddChar(c, position, prePosition);
        }

        public static void RemoteDeleteChar(Identifier[] position)
        {
            crdt.RemoteDeleteChar(position);
        }

        public static void DeleteChar(int index)
        {
            crdt.DeleteChar(index);
        }

        //TODO COMPLETE
    }
}
