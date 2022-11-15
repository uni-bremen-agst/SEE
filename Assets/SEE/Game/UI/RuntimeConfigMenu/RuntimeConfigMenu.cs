using DynamicPanels;
using SEE.Controls;
using SEE.Game.City;
using SEE.GO;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeConfigMenu : MonoBehaviour
    {
        private const string menuPrefab = "Prefabs/UI/RuntimeConfigMenu";
        private GameObject runtimeConfigMenu;
        
        void Awake()
        {
            Debug.Log("Awake Runtime Config Menu");
            runtimeConfigMenu = Instantiate(Resources.Load<GameObject>(menuPrefab), Vector3.zero, Quaternion.identity);
            runtimeConfigMenu.SetActive(false);
            
            // adds a listener to the debug button
            GameObject debugButton = runtimeConfigMenu.transform.Find("Canvas/DebugButton").gameObject;
            Button debugButtonComponent = debugButton.GetComponent<Button>();
            debugButtonComponent.onClick.AddListener(OnDebugClicked);
        }

        void Update()
        {
            if (SEEInput.ToggleConfigMenu())
            {
                runtimeConfigMenu.SetActive(!runtimeConfigMenu.activeSelf);
            }
        }

        private void OnDebugClicked()
        {
            Debug.Log("On Debug Clicked");
        }

        private void SelectCity()
        {
            // TODO: Where do I get the city from?
            // SEECity city;
            // TODO: Relative to what?
            //city.ConfigurationPath.RelativePath = "StreamingAssets\mini\mini.cfg";
            // city.LoadData();
        } 
    }
}
