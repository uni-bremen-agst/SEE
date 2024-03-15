using SEE.UI.RuntimeConfigMenu;
using UnityEngine;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action when a color was changed.
    ///
    /// Color can't be send with a generic class (<see cref="UpdateCityAttributeNetAction{E}"/>).
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
        public override void ExecuteOnClient()
        {
            RuntimeConfigMenu.GetMenuForCity(CityIndex).SyncField?.Invoke(WidgetPath, Value);
        }
    }
}
