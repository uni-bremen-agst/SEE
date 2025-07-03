using Cysharp.Threading.Tasks;
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
                bool isOpen = runtimeConfigMenu.GetCurrentTab().ShowMenu;
                if (isOpen)
                {
                    runtimeConfigMenu.GetCurrentTab().ToggleMenu();
                }
                Rebuild(isOpen).Forget();
            }
            else
            {
                throw new Exception($"There is no {nameof(RuntimeConfigMenu)} on that player.");
            }

            async UniTask Rebuild(bool wasOpen)
            {
                await runtimeConfigMenu.RebuildMenuAsync();
                if (wasOpen)
                {
                    runtimeConfigMenu.GetCurrentTab().ToggleMenu();
                }
            }
        }
    }
}
