using System;
using System.Threading;
using JetBrains.Annotations;

namespace Dissonance.Networking.Client
{
    internal enum ConnectionState
    {
        None,
        Negotiating,
        Connected,
        Disconnected
    }

    internal class ConnectionNegotiator<TPeer>
        : ISession
        where TPeer : struct
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(ConnectionNegotiator<TPeer>).Name);
        private static readonly TimeSpan HandshakeRequestInterval = TimeSpan.FromSeconds(2);

        private readonly ISendQueue<TPeer> _sender;
        private readonly string _playerName;
        private readonly CodecSettings _codecSettings;

        private DateTime _lastHandshakeRequest = DateTime.MinValue;
        private bool _running;

        private int _connectionStateValue = (int)ConnectionState.None;
        public ConnectionState State { get { return (ConnectionState)_connectionStateValue; } }

        public uint SessionId { get; private set; }
        public ushort? LocalId { get; private set; }
        public string LocalName { get { return _playerName; } }
        #endregion

        public ConnectionNegotiator([NotNull] ISendQueue<TPeer> sender, string playerName, CodecSettings codecSettings)
        {
            _sender = sender;
            _playerName = playerName;
            _codecSettings = codecSettings;
        }

        public void ReceiveHandshakeResponseHeader(ref PacketReader reader)
        {
            uint session;
            ushort myId;
            reader.ReadHandshakeResponseHeader(out session, out myId);

            //Save local client info as assigned by the server
            SessionId = session;
            LocalId = myId;

            //We could receive an unbounded number of handshake responses. We only want to run this event on the *first* one (when we transition from Negotiating to Connected
            //Additionally it's possible the connection is not in the negotiating state (could already be disconnected). So check that it's the right value before exchanging.
            if (Interlocked.CompareExchange(ref _connectionStateValue, (int)ConnectionState.Connected, (int)ConnectionState.Negotiating) == (int)ConnectionState.Negotiating)
                Log.Info("Received handshake response from server, joined session '{0}'", SessionId);
        }

        public void Start()
        {
            if (State == ConnectionState.Disconnected)
                throw Log.CreatePossibleBugException("Attempted to restart a ConnectionNegotiator after it has been disconnected", "92F0B2EB-282A-4558-B3BD-6656F83A06E3");

            _running = true;
        }

        public void Stop()
        {
            _running = false;
            _connectionStateValue = (int)ConnectionState.Disconnected;
        }

        public void Update(DateTime utcNow)
        {
            if (!_running)
                return;

            var shouldResendHandshake = State == ConnectionState.Negotiating && utcNow - _lastHandshakeRequest > HandshakeRequestInterval;
            if (State == ConnectionState.None || shouldResendHandshake)
                SendHandshake(utcNow);
        }

        /// <summary>
        /// Begin negotiating a connection with the server by sending a handshake.
        /// </summary>
        /// <remarks>It is safe to call this several times, even once negotiation has finished</remarks>
        private void SendHandshake(DateTime utcNow)
        {
            //Sanity check. We can't do *anything* with a disconnected client, definitely not restart negotiation!
            Log.AssertAndThrowPossibleBug(
                State != ConnectionState.Disconnected,
                "39533F23-2DAC-4340-9A7D-960904464E23",
                "Attempted to begin connection negotiation with a client which is disconnected");

            _lastHandshakeRequest = utcNow;

            //Send the handshake request to the server (when the server replies with a response, we know we're connected)
            _sender.EnqueueReliable(
                new PacketWriter(new ArraySegment<byte>(_sender.GetSendBuffer()))
                    .WriteHandshakeRequest(_playerName, _codecSettings)
                    .Written
            );
            Log.Trace("Sent HandshakeRequest");

            //Set the state to negotiating only if the state was previously none
            Interlocked.CompareExchange(ref _connectionStateValue, (int)ConnectionState.Negotiating, (int)ConnectionState.None);
        }
    }

    internal interface ISession
    {
        /// <summary>
        /// Get the unique ID for the current network session (zero, until an ID is assigned by the server)
        /// </summary>
        uint SessionId { get; }

        /// <summary>
        /// Get the unique ID for the local peer (null, until an ID is assigned by the server)
        /// </summary>
        ushort? LocalId { get; }

        /// <summary>
        /// Get the unique name of the local peer
        /// </summary>
        [NotNull] string LocalName { get; }
    }
}
