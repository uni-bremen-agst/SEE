using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for destroying the rigid bodies and collision controllers of the objects of the drawable on all clients.
    /// </summary>
    public class RbAndCCDestroyerNetAction : DrawableNetAction
    {
        /// <summary>
        /// The id of the selected node which children's rigid bodies and collision controllers should be destroyed.
        /// </summary>
        public string NodeID;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        public RbAndCCDestroyerNetAction(string drawableID, string parentDrawableID, string nodeID)
            : base(drawableID, parentDrawableID)
        {
            NodeID = nodeID;
        }

        /// <summary>
        /// Destroys all rigid bodies and collision controller of all drawable types object of the given drawable on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="NodeID"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameMoveRotator.DestroyRigidBodysAndCollisionControllersOfChildren(FindChild(NodeID));
        }
    }
}
