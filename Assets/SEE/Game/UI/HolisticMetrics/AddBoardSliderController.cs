using System.IO;
using Michsky.UI.ModernUIPack;
using SEE.Game.HolisticMetrics;
using UnityEngine;

namespace SEE.Game.UI.HolisticMetrics
{
    internal class AddBoardSliderController : MonoBehaviour
    {
        [SerializeField] private SliderManager slider;

        [SerializeField] private WindowDragger windowDragger;

        private BoardsManager boardsManager;

        private BoardConfiguration boardConfiguration;

        private GameObject dummyBoard;
        
        internal void Setup(BoardConfiguration boardConfigurationReference, BoardsManager boardsManagerReference)
        {
            boardConfiguration = boardConfigurationReference;
            boardsManager = boardsManagerReference;

            windowDragger.dragArea = transform.parent.GetComponent<RectTransform>();
            
            slider.mainSlider.onValueChanged.AddListener(Rotate);
            string pathToBoard = Path.Combine("Prefabs", "HolisticMetrics", "SceneComponents", "MetricsBoard");
            GameObject boardPrefab = Resources.Load<GameObject>(pathToBoard);
            dummyBoard = Instantiate(boardPrefab, boardConfiguration.Position, Quaternion.identity);
        }
        
        private void Rotate(float yAxisRotation)
        {
            Quaternion rotation = Quaternion.identity;
            rotation.eulerAngles = new Vector3(0, yAxisRotation, 0);
            dummyBoard.transform.rotation = rotation;
        }

        /// <summary>
        /// Will be called by the button when it is being pressed. This will finally create the new board.
        /// </summary>
        public void CreateBoard()
        {
            boardConfiguration.Rotation = dummyBoard.transform.rotation;
            Destroy(dummyBoard);
            boardsManager.CreateNewBoard(boardConfiguration);
            Destroy(gameObject);
        }
    }
}
