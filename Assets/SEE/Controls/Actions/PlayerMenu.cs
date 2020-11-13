using SEE.GO;
using SEE.GO.Menu;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Creates the in-game menu.
    /// 
    /// NOTE: This class is currently just a stub.
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
        /// Creates the <see cref="menu"/> if it does not exist yet.
        /// Sets <see cref="mainCamera"/>.
        /// </summary>
        protected virtual void Start()
        {
            GameObject menu = MenuFactory.CreateMenu(EntriesParameter, Radius, Depth);
        }

        private static void MoveOn()
        {
            Debug.Log("MoveOn\n");
        }
        private static void MoveOff()
        {
            Debug.Log("MoveOff\n");
        }

        private static void AddOn()
        {
            Debug.Log("EntryBOn\n");
        }

        private static void AddOff()
        {
            Debug.Log("AddOn\n");
        }

        private static void DeleteOn()
        {
            Debug.Log("DeleteOn\n");
        }

        private static void DeleteOff()
        {
            Debug.Log("DeleteOff.\n");
        }

        /// <summary>
        /// Path of the prefix for the sprite to be instantiated for the menu entries.
        /// </summary>
        private const string menuEntrySprite = "Icons/Circle";

        /// <summary>
        /// The color gold.
        /// </summary>
        private static readonly Color gold = new Color(1.0F, 0.84F, 0.0F);

        /// <summary>
        /// Returns given <paramref name="color"/> lightened by 50%.
        /// </summary>
        /// <param name="color">base color to be lightened</param>
        /// <returns>given <paramref name="color"/> lightened by 50%</returns>
        private static Color Lighter(Color color)
        {
            return Color.Lerp(color, Color.white, 0.5f); // To lighten by 50 %
        }

        private static readonly MenuDescriptor[] EntriesParameter =
            {
                new MenuDescriptor(label: "Move",
                                   spriteFile: menuEntrySprite,
                                   activeColor: Color.red,
                                   inactiveColor: Lighter(Color.red),
                                   entryOn: MoveOn,
                                   entryOff: MoveOff,
                                   isTransient: false),
                new MenuDescriptor(label: "Add",
                                   spriteFile: menuEntrySprite,
                                   activeColor: gold,
                                   inactiveColor: Lighter(gold),
                                   entryOn: AddOn,
                                   entryOff: AddOff,
                                   isTransient: true),
                new MenuDescriptor(label: "Delete",
                                   spriteFile: menuEntrySprite,
                                   activeColor: Color.green,
                                   inactiveColor: Lighter(Color.green),
                                   entryOn: DeleteOn,
                                   entryOff: DeleteOff,
                                   isTransient: true),
            };
    }
}