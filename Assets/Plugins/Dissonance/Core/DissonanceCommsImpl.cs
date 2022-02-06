using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Audio;
using Dissonance.Audio.Capture;
using Dissonance.Audio.Codecs.Opus;
using Dissonance.Audio.Playback;
using Dissonance.Config;
using Dissonance.Networking;
using Dissonance.VAD;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Profiling;

namespace Dissonance
{
    public partial class DissonanceComms
        : IPriorityManager, IAccessTokenCollection, IChannelPriorityProvider, IVolumeProvider
    {
        #region fields
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(DissonanceComms).Name);

        [SerializeField, UsedImplicitly] private string _lastPrefabError;

        private bool _started;
        internal bool IsStarted { get { return _started; } }

        private readonly Rooms _rooms = new Rooms();
        private readonly PlayerChannels _playerChannels;
        private readonly RoomChannels _roomChannels;
        private readonly TextChat _text;
        private readonly OpenChannelVolumeDuck _autoChannelDuck;

        private readonly PlayerTrackerManager _playerTrackers;
        private readonly PlaybackPool _playbackPool;
        private readonly PlayerCollection _players = new PlayerCollection();
        private readonly CodecSettingsLoader _codecSettingsLoader = new CodecSettingsLoader();
        private readonly PriorityManager _playbackPriorityManager;
        private readonly CapturePipelineManager _capture;

        private ICommsNetwork _net;
        private string _localPlayerName;

        [SerializeField, UsedImplicitly] private bool _isMuted;
        [SerializeField, UsedImplicitly] private bool _isDeafened;
        [SerializeField, UsedImplicitly] private float _oneMinusBaseRemoteVoiceVolume;
        #pragma warning disable 0649 //field not used
        [SerializeField, UsedImplicitly] private VoicePlayback _playbackPrefab;
        #pragma warning restore 0649
        [SerializeField, UsedImplicitly] private GameObject _playbackPrefab2;
        [SerializeField, UsedImplicitly] private string _micName;
        [SerializeField, UsedImplicitly] private ChannelPriority _playerPriority = ChannelPriority.Default;
        [SerializeField, UsedImplicitly] private TokenSet _tokens = new TokenSet();

        // ReSharper disable EventNeverSubscribedTo.Global (Justification: Part of public API)
        public event Action<VoicePlayerState> OnPlayerJoinedSession;
        public event Action<VoicePlayerState> OnPlayerLeftSession;
        public event Action<VoicePlayerState> OnPlayerStartedSpeaking;
        public event Action<VoicePlayerState> OnPlayerStoppedSpeaking;
        public event Action<VoicePlayerState, string> OnPlayerEnteredRoom;
        public event Action<VoicePlayerState, string> OnPlayerExitedRoom;
        public event Action<string> LocalPlayerNameChanged;
        // ReSharper restore EventNeverSubscribedTo.Global
        #endregion

        public DissonanceComms()
        {
            _playbackPool = new PlaybackPool((IPriorityManager)this, (IVolumeProvider)this);
            _playerChannels = new PlayerChannels((IChannelPriorityProvider)this);
            _roomChannels = new RoomChannels((IChannelPriorityProvider)this);
            _text = new TextChat(() => _net);
            _autoChannelDuck = new OpenChannelVolumeDuck(_roomChannels, _playerChannels);
            _playerTrackers = new PlayerTrackerManager(_players);
            _playbackPriorityManager = new PriorityManager(_players);
            _capture = new CapturePipelineManager(_codecSettingsLoader, _roomChannels, _playerChannels, Players, 3);
        }

        #region properties
        internal float PacketLoss
        {
            get { return _capture.PacketLoss; }
        }

        /// <summary>
        /// Get or set the local player name (may only be set before this component starts)
        /// </summary>
        public string LocalPlayerName
        {
            get { return _localPlayerName; }
            set
            {
                if (_localPlayerName == value)
                    return;

                if (_started)
                {
                    throw Log.CreateUserErrorException(
                        "Cannot set player name when the component has been started",
                        "directly setting the 'LocalPlayerName' property too late",
                        "https://placeholder-software.co.uk/dissonance/docs/Reference/Components/Dissonance-Comms",
                        "58973EDF-42B5-4FF1-BE01-FFF28300A97E"
                    );
                }

                _localPlayerName = value;

                var handler = LocalPlayerNameChanged;
                if (handler != null) handler(value);
            }
        }

