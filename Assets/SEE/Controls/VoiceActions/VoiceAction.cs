using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEE.Controls.VoiceActions
{

    /// <summary>
    /// The available user actions that can be triggered by voice.
    /// </summary>
    internal enum VoiceAction
    {
        //// <summary>
        /// User asks for help.
        /// </summary>
        Help,
        /// <summary>
        /// User toggles the player menu.
        /// </summary>
        ToggleMenu,
        /// <summary>
        /// Turns on/off the settings menu.
        /// </summary>
        ToggleSettings,
        /// <summary>
        /// Turns on/off the built-in Internet browser.
        /// </summary>
        ToggleBrowser,
        /// <summary>
        /// Turns on/off the mirror.
        /// </summary>
        ToggleMirror,
        ToggleCharts,
        /// <summary>
        /// Undoes the last action.
        /// </summary>
        Undo,
        /// <summary>
        /// Re-does the last action.
        /// </summary>
        Redo,
        /// <summary>
        /// Opens/closes the configuration menu.
        /// </summary>
        ConfigMenu,
        /// <summary>
        /// Opens/closes the tree view window.
        /// </summary>
        TreeView,
        /// <summary>
        /// Toggles the visibility of all edges of a hovered code city.
        /// </summary>
        ToggleEdges,
        /// <summary>
        /// Cancels an action.
        /// </summary>
        Cancel,
        /// <summary>
        /// Toggles between pointing.
        /// </summary>
        Pointing,
        /// <summary>
        /// The previous element in the animation is to be shown.
        /// </summary>
        Previous,
        /// <summary>
        /// The next element in the animation is to be shown.
        /// </summary>
        Next,
        /// <summary>
        /// Opens the text chat. --------> Needs TODO
        /// </summary>
        ToggleTextChat,
        /// <summary>
        /// Closes all open notifications.
        /// </summary>
        CloseNotifications,
        /// <summary>
        /// Toggles the face camera.
        /// </summary>
        ToggleFaceCam,
        /// <summary>
        /// Toggles the position of the FaceCam on the player's face.
        /// </summary>
        ToggleFaceCamPosition,
        /// <summary>
        /// Opens/closes the drawable manager view.
        /// </summary>
        DrawableManagerView,
        /// <summary>
        /// Opens the context Menu of a GameObject
        /// </summary>
        OpenContextMenu

    }

}

