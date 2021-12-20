using Cysharp.Threading.Tasks;
using SEE.Net;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using static SEE.Game.UI.CodeWindow.CodeWindow;
using static SEE.Utils.CRDT;

namespace SEE.Utils
{
    public static class ICRDT
    {

        //private static CRDT crdt = new CRDT(new Guid().ToString());//TODO wie bekomme ich die SiteID hier richtig?
        private static Dictionary<string, CRDT> crdts = new Dictionary<string, CRDT>();

        private static PlayerIdManager playerIdManager = new PlayerIdManager();

        /// <summary>
        /// Finds or creates an CRDT instance by the name of the file
        /// </summary>
        /// <param name="file">The name of the file</param>
        /// <param name="size">optinal File Size for more effizient sizing of the CRDT</param>
        /// <returns>A crdt instance</returns>
        private static CRDT GetInstanceByName(string file, int size = 1000)
        {
            
            if(crdts != null && crdts.Count > 0 && crdts.ContainsKey(file))
            {
                return crdts[file];
            }
            else
            {
                Debug.Log("ID REEAL " + playerIdManager.GetClientID());
                crdts.Add(file, new CRDT(playerIdManager.GetClientID().ToString(), file, size));
                return crdts[file];
            }
        }

        public static void UpdateAlleCRDTids()
        {
            if(crdts.Count > 0)
            {
                foreach(KeyValuePair<string, CRDT> entry in crdts)
                {
                    if(entry.Value.getId() == "0")
                    {
                        entry.Value.setId(playerIdManager.GetClientID().ToString());
                    }
                }
            }
        }
        /// <summary>
        /// Syncs all Code Windows to a new Client
        /// </summary>
        public static void SyncCodeWindows(IPEndPoint[] recipient)
        {
            foreach(KeyValuePair<string, CRDT> elm in crdts)
            {
                elm.Value.SyncCodeWindows(recipient);
            }
        }

        /// <summary>
        /// ATTANTION ONLY FOR THE USE IN EXECUTE ON SERVER
        /// </summary>
        /// <returns>The requested id</returns>

        public static int RequestID()
        {
            return playerIdManager.RequestID();
        }

        public static int GetLocalID()
        {
            return playerIdManager.GetClientID();
        }

        public static void SetLocalID(int id)
        {
            playerIdManager.SetClientID(id);
        }

        public static int GetIndexByPosition(Identifier[] position, string file)
        {
            return GetInstanceByName(file).GetIndexByPosition(position);
        }

        public static void RemoteAddChar(char c, Identifier[] position, Identifier[] prePosition, string file)
        {
            GetInstanceByName(file).RemoteAddChar(c, position, prePosition);
        } 
        public static void SingleRemoteAddChar(char c, Identifier[] position, Identifier[] prePosition, string file)
        {
            GetInstanceByName(file).SingleRemoteAddChar(c, position, prePosition);
        }

        public static void RemoteDeleteChar(Identifier[] position, string file)
        {
            GetInstanceByName(file).RemoteDeleteChar(position);
        }

        public static void AddString(string s, int startIdx, string file)
        {
            CRDT crdt = GetInstanceByName(file, s.Length);
            if (GetLocalID() > 0  && crdt.getId().Equals("0"))
            {
                crdt.setId(GetLocalID().ToString());
            }
             crdt.AddString(s, startIdx);
            Debug.Log("CRDT ID " + crdt.getId());
        } 
        public static async UniTask AsyncAddString(string s, int startIdx, string file, bool startUp = false)
        {
            await GetInstanceByName(file, s.Length).AsyncAddString(s, startIdx, startUp);
        }
        public static void DeleteString(int startIdx, int endIdx, string file)
        {
            CRDT crdt = GetInstanceByName(file);
            if (GetLocalID() > 0 && crdt.getId().Equals("0"))
            {
                crdt.setId(GetLocalID().ToString());
            }
            crdt.DeleteString(startIdx, endIdx);
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

        public static UnityEvent<char, int, operationType> GetChangeEvent(string file)
        {
            return GetInstanceByName(file).changeEvent;
        }
        
        public static void Undo(string file)
        {
            GetInstanceByName(file).Undo();
        }

        public static void Redo(string file)
        {
            GetInstanceByName(file).Redo();
        }
        public static void RemoteAddString(string text, string file)
        {
            GetInstanceByName(file, text.Length).RemoteAddString(text);
        }
    }

    class  PlayerIdManager
    {
        private int playerIDcounter = 0;

        private int clientID = 0;
        /// <summary>
        /// Generates the new Player ID
        /// </summary>
        /// <returns>The Id for the user</returns>
        public int RequestID()
        {
            return ++playerIDcounter;
        }

        public void SetClientID(int id)
        {
            clientID = id;
        }

        public int GetClientID()
        {
            return clientID;
        }



    }
}
