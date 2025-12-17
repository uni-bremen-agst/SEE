using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing all values (<see cref="EditAction"/>) of a mind map node on all clients.
    /// </summary>
    public class EditMMNodeNetAction : DrawableNetAction
    {
        /// <summary>
        /// The Mind map node that should be changed. The conf object contains all relevant values to change.
        /// </summary>
        public MindMapNodeConf Conf;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the node is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="conf">The configuration that contains the values to change the associated game object.</param>
        public EditMMNodeNetAction(string drawableID, string parentDrawableID, MindMapNodeConf conf)
            : base(drawableID, parentDrawableID)
        {
            Conf = conf;
        }

        /// <summary>
        /// Changes the values of the given mind map node configuration on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="Conf"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (FindChild(Conf.BorderConf.ID).transform.parent.gameObject != null)
            {
                GameEdit.ChangeMindMapNode(FindChild(Conf.BorderConf.ID).transform.parent.gameObject, Conf);
            }
        }
    }
}