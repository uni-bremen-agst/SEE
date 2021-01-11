using System.Linq;
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
        /// Available modes are:
        /// - Browsing
        /// - Moving
        /// - Mapping
        /// </summary>
        /// <returns>the newly created mode menu game object.</returns>
        public static GameObject CreateModeMenu()
        {
            //TODO Set entry/exit actions to methods specified in comment.
            // For this, refer to file "PlayerMenu.cs", as this menu is supposed to be the replacement for the
            // menu constructed in there. The referenced methods are also present in that class.
            
            ToggleMenuEntry[] entries = {
                new ToggleMenuEntry(
                    active: true,
                    entryAction: () => Debug.Log("TODO"), // Should be BrowseOn
                    exitAction: null,
                    title: "Browse",
                    description: "Normal browsing mode",
                    color: Color.blue
                    ),
                new ToggleMenuEntry(
                    active: false,
                    entryAction: () => Debug.Log("TODO"), // Should be MoveOn
                    exitAction: null,
                    title: "Move",
                    description: "Move a node within a graph",
                    color: Color.red
                    ),
                new ToggleMenuEntry(
                    active: false,
                    entryAction: () => Debug.Log("TODO"), // Should be MapOn
                    exitAction: null,
                    title: "Map",
                    description: "Map a node from one graph to another graph",
                    color: Color.green
                    ),
            };
            
            GameObject modeMenuGO = new GameObject();
            SelectionMenu modeMenu = modeMenuGO.AddComponent<SelectionMenu>();
            entries.ToList().ForEach(x => modeMenu.AddEntry(x));
            return modeMenuGO;
        }
    }
}