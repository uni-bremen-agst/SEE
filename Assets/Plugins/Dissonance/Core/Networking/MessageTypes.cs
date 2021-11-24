namespace Dissonance.Networking
{
    internal enum MessageTypes
        : byte
    {
        /// <summary>
        /// A message containing the state of a client - it's name and a list of rooms it is listening to.
        /// Sent from client to server whenever the client enters or exits a room
        /// </summary>
        ClientState = 1,

        /// <summary>
        /// A packet of voice data prefixed with a list of channel IDs.
        /// Sent from client to server, and then from server to listening clients.
        /// </summary>
        VoiceData = 2,

        /// <summary>
        /// A packet of text data, prefixed with a list of channel IDs.
        /// Sent from client to server, and then from server to listening clients.
        /// </summary>
        TextData = 3,

        /// <summary>
        /// A request from a client to join a Dissonance session.
        /// </summary>
        HandshakeRequest = 4,

        /// <summary>
        /// A response from the server to a client handshake request, contains the session ID.
        /// </summary>
        HandshakeResponse = 5,

        /// <summary>
        /// Error message sent from server to clients which use the wrong session ID. Forces the client
        /// to disconnect and reconnect (with a new handshake to establish the correct session ID).
        /// </summary>
        ErrorWrongSession = 6,

        /// <summary>
        /// A message from client to server containing a list of destination peers and a packet of data. Server unwraps the
        /// data packet and sends it on to the peers in the list. Can be used when direct P2P routing isn't available.
        /// </summary>
        ServerRelayReliable = 7,

        /// <summary>
        /// A message from client to server containing a list of destination peers and a packet of data. Server unwraps the
        /// data packet and sends it on to the peers in the list. Can be used when direct P2P routing isn't available.
        /// </summary>
        ServerRelayUnreliable = 8,

        /// <summary>
        /// Change in state of clients and channels, sent from server to client whenever clients open or close a channel
        /// </summary>
        DeltaChannelState = 9,

        /// <summary>
        /// A signal from the server to remove a client from the session
        /// </summary>
        RemoveClient = 10,

        /// <summary>
        /// A p2p handshake from another peer
        /// </summary>
        HandshakeP2P = 11,
    }
}
