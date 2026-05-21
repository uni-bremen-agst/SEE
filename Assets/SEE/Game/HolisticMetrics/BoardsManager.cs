using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Controls.Actions.HolisticMetrics;
using SEE.Game.HolisticMetrics.ActionHelpers;
using SEE.UI.Notification;
using UnityEngine;
using SEE.Utils;
using Object = UnityEngine.Object;

namespace SEE.Game.HolisticMetrics
{
    /// <summary>
    /// This class manages all metric boards.
    /// </summary>
    public static class BoardsManager
    {
        /// <summary>
        /// The board prefab we will be instantiating here.
        /// </summary>
        private static readonly GameObject boardPrefab =
            Resources.Load<GameObject>("Prefabs/HolisticMetrics/SceneComponents/MetricsBoard");

        /// <summary>
        /// List of all the <see cref="WidgetsManager"/>s that this manager manages (there should not be any
        /// <see cref="WidgetsManager"/>s in the scene that are not in this list).
        /// </summary>
        private static readonly List<WidgetsManager> widgetsManagers = new();

        /// <summary>
        /// Creates a new metrics board and puts its <see cref="WidgetsManager"/> into the list of
        /// <see cref="WidgetsManager"/>s.
        /// </summary>
        /// <param name="boardConfig">The board configuration for the new board.</param>
        internal static void Create(BoardConfig boardConfig)
        {
            widgetsManagers.RemoveAll(x => x == null); // remove stale managers
            if (widgetsManagers.Any(widgetsManager => widgetsManager.GetTitle().Equals(boardConfig.Title)))
            {
                ShowNotification.Error("Cannot create that board", "The name has to be unique.");
                return;
            }

            GameObject newBoard = Object.Instantiate(boardPrefab, boardConfig.Position, boardConfig.Rotation);
            WidgetsManager newWidgetsManager = newBoard.GetComponent<WidgetsManager>();
            newWidgetsManager.SetTitle(boardConfig.Title);
            foreach (WidgetConfig widgetConfiguration in boardConfig.WidgetConfigs)
            {
                newWidgetsManager.Create(widgetConfiguration);
            }

            widgetsManagers.Add(newWidgetsManager);
        }

        /// <summary>
        /// Deletes a metrics board identified by its name.
        /// </summary>
        /// <param name="boardName">The name/title of the board to delete.</param>
        internal static void Delete(string boardName)
        {
            WidgetsManager widgetsManager = Find(boardName);
            if (widgetsManager is null)
            {
                Debug.LogError($"Tried to delete a board named {boardName} that does not seem to exist\n");
                return;
            }
            Destroyer.Destroy(widgetsManager.gameObject);
            widgetsManagers.Remove(widgetsManager);
            Destroyer.Destroy(widgetsManager);
        }

        /// <summary>
        /// Changes the position and rotation of a metrics board to the new position and rotation from the parameters.
        /// </summary>
        /// <param name="boardName">The name that identifies the board.</param>
        /// <param name="position">The new position of the board.</param>
        /// <param name="rotation">The new rotation of the board.</param>
        internal static void Move(string boardName, Vector3 position, Quaternion rotation)
        {
            WidgetsManager widgetsManager = Find(boardName);
            if (widgetsManager == null)
            {
                Debug.LogError($"Tried to move a board named {boardName} that does not seem to exist\n");
                return;
            }
            Transform boardTransform = widgetsManager.transform;
            boardTransform.position = position;
            boardTransform.rotation = rotation;
        }

        /// <summary>
        /// Toggles the small buttons underneath the boards that allow the player to drag the boards around.
        /// </summary>
        /// <param name="enable">Whether to enable the move buttons.</param>
        internal static void ToggleMoving(bool enable)
        {
            foreach (WidgetsManager controller in widgetsManagers)
            {
                controller.ToggleMoving(enable);
            }
        }

        /// <summary>
        /// If any of the widget managers this manager manages has a movement that has not yet been fetched
        /// by <see cref="MoveBoardAction"/>,
        /// </summary>
        /// <param name="boardName">The name of the board; undefined if none was moved.</param>
        /// <param name="oldPosition">The position of the board before the movement; undefined if none was moved.</param>
        /// <param name="newPosition">The new position of the board; undefined if none was moved.</param>
        /// <param name="oldRotation">The rotation of the board before the movement; undefined if none was moved.</param>
        /// <param name="newRotation">The new rotation of the board; undefined if none was moved.</param>
        /// <returns>Whether the any of the boards managed by this manager has a movement that has not yet been fetched by
        /// <see cref="MoveBoardAction"/>.</returns>
        internal static bool TryGetMovement(out string boardName, out Vector3 oldPosition, out Vector3 newPosition,
            out Quaternion oldRotation, out Quaternion newRotation)
        {
            foreach (WidgetsManager widgetsManager in widgetsManagers)
            {
                if (widgetsManager.TryGetMovement(out oldPosition, out newPosition, out oldRotation, out newRotation))
                {
                    boardName = widgetsManager.GetTitle();
                    return true;
                }
            }

            boardName = null;
            oldPosition = Vector3.zero;
            newPosition = Vector3.zero;
            oldRotation = Quaternion.identity;
            newRotation = Quaternion.identity;
            return false;
        }

