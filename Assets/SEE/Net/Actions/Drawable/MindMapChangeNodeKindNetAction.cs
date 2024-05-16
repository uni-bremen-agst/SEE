using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the <see cref="GameMindMap.NodeKind"/> <see cref="EditAction"/> of a mind map node on the given drawable on all clients.
    /// </summary>
    public class MindMapChangeNodeKindNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the drawable on which the node is located
        /// </summary>
        public string DrawableID;

        /// <summary>
        /// The id of the drawable parent
        /// </summary>
        public string ParentDrawableID;

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
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.Node = node;
            this.NodeKind = nodeKind;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }
        /// <summary>
        /// Changes the node kind of a node on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="MindMapNodeConf.Id"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.FindDrawable(DrawableID, ParentDrawableID);
                if (drawable == null)
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID}.");
                }

                if (Node != null && Node.Id != "")
                {
                    GameObject attached = GameFinder.GetAttachedObjectsObject(drawable);
                    GameObject node = GameFinder.FindChild(attached, Node.BorderConf.Id).transform.parent.gameObject;
                    GameMindMap.ChangeNodeKind(node, NodeKind, Node.BorderConf);
                }
                else
                {
                    throw new System.Exception($"The node with the ID {Node.Id} or the parent node with the ID {Node.ParentNode} dont exists.");
                }
            }
        }
    }
}