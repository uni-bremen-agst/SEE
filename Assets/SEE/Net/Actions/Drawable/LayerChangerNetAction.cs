using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using SEE.Game;
using SEE.Utils;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class propagates a <see cref="DeleteAction"/> to all clients in the network.
    /// </summary>
    public class LayerChangerNetAction : AbstractNetAction
    {
        public string DrawableID;
        public string ParentDrawableID;
        public string ObjectName;
        public GameLayerChanger.LayerChangerStates State;
        public DrawableTypes Type;
        public int Order;


        /// <summary>
        /// Creates a new FastEraseNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a line
        /// that has to be deleted</param>
        public LayerChangerNetAction(string drawableID, string parentDrawableID, string objectName, GameLayerChanger.LayerChangerStates state, DrawableTypes type, int order) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            ObjectName = objectName;
            State = state;
            Type = type;
            Order = order;
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
                GameObject drawable = GameDrawableFinder.Find(DrawableID,ParentDrawableID);
                GameObject obj = GameDrawableFinder.FindChild(drawable, ObjectName);
                if (drawable == null)
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID}.");
                }
                if (obj == null)
                {
                    throw new System.Exception($"There is no object with the name {ObjectName}.");
                }

                switch (State) {
                    case GameLayerChanger.LayerChangerStates.Increase:
                        GameLayerChanger.Increase(Type, obj, Order);
                        break;

                    case GameLayerChanger.LayerChangerStates.Decrease:
                        GameLayerChanger.Decrease(Type, obj, Order);
                        break;
                }
            }
        }
    }
}
