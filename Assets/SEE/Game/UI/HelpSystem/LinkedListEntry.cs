namespace SEE.Game.UI.HelpSystem
{
    /// <summary>
    /// An entry for the linkedList of an HelpSystemEntry.
    /// </summary>
    public class LinkedListEntry
    {
        /// <summary>
        /// The text of this entry which will be spoken by SEE and displayed in the entry.
        /// </summary>
        private readonly string text;

        /// <summary>
        /// The cumulatedTome of this entry and the previous entries.
        /// </summary>
        private readonly int cumulatedTime;

        /// <summary>
        /// The position of the linkedListEntry in the linkedList.
        /// </summary>
        private readonly int index;

        /// <summary>
        /// Creates a new LinkedListEntry.
        /// </summary>
        /// <param name="index">the index of the entry.</param>
        /// <param name="text">the text of the entry.</param>
        /// <param name="cumulatedTime">the cumulated time of this entry and his previous.</param>
        public LinkedListEntry(int index, string text, int cumulatedTime)
        {
            this.text = text;
            this.cumulatedTime = cumulatedTime;
            this.index = index;
        }

        public int CumulatedTime { get => cumulatedTime; }
        public string Text { get => text; }
        public int Index { get => index; }
    }
}
