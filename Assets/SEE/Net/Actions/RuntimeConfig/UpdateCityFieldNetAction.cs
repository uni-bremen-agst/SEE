using SEE.DataModel;
using SEE.Net.Actions;
using UnityEngine;

public class UpdateCityFieldNetAction : AbstractNetAction
{
    public string widgetPath;
    public string newValue;
    
    protected override void ExecuteOnServer()
    {
    }

    protected override void ExecuteOnClient()
    {
        Debug.LogError("Executed UpdateCityFieldNetAction\t" + widgetPath + "\t" + newValue);
        //GetConfigMenu().OnUpdateField?.Invoke(widgetPath, newValue);
        //GetConfigMenu().OnUpdateMenuValues?.Invoke();
    }


    private static RuntimeTabMenu GetConfigMenu()
    {
        return GameObject.FindGameObjectWithTag(Tags.MainCamera).GetComponent<RuntimeTabMenu>();
    }
    
    
}
