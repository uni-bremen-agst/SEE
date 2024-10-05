using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for removing a mind map node from the child list of the parent node on all clients.
    /// </summary>
    public class MindMapRemoveChildNetAction : DrawableNetAction
    {

        /// <summary>
        /// The mind map node that should be removed as <see cref="MindMapNodeConf"/> object.
        /// </summary>
        public MindMapNodeConf ChildNode;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the node is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="child">The node that should be removed.</param>
        public MindMapRemoveChildNetAction(string drawableID, string parentDrawableID, MindMapNodeConf child)
            : base(drawableID, parentDrawableID)
        {
            ChildNode = child;
        }

        /// <summary>
        /// Removes the node from old parent's children list on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="ChildNode"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (ChildNode != null && ChildNode.Id != "" && ChildNode.ParentNode != "")
            {
                FindChild(ChildNode.ParentNode).GetComponent<MMNodeValueHolder>().RemoveChild(FindChild(ChildNode.Id));
            }
            else
            {
                throw new System.Exception($"The node with the ID {ChildNode.Id} or the parent node with the ID {ChildNode.ParentNode} dont exists.");
            }
        }
    }
}