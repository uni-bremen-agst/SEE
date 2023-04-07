using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuntimeConfigMenuCollapse : MonoBehaviour
{
    /// <summary>
    /// Are the settings collapsed
    /// </summary>
    private bool settingVisibility = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    /// <summary>
    /// All settings of the SettingObject are shown/hidden
    /// </summary>
    /// <see cref="settingVisibility"/>
    public void OnClickCollapse()
    {
        settingVisibility = !settingVisibility;
        this.transform.parent.parent.Find("Content").gameObject.SetActive(settingVisibility);

        // change rotation when pressed
        if (settingVisibility) this.transform.Find("Icon").transform.Rotate(0,0,-90);
        else this.transform.Find("Icon").transform.Rotate(0,0,90);
    }
    
    
}
