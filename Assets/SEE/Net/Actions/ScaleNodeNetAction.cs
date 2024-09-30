using SEE.Game.Operator;
using SEE.GO;
using SEE.Utils;
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
        /// The factor by which the animation should be sped up or slowed down.
        /// </summary>
        public float AnimationFactor;

        /// <summary>
        /// Constructs a ScaleNodeNetAction
        /// </summary>
        /// <param name="gameObjectID">The unique name of the GameObject that should be scaled through the network</param>
        /// <param name="localScale">The new local scale of the GameObject</param>
        /// <param name="factor">The factor by which the animation should be sped up or slowed down</param>
        public ScaleNodeNetAction(string gameObjectID, Vector3 localScale, float factor = 1)
        {
            GameObjectID = gameObjectID;
            LocalScale = localScale;
            AnimationFactor = factor;
        }

        /// <summary>
        /// Finds the GameObject on the Client and sets its scale.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Find(GameObjectID).NodeOperator().ScaleTo(LocalScale, AnimationFactor);
        }
    }
}
