using Assets.SEE.Game;
using SEE.Game;
using SEE.Utils;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class propagates a <see cref="DeleteAction"/> to all clients in the network.
    /// </summary>
    public class CleanerNetAction : AbstractNetAction
    {
        public string DrawableID;
        public string ParentDrawableID;
        public string Name;
        public DrawableTypes Type;

        /// <summary>
        /// Creates a new FastEraseNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a line
        /// that has to be deleted</param>
        public CleanerNetAction(string drawableID, string parentDrawableID, string name, DrawableTypes type) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Deletes the game object identified by <see cref="GameObjectID"/> on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                // TODO implement other things than lines
                GameObject drawable = GameDrawableIDFinder.Find(DrawableID, ParentDrawableID);
                if (drawable != null)
                {
                    if (Type == DrawableTypes.Line && GameDrawableIDFinder.FindChild(drawable, Name) != null)
                    {
                        Destroyer.Destroy(GameDrawableIDFinder.FindChild(drawable, Name));
                    }
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID} or thing on the drawable with the ID {Name}.");
                }
            }
        }
    }
}
