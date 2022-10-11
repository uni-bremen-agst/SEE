using System.Collections.Generic;
using System.Linq;
using SEE.Game.UI.Notification;
using UnityEngine;
using SEE.Game.HolisticMetrics.Components;

namespace SEE.Game.HolisticMetrics
{
    /// <summary>
    /// This class manages all metrics boards.
    /// </summary>
    public static class BoardsManager
    {
        /// <summary>
        /// The board prefab we will be instantiating here.
        /// </summary>
        private static readonly GameObject boardPrefab = 
            Resources.Load<GameObject>("Prefabs/HolisticMetrics/SceneComponents/MetricsBoard");

        private static bool movingEnabled;

        /// <summary>
        /// List of all the BoardControllers that this manager manages (there should not be any BoardControllers in the
        /// scene that are not in this list).
        /// </summary>
        private static readonly List<BoardController> boardControllers = new List<BoardController>();

        /// <summary>
        /// Creates a new metrics board and puts its BoardController into the list of BoardControllers.
        /// </summary>
        /// <param name="boardConfiguration">The board configuration for the new board.</param>
        internal static void CreateNewBoard(BoardConfiguration boardConfiguration)
        {
            bool nameExists = boardControllers.Any(boardController =>
                boardController.GetTitle().Equals(boardConfiguration.Title));
            if (nameExists)
            {
                ShowNotification.Error("Cannot create that board", "The name has to be unique.");
                return;
            }

            GameObject newBoard = Object.Instantiate(
                boardPrefab, 
                boardConfiguration.Position, 
                boardConfiguration.Rotation);
            
            BoardController newBoardController = newBoard.GetComponent<BoardController>();

            // Set the title of the new board
            newBoardController.SetTitle(boardConfiguration.Title);

            // Add the widgets to the new board
            foreach (WidgetConfiguration widgetConfiguration in boardConfiguration.WidgetConfigurations)
            {
                    newBoardController.AddMetric(widgetConfiguration);
            }

            boardControllers.Add(newBoardController);
        }
        
        internal static void Delete(string boardName)
        {
            BoardController boardController = FindControllerByName(boardName);
            Object.Destroy(boardController.gameObject);
            boardControllers.Remove(boardController);
            Object.Destroy(boardController);
        }

        internal static bool ToggleMoving()
        {
            movingEnabled = !movingEnabled;
            foreach (BoardController controller in boardControllers)
            {
                controller.ToggleMoving(movingEnabled);
            }
            return movingEnabled;
        }

        /// <summary>
        /// Finds a MetricsBoard GameObject by its name.
        /// </summary>
        /// <param name="boardName">The name to look for.</param>
        /// <returns>Returns the desired GameObject if it exists or null if it doesn't.</returns>
        internal static BoardController FindControllerByName(string boardName)
        {
            return boardControllers.Find(boardController => boardController.GetTitle().Equals(boardName));
        }

        /// <summary>
        /// Returns the names of all BoardControllers. The names can also be used to identify the BoardControllers
        /// because they have to be unique.
        /// </summary>
        /// <returns>The names of all BoardControllers.</returns>
        internal static string[] GetNames()
        {
            string[] names = new string[boardControllers.Count];
            for (int i = 0; i < boardControllers.Count; i++)
            {
                names[i] = boardControllers[i].GetTitle();
            }

            return names;
        }

        /// <summary>
        /// Updates all the widgets on all the metrics boards.
        /// </summary>
        internal static void OnGraphLoad()
        {
            foreach (BoardController boardController in boardControllers)
            {
                boardController.Redraw();
            }
        }

        /// <summary>
        /// This method can be invoked when you wish to let the user click on a board to add a widget.
        /// </summary>
        /// <param name="widgetConfiguration">Information on how the widget to add should be configured</param>
        internal static void PositionWidget(WidgetConfiguration widgetConfiguration)
        {
            foreach (BoardController controller in boardControllers)
            {
                controller.gameObject.AddComponent<WidgetPositioner>();
                WidgetPositioner.Setup(widgetConfiguration);
            }
        }

        internal static void DeleteWidget()
        {
            foreach (BoardController boardController in boardControllers)
            {
                boardController.GetWidgetToDelete();
            }
        }
    }
}
