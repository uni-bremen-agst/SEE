using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the <see cref="GameMindMap.NodeKind"/> <see cref="EditAction"/> of a mind map node on the given drawable on all clients.
    /// </summary>
    public class MindMapChangeNodeKindNetAction : DrawableNetAction
    {
        /// <summary>
        /// The mind map node that should be change the node kind as <see cref="MindMapNodeConf"/> object.
        /// </summary>
        public MindMapNodeConf Node;

        /// <summary>
        /// The new node kind for the node.
        /// </summary>
        public GameMindMap.NodeKind NodeKind;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the node is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="node">The node that should be change the node kind.</param>
        /// <param name="nodeKind">The new node kind.</param>
        public MindMapChangeNodeKindNetAction(string drawableID, string parentDrawableID, MindMapNodeConf node, GameMindMap.NodeKind nodeKind)
            : base (drawableID, parentDrawableID)
        {
            Node = node;
            NodeKind = nodeKind;
        }

        /// <summary>
        /// Changes the node kind of a node on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="MindMapNodeConf.Id"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (Node != null && Node.Id != "")
            {
                GameMindMap.ChangeNodeKind(FindChild(Node.BorderConf.Id).transform.parent.gameObject,
                    NodeKind, Node.BorderConf);
            }
            else
            {
                throw new System.Exception($"The node with the ID {Node.Id} or the parent node with the ID {Node.ParentNode} dont exists.");
            }
        }
    }
}