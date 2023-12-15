using SEE.Game.Drawable.Configurations;
using SEE.Game;
using System.Collections;
using UnityEngine;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing all values (<see cref="EditAction"/>) of a mind map node on all clients.
    /// </summary>
    public class EditMMNodeNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the drawable on which the object is located
        /// </summary>
        public string DrawableID;
        /// <summary>
        /// The id of the drawable parent
        /// </summary>
        public string ParentDrawableID;
        /// <summary>
        /// The Mind map node that should be changed. The conf object contains all relevant values to change.
        /// </summary>
        public MindMapNodeConf Conf;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="conf">The configuration that contains the values to change the associated game object.</param>
        public EditMMNodeNetAction(string drawableID, string parentDrawableID, MindMapNodeConf conf) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            Conf = conf;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Changes the values of the given mind map node configuration on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="LineName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.FindDrawable(DrawableID, ParentDrawableID);
                GameObject attached = GameFinder.GetAttachedObjectsObject(drawable);
                if (drawable != null && GameFinder.FindChild(attached, Conf.borderConf.id).transform.parent.gameObject != null)
                {
                    GameObject nodeObj = GameFinder.FindChild(attached, Conf.borderConf.id).transform.parent.gameObject;
                    GameEdit.ChangeMindMapNode(nodeObj, Conf);
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID} or node with the ID {Conf.id}.");
                }
            }
        }
    }
}