using System;
using Dissonance.Audio;
using Dissonance.VAD;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance
{
    /// <summary>
    ///     Opens and closes voice comm channels to a room or specific player in response to events
    ///     such as voice activation, push to talk, or local player proximity.
    /// </summary>
    // ReSharper disable once InheritdocConsiderUsage
    [HelpURL("https://placeholder-software.co.uk/dissonance/docs/Reference/Components/Voice-Broadcast-Trigger/")]
    public class VoiceBroadcastTrigger
        : BaseCommsTrigger, IVoiceActivationListener
    {
        #region field and properties
        [SerializeField] private bool _channelTypeExpanded;     // <
        [SerializeField] private bool _metadataExpanded;        // < These properties contain state used by the inspector. It needs
        [SerializeField] private bool _activationModeExpanded;  // < to be stored here because the inspector is sometimes recreated
        [SerializeField] private bool _tokensExpanded;          // < by the editor, discarding all state (for example, making things
        [SerializeField] private bool _ampExpanded;             // < inside foldouts inaccessible!

        private PlayerChannel? _playerChannel;
        private RoomChannel? _roomChannel;

        private bool _isVadSpeaking;
        private CommActivationMode? _previousMode;
        private IDissonancePlayer _self;

        private Fader _activationFader;
        // ReSharper disable once FieldCanBeMadeReadOnly.Local (Justification: Confuses unity serialization)
        [SerializeField] private VolumeFaderSettings _activationFaderSettings = new VolumeFaderSettings {
            Volume = 1,
            FadeIn = TimeSpan.Zero,
            FadeOut = TimeSpan.FromSeconds(0.15f)
        };
        /// <summary>
        /// Access volume fader settings which are applied every time the trigger activates with PTT/VAD
        /// </summary>
        [NotNull] public VolumeFaderSettings ActivationFader
        {
            get { return _activationFaderSettings; }
        }

        private Fader _triggerFader;
        // ReSharper disable once FieldCanBeMadeReadOnly.Local (Justification: Confuses unity serialization)
        [SerializeField] private VolumeFaderSettings _triggerFaderSettings = new VolumeFaderSettings {
            Volume = 1,
            FadeIn = TimeSpan.FromSeconds(0.75f),
            FadeOut = TimeSpan.FromSeconds(1.15f)
        };
        /// <summary>
        /// Access volume fader settings which are applied every time the collider trigger is entered/exited
        /// </summary>
        [NotNull] public VolumeFaderSettings ColliderTriggerFader
        {
            get { return _triggerFaderSettings; }
        }

        /// <summary>
        /// Get the current attenuation applied to this channel by all faders
        /// </summary>
        public float CurrentFaderVolume
        {
            get { return _activationFader.Volume * (UseColliderTrigger ? _triggerFader.Volume : 1); }
        }

        [SerializeField]private bool _broadcastPosition = true;
        /// <summary>
        /// Get or set if voice sent with this broadcast trigger should use positional playback
        /// </summary>
        public bool BroadcastPosition
        {
            get { return _broadcastPosition; }
            set
            {
                if (_broadcastPosition != value)
                {
                    _broadcastPosition = value;

                    if (_playerChannel.HasValue)
                    {
                        var channel = _playerChannel.Value;
                        channel.Positional = value;
                    }

                    if (_roomChannel.HasValue)
                    {
                        var channel = _roomChannel.Value;
                        channel.Positional = value;
                    }
                }
            }
        }

        [SerializeField]private CommTriggerTarget _channelType;
        /// <summary>
        /// Get or set the target type of voice sent with this trigger
        /// </summary>
        public CommTriggerTarget ChannelType
        {
            get { return _channelType; }
            set
            {
                if (_channelType != value)
                {
                    _channelType = value;

                    //Close the channel because it's type has been changed. Next update will automatically open the channel if necessary.
                    CloseChannel();
                }
            }
        }

        [SerializeField]private string _inputName;
        /// <summary>
        /// Get or set the input axis name (only applicable if this trigger is using Push-To-Talk)
        /// </summary>
        public string InputName
        {
            get { return _inputName; }
            set { _inputName = value; }
        }

        [SerializeField]private CommActivationMode _mode = CommActivationMode.VoiceActivation;
        /// <summary>
        /// Get or set how the player indicates speaking intent to this trigger
        /// </summary>
        public CommActivationMode Mode
        {
            get { return _mode; }
            set { _mode = value; }
        }

        [SerializeField]private bool _muted;
        /// <summary>
        /// Get or set if this voice broadcast trigger is muted
        /// </summary>
        public bool IsMuted
        {
            get { return _muted; }
            set
            {
                string target;
                switch (_channelType)
                {
                    case CommTriggerTarget.Room:
                        target = RoomName;
                        break;
                    case CommTriggerTarget.Player:
                        target = PlayerId;
                        break;
                    case CommTriggerTarget.Self:
                        target = "Self";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Log.Debug("Mute Broadcast Trigger '{0}' = {1}", target, value);
                _muted = value;
            }
        }

        [SerializeField]private string _playerId;
        /// <summary>
        /// Get or set the target player ID of this trigger (only applicable if the channel type is 'player')
        /// </summary>
        public string PlayerId
        {
            get { return _playerId; }
            set
            {
                if (_playerId != value)
                {
                    _playerId = value;

                    //Since the player ID has changed we need to close the channel. Next update will open it if necessary
                    if (_channelType == CommTriggerTarget.Player)
                        CloseChannel();
                }
            }
        }

        [SerializeField]private bool _useTrigger;
        /// <summary>
        /// Get or set if this broadcast trigger should use a unity trigger volume
        /// </summary>
        public override bool UseColliderTrigger
        {
            get { return _useTrigger; }
            set { _useTrigger = value; }
        }

        [SerializeField]private string _roomName;
        /// <summary>
        /// Get or set the target room of this trigger (only applicable if the channel type is 'room')
        /// </summary>
        public string RoomName
        {
            get { return _roomName; }
            set
            {
                if (_roomName != value)
                {
                    _roomName = value;

                    //Since the room has changed we need to close the channel. Next update will open it if necessary
                    if (_channelType == CommTriggerTarget.Room)
                        CloseChannel();
                }
            }
        }

        [SerializeField]private ChannelPriority _priority = ChannelPriority.None;
        /// <summary>
        /// Get or set the priority of voice sent with this trigger
        /// </summary>
        public ChannelPriority Priority
        {
            get { return _priority; }
            set
            {
                if (_priority != value)
                {
                    _priority = value;

                    if (_playerChannel.HasValue)
                    {
                        var channel = _playerChannel.Value;
                        channel.Priority = value;
                    }

                    if (_roomChannel.HasValue)
                    {
                        var channel = _roomChannel.Value;
                        channel.Priority = value;
                    }
                }
            }
        }

        /// <summary>
        /// Get if this voice broadcast trigger is currently transmitting voice
        /// </summary>
        public bool IsTransmitting
        {
            get { return _playerChannel != null || _roomChannel != null; }
        }

        public override bool CanTrigger
        {
            get
            {
                // - Cannot broadcast if Dissonance is not ready
                if (Comms == null || !Comms.IsStarted)
                    return false;

                // - Cannot broadcast to self if self is null
                if (_channelType == CommTriggerTarget.Self && _self == null)
                    return false;

                // - Cannot broadcast to yourself (by sibling component)!
                if (_channelType == CommTriggerTarget.Self && _self != null && _self.Type == NetworkPlayerType.Local)
                    return false;

                // - Cannot broadcast to yourself (by name)
                if (_channelType == CommTriggerTarget.Player && Comms.LocalPlayerName == _playerId)
                    return false;

                return true;
            }
        }
        #endregion

        protected override void Start()
        {
            base.Start();

            _self = GetComponent<IDissonancePlayer>() ?? GetComponentInParent<IDissonancePlayer>();
        }

        protected override void OnDisable()
        {
            CloseChannel();

            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            CloseChannel();

            if (Comms != null)
                Comms.UnsubscribeFromVoiceActivation(this);

            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();

            //Early exit sanity check (we can't do anything useful if there's no voice comms object)
            if (!CheckVoiceComm())
                return;

            //Reconfigure the trigger to (not) use VAD as necessary
            if (_previousMode != Mode)
                SwitchMode();

            //Update volume fader and apply to open channel
            _triggerFader.Update(Time.unscaledDeltaTime);
            _activationFader.Update(Time.unscaledDeltaTime);
            SetChannelVolume(CurrentFaderVolume);

            //Decide if we need to change state
            var intent = IsUserActivated();
            var next = ShouldActivate(intent);

            // Change the activation fader based on user intent
            if (intent)
            {
                // If we're speaking and the activation fader is not going to the max volume yet, start fading in
                if (Math.Abs(_activationFader.EndVolume - _activationFaderSettings.Volume) > float.Epsilon)
                    _activationFader.FadeTo(_activationFaderSettings.Volume, (float)_activationFaderSettings.FadeIn.TotalSeconds);
            }
            else
            {
                // Begin fade out (if it's not already fading to zero)
                if (Math.Abs(_activationFader.EndVolume) > float.Epsilon)
                    _activationFader.FadeTo(0, (float)_activationFaderSettings.FadeOut.TotalSeconds);
            }

            // Apply state if changed
            if (IsTransmitting != next)
            {
                // Check if we need to start or stop transmitting
                if (!next)
                {
                    // We need to stop transmitting, but is that because the intent has changed or something else?
                    // If the intent is active (user wants to talk) and the collider is triggered then something else
                    // is blocking speech, slam this connection closed immediately.
                    if (intent && (IsColliderTriggered || !UseColliderTrigger))
                    {
                        CloseChannel();
                    }
                    else
                    {
                        // Stop transmitting once fade out is complete
                        if (CurrentFaderVolume <= float.Epsilon)
                            CloseChannel();
                    }
                }
                else
                {
                    // Start transmitting
                    OpenChannel();
                }
            }
        }

        protected override void ColliderTriggerChanged()
        {
            base.ColliderTriggerChanged();

            //Collision state has changed, begin a fade in or out with the collider fader;
            if (IsColliderTriggered)
                _triggerFader.FadeTo(_triggerFaderSettings.Volume, (float)_triggerFaderSettings.FadeIn.TotalSeconds);
            else
                _triggerFader.FadeTo(0, (float)_triggerFaderSettings.FadeOut.TotalSeconds);
        }

        private void SwitchMode()
        {
            if (!CheckVoiceComm())
                return;

            CloseChannel();

            if (_previousMode == CommActivationMode.VoiceActivation && Mode != CommActivationMode.VoiceActivation)
            {
                Comms.UnsubscribeFromVoiceActivation(this);
                _isVadSpeaking = false;
            }

            if (Mode == CommActivationMode.VoiceActivation)
                Comms.SubcribeToVoiceActivation(this);

            _previousMode = Mode;
        }

        private bool ShouldActivate(bool intent)
        {
            //Early exit if the user isn't trying to broadcast
            if (!intent)
                return false;

            //Cannot activate if specifically muted
            if (_muted)
                return false;

            //Check some situations where activating is impossible...
            if (!CanTrigger)
            {
                if (_channelType == CommTriggerTarget.Self && _self == null)
                    Log.Error("Attempting to broadcast to 'Self' but no sibling IDissonancePlayer component found");
                return false;
            }

            //Cannot broadcast to null or empty room name
            if (_channelType == CommTriggerTarget.Room && string.IsNullOrEmpty(RoomName))
                return false;

            //Only activate if tokens are satisfied
            if (!TokenActivationState)
                return false;

            //Only activate if collider trigger is triggered (or collider trigger isn't in use)
            if (UseColliderTrigger && !IsColliderTriggered)
                return false;

            //No reasons not to broadcast
            return true;
        }

        /// <summary>
        /// Invert the `IsMuted` property. This is convenient for integration with UI elements.
        /// e.g. a UI button can directly call this while in `Voice Activated` mode to enable/disable voice on click.
        /// </summary>
        public void ToggleMute()
        {
            IsMuted = !IsMuted;
        }

        /// <summary>
        /// Get a value indicating if the user wants to speak
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsUserActivated()
        {
            switch (Mode)
            {
                case CommActivationMode.VoiceActivation:
                    return _isVadSpeaking;

                case CommActivationMode.PushToTalk:
                    return Input.GetAxis(InputName) > 0.5f;

                case CommActivationMode.Open:
                    return true;

                case CommActivationMode.None:
                    return false;

                default:
                    Log.Error("Unknown Activation Mode '{0}'", Mode);
                    return false;
            }
        }

        #region channel management
        private void SetChannelVolume(float value)
        {
            if (_playerChannel.HasValue)
            {
                var c = _playerChannel.Value;
                c.Volume = value;
            }

            if (_roomChannel.HasValue)
            {
                var c = _roomChannel.Value;
                c.Volume = value;
            }
        }

        private void OpenChannel()
        {
            if (!CheckVoiceComm())
                return;

            if (ChannelType == CommTriggerTarget.Room)
            {
                _roomChannel = Comms.RoomChannels.Open(RoomName, _broadcastPosition, _priority, CurrentFaderVolume);
            }
            else if (ChannelType == CommTriggerTarget.Player)
            {
                if (PlayerId != null)
                    _playerChannel = Comms.PlayerChannels.Open(PlayerId, _broadcastPosition, _priority, CurrentFaderVolume);
                else
                    Log.Warn("Attempting to transmit to a null player ID");
            }
            else if (ChannelType == CommTriggerTarget.Self)
            {
                //Don't warn if self does not have an ID yet - it could just be initialising
                if (_self == null)
                    Log.Warn("Attempting to transmit to a null player object");
                else if (_self.PlayerId != null)
                    _playerChannel = Comms.PlayerChannels.Open(_self.PlayerId, _broadcastPosition, _priority);
            }
        }

        private void CloseChannel()
        {
            if (_roomChannel != null)
            {
                _roomChannel.Value.Dispose();
                _roomChannel = null;
            }

            if (_playerChannel != null)
            {
                _playerChannel.Value.Dispose();
                _playerChannel = null;
            }
        }
        #endregion

        #region IVoiceActivationListener impl
        void IVoiceActivationListener.VoiceActivationStart()
        {
            _isVadSpeaking = true;
        }

        void IVoiceActivationListener.VoiceActivationStop()
        {
            _isVadSpeaking = false;
        }
        #endregion
    }
}
