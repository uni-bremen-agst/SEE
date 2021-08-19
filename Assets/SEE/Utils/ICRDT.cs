using System;
using System.Collections.Generic;
using System.Net;
using static SEE.Utils.CRDT;

namespace SEE.Utils
{
    public static class ICRDT
    {


        private static CRDT crdt = new CRDT(new Guid().ToString());//TODO wie bekomme ich die SiteID hier richtig?


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

        public static void AddChar(char c, int idx)
        {
            crdt.AddChar(c, idx);
        }

        public static string PrintString()
        {
            return crdt.PrintString();
        }

        public static Identifier[] StringToPosition(string s)
        {
            return crdt.StringToPosition(s);
        }

        public static string PositionToString(Identifier[] position)
        {
            return crdt.PositionToString(position);
        }
        //TODO COMPLETE
    }
}