        /// <summary>
        /// Get a value indicating if Dissonance has successfully connected to a voice network yet
        /// </summary>
        public bool IsNetworkInitialized
        {
            get { return _net != null && _net.Status == ConnectionStatus.Connected; }
        }
        
        /// <summary>
        /// Get an object to control which rooms the local player is listening to
        /// </summary>
        [NotNull] public Rooms Rooms
        {
            get { return _rooms; }
        }

        /// <summary>
        /// Get an object to control channels to other players
        /// </summary>
        [NotNull] public PlayerChannels PlayerChannels
        {
            get { return _playerChannels; }
        }

        /// <summary>
        /// Get an object to control channels to rooms (transmitting)
        /// </summary>
        [NotNull] public RoomChannels RoomChannels
        {
            get { return _roomChannels; }
        }

        /// <summary>
        /// Get an object to send and receive text messages
        /// </summary>
        [NotNull] public TextChat Text
        {
            get { return _text; }
        }

        /// <summary>
        /// Get a list of states of all players in the Dissonance voice session
        /// </summary>
        [NotNull] public ReadOnlyCollection<VoicePlayerState> Players
        {
            get { return _players.Readonly; }
        }

        /// <summary>
        /// Get the priority of the current highest priority speaker
        /// </summary>
        public ChannelPriority TopPrioritySpeaker
        {
            get { return _playbackPriorityManager.TopPriority; }
        }

        /// <summary>
        /// Get the set of tokens the local player has knowledge of
        /// </summary>
        [NotNull] public IEnumerable<string> Tokens
        {
            get { return _tokens; }
        }

        /// <summary>
        /// The default priority to use for this player if a broadcast trigger does not specify a priority
        /// </summary>
        public ChannelPriority PlayerPriority
        {
            get { return _playerPriority; }
            set { _playerPriority = value; }
        }

        /// <summary>
        /// Get or set the prefab to use for voice playback (may only be set before this component Starts)
        /// </summary>
        public GameObject PlaybackPrefab
        {
            get { return _playbackPrefab2; }
            set
            {
                if (_started)
                {
                    throw Log.CreateUserErrorException(
                        "Cannot set playback prefab when the component has been started",
                        "directly setting the 'PlaybackPrefab' property too late",
                        "https://placeholder-software.co.uk/dissonance/docs/Reference/Components/Dissonance-Comms",
                        "A0796DA8-A0BC-49E4-A1B3-F0AA0F51BAA0"
                    );
                }

                //Sanity check that the value has an IVoicePlayback component attached
                if (value != null)
                {
                    if (value.GetComponent<IVoicePlayback>() == null)
                    {
                        throw Log.CreateUserErrorException(
                            "Cannot set playback prefab to a prefab without an implemented of IVoicePlayback",
                            "Setting the 'PlaybackPrefab' property to an incorrect prefab",
                            "https://placeholder-software.co.uk/dissonance/docs/Reference/Components/Dissonance-Comms",
                            "543EB2C1-8911-405B-8BEA-5DBC185DF0C3"
                        );
                    }
                }

                _playbackPrefab2 = value;
            }
        }

        /// <summary>
        /// Get or set if the local player is muted (prevented from sending any voice transmissions)
        /// </summary>
        public bool IsMuted
        {
            get { return _isMuted; }
            set
            {
                if (_isMuted != value)
                {
                    _isMuted = value;
                    Log.Debug("Set IsMuted to '{0}'", value);

                    // Refresh all open channels to account for the fact the muting has ended, this essentially re-opens them immediately
                    // and ensures the open channels work properly even though they've been suppressed for a while by mute.
                    if (!_isMuted)
                    {
                        _playerChannels.Refresh();
                        _roomChannels.Refresh();
                    }
                }

            }
        }

        /// <summary>
        /// Get or set if the local player is deafened (prevented from hearing any other voice transmissions)
        /// </summary>
        public bool IsDeafened
        {
            get { return _isDeafened; }
            set
            {
                if (_isDeafened != value)
                {
                    _isDeafened = value;
                    Log.Debug("Set IsDeafened to '{0}'", value);
                }
            }
        }

        /// <summary>
        /// Base volume attenuation to use for all remote voices (must be between 0 and 1)
        /// </summary>
        public float RemoteVoiceVolume
        {
            get
            {
                //store as (1 - value) so that the default value (0) is interpreted as (1) instead
                return Mathf.Clamp01(1 - _oneMinusBaseRemoteVoiceVolume);
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value", "Value must be greater than or equal to zero");
                if (value > 1) throw new ArgumentOutOfRangeException("value", "Value must be less than or equal to one");
                _oneMinusBaseRemoteVoiceVolume = 1 - value;
            }
        }

