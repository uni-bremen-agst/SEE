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
            /// Adds a List of single char operation to the crdt to save network load.
            /// </summary>
            AddString,

            /// <summary>
            /// For ID requesting from the server.
            /// </summary>
            RequestID,

            /// <summary>
            /// The return of the IP from the Server.
            /// </summary>
            SetID,

            /// <summary>
            /// IUf a new client joins the game.
            /// </summary>
            SingleAddChar,

        };

        /// <summary>
        /// The specific instance of <see cref="RemoteAction"/>
        /// </summary>
        public RemoteAction state = RemoteAction.Init;

        /// <summary>
        /// The character that will be transmitted.
        /// </summary>
        public char c;
        
        /// <summary>
        ///  The position of the character.
        /// </summary>
        public string position;

        //TODO: CAN WARSCHEINLICH BE REMOVED NO LONGER NEEDED IF THE CHANGE WORKS
        public string prePosition;

        /// <summary>
        /// The name of the file in which changes should be made.
        /// </summary>
        public string file;

        /// <summary>
        /// The text that should be transmitted.
        /// </summary>
        public string text;

        /// <summary>
        /// The siteID of the user
        /// </summary>
        public int id;

        /// <summary>
        /// Initaily left empty
        /// </summary>
        public NetCRDT() : base()
        {
        }

        /// <summary>
        /// Things to execute on the server.
        /// Generates a ID for each client.
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
        /// Adds or deletes characters on each client to synchronize the code window or sets the siteId for a user.
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
                        ICRDT.RemoteAddString(text, file);
                        break;
                }
            }

            if (state.Equals(RemoteAction.SetID))
            {
                ICRDT.SetLocalID(id);
            }
        }

        /// <summary>
        /// Deletes a char on each client.
        /// </summary>
        /// <param name="position">The position at which a char should be deleted.</param>
        /// <param name="file">The filename of the crdt in which the char should be deleted.</param>
        public void DeleteChar(Identifier[] position, string file)
        {
            this.file = file;
            this.position = ICRDT.PositionToString(position, file);
            state = RemoteAction.DelteChar;
            Execute(null);
        }

        /// <summary>
        /// Adds a char on each client.
        /// </summary>
        /// <param name="c">The char that should be added.</param>
        /// <param name="position">The position at which it should be added.</param>
        /// <param name="file">The filename of the crdt in which it should be added.</param>
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

        /// <summary>
        /// Adds a char on a specific client. Used for synchronising 
        /// the content of the crdt with a new client that joins an existing session.
        /// </summary>
        /// <param name="c">The char that should be added.</param>
        /// <param name="position">The position at which it should be added.</param>
        /// <param name="file">The filename of the crdt in which it should be added.</param>
        /// <param name="recipient">The recipient that should receive the cahnge.</param>
        public void SingleAddChar(char c, Identifier[] position/*, Identifier[] prePosition*/, string file, IPEndPoint[] recipient)
        {

            this.file = file;
            this.c = c;
            this.position = ICRDT.PositionToString(position, file);
            //this.prePosition = ICRDT.PositionToString(prePosition, file);
            state = RemoteAction.SingleAddChar;
            Execute(recipient);

        }

        /// <summary>
        /// Adds a string on every client.
        /// </summary>
        /// <param name="text">The string that should be added, containing the positions as a string.</param>
        /// <param name="filename">The filename of the crdt in which the string should be added.</param>
        public void AddString(string text, string filename)
        {
            Debug.Log("ADD" + text);
            this.file = filename;
            this.text = text;
            this.state = RemoteAction.AddString;
            Execute(null);
        }

        /// <summary>
        /// Requests an id from the server.
        /// </summary>
        public void RequestID()
        {
            state = RemoteAction.RequestID;
            Execute(null);
        }

        /// <summary>
        /// Sets the local id of a client.
        /// </summary>
        /// <param name="player">The client who should receive the id.</param>
        /// <param name="id">The id that should be set.</param>
        public void SetID(IPEndPoint player, int id)
        {
            IPEndPoint[] playerArr = { player };
            this.id = id;
            state = RemoteAction.SetID;
            Execute(playerArr);
        }
    }
}
