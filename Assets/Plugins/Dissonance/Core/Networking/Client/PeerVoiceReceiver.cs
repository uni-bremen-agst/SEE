using System;
using System.Collections.Generic;
using Dissonance.Audio.Playback;
using Dissonance.Datastructures;
using Dissonance.Extensions;
using JetBrains.Annotations;

namespace Dissonance.Networking.Client
{
    /// <summary>
    /// All the state to do with a remote player we are receiving audio from
    /// </summary>
    internal class PeerVoiceReceiver
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(PeerVoiceReceiver).Name);

        private readonly string _name;
        public string Name { get { return _name; } }

        private readonly EventQueue _events;
        private readonly Rooms _localListeningRooms;
        private readonly ConcurrentPool<List<RemoteChannel>> _channelListPool;

        private readonly ushort _localId;
        private readonly string _localName;

        private DateTime _lastReceiptTime;
        private ushort _remoteSequenceNumber;
        private uint _localSequenceNumber;

        public bool Open { get; private set; }

        private bool _receivedInitialPacket;
        private ushort _currentChannelSession;
        private readonly Dictionary<int, int> _expectedPerChannelSessions = new Dictionary<int, int>();

        private readonly List<int> _tmpCompositeIdBuffer = new List<int>();
        #endregion

        #region constructor
        public PeerVoiceReceiver(string remoteName, ushort localId, string localName, EventQueue events, Rooms listeningRooms, ConcurrentPool<List<RemoteChannel>> channelListPool)
        {
            _name = remoteName;
            _localId = localId;
            _localName = localName;
            _events = events;
            _localListeningRooms = listeningRooms;
            _channelListPool = channelListPool;
        }
        #endregion

        /// <summary>
        /// Stop speaking if we've gone for too long without any packets from this peer
        /// </summary>
        /// <param name="utcNow"></param>
        /// <param name="activeTimeout">How much "dead air" will be tolerated during a session before the session is timed out</param>
        /// <param name="inactiveTimeout">How many seconds of no sessions at all until the receiver is set back to it's initial state</param>
        public void CheckTimeout(DateTime utcNow, TimeSpan activeTimeout, TimeSpan inactiveTimeout)
        {
            if (Open)
            {
                if ((utcNow - _lastReceiptTime) > activeTimeout)
                {
                    // There is an active speech session, but no packets have been received for a while.
                    // Terminate the session, if more packets arrive later the session will be restarted.
                    Log.Debug("Client '{0}' timed out active speech session", Name);
                    StopSpeaking();
                }
            }
            else
            {
                if ((utcNow - _lastReceiptTime) > inactiveTimeout)
                {
                    // There is not active speech session and no packets have been received for a while. Set the receiver
                    // back to an "uninitialised" state.
                    if (_receivedInitialPacket)
                    {
                        Log.Debug("Client '{0}' set back to initial receiver state", Name);
                        _receivedInitialPacket = false;
                    }
                }
            }
        }

        #region start/stop
        public void StopSpeaking()
        {
            Log.AssertAndThrowPossibleBug(Open, "E8A0D33E-8C74-45F9-AA8C-3889012498D7", "Attempted to stop speaking when not speaking");

            Open = false;
            _events.EnqueueStoppedSpeaking(Name);
        }

        private void StartSpeaking(ushort startSequenceNumber, ushort channelSession, DateTime utcNow)
        {
            Log.AssertAndThrowPossibleBug(!Open, "E8A0D33E-8C74-45F9-AA8C-3889012498D7", "Attempted to start speaking when already speaking");

            // Start speaking, setup up all the speech stream data
            _currentChannelSession = channelSession;
            _remoteSequenceNumber = startSequenceNumber;
            _localSequenceNumber = 0;
            _lastReceiptTime = utcNow;
            Open = true;

            _events.EnqueueStartedSpeaking(Name);
        }
        #endregion

        public void ReceivePacket(ref PacketReader reader, DateTime utcNow)
        {
            //Read second part of the header from the packet
            VoicePacketOptions metadata;
            ushort sequenceNumber, numChannels;
            reader.ReadVoicePacketHeader2(out metadata, out sequenceNumber, out numChannels);

            // if this receiver has never received a packet before then just take whatever session this packet contains as the current session
            // this ensures that late joining peers cannot miss packets due to their channel session being initialised to zero
            if (!_receivedInitialPacket)
                _currentChannelSession = metadata.ChannelSession;
            _receivedInitialPacket = true;

            //If this is a packet from an earlier channel session then we should discard it, it's arrived far too late to be useful.
            if (IsPacketFromPreviousSession(_currentChannelSession, metadata.ChannelSession, metadata.IsChannelSessionExtendedRange))
            {
                Log.Debug("Discarding voice packet from old session. Current:{0}, metadata:{1}", _currentChannelSession, metadata.ChannelSession);
                return;
            }

            //Read the list of channels this voice data is being broadcast on and accumulate info about the channels we're listening on
            bool allClosing, forceReset;
            ChannelsMetadata playbackSettings;
            var channels = _channelListPool.Get();
            ReadChannels(ref reader, numChannels, out allClosing, out forceReset, out playbackSettings, channels);

            //Update the statistics for the channel this data is coming in over and then if it's successful Send voice data onwards
            if (UpdateSpeakerState(allClosing, forceReset, metadata.ChannelSession, sequenceNumber, utcNow))
            {
                //Copy voice data into another buffer (we can't keep hold of this one, it will be recycled after we finish processing this packet)
                var buffer = _events.GetEventBuffer();
                var frame = reader.ReadByteSegment().CopyToSegment(buffer);

                //Send the event (buffer will be recycled after the event has been dispatched)
                _events.EnqueueVoiceData(new VoicePacket(
                    Name,
                    playbackSettings.Priority, playbackSettings.AmplitudeMultiplier, playbackSettings.IsPositional,
                    frame, _localSequenceNumber,
                    channels
                ));
            }

            //If necessary stop speaking
            if (Open && allClosing)
                StopSpeaking();
        }

        #region channels
        /// <summary>
        /// Read all the channel data from the packet and accumulate data about the channels we are listening to
        /// </summary>
        /// <param name="reader">Packetreader to read data from</param>
        /// <param name="numChannels">The number of channels in the packet reader</param>
        /// <param name="allClosing">Indicates if all channels are closing (i.e. we should stop speech after this packet)</param>
        /// <param name="forceReset">Indicates if a reset of the playback system should be forced (i.e. stop and immediately start speaking)</param>
        /// <param name="channelsMetadata">Aggregate metadata about all playing channels</param>
        /// <param name="channelsOut"></param>
        private void ReadChannels(ref PacketReader reader, ushort numChannels, out bool allClosing, out bool forceReset, out ChannelsMetadata channelsMetadata, [NotNull] ICollection<RemoteChannel> channelsOut)
        {
            //Accumulate aggregate information about all the channels
            channelsMetadata = new ChannelsMetadata(true, 0, ChannelPriority.None);
            allClosing = true;
            forceReset = true;

            //Just in case someone else left a mess, clear the list before using it
            channelsOut.Clear();
            _tmpCompositeIdBuffer.Clear();

            for (var i = 0; i < numChannels; i++)
            {
                //Parse a channel of information from the header
                ChannelBitField channel;
                ushort channelRecipient;
                reader.ReadVoicePacketChannel(out channel, out channelRecipient);

                //Skip onwards if we don't care about this channel
                var c = IsChannelToLocalPlayer(channel, channelRecipient);
                if (!c.HasValue)
                    continue;

                //Add an entry for this channel to the list
                channelsOut.Add(c.Value);

                //Form a unique ID for this channel so we can keep track of it across packets
                var compositeId = (int)channel.Type | (channelRecipient << 8);
                _tmpCompositeIdBuffer.Add(compositeId);

                //Accumulate aggregate metadata over all channels
                channelsMetadata = channelsMetadata.CombineWith(new ChannelsMetadata(channel.IsPositional, channel.AmplitudeMultiplier, channel.Priority));
                allClosing &= channel.IsClosing;

                //It's possible that a channel was closed and then re-opened between the sending of two packets. If so then we want to consider the channel...
                //...as closed then re-opened (i.e. reset the channel). However we only need to perform a reset if this applies to *all* the channels this...
                //...receiver is listening to.
                forceReset &= HasChannelSessionChanged(compositeId, channel.SessionId);
            }

            //Only perform a reset if we were already open (a reset makes no sense if we're not open).
            forceReset &= Open;

            //Remove all channels from this peer which we no longer care about
            RemoveChannelsExcept(_tmpCompositeIdBuffer);
            _tmpCompositeIdBuffer.Clear();
        }

        /// <summary>
        /// Check if the channel session for a given compositeId has *changed*
        /// </summary>
        /// <param name="compositeId"></param>
        /// <param name="expectedValue"></param>
        /// <returns></returns>
        private bool HasChannelSessionChanged(int compositeId, int expectedValue)
        {
            var diff = false;
            var none = false;

            //Check if the session ID is either not in the dict, or it's different
            int previousSession;
            if (!_expectedPerChannelSessions.TryGetValue(compositeId, out previousSession))
                none = true;
            else if (previousSession != expectedValue)
                diff = true;

            //Overwrite in either case
            if (none || diff)
                _expectedPerChannelSessions[compositeId] = expectedValue;

            //Return if it was *different*
            return diff;
        }

        /// <summary>
        /// Check if the channel is one the local player is listening to
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="recipient"></param>
        /// <returns></returns>
        private RemoteChannel? IsChannelToLocalPlayer(ChannelBitField channel, ushort recipient)
        {
            var options = new PlaybackOptions(channel.IsPositional, channel.AmplitudeMultiplier, channel.Priority);

            if (channel.Type == ChannelType.Player)
            {
                //If the channel is not to the local player we don't care about it
                if (recipient != _localId)
                    return null;

                return new RemoteChannel(_localName, ChannelType.Player, options);
            }
            else if (channel.Type == ChannelType.Room)
            {
                //This only returns the name for rooms we're currently listening to
                var roomName = _localListeningRooms.Name(recipient);
                if (roomName == null)
                    return null;

                return new RemoteChannel(roomName, ChannelType.Room, options);
            }
            else
            {
                //ncrunch: no coverage start (Justification: Impossible branch)
                throw Log.CreatePossibleBugException(string.Format("Unknown ChannelType variant '{0}'", channel.Type), "1884B2CF-35AA-46BD-93C7-80F14D8D25D8");
                //ncrunch: no coverage end
            }
        }

        /// <summary>
        /// Remove all items from _expectedPerChannelSessions which are not in the given list
        /// </summary>
        /// <param name="keys">Keys to keep</param>
        private void RemoveChannelsExcept([NotNull] List<int> keys)
        {
            //Save how many keys there are in the input. We're going to use this list for two purposes.
            // - [0, Count] is what's in the list now - it's the keys to keep
            // - [Count + 1, N] is what we'll build up in the next loop, that's the keys to *remove*
            var count = keys.Count;

            //Sort the list so it's quicker to search
            keys.Sort();

            //Manually enumerating to prevent foreach loop allocating an enumerator (longstanding Mono/Unity performance problem)
            using (var e = _expectedPerChannelSessions.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    //Find the item in the set to keep
                    var item = e.Current;
                    var ti = keys.BinarySearch(0, count, item.Key, Comparer<int>.Default);

                    //We didn't find this item in the list, add it to the end of the list
                    if (ti < 0)
                        keys.Add(item.Key);
                }
            }

            //We added all the keys we want to *remove* to the end of the list
            //iterate those (starting from the end of the input set)
            for (var i = count; i < keys.Count; i++)
                _expectedPerChannelSessions.Remove(keys[i]);

            //Remove the items we added to the list so it's in the state we were given
            keys.RemoveRange(count, keys.Count - count);
        }
        #endregion

        private bool UpdateSpeakerState(bool allClosing, bool forceReset, ushort channelSession, ushort sequenceNumber, DateTime utcNow)
        {
            //If we need to reset the playback system stop speech right now, it will be restarted if necessary
            if ((forceReset || _currentChannelSession != channelSession) && Open)
            {
                if (forceReset)
                    Log.Trace("Channel reset due to forced reset");
                else
                    Log.Trace("Channel Session has changed: {0} => {1} (Triggering forced playback reset)", _currentChannelSession, channelSession);

                StopSpeaking();
            }

            //If necessary start speaking (don't bother if allClosing, because that would create a 1 packet session, not much point to that)
            if (!allClosing && !Open)
                StartSpeaking(sequenceNumber, channelSession, utcNow);

            //If we're now in a speech session update it
            if (Open)
            {
                //Update the sequence number (discard packet if we fail for some reason)
                if (!UpdateSequenceNumber(sequenceNumber, utcNow))
                    return false;
            }

            return Open;
        }

        private bool UpdateSequenceNumber(ushort sequenceNumber, DateTime utcNow)
        {
            // We assume that the first packet we receive for a session is the first packet of that session, and it accordingly gets assigned a sequence number...
            // ...of zero. Of course it's possible that the true first packet arrives late, however this would cause a negative sequence number and generally...
            // ...cause chaos. We discard the packet (losing the first 10-40ms of speech in this circumstance).
            var sequenceDelta = _remoteSequenceNumber.WrappedDelta16(sequenceNumber);
            if (_localSequenceNumber + sequenceDelta < 0)
            {
                Log.Trace("Discarding old packet which would cause negative sequence number");
                return false;
            }

            //Push forward the sequence number
            _localSequenceNumber = (uint)(_localSequenceNumber + sequenceDelta);
            _remoteSequenceNumber = sequenceNumber;
            _lastReceiptTime = utcNow;

            return true;
        }

        /// <summary>
        /// Check if a channel session is from a previous session or not.
        /// </summary>
        /// <param name="currentChannelSession"></param>
        /// <param name="packetChannelSession"></param>
        /// <param name="isExtendedRange">indicates if the session numbers are 2 bit or 7 bit numbers</param>
        /// <returns></returns>
        private static bool IsPacketFromPreviousSession(ushort currentChannelSession, ushort packetChannelSession, bool isExtendedRange)
        {
            // A changed channel session may indicate that a new session has opened or a packet from an old session has arrived late and should be dropped
            // The session always increments, and wraps around when it reaches `VoicePacketOptions.ChannelSessionRange`. Check if the wrapped delta is greater than zero, in...
            // ...which case this is a new session. Or less than zero, in which case this is an old packet and we should ignore it.
            var delta = isExtendedRange
                      ? currentChannelSession.WrappedDelta7(packetChannelSession)
                      : currentChannelSession.WrappedDelta2(packetChannelSession);

            return delta < 0;
        }

        private struct ChannelsMetadata
        {
            public readonly bool IsPositional;
            public readonly float AmplitudeMultiplier;
            public readonly ChannelPriority Priority;

            public ChannelsMetadata(bool isPositional, float amplitudeMultiplier, ChannelPriority priority)
            {
                IsPositional = isPositional;
                AmplitudeMultiplier = amplitudeMultiplier;
                Priority = priority;
            }

            public ChannelsMetadata CombineWith(ChannelsMetadata other)
            {
                return new ChannelsMetadata(

                    // Positional playback if *all* channels are positional
                    IsPositional & other.IsPositional,

                    // Amplitude is max amplitude of channels
                    Math.Max(AmplitudeMultiplier, other.AmplitudeMultiplier),

                    // Priority is max priority of channels
                    (ChannelPriority)Math.Max((int)Priority, (int)other.Priority)

                );
            }
        }
    }
}
