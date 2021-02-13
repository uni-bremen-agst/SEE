using System;
using SEE.Controls.Actions;
using SEE.Game.UI;
using SEE.Utils;
using UnityEditor;
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
        /// An array of all possible action state types the player can be in.
        /// </summary>
        private readonly ActionState.Type[] actionStateTypes = (ActionState.Type[])Enum.GetValues(typeof(ActionState.Type));

        /// <summary>
        /// The UI object representing the menu the user chooses the action state from.
        /// </summary>
        private SelectionMenu ModeMenu;
        
        /// <summary>
        /// This creates and returns the mode menu, with which you can select the active game mode.
        /// 
        /// Available modes are:
        /// - Move
        /// - Map
        /// - Draw Edges
        /// - Rotate
        /// - Delete
        /// </summary>
        /// <param name="attachTo">The game object the menu should be attached to. If <c>null</c>, a
        /// new game object will be created.</param>
        /// <returns>the newly created mode menu game object, or if it wasn't null
        /// <paramref name="attachTo"/> with the mode menu attached.</returns>
        private static GameObject CreateModeMenu(GameObject attachTo = null)
        {
            UnityEngine.Assertions.Assert.IsTrue(System.Enum.GetNames(typeof(ActionState.Type)).Length == 8);
            UnityEngine.Assertions.Assert.IsTrue((int)ActionState.Type.Move == 0);
            UnityEngine.Assertions.Assert.IsTrue((int)ActionState.Type.Rotate == 1);
            UnityEngine.Assertions.Assert.IsTrue((int)ActionState.Type.Map == 2);
            UnityEngine.Assertions.Assert.IsTrue((int)ActionState.Type.NewEdge == 3);
            UnityEngine.Assertions.Assert.IsTrue((int)ActionState.Type.NewNode == 4);
            UnityEngine.Assertions.Assert.IsTrue((int)ActionState.Type.EditNode == 5);
            UnityEngine.Assertions.Assert.IsTrue((int)ActionState.Type.ScaleNode == 6);
            UnityEngine.Assertions.Assert.IsTrue((int)ActionState.Type.Delete == 7);

            // IMPORTANT NOTE: Because a ActionState.Type value will be used as an index into 
            // the following field of menu entries, the rank of an entry in this field of entry
            // must correspond to the ActionState.Type value. If this is not the case, we will
            // run into an endless recursion.

            ToggleMenuEntry[] entries = {
                new ToggleMenuEntry(
                    active: true,
                    entryAction: () => ActionState.Value = ActionState.Type.Move,
                    exitAction: null,
                    title: "Move",
                    description: "Move a node within a graph",
                    entryColor: Color.red.Darker(),
                    icon: AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Materials/Charts/MoveIcon.png")
                    ),
                new ToggleMenuEntry(
                    active: false,
                    entryAction: () => ActionState.Value = ActionState.Type.Rotate,
                    exitAction: null,
                    title: "Rotate",
                    description: "Rotate everything around the selected node within a graph",
                    entryColor: Color.blue.Darker(),
                    icon: AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Modern UI Pack/Textures/Icon/Navigation/Refresh.png")
                    ),
                new ToggleMenuEntry(
                    active: false,
                    entryAction: () => ActionState.Value = ActionState.Type.Map,
                    exitAction: null,
                    title: "Map",
                    description: "Map a node from one graph to another graph",
                    entryColor: Color.green.Darker(),
                    icon: AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Modern UI Pack/Textures/Icon/Map/Map.png")
                    ),
                new ToggleMenuEntry(
                    active: false,
                    entryAction: () => ActionState.Value = ActionState.Type.NewEdge,
                    exitAction: null,
                    title: "New Edge",
                    description: "Draw a new edge between two nodes",
                    entryColor: Color.green.Darker(),
                    icon: AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Modern UI Pack/Textures/Icon/Navigation/Minus.png")
                    ),
                 new ToggleMenuEntry(
                    active: false,
                    entryAction: () => ActionState.Value = ActionState.Type.NewNode,
                    exitAction: null,
                    title: "New Node",
                    description: "Creates a new node",
                    entryColor: Color.green.Darker(),
                    icon: AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Modern UI Pack/Textures/Icon/Navigation/Plus.png")
                    ),
                 new ToggleMenuEntry(
                    active: false,
                    entryAction: () => ActionState.Value = ActionState.Type.EditNode,
                    exitAction: null,
                    title: "Edit Node",
                    description: "Edits a node",
                    entryColor: Color.green.Darker(),
                    icon: AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Modern UI Pack/Textures/Icon/Common/Settings.png")
                    ),
                 new ToggleMenuEntry(
                    active: false,
                    entryAction: () => ActionState.Value = ActionState.Type.ScaleNode,
                    exitAction: null,
                    title: "Scale Node",
                    description: "Scales a node",
                    entryColor: Color.green.Darker(),
                    icon: AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Modern UI Pack/Textures/Icon/Common/Crop.png")
                    ),
                new ToggleMenuEntry(
                    active: false,
                    entryAction: () => ActionState.Value = ActionState.Type.Delete,
                    exitAction: null,
                    title: "Delete",
                    description: "Delete nodes and edges",
                    entryColor: Color.yellow.Darker(),
                    icon: AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Modern UI Pack/Textures/Icon/Common/Trash.png")
                    ),
            };
            
            GameObject modeMenuGO = attachTo ?? new GameObject { name = "Mode Menu" };
            SelectionMenu modeMenu = modeMenuGO.AddComponent<SelectionMenu>();
            modeMenu.Title = "Mode Selection";
            modeMenu.Description = "Please select the mode you want to activate.";
            foreach (ToggleMenuEntry entry in entries)
            {
                modeMenu.AddEntry(entry);
            }

            return modeMenuGO;
        }

        private void Start()
        {
            if (!CreateModeMenu(gameObject).TryGetComponentOrLog(out ModeMenu))
            {
                Destroyer.DestroyComponent(this);
            }

            ActionState.OnStateChanged += OnStateChanged;
            Assert.IsTrue(actionStateTypes.Length <= 9, 
                          "Only up to 9 (10 if zero is included) entries can be selected via the numbers on the keyboard!");
        }
        
        /// <summary>
        /// Called whenever the action state changes.
        /// This updates the menu to indicate the selected value.
        /// </summary>
        /// <param name="value">The new action state</param>
        private void OnStateChanged(ActionState.Type value)
        {
            ModeMenu.SelectEntry((int) value);
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
