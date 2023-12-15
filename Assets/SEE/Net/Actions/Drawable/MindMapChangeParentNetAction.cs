using System.Collections;
using UnityEngine;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the parent <see cref="EditAction"/> of a mind map node on the given drawable on all clients.
    /// </summary>
    public class MindMapChangeParentNetAction : AbstractNetAction
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
        /// The mind map node that should be change the parent as <see cref="MindMapNodeConf"/> object.
        /// </summary>
        public MindMapNodeConf Node;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="node">The node that should be change the parent.</param>
        public MindMapChangeParentNetAction(string drawableID, string parentDrawableID, MindMapNodeConf node)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.Node = node;
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

                if (Node != null && Node.id != "")
                {
                    GameObject attached = GameFinder.GetAttachedObjectsObject(drawable);
                    GameObject node = GameFinder.FindChild(attached, Node.borderConf.id).transform.parent.gameObject;
                    GameObject parent = GameFinder.FindChild(attached, Node.parentNode);
                    GameMindMap.ChangeParent(node, parent);
                }
                else
                {
                    throw new System.Exception($"The node with the ID {Node.id} or the parent node with the ID {Node.parentNode} dont exists.");
                }
            }
        }
    }
}