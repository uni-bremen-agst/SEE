using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Datastructures;
using Dissonance.Threading;
using JetBrains.Annotations;

namespace Dissonance.Networking.Client
{
    internal class EventQueue
    {
        #region helper types
        private enum EventType
        {
            PlayerJoined,
            PlayerLeft,

            PlayerEnteredRoom,
            PlayerExitedRoom,

            PlayerStartedSpeaking,
            PlayerStoppedSpeaking,

            VoiceData,
            TextMessage
        }

        private struct NetworkEvent
        {
            public readonly EventType Type;

            private string _playerName;
            public string PlayerName
            {
                get
                {
                    return _playerName;
                }
                set
                {
                    _playerName = value;
                }
            }

            private CodecSettings _codecSettings;
            public CodecSettings CodecSettings
            {
                get
                {
                    Check(EventType.PlayerJoined);
                    return _codecSettings;
                }
                set
                {
                    Check(EventType.PlayerJoined);
                    _codecSettings = value;
                }
            }

            private string _room;
            public string Room
            {
                get
                {
                    Check(EventType.PlayerEnteredRoom, EventType.PlayerExitedRoom);
                    return _room;
                }
                set
                {
                    Check(EventType.PlayerEnteredRoom, EventType.PlayerExitedRoom);
                    _room = value;
                }
            }

            private ReadOnlyCollection<string> _allRooms;
            [NotNull] public ReadOnlyCollection<string> AllRooms
            {
                get
                {
                    Check(EventType.PlayerEnteredRoom, EventType.PlayerExitedRoom);
                    return _allRooms;
                }
                set
                {
                    Check(EventType.PlayerEnteredRoom, EventType.PlayerExitedRoom);
                    _allRooms = value;
                }
            }

            private readonly VoicePacket _voicePacket;
            public VoicePacket VoicePacket
            {
                get
                {
                    Check(EventType.VoiceData);
                    return _voicePacket;
                }
            }

            private readonly TextMessage _textMessage;
            public TextMessage TextMessage
            {
                get
                {
                    Check(EventType.TextMessage);
                    return _textMessage;
                }
            }
            
            #region constructors
            public NetworkEvent(EventType type)
            {
                Type = type;

                _playerName = null;
                _room = null;
                _allRooms = null;
                _codecSettings = default(CodecSettings);
                _voicePacket = default(VoicePacket);
                _textMessage = default(TextMessage);
            }

            public NetworkEvent(VoicePacket voice)
                : this(EventType.VoiceData)
            {
                _voicePacket = voice;
            }

            public NetworkEvent(TextMessage text)
                : this(EventType.TextMessage)
            {
                _textMessage = text;
            }
            #endregion

            #region accessor sanity checks
            private void Check(EventType type)
            {
                //This is a sanity check against developer mistakes. We can exclude it from final builds.
                #if UNITY_EDITOR
                    Log.AssertAndThrowPossibleBug(type == Type, "EA60F116-8B43-49B9-8625-2E19CF5137BD", "Attempted to access as {0}, but type is {1}", type, Type);
                #endif
            }

            private void Check(EventType typeA, EventType typeB)
            {
                //This is a sanity check against developer mistakes. We can exclude it from final builds.
                #if UNITY_EDITOR
                    Log.AssertAndThrowPossibleBug(
                        typeA == Type || typeB == Type,
                        "EA60F116-8B43-49B9-8625-2E19CF5137BD",
                        "Attempted to access as {0}|{1}, but type is {2}",
                        typeA, typeB, Type
                    );
                #endif
            }
            #endregion
        }
        #endregion

        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(EventQueue).Name);

        private readonly ReadonlyLockedValue<List<NetworkEvent>> _queuedEvents = new ReadonlyLockedValue<List<NetworkEvent>>(new List<NetworkEvent>());

        private readonly ReadonlyLockedValue<Pool<byte[]>> _byteArrayPool;
        [NotNull] private readonly IRecycler<List<RemoteChannel>> _channelsListPool;

        public event Action<string, CodecSettings> PlayerJoined;
        public event Action<string> PlayerLeft;
        public event Action<RoomEvent> PlayerEnteredRoom;
        public event Action<RoomEvent> PlayerExitedRoom;
        public event Action<VoicePacket> VoicePacketReceived;
        public event Action<TextMessage> TextMessageReceived;
        public event Action<string> PlayerStartedSpeaking;
        public event Action<string> PlayerStoppedSpeaking;

        internal event Action<string> OnEnqueuePlayerLeft;

        private const int MinWarnPacketCountThreshold = 12;
        private static readonly TimeSpan MinWarnDispatchTimeThreshold = TimeSpan.FromMilliseconds(64);
        private int _voicePacketWarnThreshold = MinWarnPacketCountThreshold;
        private int _pendingVoicePackets;
        private DateTime _previousFlush = DateTime.MaxValue;
        #endregion

        public EventQueue([NotNull] ReadonlyLockedValue<Pool<byte[]>> byteArrayPool, [NotNull] IRecycler<List<RemoteChannel>> channelsListPool)
        {
            if (byteArrayPool == null) throw new ArgumentNullException("byteArrayPool");
            if (channelsListPool == null) throw new ArgumentNullException("channelsListPool");

            _byteArrayPool = byteArrayPool;
            _channelsListPool = channelsListPool;
        }

