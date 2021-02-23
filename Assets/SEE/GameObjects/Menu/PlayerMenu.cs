using System.Collections.Generic;
using SEE.Controls.Actions;
using SEE.Game.UI;
using SEE.Game.UI.CodeWindow;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.GO.Menu
{
    /// <summary>
    /// Implements the behaviour of the in-game player menu, in which action states can be selected.
    /// </summary>
    public class PlayerMenu : MonoBehaviour
    {
        /// <summary>
        /// The UI object representing the menu the user chooses the action state from.
        /// </summary>
        private SelectionMenu ModeMenu;

        /// <summary>
        /// The UI object representing the indicator, which displays the current action state on the screen.
        /// </summary>
        private ActionStateIndicator Indicator;
        
        /// <summary>
        /// This creates and returns the mode menu, with which you can select the active game mode.
        /// 
        /// Available modes can be found in <see cref="ActionStateType"/>.
        /// </summary>
        /// <param name="attachTo">The game object the menu should be attached to. If <c>null</c>, a
        /// new game object will be created.</param>
        /// <returns>the newly created mode menu component.</returns>
        private static SelectionMenu CreateModeMenu(GameObject attachTo = null)
        {
            Assert.IsTrue(ActionStateType.AllTypes.Count == 8);
            Assert.IsTrue(ActionStateType.Move.Value == 0);
            Assert.IsTrue(ActionStateType.Rotate.Value == 1);
            Assert.IsTrue(ActionStateType.Map.Value == 2);
            Assert.IsTrue(ActionStateType.NewEdge.Value == 3);
            Assert.IsTrue(ActionStateType.NewNode.Value == 4);
            Assert.IsTrue(ActionStateType.EditNode.Value == 5);
            Assert.IsTrue(ActionStateType.ScaleNode.Value == 6);
            Assert.IsTrue(ActionStateType.Delete.Value == 7);

            // IMPORTANT NOTE: Because an ActionState.Type value will be used as an index into 
            // the following field of menu entries, the rank of an entry in this field of entry
            // must correspond to the ActionState.Type value. If this is not the case, we will
            // run into an endless recursion.

            List<ToggleMenuEntry> entries = new List<ToggleMenuEntry>();
            bool first = true;
            foreach (ActionStateType type in ActionStateType.AllTypes)
            {
                entries.Add(new ToggleMenuEntry(
                    active: first,
                    entryAction: () => ActionState.Value = type,
                    exitAction: null,
                    title: type.Name,
                    description: type.Description,
                    entryColor: type.Color,
                    icon: Resources.Load<Sprite>(type.IconPath)
                    ));
                first = false;
            }

            GameObject modeMenuGO = attachTo ?? new GameObject { name = "Mode Menu" };
            SelectionMenu modeMenu = modeMenuGO.AddComponent<SelectionMenu>();
            modeMenu.Title = "Mode Selection";
            modeMenu.Description = "Please select the mode you want to activate.";
            foreach (ToggleMenuEntry entry in entries)
            {
                modeMenu.AddEntry(entry);
            }
            return modeMenu;
        }

        /// <summary>
        /// This creates and returns the <see cref="ActionStateIndicator"/>, which displays the current mode.
        /// The indicator will either be attached to the given GameObject or to a new GameObject if
        /// <paramref name="attachTo"/> is null.
        /// </summary>
        /// <param name="attachTo">The GameObject the indicator shall be attached to.
        /// If <c>null</c>, a new one will be created.</param>
        /// <returns>The newly created ActionStateIndicator.</returns>
        private static ActionStateIndicator CreateActionStateIndicator(GameObject attachTo = null)
        {
            GameObject actionStateGO = attachTo ?? new GameObject {name = "Action State Indicator"};
            ActionStateIndicator indicator = actionStateGO.AddComponent<ActionStateIndicator>();
            return indicator;
        }

        private void Start()
        {
            ModeMenu = CreateModeMenu(gameObject);
            Indicator = CreateActionStateIndicator(gameObject);

            ActionState.OnStateChanged += OnStateChanged;
            Assert.IsTrue(ActionStateType.AllTypes.Count <= 9, 
                          "Only up to 9 (10 if zero is included) entries can be selected via the numbers on the keyboard!");
            
            //TODO remove this! this is just here temporarily to test the code window!
            CodeWindow window = gameObject.AddComponent<CodeWindow>();
            window.Title = "Hello!";
            window.EnterFromFile(@"C:\Users\drasa\Documents\SEE\Data\GXL\TestProjectToDebug\Main.java");
            window.Anchor = GameObject.Find("Implementation");
        }
        
        /// <summary>
        /// Called whenever the action state changes.
        /// This updates the menu to indicate the selected value, and updates the action state indicator.
        /// </summary>
        /// <param name="value">The new action state</param>
        private void OnStateChanged(ActionStateType value)
        {
            ModeMenu.SelectEntry(value.Value);
            Indicator.ChangeState(value);
        }

        /// <summary>
        /// The menu can be enabled/disabled by pressing the space bar.
        /// Additionally, the action state can be selected via number keys.
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
