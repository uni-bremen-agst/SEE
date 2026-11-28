using System;
using System.Collections.Generic;
using SEE.Dissonance;
using UnityEngine;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// The transcript of the voice chat as far as this client has received it:
    /// the entries of the local player and those sent by the other players.
    /// </summary>
    /// <remarks>The transcript is kept in memory only. It is neither written to disk
    /// nor to the log. All members must be called on Unity's main thread.</remarks>
    public static class TranscriptLog
    {
        /// <summary>
        /// The color in which speech is shown in the chat window.
        /// </summary>
        private static readonly Color speechColor = new(0.19f, 0.19f, 0.19f);

        /// <summary>
        /// The color in which the notices that a player started or stopped
        /// the transcription are shown in the chat window.
        /// </summary>
        private static readonly Color noticeColor = new(0.6f, 0.3f, 0f);

        /// <summary>
        /// The entries in the order in which they were added.
        /// </summary>
        private static readonly List<TranscriptEntry> entries = new();

        /// <summary>
        /// The chat window in which the entries are shown; null if not yet looked up
        /// or if there is none.
        /// </summary>
        private static ChatLogController chatLog;

        /// <summary>
        /// True if the user was already warned that there is no chat window.
        /// </summary>
        private static bool warnedAboutMissingChatLog;

        /// <summary>
        /// All entries in the order in which they were added.
        /// </summary>
        public static IReadOnlyList<TranscriptEntry> Entries => entries;

        /// <summary>
        /// Called whenever an entry was added, with that entry.
        /// </summary>
        public static event Action<TranscriptEntry> OnEntryAdded;

        /// <summary>
        /// Appends <paramref name="entry"/> to the transcript and shows it to the local player.
        /// </summary>
        /// <param name="entry">The entry to be added.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="entry"/> is null.</exception>
        public static void Add(TranscriptEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }
            entries.Add(entry);
            Show(entry);
            OnEntryAdded?.Invoke(entry);
        }

        /// <summary>
        /// Removes all entries.
        /// </summary>
        public static void Clear()
        {
            entries.Clear();
        }

        /// <summary>
        /// Shows <paramref name="entry"/> in the chat window, if there is one.
        /// </summary>
        /// <param name="entry">The entry to be shown.</param>
        private static void Show(TranscriptEntry entry)
        {
            // Outside of play mode (e.g., in unit tests), a chat window found in
            // the open scene is not initialized and cannot show anything.
            if (!Application.isPlaying)
            {
                return;
            }

            if (chatLog == null)
            {
                chatLog = UnityEngine.Object.FindAnyObjectByType<ChatLogController>();
            }
            if (chatLog == null || !chatLog.isActiveAndEnabled)
            {
                if (!warnedAboutMissingChatLog)
                {
                    warnedAboutMissingChatLog = true;
                    Debug.LogWarning($"There is no active {nameof(ChatLogController)} in the scene. "
                                     + "The transcript is recorded but cannot be shown.\n");
                }
                return;
            }

            Color color;
            if (entry.Kind == TranscriptEntryKind.Speech)
            {
                color = speechColor;
            }
            else
            {
                color = noticeColor;
            }
            chatLog.AddMessage(entry.ToDisplayText(), color);
        }
    }
}
