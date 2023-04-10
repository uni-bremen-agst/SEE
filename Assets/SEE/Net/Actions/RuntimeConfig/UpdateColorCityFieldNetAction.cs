using SEE.Game.UI.RuntimeConfigMenu;
using UnityEngine;

namespace SEE.Net.Actions.RuntimeConfig
{
    public class UpdateColorCityFieldNetAction : AbstractNetAction
    {
        public int CityIndex;
        public string WidgetPath;
        public Color Value;

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
                RuntimeConfigMenu.GetMenuForCity(CityIndex).OnSyncField?.Invoke(WidgetPath, Value);
        }
    }
}
