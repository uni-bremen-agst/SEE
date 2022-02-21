namespace Dissonance.Audio.Playback
{
    public interface IPriorityManager
    {
        /// <summary>
        /// Get the highest priority of all current speakers
        /// </summary>
        ChannelPriority TopPriority { get; }
    }

    internal class PriorityManager
        : IPriorityManager
    {
        private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof(PriorityManager).Name);

        private readonly PlayerCollection _players;

        public ChannelPriority TopPriority { get; private set; }

        public PriorityManager(PlayerCollection players)
        {
            _players = players;

            TopPriority = ChannelPriority.None;
        }

        /// <summary>
        /// Determine what the top priority speaker currently is and publish this priority
        /// </summary>
        public void Update()
        {
            var topPriority = ChannelPriority.None;
            string topSpeaker = null;

            //Run through all the current players and find which currently speaking player has the highest priority
            var players = _players.Readonly;
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var priority = player.SpeakerPriority;

                if (priority.HasValue && priority > topPriority)
                {
                    topPriority = priority.Value;
                    topSpeaker = player.Name;
                }
            }

            if (TopPriority != topPriority)
            {
                TopPriority = topPriority;
                Log.Trace("Highest speaker priority is: {0} ({1})", topPriority, topSpeaker);
            }
        }
    }
}
