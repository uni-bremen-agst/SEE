using SEE.DataModel;
using SEE.Game.UI.RuntimeConfigMenu;
using SEE.Net.Actions;
using UnityEngine;

public class UpdateCityFieldNetAction : AbstractNetAction
{
    public int CityIndex;
    public string WidgetPath;
    public string Value;
    
    protected override void ExecuteOnServer()
    {
    }

    protected override void ExecuteOnClient()
    {
        // if (!IsRequester())
        RuntimeConfigMenu.GetMenuForCity(CityIndex).OnSyncField?.Invoke(WidgetPath, Value);
    }
}
