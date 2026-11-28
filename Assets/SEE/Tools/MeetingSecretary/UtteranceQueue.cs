using System.Collections.Generic;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// The utterances waiting to be transcribed. If the computer transcribes more
    /// slowly than the player speaks, the oldest waiting utterances are dropped,
    /// so that the transcript does not fall further and further behind.
    /// </summary>
    /// <typeparam name="T">The type of a waiting utterance.</typeparam>
    /// <remarks>Not thread safe; to be used on Unity's main thread only.</remarks>
    public class UtteranceQueue<T>
    {
        /// <summary>
        /// The maximal number of waiting utterances.
        /// </summary>
        public const int MaxWaiting = 3;

        /// <summary>
        /// The waiting utterances, oldest first.
        /// </summary>
        private readonly Queue<T> waiting = new();

        /// <summary>
        /// The number of waiting utterances.
        /// </summary>
        public int Count => waiting.Count;

        /// <summary>
        /// The total number of utterances that were dropped because too many were waiting.
        /// </summary>
        public int Dropped { get; private set; }

        /// <summary>
        /// Adds <paramref name="utterance"/>. If more than <see cref="MaxWaiting"/>
        /// utterances are waiting then, the oldest one is dropped.
        /// </summary>
        /// <param name="utterance">The utterance to be added.</param>
        /// <returns>True if an utterance had to be dropped.</returns>
        public bool Enqueue(T utterance)
        {
            waiting.Enqueue(utterance);
            if (waiting.Count > MaxWaiting)
            {
                waiting.Dequeue();
                Dropped++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the oldest waiting utterance.
        /// </summary>
        /// <param name="utterance">The oldest waiting utterance, if there is one.</param>
        /// <returns>True if there was a waiting utterance.</returns>
        public bool TryDequeue(out T utterance)
        {
            return waiting.TryDequeue(out utterance);
        }

        /// <summary>
        /// Removes all waiting utterances. They are not counted in <see cref="Dropped"/>.
        /// </summary>
        public void Clear()
        {
            waiting.Clear();
        }
    }
}
