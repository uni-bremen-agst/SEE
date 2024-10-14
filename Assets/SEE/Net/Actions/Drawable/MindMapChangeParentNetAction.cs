using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the parent <see cref="EditAction"/> of a mind map node on the given drawable on all clients.
    /// </summary>
    public class MindMapChangeParentNetAction : DrawableNetAction
    {
        /// <summary>
        /// The mind map node that should be change the parent as <see cref="MindMapNodeConf"/> object.
        /// </summary>
        public MindMapNodeConf Node;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the node is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="node">The node that should be change the parent.</param>
        public MindMapChangeParentNetAction(string drawableID, string parentDrawableID, MindMapNodeConf node)
            : base(drawableID, parentDrawableID)
        {
            Node = node;
        }

        /// <summary>
        /// Change the parent of a node on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="Node"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (Node != null && Node.Id != "")
            {
                GameMindMap.ChangeParent(FindChild(Node.BorderConf.Id).transform.parent.gameObject, FindChild(Node.ParentNode));
            }
            else
            {
                throw new System.Exception($"The node with the ID {Node.Id} or the parent node with the ID {Node.ParentNode} dont exists.");
            }
        }
    }
}