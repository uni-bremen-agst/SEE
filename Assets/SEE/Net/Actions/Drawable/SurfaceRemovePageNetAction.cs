using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{/// <summary>
 /// This class is responsible for remove a page of a drawable on all clients.
 /// </summary>
    public class SurfaceRemovePageNetAction : SurfaceNetAction
    {
        /// <summary>
        /// The page to be removed.
        /// </summary>
        public int Page;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="config">The current <see cref="DrawableConfig"/>.</param>
        /// <param name="page">The page to be removed.</param>
        public SurfaceRemovePageNetAction(DrawableConfig config, int page) : base(config)
        {
            Page = page;
        }

        /// <summary>
        /// Synchronize the current order in layer of the host on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameDrawableManager.RemovePage(Surface, Page);
        }
    }
}
