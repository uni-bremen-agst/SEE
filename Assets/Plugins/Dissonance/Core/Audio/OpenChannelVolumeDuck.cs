using System;
using Dissonance.Audio.Playback;
using Dissonance.Config;
using UnityEngine;

namespace Dissonance.Audio
{
    /// <summary>
    /// Automatically ducks the volume when a channel is open
    /// </summary>
    public class OpenChannelVolumeDuck
        : IVolumeProvider
    {
        #region fields and properties
        public const float FadeDurationSecondsDown = 0.3f;
        public const float FadeDurationSecondsUp = 0.5f;

        private readonly RoomChannels _rooms;
        private readonly PlayerChannels _players;

        private Fader _fader;
        public float TargetVolume
        {
            get { return _fader.Volume; }
        }
        #endregion

        public OpenChannelVolumeDuck(RoomChannels rooms, PlayerChannels players)
        {
            _rooms = rooms;
            _players = players;

            _fader.FadeTo(1, 0);
        }

        public void Update(bool isMuted, float dt)
        {
            var talking = !isMuted && (_rooms.Count > 0 || _players.Count > 0);

            // Choose target volume based on if there are any open channels
            var tgt = talking ? VoiceSettings.Instance.VoiceDuckLevel : 1;

            // If target volume is not fader target change the fader target. 
            if (Math.Abs(_fader.EndVolume - tgt) > float.Epsilon)
            {
                // Use different fade constants depending upon if there are any open channels
                _fader.FadeTo(tgt, talking ? FadeDurationSecondsDown : FadeDurationSecondsUp);
            }

            // Update fader current volume
            _fader.Update(dt);
        }
    }
}
