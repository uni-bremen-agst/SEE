using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Dissonance
{
    /// <summary>
    /// We receive player trackers and player states in an unknown order.
    /// This acts as a buffer, holding player trackers which arrive first and linking them up to players as soon as the player is available
    /// </summary>
    internal class PlayerTrackerManager
    {
        #region fields
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(PlayerTrackerManager).Name);

        private readonly Dictionary<string, IDissonancePlayer> _unlinkedPlayerTrackers = new Dictionary<string, IDissonancePlayer>();

        private readonly PlayerCollection _players;
        #endregion

        public PlayerTrackerManager([NotNull] PlayerCollection players)
        {
            if (players == null)
                throw new ArgumentNullException("players");

            _players = players;
        }

        #region players
        public void AddPlayer([NotNull] VoicePlayerState state)
        {
            if (state == null)
                throw new ArgumentNullException("state", "Cannot start tracking a null player");

            //We've got a player and we *might* already have the tracker for it. Search for an unlinked tracker
            IDissonancePlayer tracker;
            if (_unlinkedPlayerTrackers.TryGetValue(state.Name, out tracker))
            {
                state.Tracker = tracker;
                _unlinkedPlayerTrackers.Remove(state.Name);

                Log.Debug("Linked an unlinked player tracker for player '{0}'", state.Name);
            }
        }

        public void RemovePlayer([NotNull] VoicePlayerState state)
        {
            if (state == null)
                throw new ArgumentNullException("state", "Cannot stop tracking a null player");

            //Save the tracker. The player could rejoin the session, in which case we'd need this tracker again
            var tracker = state.Tracker;
            if (tracker != null)
                _unlinkedPlayerTrackers.Add(tracker.PlayerId, tracker);

            state.Tracker = null;
        }
        #endregion

        #region trackers
        public void AddTracker([NotNull] IDissonancePlayer player)
        {
            if (player == null)
                throw new ArgumentNullException("player", "Cannot track a null player");

            //Associate tracker with player state
            VoicePlayerState state;
            if (_players.TryGet(player.PlayerId, out state))
            {
                state.Tracker = player;
                Log.Debug("Associated position tracking for '{0}'", player.PlayerId);
            }
            else
            {
                _unlinkedPlayerTrackers[player.PlayerId] = player;
                Log.Debug("Got a player tracker for player '{0}' but that player doesn't exist yet", player.PlayerId);
            }
        }

        public void RemoveTracker([NotNull] IDissonancePlayer player)
        {
            if (player == null)
                throw new ArgumentNullException("player", "Cannot stop tracking a null player");

            //Try to remove the player from the list of untracked players, just in case we haven't linked it up yet
            if (_unlinkedPlayerTrackers.Remove(player.PlayerId))
                Log.Debug("Removed unlinked state tracker for '{0}' (because RemoveTracker called)", player.PlayerId);
            else
            {
                //Disassociate the tracker from the player state
                VoicePlayerState state;
                if (_players.TryGet(player.PlayerId, out state))
                {
                    state.Tracker = null;
                    Log.Debug("Disassociated position tracking for '{0}' (because RemoveTracker called)", player.PlayerId);
                }
            }
        }
        #endregion
    }
}
