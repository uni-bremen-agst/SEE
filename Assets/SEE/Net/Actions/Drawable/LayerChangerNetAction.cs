using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the order in layer (<see cref="LayerChangeAction"/>) of an object on all clients.
    /// </summary>
    public class LayerChangerNetAction : DrawableNetAction
    {
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
        /// <param name="state">The state of layer change.</param>
        /// <param name="order">The order in layer that should be set.</param>
        public LayerChangerNetAction(string drawableID, string parentDrawableID, string objectName, GameLayerChanger.LayerChangerStates state, int order)
            : base(drawableID, parentDrawableID)
        {
            ObjectName = objectName;
            State = state;
            Order = order;
        }

        /// <summary>
        /// Changes the order in layer of the given object on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="ObjectName"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameLayerChanger.ChangeOrderInLayer(FindChild(ObjectName), Order, State, false);
        }
    }
}
