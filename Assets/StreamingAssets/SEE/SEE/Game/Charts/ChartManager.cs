// Copyright 2020 Robert Bohnsack
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using SEE.Controls;
using SEE.DataModel;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.Charts
{
    /// <summary>
    /// Contains most settings and some methods needed across all charts.
    /// </summary>
    public class ChartManager : MonoBehaviour
    {
        /// <summary>
        /// The prefix of the name of a metric shown in a chart.
        /// </summary>
        public const string MetricPrefix = "Metric.";

        /// <summary>
        /// The instance of the <see cref="ChartManager" />, to ensure there will be only one.
        /// </summary>
        private static ChartManager _instance;

        /// <summary>
        /// Returns the unique chart manager component in the scene.
        /// 
        /// Precondition: There must be at least one game object tagged by ChartManagerTag
        /// holding a ChartManager component. If there is more than one such object, an error
        /// will be logged and the first game object will be used. If there is no such object,
        /// an exception is raised.
        /// </summary>
        public static ChartManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject[] chartManagers = GameObject.FindGameObjectsWithTag(Tags.ChartManager);
                    if (chartManagers.Length == 0)
                    {
                        Debug.LogError($"There is no chart manager tagged by {Tags.ChartManager} in the scene.\n");
                        throw new System.Exception("No chart manager in the scene");
                    }
                    else if (chartManagers.Length > 1)
                    {
                        Debug.LogError($"There are multiple chart managers named {Tags.ChartManager} in the scene.\n");
                    }
                    _instance = chartManagers[0].GetComponent<ChartManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning($"The game object named {Tags.ChartManager} does not have a ChartManager component. Will be added.\n");
                        _instance = chartManagers[0].AddComponent<ChartManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Metric Settings")]

        /// <summary>
        /// The code city whose nodes should be shown in the charts. If null, all nodes
        /// in the scene will be shown.
        /// </summary>
        [Tooltip("The code city whose nodes should be shown in the charts. If null, all nodes"
                  + " in the scene will be shown.")]
        public GameObject CodeCity;

        /// <summary>
        /// Whether metrics of leave nodes should be shown in the charts.
        /// </summary>
        [Tooltip("Whether the metrics of leaf nodes should be shown in the charts.")]
        public bool ShowLeafMetrics = true;

        /// <summary>
        /// Whether metrics of inner nodes should be shown in the charts.
        /// </summary>
        [Tooltip("Whether the metrics of inner nodes should be shown in the charts.")]
        public bool ShowInnerNodeMetrics = false;

        /// <summary>
        /// The minimum size a chart can have for width and height.
        /// </summary>
        [Tooltip("The minimum size a chart can have for width and height in pixels.")]
        [Range(50, 4096)]
        public int MinimumSize = 400;

        /// <summary>
        /// The minimum time for a drag to be recognized as a drag and not a click.
        /// </summary>
        [Tooltip("The minimum time for a drag to be recognized as a drag and not a click (in seconds).")]
        [Range(0.1f, 1f)] 
        public float DragDelay = 0.2f;

        /// <summary>
        /// Determines if the scene is being played in VR or not.
        /// </summary>
        private bool _isVirtualReality;

        [Header("Virtual Reality")]

        /// <summary>
        /// The length of the laser pointer attached to the controller.
        /// </summary>
        [Tooltip("The length of the laser pointer attached to the controller.")]
        [Range(0.01f, 10f)] 
        public float PointerLength = 5.0f;

        /// <summary>
        /// The speed at which charts will be moved in or out when the player scrolls.
        /// </summary>
        [Tooltip("The speed at which charts will be moved in or out when the player scrolls.")]
        [Range(0.1f, 50f)]
        public float ChartScrollSpeed = 10.0f;

        /// <summary>
        /// The minimum distance between the players head and the <see cref="GameObject" /> the charts are
        /// attached to to trigger it to follow the players head.
        /// </summary>
        [Tooltip("The minimum distance between the players head and the charts to the trigger the chart's movement.")]
        [Range(0.1f, 3f)]
        public float DistanceThreshold = 1.0f;

        [Header("Prefabs")]

        /// <summary>
        /// The canvas setup for charts that is used in non VR.
        /// </summary>
        [SerializeField] private GameObject chartsPrefab;

        /// <summary>
        /// The prefab of a new chart when in VR.
        /// </summary>
        [SerializeField] private GameObject chartPrefabVr;

        /// <summary>
        /// The sprite for the drag button when the chart is maximized.
        /// </summary>
        [SerializeField] private Sprite maximizedSprite;
        public Sprite MaximizedSprite => maximizedSprite;

        /// <summary>
        /// The sprite for the drag button when the chart is minimized.
        /// </summary>y
        [SerializeField] private Sprite minimizedSprite;
        public Sprite MinimizedSprite => minimizedSprite;

        /// <summary>
        /// Contains the chart UI.
        /// </summary>
        private GameObject _chartsOpen;

        /// <summary>
        /// Checks if the scene is started in VR and initializes it accordingly.
        /// </summary>
        private void Start()
        {
            _isVirtualReality = PlayerSettings.GetInputType() == PlayerInputType.VRPlayer;
            if (!_isVirtualReality)
            {
                _chartsOpen = GameObject.Find("ChartCanvas") != null
                    ? GameObject.Find("ChartCanvas").transform.Find("ChartsOpen").gameObject
                    : Instantiate(chartsPrefab).transform.Find("ChartsOpen").gameObject;
            }
        }

        /// <summary>
        /// Sets the positions of all charts to be in front of the player in VR.
        /// </summary>
        public static void ResetPosition()
        {
            Transform cameraPosition = MainCamera.Camera.transform;
            GameObject[] charts = GameObject.FindGameObjectsWithTag("ChartContainer");
            float offset = 0.0f;
            foreach (GameObject chart in charts)
            {
                chart.transform.position = cameraPosition.position + (2.0f + offset) * cameraPosition.forward;
                offset += 0.01f;
            }
        }

        /// <summary>
        /// Toggles the chart UI.
        /// </summary>
        /// <returns>Whether the charts are currently opened.</returns>
        public bool ToggleCharts()
        {
            bool result = !_chartsOpen.activeInHierarchy;
            _chartsOpen.SetActive(result);
            return result;
        }

        /// <summary>
        /// Whether the charts are opened.
        /// </summary>
        /// <returns></returns>
        public bool IsOpened()
        {
            return _chartsOpen.activeInHierarchy;
        }

        /// <summary>
        /// Initializes a new chart in front of the player in VR.
        /// </summary>
        public void CreateChartVR()
        {
            Transform cameraPosition = MainCamera.Camera.transform;
            Instantiate(chartPrefabVr, cameraPosition.position + 2 * cameraPosition.forward, Quaternion.identity, transform.GetChild(0));
        }
    }
}