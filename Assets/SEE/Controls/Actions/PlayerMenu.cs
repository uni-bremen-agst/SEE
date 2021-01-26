using SEE.GO;
using SEE.GO.Menu;
using System;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Creates the in-game menu with menu entries for moving a node within a 
    /// code city, mapping a node between two code cities, and undoing these
    /// two actions.
    /// </summary>
    public class PlayerMenu : MonoBehaviour
    {
        /// <summary>
        /// Radius of the menu.
        /// </summary>
        [Tooltip("The radius of the circular menu.")]
        [Range(0, 2)]
        public float Radius = 0.3f;

        /// <summary>
        /// Radius of the menu.
        /// </summary>
        [Tooltip("The depth of the circular menu (z axis).")]
        [Range(0, 0.1f)]
        public float Depth = 0.01f;

        /// <summary>
        /// The player actions attached to the gameObject. The selection of 
        /// menu entries will be forwarded to this component.
        /// </summary>
        private PlayerActions playerActions;

        /// <summary>
        /// Creates the <see cref="menu"/> if it does not exist yet.
        /// Sets <see cref="mainCamera"/>.
        /// </summary>
        protected virtual void Start()
        {
            MenuFactory.CreateMenu(EntriesParameter, Radius, Depth);
            if (!gameObject.TryGetComponent<PlayerActions>(out playerActions))
            {
                Debug.LogErrorFormat("Player {0} does not have PlayerActions.\n", name);
                enabled = false;
            }
        }

        /// <summary>
        /// Called from the menu as a callback when the user selects the move menu entry.
        /// </summary>
        private void MoveOn()
        {
            ActionState.Value = ActionState.Type.Move;
        }

        /// <summary>
        /// Called from the menu as a callback when the user selects the rotate menu entry.
        /// </summary>
        private void RotateOn()
        {
            ActionState.Value = ActionState.Type.Rotate;
        }

        /// <summary>
        /// Called from the menu as a callback when the user selects the map menu entry.
        /// </summary>
        private void MapOn()
        {
            ActionState.Value = ActionState.Type.Map;
        }

        /// <summary>
        /// Called from the menu as a callback when the user selects the map menu entry.
        /// Passes the map request on to <see cref="playerActions"/>.
        /// </summary>
        private void NewNodeOn()
        {
            playerActions.NewNode();
        }

        /// <summary>
        /// Called from the menu as callback when the user selects the map menu entry.
        /// Passes the map request on to <see cref="playerActions"/>.
        /// </summary>
        private void ScaleNodeOn()
        {
            playerActions.ScaleNode();
        }

        private void EditNodeOn()
        {
            playerActions.EditNode();
        }

        /// <summary>
        /// Path of the prefix for the sprite to be instantiated for the menu entries.
        /// </summary>
        private const string menuEntrySprite = "Icons/Circle";

        /// <summary>
        /// Returns given <paramref name="color"/> lightened by 50%.
        /// </summary>
        /// <param name="color">base color to be lightened</param>
        /// <returns>given <paramref name="color"/> lightened by 50%</returns>
        private static Color Lighter(Color color)
        {
            return Color.Lerp(color, Color.white, 0.5f); // To lighten by 50 %
        }

        /// <summary>
        /// The entries of the menu.
        /// </summary>
        private MenuDescriptor[] EntriesParameter;

        private void Awake()
        {
            EntriesParameter = new MenuDescriptor[]
            {
                // Moving a node within a graph
                new MenuDescriptor(label: "Move",
                                   spriteFile: menuEntrySprite,
                                   activeColor: Color.red,
                                   inactiveColor: Lighter(Color.red),
                                   entryOn: MoveOn,
                                   entryOff: Hide,
                                   isTransient: true),
                // Rotating everything around the selected node within a graph
                new MenuDescriptor(label: "Rotate",
                                   spriteFile: menuEntrySprite,
                                   activeColor: Color.blue,
                                   inactiveColor: Lighter(Color.blue),
                                   entryOn: RotateOn,
                                   entryOff: null,
                                   isTransient: true),
                // Mapping a node from one graph to another graph
                new MenuDescriptor(label: "Map",
                                   spriteFile: menuEntrySprite,
                                   activeColor: Color.green,
                                   inactiveColor: Lighter(Color.green),
                                   entryOn: MapOn,
                                   entryOff: null,
                                   isTransient: true),
                //Creates a new Node
                 new MenuDescriptor(label: "New Node",
                                   spriteFile: menuEntrySprite,
                                   activeColor: Color.green,
                                   inactiveColor: Lighter(Color.green),
                                   entryOn: NewNodeOn,
                                   entryOff: Hide,
                                   isTransient: true),
                 //Starts the Scaling Mode
                 new MenuDescriptor(label: "Scale Node",
                                   spriteFile: menuEntrySprite,
                                   activeColor: Color.green,
                                   inactiveColor: Lighter(Color.green),
                                   entryOn: ScaleNodeOn,
                                   entryOff: Hide,
                                   isTransient: true),
                 //Starts the Edit-Node-Mode
                    new MenuDescriptor(label: "Edit Node",
                                   spriteFile: menuEntrySprite,
                                   activeColor: Color.yellow,
                                   inactiveColor: Lighter(Color.yellow),
                                   entryOn: EditNodeOn,
                                   entryOff: Hide,
                                   isTransient: true)

            };
        }

        protected void Hide()
        {
            try
            {
                CircularMenu circularMenu = GameObject.Find("0").GetComponent<CircularMenu>();
                circularMenu.Off();
                circularMenu.MenuIsOn = false;
            }
            catch (Exception)
            {
            }
        }
    }
}