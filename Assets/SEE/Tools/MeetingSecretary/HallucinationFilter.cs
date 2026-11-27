using System;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Recognizes texts that Whisper typically invents when it is given silence
    /// or noise instead of speech. They stem from the subtitles of the videos
    /// Whisper was trained on.
    /// </summary>
    public static class HallucinationFilter
    {
        /// <summary>
        /// Texts that are dropped if they form the whole result. They must be in
        /// the form returned by <see cref="Normalize(string)"/>: lower case, without
        /// punctuation at the beginning and the end.
        /// </summary>
        /// <remarks>"thank you" and "vielen dank" may also be said for real. They are
        /// dropped nevertheless, because Whisper is known to produce them for silence;
        /// they are still kept when they are part of a longer utterance.</remarks>
        private static readonly string[] wholeTexts =
        {
            "thank you",
            "thanks for watching",
            "vielen dank",
            "music",
            "musik",
            "blank_audio",
        };

        /// <summary>
        /// A result starting with one of these texts is dropped. Same form as <see cref="wholeTexts"/>.
        /// </summary>
        private static readonly string[] textBeginnings =
        {
            "untertitel im auftrag des zdf",
            "untertitel von",
            "untertitel der amara.org",
            "untertitelung des zdf",
        };

        /// <summary>
        /// True if <paramref name="text"/> should not be shown: it is null, empty,
        /// consists of punctuation only, or is a known invention of Whisper.
        /// </summary>
        /// <param name="text">The text returned by Whisper for one utterance.</param>
        /// <returns>True if <paramref name="text"/> must be dropped.</returns>
        public static bool IsHallucination(string text)
        {
            string normalized = Normalize(text);
            if (normalized.Length == 0)
            {
                return true;
            }
            foreach (string wholeText in wholeTexts)
            {
                if (normalized == wholeText)
                {
                    return true;
                }
            }
            foreach (string beginning in textBeginnings)
            {
                if (normalized.StartsWith(beginning, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns <paramref name="text"/> in lower case and without the characters at its
        /// beginning and end that are neither letters nor digits, e.g., "[Music]" becomes "music".
        /// </summary>
        /// <param name="text">The text to be normalized; may be null.</param>
        /// <returns>The normalized text; empty if <paramref name="text"/> has no letter or digit.</returns>
        public static string Normalize(string text)
        {
            if (text == null)
            {
                return "";
            }

            int first = 0;
            while (first < text.Length && !char.IsLetterOrDigit(text[first]))
            {
                first++;
            }
            int last = text.Length - 1;
            while (last >= first && !char.IsLetterOrDigit(text[last]))
            {
                last--;
            }
            return text.Substring(first, last - first + 1).ToLowerInvariant();
        }
    }
}
