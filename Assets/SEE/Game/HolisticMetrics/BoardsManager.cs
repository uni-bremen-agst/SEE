using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SEE.Game.HolisticMetrics
{
    /// <summary>
    /// This class manages all metrics boards.
    /// </summary>
    public class BoardsManager : MonoBehaviour
    {
        private GameObject boardPrefab;

        private void Start()
        {
            string pathToBoard = Path.Combine("Prefabs", "HolisticMetrics", "SceneComponents", "MetricsBoard");
            boardPrefab = Resources.Load<GameObject>(pathToBoard);
        }

        /// <summary>
        /// List of all the BoardControllers that this manager manages (there should not be any BoardControllers in the
        /// scene that are not in this list).
        /// </summary>
        private readonly List<BoardController> boardControllers = new List<BoardController>(); 
        
        

        /// <summary>
        /// Creates a new metrics board and puts its BoardController into the list of BoardControllers.
        /// </summary>
        /// <param name="boardName">The title of the new board, this will be displayed in the top left corner of the new
        /// board. This must be unique.</param>
        internal void CreateNewBoard(string boardName)
        {
            bool nameExists = boardControllers.Any(boardController => boardController.GetTitle().Equals(boardName)); 
            if (nameExists)
            {
                // TODO: Do not throw an exception; rather show user a popup, then return
                throw new Exception("Name has to be unique!");
            }
            GameObject newBoard = Instantiate(boardPrefab, gameObject.transform);
            BoardController newBoardController = newBoard.GetComponent<BoardController>();
            newBoardController.GetTitle(boardName);
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
