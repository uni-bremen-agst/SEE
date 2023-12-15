using System.Collections;
using UnityEngine;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ValueHolders;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for removing a mind map node from the child list of the parent node on all clients.
    /// </summary>
    public class MindMapRemoveChildNetAction : AbstractNetAction
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
        /// The mind map node that should be removed as <see cref="MindMapNodeConf"/> object.
        /// </summary>
        public MindMapNodeConf ChildNode;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="child">The node that should be removed.</param>
        public MindMapRemoveChildNetAction(string drawableID, string parentDrawableID, MindMapNodeConf child)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.ChildNode = child;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }
        /// <summary>
        /// Change the parent of a node on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="MindMapNodeConf.IDLabel"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.FindDrawable(DrawableID, ParentDrawableID);
                if (drawable == null)
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID}.");
                }

                if (ChildNode != null && ChildNode.id != "" && ChildNode.parentNode != "")
                {
                    GameObject attached = GameFinder.GetAttachedObjectsObject(drawable);
                    GameObject child = GameFinder.FindChild(attached, ChildNode.id);
                    GameObject parent = GameFinder.FindChild(attached, ChildNode.parentNode);
                    parent.GetComponent<MMNodeValueHolder>().RemoveChild(child);
                }
                else
                {
                    throw new System.Exception($"The node with the ID {ChildNode.id} or the parent node with the ID {ChildNode.parentNode} dont exists.");
                }
            }
        }
    }
}