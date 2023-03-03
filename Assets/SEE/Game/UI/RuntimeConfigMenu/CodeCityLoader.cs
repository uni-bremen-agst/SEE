using SEE.Game.UI.RuntimeConfigMenu;
using UnityEngine;
using UnityEditor;
using SEE.Utils;

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
            () => 
            {
                // Open file choser dialog
                string path = null; /*EditorUtility.OpenFilePanel("W�hlen Sie eine config Datei aus", Application.streamingAssetsPath, "cfg");*/

                // If a file was selected, open it
                if(path != null && path != "")
                {
                    Debug.Log("Loading city: " + path);
                    // Might not be needed, had some trouble with loading a city while there was already an existing city but it may have just been a broken .cfg
                    RuntimeConfigMenuUtilities.ResetCity();

                    RuntimeConfigMenuUtilities.LoadCity(path);
                }
                    
            }
        );

        RuntimeConfigMenuUtilities.AddActionToButton(cityLoader, "ResetCityButton",
            () =>
            {
                Debug.Log("Reset City");
                RuntimeConfigMenuUtilities.ResetCity();
            }
        );

        RuntimeConfigMenuUtilities.AddActionToButton(cityLoader, "ContinueButton",
            () =>
            {
                RuntimeConfigMenu.InitSettings();
                cityLoader.SetActive(false);    
                seeSettingsPanel.SetActive(true);

                Canvas.ForceUpdateCanvases();
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