        private bool _muteAllRemoteVoices;
        /// <summary>
        /// This flag indicates if all remote voices should be muted (i.e. have their volume set to zero).
        /// This is exactly equivalent to IsDeafened, but this one is private so it can be used within Dissonance
        /// without worrying about something else overwriting the deafened state.
        /// </summary>
        private bool MuteAllRemoteVoices
        {
            get { return _muteAllRemoteVoices; }
            set
            {
                if (_muteAllRemoteVoices != value)
                {
                    _muteAllRemoteVoices = value;
                    Log.Debug("Set MuteAllRemoteVoices to '{0}'", value);
                }
            }
        }
        #endregion

        [UsedImplicitly] private void Start()
        {
            // Unity is unreliable about late loading DLLs so try to load dependencies as early as possible.
            try
            {
                TestDependencies();
            }
            catch (Exception e)
            {
                Log.Error("Dependency Error: {0}", e.Message);
            }

            //Ensure that all settings are loaded before we access them (potentially from other threads)
            ChatRoomSettings.Preload();
            DebugSettings.Preload();
            VoiceSettings.Preload();

            //Write multithreaded logs ASAP so the logging system knows which is the main thread
            Logs.WriteMultithreadedLogs();
            Metrics.WriteMultithreadedMetrics();

            //Sanity check (can't run without a network object)
            var net = gameObject.GetComponent<ICommsNetwork>();
            if (net == null)
                throw new Exception("Cannot find a voice network component. Please attach a voice network component appropriate to your network system to the DissonanceVoiceComms' entity.");

            //Sanity check (can't run without run in background). This value doesn't work on mobile platforms so don't perform this check there
            if (!Application.isMobilePlatform && !Application.runInBackground)
            {
                Log.Error(Log.UserErrorMessage(
                    "Run In Background is not set",
                    "The 'Run In Background' toggle on the player settings has not been checked",
                    "https://placeholder-software.co.uk/dissonance/docs/Basics/Getting-Started.html#3-run-in-background",
                    "98D123BB-CF4F-4B41-8555-41CD01108DA7")
                );
            }

            //Load default playback prefab if one has not been set
            if (PlaybackPrefab == null)
            {
                //Check if there is a legacy playback prefab set
                if (_playbackPrefab != null)
                    PlaybackPrefab = _playbackPrefab.gameObject;
                else
                {
                    Log.Info("Loading default playback prefab");
                    PlaybackPrefab = Resources.Load<GameObject>("PlaybackPrefab");

                    if (PlaybackPrefab == null)
                        throw Log.CreateUserErrorException(
                            "Failed to load PlaybackPrefab from resources - Dissonance voice will be disabled",
                            "Incorrect installation of Dissonance",
                            "https://placeholder-software.co.uk/dissonance/docs/Basics/Getting-Started.html",
                            "F542DAE5-AB78-4ADE-8FF0-4573233505AB"
                        );
                }
            }

            net.PlayerJoined += Net_PlayerJoined;
            net.PlayerLeft += Net_PlayerLeft;
            net.PlayerEnteredRoom += Net_PlayerRoomEvent;
            net.PlayerExitedRoom += Net_PlayerRoomEvent;
            net.VoicePacketReceived += Net_VoicePacketReceived;
            net.PlayerStartedSpeaking += Net_PlayerStartedSpeaking;
            net.PlayerStoppedSpeaking += Net_PlayerStoppedSpeaking;
            net.TextPacketReceived += _text.OnMessageReceived;

            //If an explicit name has not been set generate a GUID based name
            if (string.IsNullOrEmpty(LocalPlayerName))
            {
                var guid = Guid.NewGuid().ToString();
                LocalPlayerName = guid;
            }

            //mark this component as started, locking the LocalPlayerName, PlaybackPrefab and Microphone properties from changing
            _started = true;

            //Setup the playback pool so we can create pipelines to play audio
            _playbackPool.Start(PlaybackPrefab, transform);

            //Make sure we load up the codec settings so we can create codecs later
            _codecSettingsLoader.Start();

            //Start the player collection (to set local name)
            _players.Start(LocalPlayerName, _capture, Rooms, RoomChannels, PlayerChannels, _capture, net);

            net.Initialize(LocalPlayerName, Rooms, PlayerChannels, RoomChannels, _codecSettingsLoader.Config);
            _net = net;

            //Begin capture manager, this will create and destroy capture pipelines as necessary (net mode changes, mic name changes, mic requires reset etc)
            _capture.MicrophoneName = _micName;
            _capture.Start(_net, GetOrAddMicrophone());

            Log.Info("Starting Dissonance Voice Comms ({0})\n- Network: [{1}]\n- Quality Settings: [{2}]\n- Codec: [{3}]", Version, _net, VoiceSettings.Instance, _codecSettingsLoader);
        }

