using UnityEngine;

namespace SEE.Game.UI
{
    /// <summary>
    /// This class is responsible for creating various concrete menus in SEA.
    /// </summary>
    public static class MenuFactory
    {
        /// <summary>
        /// This creates and returns the mode menu, with which you can select the active game mode.
        /// 
        /// Available modes are:
        /// - Browsing
        /// - Moving
        /// - Mapping
        /// - Drawing Edges
        /// - Rotating
        /// </summary>
        /// <param name="attachTo">The game object the menu should be attached to. If <c>null</c>, a
        /// new game object will be created.</param>
        /// <returns>the newly created mode menu game object, or if it wasn't null
        /// <paramref name="attachTo"/> with the mode menu attached.</returns>
        public static GameObject CreateModeMenu(GameObject attachTo = null)
        {
                // // Rotating everything around the selected node within a graph
                // new MenuDescriptor(label: "Rotate",
                //                    spriteFile: menuEntrySprite,
                //                    activeColor: Color.blue,
                //                    inactiveColor: Lighter(Color.blue),
                //                    entryOn: RotateOn,
                //                    entryOff: null,
                //                    isTransient: true),
                // // Mapping a node from one graph to another graph
                // new MenuDescriptor(label: "Map",
                //                    spriteFile: menuEntrySprite,
                //                    activeColor: Color.green,
                //                    inactiveColor: Lighter(Color.green),
                //                    entryOn: MapOn,
                //                    entryOff: null,
                //                    isTransient: true),
                // // Drawing a new edge between two gameobjects
                // new MenuDescriptor(label: "DrawEdge",
                //                    spriteFile: menuEntrySprite,
                //                    activeColor: Color.green,
                //                    inactiveColor: Lighter(Color.green),
                //                    entryOn: DrawEdgeOn,
                //                    entryOff: null,
                //                    isTransient: true),
            ToggleMenuEntry[] entries = {
                new ToggleMenuEntry(
                    active: true,
                    entryAction: actions.Browse,
                    exitAction: null,
                    title: "Browse",
                    description: "Normal browsing mode",
                    entryColor: Color.blue
                    ),
                new ToggleMenuEntry(
                    active: false,
                    entryAction: actions.Move,
                    exitAction: null,
                    title: "Move",
                    description: "Move a node within a graph",
                    entryColor: Color.red
                    ),
                new ToggleMenuEntry(
                    active: false,
                    entryAction: actions.Map,
                    exitAction: null,
                    title: "Map",
                    description: "Map a node from one graph to another graph",
                    entryColor: Color.green
                    ),
            };
            
            GameObject modeMenuGO = attachTo ?? new GameObject { name = "Mode Menu" };
            SelectionMenu modeMenu = modeMenuGO.AddComponent<SelectionMenu>();
            foreach (ToggleMenuEntry entry in entries)
            {
                modeMenu.AddEntry(entry);
            }

            return modeMenuGO;
        }
    }
}