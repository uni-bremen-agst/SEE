using SEE.GraphElementRefs;
using System;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Common superclass for all net actions relating to game nodes or edges.
    /// </summary>
    public abstract class GraphElementNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the game object this net action refers to. It will be looked
        /// up in <see cref="GraphElementIDMap"/>.
        /// </summary>
        /// <remarks>If the graph element is an edge, this id refers to the source of
        /// the edge.</remarks>
        public string GraphElementID;

        public GraphElementNetAction(string gameObjectID)
        {
            GraphElementID = gameObjectID;
        }

        /// <summary>
        /// Retrieves and returns the game object registered at <see cref="GraphElementIDMap"/>
        /// under the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The unique ID that is to be used to retrieve the game object.</param>
        /// <returns>The game object registered at <see cref="GraphElementIDMap"/>.</returns>
        /// <exception cref="Exception">Thrown if <see cref="GraphElementIDMap"/>
        /// has no game object registered by <paramref name="id"/>.</exception>
        protected static GameObject Find(string id)
        {
            GameObject result = GraphElementIDMap.Find(id);
            if (result == null)
            {
                throw new Exception($"There is no game object with the ID {id}.");
            }
            else
            {
                return result;
            }
        }
    }
}
