using SEE.Game;
using SEE.UI.RuntimeConfigMenu;
using System;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action to synchronize <see cref="RuntimeConfigMenu"/> between different players.
    /// </summary>
    public class RebuildNetAction : AbstractNetAction
    {
        /// <summary>
        /// Rebuilds the runtime config menu.
        /// </summary>
        public override void ExecuteOnClient()
        {
            if (LocalPlayer.TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu))
            {
                runtimeConfigMenu.BuildTabMenus();
            }
            else
            {
                throw new Exception($"There is no {nameof(RuntimeConfigMenu)} on that player.");
            }
        }
    }
}
