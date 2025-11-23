using SEE.GO;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class is responsible for the scaling nodes via network from one
    /// client to all others and to the server.
    /// </summary>
    public class ScaleNodeNetAction : ConcurrentNetAction
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
        /// <param name="gameObjectID">The unique name of the GameObject that should be scaled through the network</param>
        /// <param name="localScale">The new local scale of the GameObject</param>
        /// <param name="factor">The factor by which the animation should be sped up or slowed down</param>
        public ScaleNodeNetAction(string gameObjectID, Vector3 localScale, float factor = 1) : base(gameObjectID)
        {
            LocalScale = localScale;
            AnimationFactor = factor;
            UseObjectVersion(gameObjectID);
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
            Find(GameObjectID).NodeOperator().ScaleTo(LocalScale, AnimationFactor);
            SetVersion();
        }

        /// <summary>
        /// Undos the ScaleNodeAction locally if the server rejects it.
        /// </summary>
        public override void Undo()
        {
            Vector3 inverseScale = new Vector3(
                LocalScale.x != 0 ? 1f / LocalScale.x : 0f,
                LocalScale.y != 0 ? 1f / LocalScale.y : 0f,
                LocalScale.z != 0 ? 1f / LocalScale.z : 0f);
            Find(GameObjectID).NodeOperator().ScaleTo(LocalScale, AnimationFactor);
            RollbackNotification();
        }
    }
}
