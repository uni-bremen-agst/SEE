using SEE.Game.Operator;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates changed size and position of a single game node through the network.
    /// <para>
    /// Children are not scaled or moved along with the resized node.
    /// </para>
    /// </summary>
    public class ResizeNodeNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the gameObject that has to be resized.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// The new local scale to transfer over the network.
        /// </summary>
        public Vector3 LocalScale;

        /// <summary>
        /// The new position to transfer over the network.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Constructs a <see cref="ResizeNodeNetAction"/>.
        /// </summary>
        /// <param name="gameObjectID">The unique name of the <see cref="GameObject"/> that should be resized</param>
        /// <param name="localScale">The new local scale of the <see cref="GameObject"/></param>
        /// <param name="position">The new absolute position of the <see cref="GameObject"/></param>
        public ResizeNodeNetAction(string gameObjectID, Vector3 localScale, Vector3 position)
        {
            GameObjectID = gameObjectID;
            LocalScale = localScale;
            Position = position;
        }

        /// <summary>
        /// Things to execute on the server (none for this class).
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank
        }

        /// <summary>
        /// Finds the GameObject on the Client and sets its scale.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject go = Find(GameObjectID);
            go.transform.localScale = LocalScale;
            go.transform.position = Position;
            // FIXME update edges
        }
    }
}