        private IMicrophoneCapture GetOrAddMicrophone()
        {
            var mic = GetComponent<IMicrophoneCapture>();
            if (mic == null)
                mic = gameObject.AddComponent<BasicMicrophoneCapture>();

            return mic;
        }

        [UsedImplicitly] private void OnEnable()
        {
            if (_started)
                _capture.Resume("DissonanceComms OnEnable called");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.pauseStateChanged += OnEditorPauseChanged;
#endif
        }

        [UsedImplicitly] private void OnDisable()
        {
            if (_started)
                _capture.Pause();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.pauseStateChanged -= OnEditorPauseChanged;
#endif
        }

#if UNITY_EDITOR
        private void OnEditorPauseChanged(UnityEditor.PauseState state)
        {
            if (state == UnityEditor.PauseState.Paused)
                OnApplicationPause(true);
            else if (state == UnityEditor.PauseState.Unpaused)
                OnApplicationPause(false);
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        [UsedImplicitly] private static void OnScriptsReloaded()
        {
            if (UnityEditor.EditorApplication.isPlaying)
                Log.Error("Dissonance does not support reloading scripts reloading while in play mode");
        }
#endif

        private Coroutine _resumeCo;

        private void OnApplicationPause(bool paused)
        {
            if (!_started)
                return;

            if (paused)
            {
                Log.Info("OnApplicationPause (Paused)");

                // if the coroutine to resume is still running kill it now
                if (_resumeCo != null)
                    StopCoroutine(_resumeCo);
                _resumeCo = null;

                // Pause audio recording
                _capture.Pause();

                // Suspend playback for all speakers.
                MuteAllRemoteVoices = true;
            }
            else
            {
                Log.Info("OnApplicationPause (Resumed)");

                // Force a capture pipeline reset to resume recording
                _capture.ForceReset();

                // Start a coroutine to resume now that the pause has ended
                if (_resumeCo != null)
                    StopCoroutine(_resumeCo);
                _resumeCo = StartCoroutine(CoResumePlayback());
            }
        }

        private IEnumerator CoResumePlayback()
        {
            // Many network systems are quite confused by application pauses. Some of them delivering a large number of packets all at once
            // when the pause ends. Wait for a few frames to give the network system some time to sort itself out.
            for (var i = 0; i < 30; i++)
                yield return null;

            // Force a capture pipeline reset to resume recording
            _capture.ForceReset();

            // Allow remote voices to start playing
            MuteAllRemoteVoices = false;

            // Flush playback pipelines for all remote speakers, discarding all buffered voice sessions
            var players = Players;
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player.IsLocalPlayer)
                    continue;

                var playback = player.PlaybackInternal;
                if (playback != null)
                    playback.ForceReset();
            }