        /// <summary>
        /// Dispatch all events waiting in the queue to event handlers
        /// </summary>
        /// <remarks>Returns true if any invocation caused an error</remarks>
        public bool DispatchEvents(DateTime? utcNow = null)
        {
            PreDispatchLog(utcNow ?? DateTime.UtcNow);

            var error = false;

            using (var events = _queuedEvents.Lock())
            {
                var queuedEvents = events.Value;

                for (var i = 0; i < queuedEvents.Count; i++)
                {
                    var e = queuedEvents[i];

                    switch (e.Type)
                    {
                        case EventType.PlayerJoined:
                            error |= InvokeEvent(e.PlayerName, e.CodecSettings, PlayerJoined);
                            break;
                        case EventType.PlayerLeft:
                            error |= InvokeEvent(e.PlayerName, PlayerLeft);
                            break;
                        case EventType.PlayerStartedSpeaking:
                            error |= InvokeEvent(e.PlayerName, PlayerStartedSpeaking);
                            break;
                        case EventType.PlayerStoppedSpeaking:
                            error |= InvokeEvent(e.PlayerName, PlayerStoppedSpeaking);
                            break;
                        case EventType.VoiceData:
                            error |= InvokeEvent(e.VoicePacket, VoicePacketReceived);
                            _pendingVoicePackets--;

                            // Recycle channel buffer
                            if (e.VoicePacket.Channels != null)
                            {
                                e.VoicePacket.Channels.Clear();
                                _channelsListPool.Recycle(e.VoicePacket.Channels);
                            }

                            // Recycle voice data buffer
                            var arr = e.VoicePacket.EncodedAudioFrame.Array;
                            if (arr != null)
                                using (var locker = _byteArrayPool.Lock())
                                    locker.Value.Put(arr);

                            break;
                        case EventType.TextMessage:
                            error |= InvokeEvent(e.TextMessage, TextMessageReceived);
                            break;
                        case EventType.PlayerEnteredRoom:
                            var evtEnter = CreateRoomEvent(e, true);
                            error |= InvokeEvent(evtEnter, PlayerEnteredRoom);
                            break;
                        case EventType.PlayerExitedRoom:
                            var evtExit = CreateRoomEvent(e, false);
                            error |= InvokeEvent(evtExit, PlayerExitedRoom);
                            break;

                        //ncrunch: no coverage start (Justification: It's a sanity check, we shouldn't ever hit this line)
                        default:
                            throw new ArgumentOutOfRangeException();
                        //ncrunch: no coverage end
                    }
                }

                queuedEvents.Clear();

                return error;
            }
        }

        private void PreDispatchLog(DateTime utcNow)
        {
            //Calculate how long since the previous dispatch
            var delta = utcNow - _previousFlush;
            _previousFlush = utcNow;

            //Check if we need to warn, if not decay the threshold and early exit
            if (_pendingVoicePackets < _voicePacketWarnThreshold)
            {
                _voicePacketWarnThreshold = Math.Max(MinWarnPacketCountThreshold, _voicePacketWarnThreshold - 1);
                return;
            }

            //A warning is required, exponentially backoff
            _voicePacketWarnThreshold *= 4;

            //Send a warning, exact text depends on what the cause might be
            if (delta > MinWarnDispatchTimeThreshold)
                Log.Warn("Large number of received packets pending dispatch ({0}). Possibly due to long frame times (last frame was {1}ms)", _pendingVoicePackets, delta.TotalMilliseconds);
            else
                Log.Warn("Large number of received packets pending dispatch ({0}). Possibly due to network congestion (last frame was {1}ms)", _pendingVoicePackets, delta.TotalMilliseconds);
        }

        private static RoomEvent CreateRoomEvent(NetworkEvent @event, bool joined)
        {
            return new RoomEvent
            {
                PlayerName = @event.PlayerName,
                Room = @event.Room,
                Joined = joined,
                Rooms = @event.AllRooms
            };
        }

        private static bool InvokeEvent<T>(T arg, [CanBeNull]Action<T> handler)
        {
            try
            {
                if (handler != null)
                    handler(arg);
            }
            catch (Exception e)
            {
                Log.Error("Exception invoking event handler: {0}", e);
                return true;
            }

            return false;
        }

        private static bool InvokeEvent<T1, T2>(T1 arg1, T2 arg2, [CanBeNull]Action<T1, T2> handler)
        {
            try
            {
                if (handler != null)
                    handler(arg1, arg2);
            }
            catch (Exception e)
            {
                Log.Error("Exception invoking event handler: {0}", e);
                return true;
            }

            return false;
        }

        #region enqueue
        public void EnqueuePlayerJoined(string playerName, CodecSettings codecSettings)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerJoined) { PlayerName = playerName, CodecSettings = codecSettings });
        }

        public void EnqueuePlayerLeft(string playerName)
        {
            if (OnEnqueuePlayerLeft != null)
                OnEnqueuePlayerLeft(playerName);

            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerLeft) { PlayerName = playerName });
        }

        public void EnqueuePlayerEnteredRoom([NotNull] string playerName, [NotNull] string room, [NotNull] ReadOnlyCollection<string> allRooms)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerEnteredRoom) { PlayerName = playerName, Room = room, AllRooms = allRooms });
        }

        public void EnqueuePlayerExitedRoom([NotNull] string playerName, [NotNull] string room, [NotNull] ReadOnlyCollection<string> allRooms)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerExitedRoom) { PlayerName = playerName, Room = room, AllRooms = allRooms });
        }

        public void EnqueueStartedSpeaking(string playerName)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerStartedSpeaking) { PlayerName = playerName });
        }

        public void EnqueueStoppedSpeaking(string playerName)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerStoppedSpeaking) { PlayerName = playerName });
        }

        public void EnqueueVoiceData(VoicePacket data)
        {
            using (var events = _queuedEvents.Lock())
            {
                _pendingVoicePackets++;
                events.Value.Add(new NetworkEvent(data));
            }
        }

        public void EnqueueTextData(TextMessage data)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(data));
        }
        #endregion

        public byte[] GetEventBuffer()
        {
            using (var locker = _byteArrayPool.Lock())
                return locker.Value.Get();
        }
    }
}
