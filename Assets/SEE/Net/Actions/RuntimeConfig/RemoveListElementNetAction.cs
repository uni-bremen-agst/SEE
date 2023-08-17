using SEE.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action when a list element was removed.
    /// </summary>
    public class RemoveListElementNetAction : UpdateCityNetAction
    {
        /// <summary>
        /// Triggers 'SyncRemoveListElement' on <see cref="RuntimeTabMenu"/>.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                RuntimeConfigMenu.GetMenuForCity(CityIndex).SyncRemoveListElement?.Invoke(WidgetPath);
            }
        }
    }
}
