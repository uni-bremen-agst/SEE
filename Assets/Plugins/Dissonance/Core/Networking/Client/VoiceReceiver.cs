using System;
using System.Collections.Generic;
using Dissonance.Datastructures;
using JetBrains.Annotations;

namespace Dissonance.Networking.Client
{
    internal class VoiceReceiver<TPeer>
        where TPeer : struct
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(VoiceReceiver<TPeer>).Name);

        private static readonly TimeSpan ActiveTimeout = TimeSpan.FromSeconds(1.5);
        private static readonly TimeSpan InactiveTimeout = TimeSpan.FromSeconds(15);

        private readonly ISession _session;
        private readonly IClientCollection<TPeer?> _clients;
        private readonly EventQueue _events;
        private readonly Rooms _rooms;
        private readonly ConcurrentPool<List<RemoteChannel>> _channelListPool;

        private readonly List<PeerVoiceReceiver> _receivers = new List<PeerVoiceReceiver>();
        #endregion

        #region constructor
        public VoiceReceiver(ISession session, IClientCollection<TPeer?> clients, EventQueue events, Rooms rooms, ConcurrentPool<List<RemoteChannel>> channelListPool)
        {
            _session = session;
            _clients = clients;
            _events = events;
            _rooms = rooms;
            _channelListPool = channelListPool;

            _events.OnEnqueuePlayerLeft += OnPlayerLeft;
        }
        #endregion

        private void OnPlayerLeft([NotNull] string name)
        {
            for (var i = 0; i < _receivers.Count; i++)
            {
                var r = _receivers[i];
                if (r.Name == name)
                {
                    if (r.Open)
                        r.StopSpeaking();

                    _receivers.RemoveAt(i);
                    return;
                }

            //ncrunch: no coverage start (Justification: Last brace has no coverage due to loop early exit)
            }
            //ncrunch: no coverage end
        }

        public void Stop()
        {
            //Stop all incoming voice streams
            for (var i = 0; i < _receivers.Count; i++)
            {
                var r = _receivers[i];
                if (r != null && _receivers[i].Open)
                    _receivers[i].StopSpeaking();
            }

            //Discard all receivers
            _receivers.Clear();
        }

        public void Update(DateTime utcNow)
        {
            CheckTimeouts(utcNow);
        }

        /// <summary>
        /// Transition to a non-receiving state for all receivers which have not received any packets within a short window
        /// </summary>
        private void CheckTimeouts(DateTime utcNow)
        {
            for (var i = _receivers.Count - 1; i >= 0; i--)
            {
                var r = _receivers[i];
                if (r != null)
                    r.CheckTimeout(utcNow, ActiveTimeout, InactiveTimeout);
            }
        }

        public void ReceiveVoiceData(ref PacketReader reader, DateTime? utcNow = null)
        {
            //Early exit if we don't know who we are yet
            if (!_session.LocalId.HasValue)
            {
                Log.Debug("Receiver voice packet before assigned local ID, discarding");
                return;
            }

            //Read first part of the header from voice packet
            ushort senderId;
            reader.ReadVoicePacketHeader1(out senderId);

            //Early exit if sender peer doesn't exist
            ClientInfo<TPeer?> client;
            if (!_clients.TryGetClientInfoById(senderId, out client))
            {
                Log.Debug("Received voice packet from unknown/disconnected peer '{0}'", senderId);
                return;
            }

            //Create a receiver if there isn't one yet
            if (client.VoiceReceiver == null)
            {
                client.VoiceReceiver = new PeerVoiceReceiver(client.PlayerName, _session.LocalId.Value, _session.LocalName, _events, _rooms, _channelListPool);
                _receivers.Add(client.VoiceReceiver);
            }

            //Parse the packet with the parser for this remote speaker
            client.VoiceReceiver.ReceivePacket(ref reader, utcNow ?? DateTime.UtcNow);
        }
    }
}
