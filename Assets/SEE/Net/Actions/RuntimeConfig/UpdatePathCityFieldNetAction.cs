using SEE.Game.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    public class UpdatePathCityFieldNetAction : AbstractNetAction
    {
        public int CityIndex;
        public bool IsAbsolute;
        public string Value;
        public string WidgetPath;

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
                RuntimeConfigMenu.GetMenuForCity(CityIndex).OnSyncPath?.Invoke(WidgetPath, Value, IsAbsolute);
        }
    }
}