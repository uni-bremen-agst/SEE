using Assets.SEE.Game.Drawable;
using SEE.Controls;
using UnityEngine;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the order in layer (<see cref="LayerChangerAction"/>) of an object on all clients.
    /// </summary>
    public class LayerChangerNetAction : AbstractNetAction
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
        /// The id of the object that should be changed
        /// </summary>
        public string ObjectName;
        /// <summary>
        /// The state of layer change
        /// </summary>
        public GameLayerChanger.LayerChangerStates State;
        /// <summary>
        /// The order in layer that should be set
        /// </summary>
        public int Order;

        /// <summary>
        /// Creates a new <see cref="LayerChangerNetAction"/>
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="objectName">The id of the object that should be changed.</param>
        /// <param name="state">The state of layer change</param>
        /// <param name="order">The order in layer that should be set.</param>
        public LayerChangerNetAction(string drawableID, string parentDrawableID, string objectName, GameLayerChanger.LayerChangerStates state, int order) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            ObjectName = objectName;
            State = state;
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
        /// Changes the order in layer of the given object on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="ObjectName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.Find(DrawableID,ParentDrawableID);
                GameObject obj = GameFinder.FindChild(drawable, ObjectName);
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
                        GameLayerChanger.Increase(obj, Order);
                        break;

                    case GameLayerChanger.LayerChangerStates.Decrease:
                        GameLayerChanger.Decrease(obj, Order);
                        break;
                }
            }
        }
    }
}
