using System;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// The kinds of entries of the transcript.
    /// </summary>
    public enum TranscriptEntryKind
    {
        Speech = 0,  // something a player said
        Started = 1, // a player turned the transcription on
        Stopped = 2, // a player turned the transcription off
    }

    /// <summary>
    /// One entry of the transcript of the voice chat. It cannot be changed
    /// after it was created.
    /// </summary>
    public class TranscriptEntry
    {
        /// <summary>
        /// The name of the player who spoke or who turned the transcription on or off.
        /// </summary>
        public string Speaker { get; }

        /// <summary>
        /// What the player said; empty unless <see cref="Kind"/> is <see cref="TranscriptEntryKind.Speech"/>.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// When the entry was created on the computer of the speaker (UTC).
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// The kind of this entry.
        /// </summary>
        public TranscriptEntryKind Kind { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="speaker">The name of the player; null is treated as empty.</param>
        /// <param name="text">What the player said; null is treated as empty.</param>
        /// <param name="time">When the entry was created (UTC).</param>
        /// <param name="kind">The kind of the entry.</param>
        public TranscriptEntry(string speaker, string text, DateTime time, TranscriptEntryKind kind)
        {
            Speaker = speaker ?? "";
            Text = text ?? "";
            Time = time;
            Kind = kind;
        }

        /// <summary>
        /// Returns this entry as one line of text to be shown to the players.
        /// </summary>
        /// <returns>For instance, "Steve (voice): Hello." or "Steve started the transcription".</returns>
        public string ToDisplayText()
        {
            switch (Kind)
            {
                case TranscriptEntryKind.Started:
                    return $"{Speaker} started the transcription";
                case TranscriptEntryKind.Stopped:
                    return $"{Speaker} stopped the transcription";
                default:
                    return $"{Speaker} (voice): {Text}";
            }
        }
    }
}
