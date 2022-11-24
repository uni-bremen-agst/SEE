using SEE.Utils;
using System.Net;
using static SEE.Utils.CRDT;

namespace SEE.Net.Actions
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
            DeleteChar,
            /// <summary>
            /// In this state a RemoteAddChar() should be executed on each client.
            /// </summary>
            AddChar,
            /// <summary>
            /// Adds a list of single char operations to the crdt to save network load.
            /// </summary>
            AddString,
            /// <summary>
            /// For requesting an ID from the server.
            /// </summary>
            RequestID,
            /// <summary>
            /// The return of the ID from the Server.
            /// </summary>
            SetID,
            /// <summary>
            /// Synchronizes the CRDT with only one client when a new client joins the game.
            /// </summary>
            SyncWithNewClient,
        };

        /// <summary>
        /// The specific instance of <see cref="RemoteAction"/>
        /// </summary>
        public RemoteAction State = RemoteAction.Init;

        /// <summary>
        /// The character that will be transmitted.
        /// </summary>
        public char Character;

        /// <summary>
        ///  The position of the character in the file.
        /// </summary>
        public string Position;

        /// <summary>
        /// The name of the file in which changes should be made.
        /// </summary>
        public string File;

        /// <summary>
        /// The text that should be transmitted.
        /// </summary>
        public string Text;

        /// <summary>
        /// The site ID of the user.
        /// </summary>
        public int ID;

        /// <summary>
        /// Things to execute on the server.
        /// Generates a ID for each client.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            if (State == RemoteAction.RequestID)
            {
                new NetCRDT().SetID(GetRequester(), ICRDT.RequestID());
                State = RemoteAction.Init;
            }
        }

        /// <summary>
        /// Adds or deletes characters on each client to synchronize the code window or sets the siteId for a user.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                switch (State)
                {
                    case RemoteAction.AddChar:
                        ICRDT.RemoteAddChar(Character, ICRDT.StringToPosition(Position, File), File);
                        break;
                    case RemoteAction.SyncWithNewClient:
                        ICRDT.SingleRemoteAddChar(Character, ICRDT.StringToPosition(Position, File), File);

                       break;

                    case RemoteAction.DeleteChar:
                        ICRDT.RemoteDeleteChar(ICRDT.StringToPosition(Position, File), File);

                        break;
                    case RemoteAction.AddString:
                        ICRDT.RemoteAddString(Text, File);
                        break;
                }
            }

            if (State.Equals(RemoteAction.SetID))
            {
                ICRDT.SetLocalID(ID);
            }
        }

        /// <summary>
        /// Deletes a character on each client.
        /// </summary>
        /// <param name="position">The position at which a character should be deleted.</param>
        /// <param name="file">The filename of the crdt in which the character should be deleted.</param>
        public void DeleteChar(Identifier[] position, string file)
        {
            this.File = file;
            this.Position = ICRDT.PositionToString(position, file);
            State = RemoteAction.DeleteChar;
            Execute(null);
        }

        /// <summary>
        /// Adds a character on each client.
        /// </summary>
        /// <param name="c">The character that should be added.</param>
        /// <param name="position">The position at which it should be added.</param>
        /// <param name="file">The filename of the crdt in which it should be added.</param>
        public void AddChar(char c, Identifier[] position, string file)
        {
            this.File = file;
            this.Character = c;
            this.Position = ICRDT.PositionToString(position, file);
            State = RemoteAction.AddChar;
            Execute(null);
        }

        /// <summary>
        /// Adds a character on a specific client. Used for synchronizing
        /// the content of the crdt with a new client that joins an existing session.
        /// </summary>
        /// <param name="c">The char that should be added.</param>
        /// <param name="position">The position at which it should be added.</param>
        /// <param name="file">The filename of the crdt in which it should be added.</param>
        /// <param name="recipient">The recipient that should receive the cahnge.</param>
        public void SingleAddChar(char c, Identifier[] position, string file, IPEndPoint[] recipient)
        {
            this.File = file;
            this.Character = c;
            this.Position = ICRDT.PositionToString(position, file);
            State = RemoteAction.SyncWithNewClient;
            Execute(recipient);
        }

        /// <summary>
        /// Adds a string on every client.
        /// </summary>
        /// <param name="text">The string that should be added, containing the positions as a string.</param>
        /// <param name="filename">The filename of the crdt in which the string should be added.</param>
        public void AddString(string text, string filename)
        {
            this.File = filename;
            this.Text = text;
            this.State = RemoteAction.AddString;
            Execute(null);
        }

        /// <summary>
        /// Requests an id from the server.
        /// </summary>
        public void RequestID()
        {
            State = RemoteAction.RequestID;
            Execute(null);
        }

        /// <summary>
        /// Sets the local id of a client.
        /// </summary>
        /// <param name="player">The client who should receive the id.</param>
        /// <param name="id">The id that should be set.</param>
        public void SetID(IPEndPoint player, int id)
        {
            this.ID = id;
            State = RemoteAction.SetID;
            Execute(new IPEndPoint[] { player });
        }
    }
}
