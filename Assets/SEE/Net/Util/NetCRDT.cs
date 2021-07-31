using SEE.Utils;
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
        public Identifier[] position;
        public Identifier[] prePosition;


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
                        ICRDT.RemoteAddChar(c, position, prePosition);
                        break;

                    case RemoteAction.DelteChar:
                        ICRDT.RemoteDeleteChar(position);
                        break;
                }

            }
        }

        public void DeleteChar(Identifier[] position)
        {
            this.position = position;
            state = RemoteAction.DelteChar;
            Execute(null);
        }

        public void AddChar(char c, Identifier[] position, Identifier[] prePosition)
        {
            this.c = c;
            this.position = position;
            this.prePosition = prePosition;
            state = RemoteAction.AddChar;
            Execute(null);
        }
    }
}