            // Now that this coroutine is done clear the state
            _resumeCo = null;
        }

        #region network events
        // ReSharper disable once InconsistentNaming
        private void Net_PlayerStoppedSpeaking([NotNull] string player)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode (Justification: Sanity check against network system returning incorrect values)
            if (player == null)

            {
                Log.Warn(Log.PossibleBugMessage("Received a player-stopped-speaking event for a null player ID", "5A424BF0-D384-4A63-B6E2-042A1F31A085"));
                return;
            }
            // ReSharper restore HeuristicUnreachableCode

            VoicePlayerState state;
            if (_players.TryGet(player, out state))
            {
                state.InvokeOnStoppedSpeaking();

                if (OnPlayerStoppedSpeaking != null)
                    OnPlayerStoppedSpeaking(state);
            }
        }

        // ReSharper disable once InconsistentNaming
        private void Net_PlayerStartedSpeaking([NotNull] string player)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse (Justification: Sanity check against network system returning incorrect values)
            // ReSharper disable once HeuristicUnreachableCode
            if (Log.AssertAndLogError(player != null, "CA95E783-CA35-441B-9B8B-FAA0FA0B41E3", "Received a player-started-speaking event for a null player ID"))
                return;

            VoicePlayerState state;
            if (_players.TryGet(player, out state))
            {
                state.InvokeOnStartedSpeaking();

                if (OnPlayerStartedSpeaking != null)
                    OnPlayerStartedSpeaking(state);
            }
        }

        // ReSharper disable once InconsistentNaming
        private void Net_PlayerRoomEvent(RoomEvent evt)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse (Justification: Sanity check against network system returning incorrect values)
            // ReSharper disable once HeuristicUnreachableCode
            if (Log.AssertAndLogError(evt.PlayerName != null, "221D74AD-4F5B-4215-9ADE-CC4D5B455327", "Received a remote room event with a null player name"))
                return;

            VoicePlayerState state;
            if (_players.TryGet(evt.PlayerName, out state))
            {
                if (evt.Joined)
                {
                    state.InvokeOnEnteredRoom(evt);

                    if (OnPlayerEnteredRoom != null)
                        OnPlayerEnteredRoom(state, evt.Room);
                }
                else
                {
                    state.InvokeOnExitedRoom(evt);

                    if (OnPlayerExitedRoom != null)
                        OnPlayerExitedRoom(state, evt.Room);
                }
            }
        }

        // ReSharper disable once InconsistentNaming
        private void Net_VoicePacketReceived(VoicePacket packet)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse (Justification: Sanity check against network system returning incorrect values)
            // ReSharper disable once HeuristicUnreachableCode
            if (Log.AssertAndLogError(packet.SenderPlayerId != null, "C0FE4E98-3CC9-466E-AA39-51F0B6D22D09", "Discarding a voice packet with a null player ID"))
                return;

            VoicePlayerState state;
            if (_players.TryGet(packet.SenderPlayerId, out state) && state.PlaybackInternal != null)
                state.PlaybackInternal.ReceiveAudioPacket(packet);
        }

        // ReSharper disable once InconsistentNaming
        private void Net_PlayerLeft([NotNull] string playerId)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse (Justification: Sanity check against network system returning incorrect values)
            // ReSharper disable once HeuristicUnreachableCode
            if (Log.AssertAndLogError(playerId != null, "37A2506B-6489-4679-BD72-1C53D69797B1", "Received a player-left event for a null player ID"))
                return;

            var state = _players.Remove(playerId);
            if (state != null)
            {
                var playback = state.Playback;
                if (playback != null)
                    _playbackPool.Put((VoicePlayback)playback);

                _playerTrackers.RemovePlayer(state);

                state.InvokeOnLeftSession();
                if (OnPlayerLeftSession != null)
                    OnPlayerLeftSession(state);
            }
        }

        // ReSharper disable once InconsistentNaming
        private void Net_PlayerJoined([NotNull] string playerId, CodecSettings codecSettings)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse (Justification: Sanity check against network system returning incorrect values)
            // ReSharper disable once HeuristicUnreachableCode
            if (Log.AssertAndLogError(playerId != null, "86074592-4BAD-4DF5-9B2C-1DF42A68FAF8", "Received a player-joined event for a null player ID"))
                return;

            if (playerId == LocalPlayerName)
                return;

            Log.Debug("Net_PlayerJoined. playerId:{0} codecSettings:{1}", playerId, codecSettings);

            //Get a playback component for this player
            var playback = _playbackPool.Get(playerId);
            playback.CodecSettings = codecSettings;

            //Create the state object for this player
            var state = new RemoteVoicePlayerState(playback);
            _players.Add(state);

            //Associate state with the position tracker for this player (if there is one)
            _playerTrackers.AddPlayer(state);

            //Now we've set everything up activate the playback
            playback.gameObject.SetActive(true);

            if (OnPlayerJoinedSession != null)
                OnPlayerJoinedSession(state);
        }
        #endregion

        /// <summary>
        /// Find the player state for a given player ID (or null, if it cannot be found)
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        [CanBeNull] public VoicePlayerState FindPlayer([NotNull] string playerId)
        {
            if (playerId == null)
                throw new ArgumentNullException("playerId");

            VoicePlayerState state;
            if (_players.TryGet(playerId, out state))
                return state;

            return null;
        }

        [UsedImplicitly] private void Update()
        {
            Profiler.BeginSample("Write Multithreaded Logs", this);
            Logs.WriteMultithreadedLogs();
            Profiler.EndSample();

            Profiler.BeginSample("Write Multithreaded Metrics", this);
            Metrics.WriteMultithreadedMetrics();
            Profiler.EndSample();

            Profiler.BeginSample("Update: Playback Priority Manager", this);
            _playbackPriorityManager.Update();
            Profiler.EndSample();

            Profiler.BeginSample("Update: Players", this);
            _players.Update();
            Profiler.EndSample();

            Profiler.BeginSample("Update: Capture", this);
            _capture.Update(IsMuted, Time.unscaledDeltaTime);
            Profiler.EndSample();

            Profiler.BeginSample("Update: Auto Channel Duck", this);
            _autoChannelDuck.Update(IsMuted, Time.unscaledDeltaTime);
            Profiler.EndSample();
        }

        [UsedImplicitly] private void OnDestroy()
        {
            _capture.Destroy();
        }

        #region VAD
        /// <summary>
        ///     Subscribes to automatic voice detection.
        /// </summary>
        /// <param name="listener">
        ///     The listener which is to receive notification when the player starts and stops speaking via
        ///     automatic voice detection.
        /// </param>
        public void SubcribeToVoiceActivation([NotNull] IVoiceActivationListener listener)
        {
            _capture.Subscribe(listener);
        }

        /// <summary>
        ///     Unsubsribes from automatic voice detection.
        /// </summary>
        /// <param name="listener"></param>
        public void UnsubscribeFromVoiceActivation([NotNull] IVoiceActivationListener listener)
        {
            _capture.Unsubscribe(listener);
        }
        #endregion

        #region player tracking
        /// <summary>
        /// Enable position tracking for the player represented by the given object
        /// </summary>
        /// <param name="player"></param>
        public void TrackPlayerPosition([NotNull] IDissonancePlayer player)
        {
            _playerTrackers.AddTracker(player);
        }

        /// <summary>
        /// Stop position tracking for the player represented by the given object
        /// </summary>
        /// <param name="player"></param>
        public void StopTracking([NotNull] IDissonancePlayer player)
        {
            _playerTrackers.RemoveTracker(player);
        }
        #endregion

        #region tokens
        /// <summary>
        /// Event invoked whenever a new token is added to the local set
        /// </summary>
        public event Action<string> TokenAdded
        {
            add { _tokens.TokenAdded += value; }
            remove { _tokens.TokenAdded -= value; }
        }

        /// <summary>
        /// Event invoked whenever a new token is removed from the local set
        /// </summary>
        public event Action<string> TokenRemoved
        {
            add { _tokens.TokenRemoved += value; }
            remove { _tokens.TokenRemoved -= value; }
        }

        /// <summary>
        /// Add the given token to the local player
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool AddToken(string token)
        {
            if (token == null)
                throw new ArgumentNullException("token", "Cannot add a null token");

            return _tokens.AddToken(token);
        }

        /// <summary>
        /// Removed the given token from the local player
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool RemoveToken(string token)
        {
            if (token == null)
                throw new ArgumentNullException("token", "Cannot remove a null token");

            return _tokens.RemoveToken(token);
        }

        /// <summary>
        /// Test if the local player knows the given token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool ContainsToken(string token)
        {
            if (token == null)
                return false;

            return _tokens.ContainsToken(token);
        }

        /// <summary>
        /// Tests if the local player knows has knowledge of *any* of the tokens in the given set
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public bool HasAnyToken([NotNull] TokenSet tokens)
        {
            if (tokens == null)
                throw new ArgumentNullException("tokens", "Cannot intersect with a null set");

            return _tokens.IntersectsWith(tokens);
        }
        #endregion

        #region IPriorityManager explicit impl
        ChannelPriority IPriorityManager.TopPriority
        {
            get { return _playbackPriorityManager.TopPriority; }
        }
        #endregion

        #region  IChannelPriorityProvider explicit impl
        ChannelPriority IChannelPriorityProvider.DefaultChannelPriority
        {
            get { return _playerPriority; }
            set { _playerPriority = value; }
        }
        #endregion

        #region IVolumeProvider explicit impl
        float IVolumeProvider.TargetVolume
        {
            get
            {
                if (_isDeafened || MuteAllRemoteVoices)
                    return 0;
                return RemoteVoiceVolume * _autoChannelDuck.TargetVolume;
            }
        }
        #endregion

        /// <summary>
        /// Test that the Dissonance DLLs can be loaded. This will throw an DllNotFoundException if they are not correctly installed.
        /// </summary>
        public static void TestDependencies()
        {
            // Loading the version string of opus loads the DLL, will throw if it's not there.
            OpusNative.OpusVersion();

            // Getting the filter state loads the DLL (and has no side effects, it's just a getter). Will throw if DLL is missing.
            AudioPluginDissonanceNative.Dissonance_GetFilterState();
        }
    }
}
