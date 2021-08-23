using SEE.Controls;
using SEE.Utils;
using UnityEngine;
using static SEE.Utils.CRDT;

namespace SEE.Net
{
    /// <summary>
    /// This class connects the <see cref="CRDT"/> to all clients in the network.
    /// </summary>
    public class NetCRDT : AbstractAction
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
        };

        /// <summary>
        /// The specific instance of <see cref="RemoteAction"/>
        /// </summary>
        public RemoteAction state = RemoteAction.Init;

        public char c;
        public string position;
        public string prePosition;
        public string file;


        public NetCRDT() : base()
        {
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
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
                        Debug.LogWarning("POSITION " + position + " PREE " + prePosition);
                        ICRDT.RemoteAddChar(c, ICRDT.StringToPosition(position, file), ICRDT.StringToPosition(prePosition, file), file);
                        if (CodeSpaceManager.ManagerInstance)
                        {
                            int index = ICRDT.GetIndexByPosition(ICRDT.StringToPosition(prePosition, file), file);
                            if(index == -1)
                            {
                                return;
                            }

                            CodeSpaceManager.ManagerInstance.InsertChar(RequesterIPAddress, file, c, index);

                        }
                        break;

                    case RemoteAction.DelteChar:
                        ICRDT.RemoteDeleteChar(ICRDT.StringToPosition(position, file), file);
                        if (CodeSpaceManager.ManagerInstance)
                        {
                            int index = ICRDT.GetIndexByPosition(ICRDT.StringToPosition(position, file), file);
                            if (index == -1)
                            {
                                return;
                            }

                            CodeSpaceManager.ManagerInstance.DeleteChar(RequesterIPAddress, file, index);
                        }
                        break;
                }

            }
        }

        public void DeleteChar(Identifier[] position, string file)
        {
            this.file = file;
            this.position = ICRDT.PositionToString(position, file);
            state = RemoteAction.DelteChar;
            Execute(null);
        }

        public void AddChar(char c, Identifier[] position, Identifier[] prePosition, string file)
        {
            this.file = file;
            this.c = c;
            this.position = ICRDT.PositionToString(position, file);
            this.prePosition = ICRDT.PositionToString(prePosition, file);
            state = RemoteAction.AddChar;
            Execute(null);
        }
    }
}
