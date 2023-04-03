using SEE.Game.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    public class UpdateFloatCityFieldNetAction : AbstractNetAction
    {
        public int CityIndex;
        public string WidgetPath;
        public float Value;

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            // if (!IsRequester())
            RuntimeConfigMenu.GetMenuForCity(CityIndex).OnSyncField?.Invoke(WidgetPath, Value);
        }
    }
}
