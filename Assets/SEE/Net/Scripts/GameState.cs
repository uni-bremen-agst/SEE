using System.Collections.Generic;

namespace SEE
{

    public class GameState
    {
        public readonly Stack<uint> zoomIDStack = new Stack<uint>();
        public readonly List<uint> selectedGameObjects = new List<uint>();
    }

}
