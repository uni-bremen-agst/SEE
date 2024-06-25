using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{/// <summary>
 /// This class is responsible for drawing (<see cref="DrawOnAction"/>) a line on the given drawable on all clients.
 /// </summary>
    public class SynchronizeCurrentOrderInLayer : AbstractNetAction
    {
        /// <summary>
        /// The current order in layer of the host.
        /// </summary>
        public int OrderInLayer;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="orderInLayer">The current order in layer of the host.</param>
        public SynchronizeCurrentOrderInLayer(int orderInLayer)
        {
            OrderInLayer = orderInLayer;
        }

        /// <summary>
        /// Synchronize the current order in layer of the host on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                ValueHolder.MaxOrderInLayer = OrderInLayer;
            }
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }
    }
}