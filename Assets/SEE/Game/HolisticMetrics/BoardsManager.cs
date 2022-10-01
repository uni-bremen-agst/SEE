using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SEE.Game.HolisticMetrics.Metrics;
using UnityEngine;

namespace SEE.Game.HolisticMetrics
{
    /// <summary>
    /// This class manages all metrics boards.
    /// </summary>
    public class BoardsManager : MonoBehaviour
    {
        private GameObject boardPrefab;
        private GameObject[] widgetPrefabs;

        private Type[] metricTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(domainAssembly => domainAssembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(Metric)))
            .ToArray();

        private void Start()
        {
            string pathToBoard = Path.Combine("Prefabs", "HolisticMetrics", "SceneComponents", "MetricsBoard");
            string pathToWidgets = Path.Combine("Prefabs", "HolisticMetrics", "Widgets");
            boardPrefab = Resources.Load<GameObject>(pathToBoard);
            widgetPrefabs = Resources.LoadAll<GameObject>(pathToWidgets);
        }

        /// <summary>
        /// List of all the BoardControllers that this manager manages (there should not be any BoardControllers in the
        /// scene that are not in this list).
        /// </summary>
        private readonly List<BoardController> boardControllers = new List<BoardController>();


        /// <summary>
        /// Creates a new metrics board and puts its BoardController into the list of BoardControllers.
        /// </summary>
        /// <param name="boardConfiguration">The board configuration for the new board.</param>
        internal void CreateNewBoard(BoardConfiguration boardConfiguration)
        {
            bool nameExists = boardControllers.Any(boardController =>
                boardController.GetTitle().Equals(boardConfiguration.Title));
            if (nameExists)
            {
                // TODO: Do not throw an exception; rather show user a popup, then return
                throw new Exception("Name has to be unique!");
            }

            GameObject newBoard = Instantiate(boardPrefab, gameObject.transform);
            BoardController newBoardController = newBoard.GetComponent<BoardController>();

            // Set the title of the new board
            newBoardController.GetTitle(boardConfiguration.Title);

            // Add the widgets to the new board
            foreach (WidgetConfiguration widgetConfiguration in boardConfiguration.WidgetConfigurations)
            {
                GameObject prefab = Array.Find(widgetPrefabs,
                    element => element.name.Equals(widgetConfiguration.WidgetName));
                Type metricType = Array.Find(metricTypes, 
                    type => type.Name.Equals(widgetConfiguration.MetricType));
                foreach (Type type in metricTypes)
                {
                    Debug.Log($"Type from list {type.Name} does not match given type {widgetConfiguration.MetricType}");
                }
                if (prefab is null)
                {
                    Debug.LogError("Could not load widget because the widget name from the configuration" +
                                   "file matches no existing widget prefab. This could be because the configuration" +
                                   "file was manually changed.");
                }
                else if (metricType is null)
                {
                    Debug.LogError("Could not load metric because the metric type from the configuration" +
                                   "file matches no existing metric type. This could be because the configuration" +
                                   "file was manually changed.");
                }
                else
                {
                    newBoardController.AddMetric(metricType, prefab);
                }
            }

            boardControllers.Add(newBoardController);
        }

        /// <summary>
        /// Finds a MetricsBoard GameObject by its name.
        /// </summary>
        /// <param name="boardName">The name to look for.</param>
        /// <returns>Returns the desired GameObject if it exists or null if it doesn't.</returns>
        internal BoardController FindControllerByName(string boardName)
        {
            return boardControllers.Find(boardController => boardController.GetTitle().Equals(boardName));
        }

        /// <summary>
        /// Returns the names of all BoardControllers. The names can also be used to identify the BoardControllers
        /// because they have to be unique.
        /// </summary>
        /// <returns>The names of all BoardControllers.</returns>
        internal string[] GetNames()
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
        internal void OnGraphLoad()
        {
            foreach (BoardController boardController in boardControllers)
            {
                boardController.OnGraphLoad();
            }
        }
    }
}
