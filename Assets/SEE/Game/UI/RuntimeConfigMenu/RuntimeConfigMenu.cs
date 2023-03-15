using SEE.Controls;
using SEE.Game.City;
using UnityEngine;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeConfigMenu : MonoBehaviour
    {
        private RuntimeTabMenu Menu;
        
        void Start()
        {
            Menu = gameObject.AddComponent<RuntimeTabMenu>();
            Menu.Title = "City Configuration";
            Menu.Description = "Configure the city however you like.";
            Menu.HideAfterSelection = false;
        }
        
        void Update()
        {
            if (SEEInput.ToggleConfigMenu())
            {
                Menu.ToggleMenu();
            }
        }
    }
}
