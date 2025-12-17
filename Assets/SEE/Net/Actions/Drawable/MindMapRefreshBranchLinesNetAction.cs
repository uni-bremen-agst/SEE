using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for refreshing the branch lines of a mind map node on the given drawable on all clients.
    /// </summary>
    public class MindMapRefreshBranchLinesNetAction : DrawableNetAction
    {
        /// <summary>
        /// The mind map node that should be refresh his branch lines.
        /// </summary>
        public MindMapNodeConf Node;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The ID of the drawable on which the node is located.</param>
        /// <param name="parentDrawableID">The ID of the drawable parent.</param>
        /// <param name="node">The node that should be change the parent.</param>
        public MindMapRefreshBranchLinesNetAction(string drawableID, string parentDrawableID, MindMapNodeConf node)
            : base(drawableID, parentDrawableID)
        {
            Node = node;
        }

        /// <summary>
        /// Refreshs the branch lines of a node on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="Node"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (Node != null && Node.ID != "")
            {
                GameMindMap.ReDrawBranchLines(FindChild(Node.BorderConf.ID).transform.parent.gameObject);
            }
            else
            {
                throw new System.Exception($"The node with the ID {Node.ID} dont exists.");
            }
        }
    }
}