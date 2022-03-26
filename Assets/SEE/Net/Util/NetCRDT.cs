using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Utils;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using static SEE.Utils.CRDT;

namespace SEE.Net
{
    /// <summary>
    /// This class connects the <see cref="CRDT"/> to all clients in the network.
    /// </summary>
    public class NetCRDT : AbstractNetAction
    {
        /// <summary>
        /// The state that determines which action should be performed.
        /// </summary>
        public enum RemoteAction
        {
            /// <summary>
            /// The initial state, nothing to be done here.
            /// </summary>
            Init,
            /// <summary>
            /// In this state a RemoteDelteChar() should be executed on each client.
            /// </summary>
            DelteChar,
            /// <summary>
            /// In this state a RemoteAddChar() should be executed on each client.
            /// </summary>
            AddChar,

            /// <summary>
            /// Adds a List of single char operation to the crdt to save network load
            /// </summary>
            AddString,

            /// <summary>
            /// For ID requesting from the server
            /// </summary>
            RequestID,

            /// <summary>
            /// The return of the IP from the Server
            /// </summary>
            SetID,

            /// <summary>
            /// IUf a new client joins the game
            /// </summary>
            SingleAddChar,

        };

        /// <summary>
        /// The specific instance of <see cref="RemoteAction"/>
        /// </summary>
        public RemoteAction state = RemoteAction.Init;

        public char c;
        public string position;
        public string prePosition;
        public string file;
        public string listAsString;
        public int id;

        public NetCRDT() : base()
        {
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            if (state.Equals(RemoteAction.RequestID))
            {
                int id = ICRDT.RequestID();
                new NetCRDT().SetID(GetRequester(), id);
                state = RemoteAction.Init;
            }
        }

        /// <summary>
        ///
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                switch (state)
                {
                    case RemoteAction.AddChar:
                        ICRDT.RemoteAddChar(c, ICRDT.StringToPosition(position, file)/*, ICRDT.StringToPosition(prePosition, file)*/, file);
                        Debug.Log("NETWORK ADD");
                        break;
                    case RemoteAction.SingleAddChar:
                        ICRDT.SingleRemoteAddChar(c, ICRDT.StringToPosition(position, file)/*, ICRDT.StringToPosition(prePosition, file)*/, file); 
                    
                       break;

                    case RemoteAction.DelteChar:
                        ICRDT.RemoteDeleteChar(ICRDT.StringToPosition(position, file), file);
                        
                        break;
                    case RemoteAction.AddString:
                        ICRDT.RemoteAddString(listAsString, file);
                        break;
                }

            }

            if (state.Equals(RemoteAction.SetID))
            {
                ICRDT.SetLocalID(id);
            }
        }

        public void DeleteChar(Identifier[] position, string file)
        {
            this.file = file;
            this.position = ICRDT.PositionToString(position, file);
            state = RemoteAction.DelteChar;
            Execute(null);
        }

        public void AddChar(char c, Identifier[] position /*, Identifier[] prePosition*/, string file)
        {
            Debug.Log("ADD" + c + position);
            this.file = file;
            this.c = c;
            this.position = ICRDT.PositionToString(position, file);
            //this.prePosition = ICRDT.PositionToString(prePosition, file);
            state = RemoteAction.AddChar;
            Execute(null);
           
        }

        public void SingleAddChar(char c, Identifier[] position/*, Identifier[] prePosition*/, string file, IPEndPoint[] recipient)
        {

            this.file = file;
            this.c = c;
            this.position = ICRDT.PositionToString(position, file);
            //this.prePosition = ICRDT.PositionToString(prePosition, file);
            state = RemoteAction.SingleAddChar;
            Execute(recipient);

        }

        public void AddString(string text, string filename)
        {
            Debug.Log("ADD" + text);
            this.file = filename;
            this.listAsString = text;
            this.state = RemoteAction.AddString;
            Execute(null);
        }

        public void RequestID()
        {
            state = RemoteAction.RequestID;
            Execute(null);
        }

        public void SetID(IPEndPoint player, int id)
        {
            IPEndPoint[] playerArr = { player };
            this.id = id;
            state = RemoteAction.SetID;
            Execute(playerArr);
        }
    }
}
