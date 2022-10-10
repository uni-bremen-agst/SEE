using System.IO;
using SEE.Game.UI.HolisticMetrics;
using SEE.Game.UI.PropertyDialog;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.HolisticMetrics
{
    internal class BoardAdder : MonoBehaviour
    {
        // Currently, there is only one GameObject this BoardAdder is attached to. But should there ever be more than
        // one surface this BoardAdder will be attached to, it makes sense to make this bool static so that all
        // instances share it.
        /// <summary>
        /// Whether or not the addition is done which would mean that all BoardAdders can be deleted.
        /// </summary>
        private static bool addingDone;

        private static BoardsManager boardsManager;

        private static BoardConfiguration boardConfiguration;

        private static GameObject sliderPrefab;

        private static GameObject boardPrefab;
        
        internal static void Setup(BoardsManager boardsManagerReference, BoardConfiguration boardConfigurationReference)
        {
            boardConfiguration = boardConfigurationReference;
            string pathToBoard = Path.Combine("Prefabs", "HolisticMetrics", "SceneComponents", "MetricsBoard");
            boardPrefab = Resources.Load<GameObject>(pathToBoard);
            boardsManager = boardsManagerReference;
            addingDone = false;
        }
        
        /// <summary>
        /// When the GameObject registers a mouse click, we get the position of the hit, create a new BoardConfiguration
        /// with that position and show the player a dialog where he can finish the configuration.
        /// </summary>
        private void OnMouseUp()
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector3 newPosition = hit.point;
                    newPosition.y = boardPrefab.transform.position.y;

                    boardConfiguration.Position = newPosition;

                    
                    string pathToSlider = Path.Combine("Prefabs", "UI", "MetricsBoardRotation");
                    PrefabInstantiator.InstantiatePrefab(
                            pathToSlider, 
                            GameObject.Find("UI Canvas").transform, 
                            instantiateInWorldSpace: false)
                        .GetComponent<AddBoardSliderController>()
                        .Setup(boardConfiguration, boardsManager);
                    addingDone = true;
                }
            }
        }

        private void Update()
        {
            if(addingDone)
            {
                Destroy(this);
            }
        }
    }
}