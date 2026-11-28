using System;
using SEE.UserSettings;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Extensions for <see cref="TranscriptionLanguage"/>.
    /// </summary>
    public static class TranscriptionLanguageExtensions
    {
        /// <summary>
        /// Returns the language code expected by Whisper for the given <paramref name="language"/>.
        /// </summary>
        /// <param name="language">The language to be converted.</param>
        /// <returns>"de", "en", or "auto".</returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="language"/> is unknown.</exception>
        public static string ToWhisperCode(this TranscriptionLanguage language)
        {
            switch (language)
            {
                case TranscriptionLanguage.German:
                    return "de";
                case TranscriptionLanguage.English:
                    return "en";
                case TranscriptionLanguage.Auto:
                    return "auto";
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), language, "Unhandled transcription language.");
            }
        }
    }
}
