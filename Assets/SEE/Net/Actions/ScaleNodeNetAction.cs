using SEE.Game.Operator;
using SEE.GO;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class is responsible for the scaling nodes via network from one
    /// client to all others and to the server.
    /// </summary>
    public class ScaleNodeNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the gameObject that has to be scaled.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// The new local scale to transfer over the network.
        /// </summary>
        public Vector3 LocalScale;

        /// <summary>
        /// The duration of the animation in seconds.
        /// </summary>
        public float AnimationDuration;

        /// <summary>
        /// Constructs a ScaleNodeNetAction
        /// </summary>
        /// <param name="gameObjectID">The unique name of the GameObject that should be scaled through the network</param>
        /// <param name="localScale">The new local scale of the GameObject</param>
        /// <param name="duration">The duration of the animation in seconds</param>
        public ScaleNodeNetAction(string gameObjectID, Vector3 localScale, float duration = 0) : base()
        {
            GameObjectID = gameObjectID;
            LocalScale = localScale;
            AnimationDuration = duration;
        }

        /// <summary>
        /// Things to execute on the server (none for this class).
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank
        }

        /// <summary>
        /// Finds the GameObject on the Client and sets its scale and position
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                if (Find(GameObjectID).TryGetComponentOrLog(out NodeOperator nodeOperator))
                {
                    nodeOperator.ScaleTo(LocalScale, AnimationDuration);
                }
            }
        }
    }
}