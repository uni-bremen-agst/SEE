using SEE.GO;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// This class is responsible for the scaling nodes via network from one
    /// client to all others and to the server.
    /// </summary>
    public class ScaleNodeNetAction : NodeNetAction
    {
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
        /// <param name="gameNodeID">The unique name of the GameObject that should be scaled through the network.</param>
        /// <param name="localScale">The new local scale of the GameObject.</param>
        /// <param name="factor">The factor by which the animation should be sped up or slowed down.</param>
        public ScaleNodeNetAction(string gameNodeID, Vector3 localScale, float factor = 1) : base(gameNodeID)
        {
            LocalScale = localScale;
            AnimationFactor = factor;
        }

        /// <summary>
        /// Finds the GameObject on the Client and sets its scale.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Find(SourceGameNodeId).NodeOperator().ScaleTo(LocalScale, AnimationFactor);
        }
    }
}
