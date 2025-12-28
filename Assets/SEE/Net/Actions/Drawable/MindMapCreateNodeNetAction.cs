using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for create <see cref="MindMapAction"/> a mind map node on the given drawable on all clients.
    /// </summary>
    public class MindMapCreateNodeNetAction : DrawableNetAction
    {
        /// <summary>
        /// The mind map node that should be created as <see cref="MindMapNodeConf"/> object.
        /// </summary>
        public MindMapNodeConf Node;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The ID of the drawable on which the node should be created.</param>
        /// <param name="parentDrawableID">The ID of the drawable parent.</param>
        /// <param name="node">The node that should be created.</param>
        public MindMapCreateNodeNetAction(string drawableID, string parentDrawableID, MindMapNodeConf node)
            : base(drawableID, parentDrawableID)
        {
            Node = node;
        }

        /// <summary>
        /// Creates the node on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="Node"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (Node != null && Node.ID != "")
            {
                GameMindMap.ReCreate(Surface, Node);
            }
            else
            {
                throw new System.Exception($"There is no node to create.");
            }
        }
    }
}