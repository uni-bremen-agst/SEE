using System.Collections.Generic;
using SEE.Graphs.Config;

namespace SEE.UserSettings
{
    /// <summary>
    /// The languages in which the voice chat can be transcribed.
    /// </summary>
    public enum TranscriptionLanguage
    {
        German,  // spoken language is German
        English, // spoken language is English
        Auto,    // spoken language is detected automatically
    }

    /// <summary>
    /// Configuration for the transcription of the voice chat.
    /// </summary>
    /// <remarks>Whether the transcription is turned on is intentionally not part
    /// of these settings. It is off at every start so that the user must consent
    /// to it in each session.</remarks>
    public class Transcription
    {
        /// <summary>
        /// The language spoken by the local player.
        /// </summary>
        public TranscriptionLanguage Language = TranscriptionLanguage.German;

        /// <summary>
        /// The file name of the speech-to-text model to be used.
        /// </summary>
        public string ModelFileName = "ggml-small-q5_1.bin";

        /// <summary>
        /// Label of <see cref="Language"/> in the configuration file.
        /// </summary>
        private const string languageLabel = "language";

        /// <summary>
        /// Label of <see cref="ModelFileName"/> in the configuration file.
        /// </summary>
        private const string modelFileNameLabel = "modelFileName";

        /// <summary>
        /// Saves the settings of this <see cref="Transcription"/> using <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The writer to be used to save the settings.</param>
        /// <param name="label">The label under which to group the settings.</param>
        public virtual void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(Language.ToString(), languageLabel);
            writer.Save(ModelFileName, modelFileNameLabel);
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

                ConfigIO.RestoreEnum(values, languageLabel, ref Language);
                ConfigIO.Restore(values, modelFileNameLabel, ref ModelFileName);
            }
        }
    }
}
