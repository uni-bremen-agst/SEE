using System;
using System.Text.RegularExpressions;

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
        /// Matches an annotation: a text in square brackets, in parentheses, or
        /// between two asterisks, e.g., "[Musik]", "(Husten)", or "*klopft*".
        /// Whisper writes noises that way.
        /// </summary>
        private static readonly Regex annotation = new(@"\[[^\]]*\]|\([^)]*\)|\*[^*]*\*");

        /// <summary>
        /// Matches one or more white spaces in a row.
        /// </summary>
        private static readonly Regex whiteSpaces = new(@"\s+");

        /// <summary>
        /// Matches a space in front of a punctuation mark. Such a space is left
        /// behind when an annotation in front of the punctuation mark is removed.
        /// </summary>
        private static readonly Regex spaceBeforePunctuation = new(@" (?=[,.!?;:])");

        /// <summary>
        /// Returns <paramref name="text"/> without the annotations Whisper writes for
        /// noises, e.g., "Hallo [Husten] zusammen" becomes "Hallo zusammen". Repeated
        /// white spaces and those at the beginning and the end are removed, too.
        /// </summary>
        /// <param name="text">The text returned by Whisper for one utterance; may be null.</param>
        /// <returns>The cleaned text; empty if nothing but annotations was in <paramref name="text"/>.</returns>
        public static string RemoveAnnotations(string text)
        {
            if (text == null)
            {
                return "";
            }

            string result = annotation.Replace(text, " ");
            result = whiteSpaces.Replace(result, " ");
            result = spaceBeforePunctuation.Replace(result, "");
            return result.Trim();
        }

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
