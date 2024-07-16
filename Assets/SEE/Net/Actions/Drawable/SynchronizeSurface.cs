using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;

namespace SEE.Net.Actions.Drawable
{/// <summary>
 /// This class is responsible for synchronize a drawable on all clients.
 /// </summary>
    public class SynchronizeSurface : DrawableNetAction
    {
        /// <summary>
        /// The config of the drawable that should be synchronized.
        /// </summary>
        public DrawableConfig Config;

        /// <summary>
        /// Whether the current page change should be forced.
        /// </summary>
        public bool ForceChange;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="orderInLayer">The current order in layer of the host.</param>
        public SynchronizeSurface(DrawableConfig config, bool forceChange = false) : base(config.ID, config.ParentID)
        {
            Config = config;
            ForceChange = forceChange;
        }

        /// <summary>
        /// Synchronize the current order in layer of the host on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            DrawableHolder holder = Surface.GetComponent<DrawableHolder>();
            holder.OrderInLayer = Config.OrderInLayer;
            holder.Description = Config.Description;
            holder.MaxPageSize = Config.MaxPageSize;
            GameDrawableManager.ChangeCurrentPage(Surface, Config.CurrentPage, ForceChange);
        }
    }
}