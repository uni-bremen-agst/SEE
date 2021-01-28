using SEE.GO;
using SEE.GO.Menu;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Creates the in-game menu with menu entries for moving a node within a 
    /// code city, mapping a node between two code cities, and undoing these
    /// two actions.
    /// </summary>
    public sealed class PlayerMenu : MonoBehaviour
    {
        /// <summary>
        /// Path of the prefix for the sprite to be instantiated for the menu entries.
        /// </summary>
        private const string menuEntrySprite = "Icons/Circle";

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
        /// Creates the <see cref="menu"/> if it does not exist yet.
        /// </summary>
        private void Start()
        {
            UnityEngine.Assertions.Assert.IsTrue(System.Enum.GetNames(typeof(ActionState.Type)).Length == 4);
            UnityEngine.Assertions.Assert.IsTrue((int)ActionState.Type.Move == 0);
            UnityEngine.Assertions.Assert.IsTrue((int)ActionState.Type.Rotate == 1);
            UnityEngine.Assertions.Assert.IsTrue((int)ActionState.Type.Map == 2);
            UnityEngine.Assertions.Assert.IsTrue((int)ActionState.Type.DrawEdge == 3);

            MenuDescriptor[] menuDescriptors = new MenuDescriptor[]
            {
                // Moving a node within a graph
                new MenuDescriptor(label: "Move",
                                   spriteFile: menuEntrySprite,
                                   activeColor: Color.red,
                                   inactiveColor: Lighter(Color.red),
                                   entryOn: MoveOn,
                                   entryOff: null,
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
                // Drawing a new edge between two gameobjects
                new MenuDescriptor(label: "DrawEdge",
                                   spriteFile: menuEntrySprite,
                                   activeColor: Color.green,
                                   inactiveColor: Lighter(Color.green),
                                   entryOn: DrawEdgeOn,
                                   entryOff: null,
                                   isTransient: true),
            };
            MenuFactory.CreateMenu(menuDescriptors, Radius, Depth);
        }

        private void MoveOn() => ActionState.Value = ActionState.Type.Move;
        private void RotateOn() => ActionState.Value = ActionState.Type.Rotate;
        private void MapOn() => ActionState.Value = ActionState.Type.Map;
        private void DrawEdgeOn() => ActionState.Value = ActionState.Type.DrawEdge;

        /// <summary>
        /// Returns given <paramref name="color"/> lightened by 50%.
        /// </summary>
        /// <param name="color">base color to be lightened</param>
        /// <returns>given <paramref name="color"/> lightened by 50%</returns>
        private static Color Lighter(Color color)
        {
            return Color.Lerp(color, Color.white, 0.5f); // To lighten by 50 %
        }
    }
}
