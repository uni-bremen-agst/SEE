using SEE.Game.UI.RuntimeConfigMenu;
using UnityEngine;

public class CodeCityLoader : MonoBehaviour
{
    private GameObject cityLoader;
    private GameObject seeTables;
    private GameObject seeSettingsPanel;

    private void Awake()
    {
        cityLoader = GameObject.Find("CodeCityLoader");
        seeSettingsPanel = cityLoader.transform.parent.transform.Find("SeeSettingsPanel").gameObject;
        seeTables = cityLoader.transform.parent.transform.Find("SeeTables").gameObject;

        RuntimeConfigMenuUtilities.AddActionToButton(cityLoader, "LoadCityButton",
                () => {
                    Debug.Log("Load City: mini.cfg");
                    RuntimeConfigMenuUtilities.LoadCity("mini/mini.cfg");
                    cityLoader.SetActive(false);
                    seeSettingsPanel.SetActive(true);
                }
        );

        RuntimeConfigMenuUtilities.AddActionToButton(cityLoader, "ResetCityButton",
            () =>
            {
                Debug.Log("Reset City.");
                RuntimeConfigMenuUtilities.ResetCity();
                cityLoader.SetActive(false);
                seeSettingsPanel.SetActive(true);
            }
        );
    }

    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {

    }
}
