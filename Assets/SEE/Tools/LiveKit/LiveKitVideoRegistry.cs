using System.Collections.Generic;

namespace SEE.Tools.LiveKit
{
    /// <summary>
    /// Provides a global registry that maps Netcode client IDs to their
    /// associated <see cref="LiveKitVideo"/> instances.
    /// </summary>
    public class LiveKitVideoRegistry
    {
        /// <summary>
        /// Stores the mapping between client IDs and their corresponding
        /// <see cref="LiveKitVideo"/> components.
        /// </summary>
        private static readonly Dictionary<ulong, LiveKitVideo> registry = new();

        /// <summary>
        /// Registers a <see cref="LiveKitVideo"/> instance for the specified <paramref name="clientID"/>.
        /// </summary>
        /// <param name="clientID">The Netcode client ID associated with the player.</param>
        /// <param name="instance">The <see cref="LiveKitVideo"/> instance to register.</param>
        public static void Register(ulong clientID, LiveKitVideo instance)
        {
            registry[clientID] = instance;
        }

        /// <summary>
        /// Removes a previously registered <see cref="LiveKitVideo"/> instance.
        /// </summary>
        /// <param name="clientId">The client ID whose video object should be removed.</param>
        public static void Unregister(ulong clientId)
        {
            registry.Remove(clientId);
        }

        /// <summary>
        /// Attempts to retrieve the <see cref="LiveKitVideo"/> instance associated
        /// with the specified client ID.
        /// </summary>
        /// <param name="clientId">The Netcode client ID to look up.</param>
        /// <param name="instance">
        /// When successful, contains the <see cref="LiveKitVideo"/> instance associated
        /// with the client ID; otherwise, null.
        /// </param>
        /// <returns>
        /// True if a matching instance was found; otherwise false.
        /// </returns>
        public static bool TryGet(ulong clientId, out LiveKitVideo instance)
        {
            return registry.TryGetValue(clientId, out instance);
        }

        /// <summary>
        /// Returns all currently registered LiveKitVideo instances.
        /// Useful for iterating over local and remote videos.
        /// </summary>
        /// <returns>An enumerable of all LiveKitVideo instances.</returns>
        public static IEnumerable<LiveKitVideo> GetAll()
        {
            return registry.Values;
        }
    }
}
