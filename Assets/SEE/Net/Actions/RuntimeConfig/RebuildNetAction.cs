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
                RuntimeTabMenu currentTab = runtimeConfigMenu.GetCurrentTab();
                bool isOpen = currentTab.ShowMenu;
                if (isOpen)
                {
                    currentTab.ToggleMenu();
                }
                Rebuild(isOpen, currentTab).Forget();
            }
            else
            {
                throw new Exception($"There is no {nameof(RuntimeConfigMenu)} on that player.");
            }

            async UniTask Rebuild(bool wasOpen, RuntimeTabMenu currentTab)
            {
                await runtimeConfigMenu.RebuildMenuAsync().ContinueWith(() => UniTask.DelayFrame(1));
                if (wasOpen)
                {
                    if (currentTab != null)
                    {
                        runtimeConfigMenu.SwitchCity(currentTab.CityIndex);
                    }
                    else
                    {
                        runtimeConfigMenu.GetCurrentTab().ToggleMenu();
                    }
                }
            }
        }
    }
}
