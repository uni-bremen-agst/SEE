using SEE.UI;
using SEE.Utils.Config;
using System.Collections.Generic;

namespace SEE.User
{
    /// <summary>
    /// Represents the audio settings for the SEE application. These are attributes
    /// that are generally set by the user at the <see cref="SettingsMenu"/> of the application.
    /// </summary>
    internal class Audio
    {
        /// <summary>
        /// Current music volume.
        /// </summary>
        public float MusicVolume = 1f;

        /// <summary>
        /// Indicates whether music is muted.
        /// </summary>
        public bool MusicMuted = false;

        /// <summary>
        /// Current sound effects volume.
        /// </summary>
        public float SoundEffectsVolume = 1f;

        /// <summary>
        /// Indicates whether sound effects are muted.
        /// </summary>
        public bool SoundEffectsMuted = false;

        /// <summary>
        /// Stores whether remote sound effects are muted or not.
        /// </summary>
        public bool RemoteSoundEffectsMuted = false;

        #region Configuration I/O
        /// <summary>
        /// Label of attribute <see cref="MusicVolume"/> in the configuration file.
        /// </summary>
        private const string musicVolumeLabel = "musicVolume";

        /// <summary>
        /// Label of attribute <see cref="SoundEffectsVolume"/> in the configuration file.
        /// </summary>
        private const string soundEffectsVolumeLabel = "soundEffectVolume";

        /// <summary>
        /// Label of attribute <see cref="MusicMuted"/> in the configuration file.
        /// </summary>
        private const string musicMutedLabel = "musicMuted";

        /// <summary>
        /// Label of attribute <see cref="SoundEffectsMuted"/> in the configuration file.
        /// </summary>
        private const string soundEffectsMutedLabel = "soundEffectsMuted";

        /// <summary>
        /// Label of attribute <see cref="RemoteSoundEffectsMuted"/> in the configuration file.
        /// </summary>
        private const string remoteSoundEffectsMutedLabel = "remoteSoundEffectsMuted";

        /// <summary>
        /// Saves the settings of this <see cref="Video"/> using <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The writer to be used to save the settings.</param>
        /// <param name="label">The label under which to group the settings.</param>
        public virtual void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(MusicVolume, musicVolumeLabel);
            writer.Save(SoundEffectsVolume, soundEffectsVolumeLabel);
            writer.Save(MusicMuted, musicMutedLabel);
            writer.Save(SoundEffectsMuted, soundEffectsMutedLabel);
            writer.Save(RemoteSoundEffectsMuted, remoteSoundEffectsMutedLabel);
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">The attributes from which to restore the settings.</param>
        /// <param name="label">The label under which to look up the settings in <paramref name="attributes"/>.</param>
        public virtual void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.Restore(values, musicVolumeLabel, ref MusicVolume);
                ConfigIO.Restore(values, soundEffectsVolumeLabel, ref SoundEffectsVolume);
                ConfigIO.Restore(values, musicMutedLabel, ref MusicMuted);
                ConfigIO.Restore(values, soundEffectsMutedLabel, ref SoundEffectsMuted);
                ConfigIO.Restore(values, remoteSoundEffectsMutedLabel, ref RemoteSoundEffectsMuted);
            }
        }
        #endregion
    }
}
