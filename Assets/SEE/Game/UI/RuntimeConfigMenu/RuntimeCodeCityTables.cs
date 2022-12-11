using SEE.DataModel;
using SEE.Game.UI.RuntimeConfigMenu;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RuntimeCodeCityTables : MonoBehaviour
{
    private GameObject tableView;
    private GameObject seeTables;
    private GameObject codeCityLoader;
    
    private void Awake()
    {
        seeTables = GameObject.Find("SeeTables");
        codeCityLoader = seeTables.transform.parent.transform.Find("CodeCityLoader").gameObject;
        tableView = seeTables.transform.Find("CodeCityTables").Find("Content").gameObject;
    }

    // Start is called before the first frame update
    void Start()
    {
        GameObject[] tables = GameObject.FindGameObjectsWithTag(Tags.CodeCity);

        foreach (var table in tables)
        {
            GameObject tableButton = PrefabInstantiator.InstantiatePrefab(RuntimeConfigMenu.TABLE_BUTTON_PREFAB_PATH);
            tableButton.transform.parent = tableView.transform;
            tableButton.name = "DebugButton";
            Button debugButtonComponent = tableButton.GetComponent<Button>();
            UnityAction action = () =>
            {
                seeTables.SetActive(false);
                codeCityLoader.SetActive(true);
            };
            
            debugButtonComponent.onClick.AddListener(action);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
