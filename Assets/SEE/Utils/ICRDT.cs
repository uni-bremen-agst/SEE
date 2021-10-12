using System;
using System.Collections.Generic;
using System.Net;
using static SEE.Utils.CRDT;

namespace SEE.Utils
{
    public static class ICRDT
    {


        //private static CRDT crdt = new CRDT(new Guid().ToString());//TODO wie bekomme ich die SiteID hier richtig?
        private static Dictionary<string, CRDT> crdts = new Dictionary<string, CRDT>();


        /// <summary>
        /// Finds or creates an CRDT instance by the name of the file
        /// </summary>
        /// <param name="file">The name of the file</param>
        /// <returns>A crdt instance</returns>
        private static CRDT GetInstanceByName(string file)
        {
            if(crdts != null && crdts.Count > 0 && crdts.ContainsKey(file))
            {
                return crdts[file];
            }
            else
            {
                Guid guid =  Guid.NewGuid();
                crdts.Add(file, new CRDT(guid.ToString(), file));
                return crdts[file];
            }
        }

        public static int GetIndexByPosition(Identifier[] position, string file)
        {
            return GetInstanceByName(file).GetIndexByPosition(position);
        }

        public static void RemoteAddChar(char c, Identifier[] position, Identifier[] prePosition, string file)
        {
            GetInstanceByName(file).RemoteAddChar(c, position, prePosition);
        }

        public static void RemoteDeleteChar(Identifier[] position, string file)
        {
            GetInstanceByName(file).RemoteDeleteChar(position);
        }

        public static void AddString(string s, int startIdx, string file)
        {
            GetInstanceByName(file).AddString(s, startIdx);
        }
        public static void DeleteString(int startIdx, int endIdx, string file)
        {
            GetInstanceByName(file).DeleteString(startIdx, endIdx);
        }

        public static void DeleteChar(int index, string file)
        {
            GetInstanceByName(file).DeleteChar(index);
        }

        public static void AddChar(char c, int idx, string file)
        {
            GetInstanceByName(file).AddChar(c, idx);
        }

        public static string PrintString(string file)
        {
            return GetInstanceByName(file).PrintString();
        }
        
        public static string ToString(string file)
        {
            return GetInstanceByName(file).ToString();
        }
        public static Identifier[] StringToPosition(string s, string file)
        {
            return GetInstanceByName(file).StringToPosition(s);
        }

        public static string PositionToString(Identifier[] position, string file)
        {
            return GetInstanceByName(file).PositionToString(position);
        }

        public static bool IsEmpty(string file)
        {
            return GetInstanceByName(file).IsEmpty();
        }
        //TODO COMPLETE
    }
}