        /// <summary>
        /// Finds a board (its <see cref="WidgetsManager"/>, actually) by its name.
        /// </summary>
        /// <param name="boardName">The name to look for.</param>
        /// <returns>Returns the desired <see cref="WidgetsManager"/> if it exists or null if it doesn't.</returns>
        internal static WidgetsManager Find(string boardName)
        {
            return widgetsManagers.Find(manager => manager.GetTitle().Equals(boardName));
        }

        /// <summary>
        /// Returns the names of all <see cref="WidgetsManager"/>s. The names can also be used to identify the
        /// <see cref="WidgetsManager"/>s because they have to be unique.
        /// </summary>
        /// <returns>The names of all <see cref="WidgetsManager"/>s.</returns>
        internal static string[] GetNames()
        {
            string[] names = new string[widgetsManagers.Count];
            for (int i = 0; i < widgetsManagers.Count; i++)
            {
                names[i] = widgetsManagers[i].GetTitle();
            }

            return names;
        }

        /// <summary>
        /// Updates all the widgets on all the metrics boards.
        /// </summary>
        internal static void OnGraphDraw()
        {
            foreach (WidgetsManager widgetsManager in widgetsManagers)
            {
                widgetsManager.OnGraphDraw();
            }
        }

        /// <summary>
        /// Depending on the value of <paramref name="enable"/>, <see cref="WidgetAdder"/>s will be added or removed on
        /// all boards.
        /// </summary>
        /// <param name="enable">Whether there should be WidgetAdders on all boards.</param>
        internal static void ToggleWidgetAdders(bool enable)
        {
            if (enable)
            {
                foreach (WidgetsManager controller in widgetsManagers)
                {
                    controller.gameObject.AddComponent<WidgetAdder>();
                }
            }
            else
            {
                foreach (WidgetsManager widgetsManager in widgetsManagers)
                {
                    Destroyer.Destroy(widgetsManager.gameObject.GetComponent<WidgetAdder>());
                }
            }
        }

        /// <summary>
        /// Fetches the position where a widget should be added.
        /// </summary>
        /// <param name="boardName">The name of the board on which the widget should be added. If there is no such
        /// board, this will be null.</param>
        /// <param name="position">The position at which the widget should be added. If there is no such position, this
        /// should be considered undefined.</param>
        /// <returns>Whether a position was fetched successfully.</returns>
        internal static bool TryGetWidgetAdditionPosition(out string boardName, out Vector3 position)
        {
            foreach (WidgetsManager widgetsManager in widgetsManagers)
            {
                if (widgetsManager.gameObject.GetComponent<WidgetAdder>().GetPosition(out position))
                {
                    boardName = widgetsManager.GetTitle();
                    return true;
                }
            }

            boardName = null;
            position = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Toggles the moveability of all widgets.
        /// </summary>
        /// <param name="enable">Whether the widgets should be movable.</param>
        internal static void ToggleWidgetsMoving(bool enable)
        {
            foreach (WidgetsManager manager in widgetsManagers)
            {
                manager.ToggleWidgetsMoving(enable);
            }
        }

        /// <summary>
        /// Checks whether one of the widgets on one of the boards managed by this class has a movement that hasn't yet
        /// been fetched by the <see cref="MoveWidgetAction"/>.
        /// </summary>
        /// <param name="originalPosition">The position of the widget before the movement.</param>
        /// <param name="newPosition">The position of the widget after the movement.</param>
        /// <param name="containingBoardName">The title of the board that contains the widget.</param>
        /// <param name="widgetID">The ID of the widget.</param>
        /// <returns>Whether one of the widgets on one of the boards managed by this class has a movement that hasn't yet
        /// been fetched by the <see cref="MoveWidgetAction"/>.</returns>
        internal static bool TryGetWidgetMovement(
            out Vector3 originalPosition,
            out Vector3 newPosition,
            out string containingBoardName,
            out Guid widgetID)
        {
            foreach (WidgetsManager widgetsManager in widgetsManagers)
            {
                if (widgetsManager.TryGetWidgetMovement(out originalPosition, out newPosition, out widgetID))
                {
                    containingBoardName = widgetsManager.GetTitle();
                    return true;
                }
            }

            originalPosition = Vector3.zero;
            newPosition = Vector3.zero;
            containingBoardName = null;
            widgetID = Guid.NewGuid();
            return false;
        }

        /// <summary>
        /// Adds/removes all <see cref="WidgetDeleter"/> components to/from all widgets on all boards.
        /// </summary>
        /// <param name="enable">Whether we want to listen for clicks on widgets for deletion.</param>
        internal static void ToggleWidgetDeleting(bool enable)
        {
            foreach (WidgetsManager widgetsManager in widgetsManagers)
            {
                widgetsManager.ToggleWidgetDeleting(enable);
            }
        }

        /// <summary>
        /// Tries to get a pending deletion of a widget from any of the boards managed by this manager.
        /// </summary>
        /// <param name="boardName">The name of the board that contains the widget that's to be deleted.</param>
        /// <param name="widgetConfig">The configuration of the widget that's to be deleted.</param>
        /// <returns>Whether a pending deletion was found.</returns>
        internal static bool TryGetWidgetDeletion(out string boardName, out WidgetConfig widgetConfig)
        {
            foreach (WidgetsManager widgetsManager in widgetsManagers)
            {
                if (widgetsManager.GetWidgetDeletion(out widgetConfig))
                {
                    boardName = widgetsManager.GetTitle();
                    return true;
                }
            }

            boardName = null;
            widgetConfig = null;
            return false;
        }
    }
}
