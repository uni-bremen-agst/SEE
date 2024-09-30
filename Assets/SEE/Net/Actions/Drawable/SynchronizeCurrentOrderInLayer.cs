using SEE.Game.Drawable;
using Unity.Netcode;

namespace SEE.Net.Actions.Drawable
{/// <summary>
 /// This class is responsible for drawing (<see cref="DrawOnAction"/>) a line on the given drawable on all clients.
 /// </summary>
    public class SynchronizeCurrentOrderInLayer : AbstractNetAction
    {
        /// <summary>
        /// Should not be sent to newly connecting clients
        /// </summary>
        public override bool ShouldBeSentToNewClient { get => false; }

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
        public override void ExecuteOnClient()
        {
            ValueHolder.MaxOrderInLayer = OrderInLayer;
        }
    }
}
