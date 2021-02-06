using System;
using SEE.Controls.Actions;
using SEE.Game.UI;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.GO.Menu
{
    /// <summary>
    /// Implements the behaviour of the in-game circular menu.
    /// </summary>
    public class ActionSelectionMenu : MonoBehaviour
    {

        private readonly ActionState.Type[] actionStateTypes = (ActionState.Type[])Enum.GetValues(typeof(ActionState.Type));

        private SelectionMenu ModeMenu;

        private void Start()
        {
            if (!MenuFactory.CreateModeMenu(gameObject).TryGetComponentOrLog(out ModeMenu))
            {
                Destroyer.DestroyComponent(this);
            }

            ActionState.OnStateChanged += OnStateChanged;
            Assert.IsTrue(actionStateTypes.Length <= 9, 
                          "Only up to 9 (10 if zero is included) entries can be selected via the numbers on the keyboard!");
        }
        
        private void OnStateChanged(ActionState.Type value)
        {
            ModeMenu.SelectEntry((int) value);
        }

        /// <summary>
        /// The menu can be enabled/disabled by pressing the space bar.
        /// </summary>
        private void Update()
        {
            // Select action state via numbers on the keyboard
            for (int i = 0; i < ModeMenu.Entries.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    ModeMenu.SelectEntry(i);
                    break;
                }
            }

            // space bar toggles menu            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ModeMenu.ToggleMenu();
            }
        }
    }
}