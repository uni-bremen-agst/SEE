using System.Collections.Generic;

namespace SEE
{

    /// <summary>
    /// The current game state of the game. This will be sent to newly connecting
    /// clients.
    /// </summary>
    public class GameState
    {
        /// <summary>
        /// The IDs of all the nodes on the zoom stack.
        /// </summary>
        public readonly Stack<uint> zoomIDStack = new Stack<uint>();

        /// <summary>
        /// The IDs of all the selected <see cref="GameObject"/>s.
        /// </summary>
        public readonly List<uint> selectedGameObjectIDs = new List<uint>();
    }

}
