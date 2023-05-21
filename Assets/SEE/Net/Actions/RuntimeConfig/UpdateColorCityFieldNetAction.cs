using SEE.Game.UI.RuntimeConfigMenu;
using UnityEngine;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action when a color was changed. 
    /// </summary>
    public class UpdateColorCityFieldNetAction : UpdateCityNetAction
    {
        /// <summary>
        /// The changed value
        /// </summary>
        public Color Value;

        /// <summary>
        /// Triggers 'SyncField' on <see cref="RuntimeTabMenu"/>.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                RuntimeConfigMenu.GetMenuForCity(CityIndex).SyncField?.Invoke(WidgetPath, Value);
            }
        }
    }
}