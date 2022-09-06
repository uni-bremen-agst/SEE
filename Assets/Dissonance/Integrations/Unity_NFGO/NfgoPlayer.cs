using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;

namespace Dissonance.Integrations.Unity_NFGO
{
    [RequireComponent(typeof(NetworkObject))]
    public class NfgoPlayer
        : NetworkBehaviour, IDissonancePlayer
    {
        private static readonly Log Log = Logs.Create(LogCategory.Network, nameof(NfgoPlayer));

        private DissonanceComms _comms;

        private Transform _transform;
        [NotNull] private Transform Transform
        {
            get
            {
                if (_transform == null)
                    _transform = transform;
                return _transform;
            }
        }

        public Vector3 Position => Transform.position;
        public Quaternion Rotation => Transform.rotation;

        public bool IsTracking { get; private set; }

        private string _playerIdString;
        private readonly NetworkVariable<FixedString128Bytes> _playerId = new NetworkVariable<FixedString128Bytes>(new FixedString128Bytes(""));

        public string PlayerId
        {
            get
            {
                // Cache the player ID converted into a string, to prevent this being repeated and generating lots of garbage
                if (_playerIdString == null || !_playerId.Value.Equals(_playerIdString))
                    _playerIdString = _playerId.Value.ToString();
                return _playerIdString;
            }
        }

        public NetworkPlayerType Type
        {
            get
            {
                if (_comms == null || _playerId.Value.IsEmpty)
                    return NetworkPlayerType.Unknown;
                return _playerId.Value.Equals(_comms.LocalPlayerName) ? NetworkPlayerType.Local : NetworkPlayerType.Remote;
            }
        }

        public override void OnDestroy()
        {
            if (_comms != null)
                _comms.LocalPlayerNameChanged -= OnLocalPlayerIdChanged;
            _playerId.OnValueChanged -= OnNetworkVariablePlayerIdChanged;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _comms = FindObjectOfType<DissonanceComms>();
            if (_comms == null)
            {
                throw Log.CreateUserErrorException(
                    "cannot find DissonanceComms component in scene",
                    "not placing a DissonanceComms component on a game object in the scene",
                    "https://placeholder-software.co.uk/dissonance/docs/Basics/Quick-Start-UNet-HLAPI.html",
                    "A6A291D8-5B53-417E-95CD-EC670637C532"
                );
            }

            _playerId.OnValueChanged += OnNetworkVariablePlayerIdChanged;

            if (IsLocalPlayer)
            {
                Log.Debug("Tracking `NetworkStart` for local player. Name={0}", _comms.LocalPlayerName);

                // If Dissonance has been started set the local player name to the correct value
                if (_comms.LocalPlayerName != null)
                    SetNameServerRpc(_comms.LocalPlayerName);

                // It's possible the name will change in the future (if Dissonance is restarted)
                _comms.LocalPlayerNameChanged += OnLocalPlayerIdChanged;
            }
            else
            {
                if (!_playerId.Value.IsEmpty)
                    StartTracking();
            }
        }

        [ServerRpc]
        public void SetNameServerRpc(string playerName)
        {
            _playerId.Value = playerName;
        }

        private void OnLocalPlayerIdChanged(string _)
        {
            // Stop tracking in Dissonance
            if (IsTracking)
                StopTracking();

            //Inform the server the name has changed
            if (IsLocalPlayer)
                SetNameServerRpc(_comms.LocalPlayerName);

            // Restart tracking
            StartTracking();
        }

        private void OnNetworkVariablePlayerIdChanged<T>(T previousvalue, T newvalue)
        {
            if (IsTracking)
                StopTracking();
            StartTracking();
        }

        private void StartTracking()
        {
            if (IsTracking)
                throw Log.CreatePossibleBugException("Attempting to start player tracking, but tracking is already started", "4C2E74AA-CA09-4F98-B820-F2518A4E87D2");

            if (_comms != null)
            {
                _comms.TrackPlayerPosition(this);
                IsTracking = true;
            }
        }

        private void StopTracking()
        {
            if (!IsTracking)
                throw Log.CreatePossibleBugException("Attempting to stop player tracking, but tracking is not started", "BF8542EB-C13E-46FA-A8A0-B162F188BBA3");

            if (_comms != null)
            {
                _comms.StopTracking(this);
                IsTracking = false;
            }
        }
    }
}
