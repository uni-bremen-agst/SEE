using System;
using SEE.Tools.MeetingSecretary;
using Unity.Netcode;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Sends one entry of the transcript of the voice chat (something the local
    /// player said, or the notice that the local player started or stopped the
    /// transcription) to all other players.
    /// </summary>
    public class TranscriptNetAction : AbstractNetAction
    {
        /// <summary>
        /// The name of the player who spoke.
        /// </summary>
        public string Speaker;

        /// <summary>
        /// What the player said; empty for the notices.
        /// </summary>
        public string Text;

        /// <summary>
        /// The kind of the entry.
        /// </summary>
        public TranscriptEntryKind Kind;

        /// <summary>
        /// When the entry was created on the computer of the speaker, as the ticks
        /// of a <see cref="DateTime"/> in UTC. A <see cref="DateTime"/> itself
        /// cannot be serialized.
        /// </summary>
        public long TimeUtcTicks;

        /// <summary>
        /// The transcript is not replayed to players who join later: they
        /// were not present when it was said. As a consequence, the server
        /// does not keep the entries either.
        /// </summary>
        public override bool ShouldBeSentToNewClient { get => false; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="entry">The entry to be sent.</param>
        public TranscriptNetAction(TranscriptEntry entry)
        {
            Speaker = entry.Speaker;
            Text = entry.Text;
            Kind = entry.Kind;
            TimeUtcTicks = entry.Time.Ticks;
        }

        /// <summary>
        /// Returns the transcript entry carried by this action.
        /// </summary>
        /// <returns>The entry made of the fields of this action.</returns>
        public TranscriptEntry ToEntry()
        {
            // The value comes from the network; a wrong value must not raise an exception.
            bool validTicks = TimeUtcTicks >= DateTime.MinValue.Ticks && TimeUtcTicks <= DateTime.MaxValue.Ticks;
            DateTime time = validTicks ? new DateTime(TimeUtcTicks, DateTimeKind.Utc) : DateTime.UtcNow;
            return new TranscriptEntry(Speaker, Text, time, Kind);
        }

        /// <summary>
        /// Adds the entry to the local transcript. This is run by all clients
        /// except for the sender, who has added the entry itself already.
        /// </summary>
        public override void ExecuteOnClient()
        {
            TranscriptLog.Add(ToEntry());
        }

        /// <summary>
        /// Adds the entry to the local transcript if this server is a host, that
        /// is, if a player is taking part on it.
        /// </summary>
        /// <remarks>A host never runs <see cref="ExecuteOnClient"/> (see
        /// <see cref="ActionNetwork"/>), so without this the player on the host
        /// would not see what the others say. A server without a player keeps nothing.</remarks>
        public override void ExecuteOnServer()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsHost && Requester != networkManager.LocalClientId)
            {
                TranscriptLog.Add(ToEntry());
            }
        }
    }
}
