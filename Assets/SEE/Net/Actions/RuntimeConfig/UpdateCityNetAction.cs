using SEE.Game;
using SEE.UI.RuntimeConfigMenu;
using System;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action to synchronize cities between different players.
    /// </summary>
    public abstract class UpdateCityNetAction : AbstractNetAction
    {
        /// <summary>
        /// City index
        /// </summary>
        public int CityIndex;

        /// <summary>
        /// Widget path
        /// </summary>
        public string WidgetPath;

        /// <summary>
        /// Does nothing on the server.
        /// </summary>
        public override void ExecuteOnServer()
        {
        }

        /// <summary>
        /// Updates the runtime config menu if it is necessary.
        /// </summary>
        public override void ExecuteOnClient()
        {
            if (LocalPlayer.TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu))
            {
                runtimeConfigMenu.PerformRebuildIfRequired();
            }
            else
            {
                throw new Exception($"There is no RuntimeConfigMenu on that player.");
            }
        }
    }
}
