using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for refreshing the branch lines of a mind map node on the given drawable on all clients.
    /// </summary>
    public class MindMapRefreshBranchLinesNetAction : AbstractNetAction
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
        /// The mind map node that should be refresh his branch lines.
        /// </summary>
        public MindMapNodeConf Node;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the node is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="node">The node that should be change the parent.</param>
        public MindMapRefreshBranchLinesNetAction(string drawableID, string parentDrawableID, MindMapNodeConf node)
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
        /// Refreshs the branch lines of a node on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="Node"/> don't exists.</exception>
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

                    GameMindMap.ReDrawBranchLines(node);
                }
                else
                {
                    throw new System.Exception($"The node with the ID {Node.id} dont exists.");
                }
            }
        }
    }
}