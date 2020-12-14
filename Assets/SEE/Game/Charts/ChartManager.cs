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
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

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
        /// Tag of a game object having a ChartManager as a component.
        /// </summary>
        private const string ChartManagerTag = "ChartManager";

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
                    GameObject[] chartManagers = GameObject.FindGameObjectsWithTag(ChartManagerTag);
                    if (chartManagers.Length == 0)
                    {
                        Debug.LogErrorFormat("There is no chart manager tagged by {0} in the scene.\n", ChartManagerTag);
                        throw new System.Exception("No chart manager in the scene");
                    }
                    else if (chartManagers.Length > 1)
                    {
                        Debug.LogErrorFormat("There are multiple chart managers named {0} in the scene.\n", ChartManagerTag);
                    }
                    _instance = chartManagers[0].GetComponent<ChartManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarningFormat("The game object named {0} does not have a ChartManager component. Will be added.\n", ChartManagerTag);
                        _instance = chartManagers[0].AddComponent<ChartManager>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Whether metrics of leave nodes should be shown in the charts.
        /// </summary>
        [Header("Metric Settings"), Tooltip("Whether the metrics of leaf nodes should be shown in the charts")]
        public bool ShowLeafMetrics = true;

        /// <summary>
        /// Whether metrics of inner nodes should be shown in the charts.
        /// </summary>
        [Tooltip("Whether the metrics of inner nodes should be shown in the charts")]
        public bool ShowInnerNodeMetrics = false;

        /// <summary>
        /// The minimum size a chart can have for width and height.
        /// </summary>
        public int minimumSize = 400;

        /// <summary>
        /// The minimum time for a drag to be recognized as a drag and not a click.
        /// </summary>
        [Range(0.1f, 1f)] public float dragDelay = 0.2f;

        /// <summary>
        /// The <see cref="Material" /> making the object look highlighted.
        /// </summary>
        [Header("Highlights"), Tooltip("Material for highlighting a selected game node.")]
        public Material buildingHighlightMaterial;

        /// <summary>
        /// The <see cref="Material" /> making the object look accentuated.
        /// </summary>
        [Tooltip("Material for highlighting a selected accentuated game node.")]
        public Material buildingHighlightMaterialAccentuated;

        /// <summary>
        /// The thickness of the highlight outline of <see cref="buildingHighlightMaterial" />.
        /// </summary>
        [SerializeField, Tooltip("Width of the line drawn around selected game nodes as an outline")]
        private float highlightOutline = 0.1f;

        /// <summary>
        /// The color highlighted objects will have.
        /// </summary>
        [Tooltip("Color for selected game nodes")]
        public Color standardColor = Color.red;

        /// <summary>
        /// The color accentuated highlighted objects will have.
        /// </summary>
        [Tooltip("Color for accentuated selected game nodes")]
        public Color accentuationColor = Color.blue;

        /// <summary>
        /// Determines if the scene is being played in VR or not.
        /// </summary>
        private bool _isVirtualReality;

        /// <summary>
        /// The length of the pointer attached to the controller.
        /// </summary>
        [Header("Virtual Reality")] public float pointerLength = 5.0f;

        /// <summary>
        /// The speed at which charts will be moved in or out when the player scrolls.
        /// </summary>
        public float chartScrollSpeed = 10.0f;

        /// <summary>
        /// The minimum distance between the players head and the <see cref="GameObject" /> the charts are
        /// attached to to trigger it to follow the players head.
        /// </summary>
        public float distanceThreshold = 1.0f;

        /// <summary>
        /// The canvas setup for charts that is used in non VR.
        /// </summary>
        [Header("Prefabs"), SerializeField] private GameObject chartsPrefab;

        /// <summary>
        /// The prefab of a new chart when in VR.
        /// </summary>
        [SerializeField] private GameObject chartPrefabVr;

        /// <summary>
        /// All objects in the scene that are used by the non VR representation of charts before the game
        /// starts.
        /// </summary>
        [SerializeField] private GameObject[] nonVrObjects;

        /// <summary>
        /// The sprite for the drag button when the chart is maximized.
        /// </summary>
        public Sprite maximizedSprite;

        /// <summary>
        /// The sprite for the drag button when the chart is minimized.
        /// </summary>
        public Sprite minimizedSprite;

        /// <summary>
        /// The current thickness of the highlight outline of <see cref="buildingHighlightMaterial" /> used in
        /// animations.
        /// </summary>
        [Header("DO NOT CHANGE THIS"), SerializeField]
        private float highlightOutlineAnim = 0.001f;

        /// <summary>
        /// Contains the chart UI.
        /// </summary>
        private GameObject _chartsOpen;

        /// <summary>
        /// Checks if the scene is started in VR and initializes it accordingly.
        /// </summary>
        private void Start()
        {
            _isVirtualReality = GameObject.Find("Player Settings").GetComponent<PlayerSettings>().playerInputType == PlayerSettings.PlayerInputType.VR;
            if (!_isVirtualReality)
            {
                _chartsOpen = GameObject.Find("ChartCanvas") != null
                    ? GameObject.Find("ChartCanvas").transform.Find("ChartsOpen").gameObject
                    : Instantiate(chartsPrefab).transform.Find("ChartsOpen").gameObject;
            }
            else
            {
                foreach (GameObject nonVrObject in nonVrObjects)
                {
                    Destroy(nonVrObject);
                }
            }
        }

        /// <summary>
        /// Update is called once per frame.
        /// </summary>
        private void Update()
        {
            AnimateHighlight();
        }

        /// <summary>
        /// Animates the highlight material.
        /// </summary>
        private void AnimateHighlight()
        {
            buildingHighlightMaterial.SetFloat("g_flOutlineWidth", highlightOutlineAnim);
            buildingHighlightMaterialAccentuated.SetFloat("g_flOutlineWidth", highlightOutlineAnim);
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

        // All created and still existing charts. Charts are game objects tagged by Tags.Chart
        // representing a metric chart.
        private readonly HashSet<GameObject> allCharts = new HashSet<GameObject>();

        private ICollection<GameObject> AllCharts()
        {
            return allCharts;
        }

        /// <summary>
        /// Registers the descendant of given <paramref name="gameObject"/> tagged by Tags.Chart
        /// in allCharts. Hightlighting and accentuation works only for elements of registered
        /// charts.
        /// </summary>
        /// <param name="gameObject">a game object containing a chart</param>
        public void RegisterChart(GameObject gameObject)
        {
            GameObject chart = Tags.FindChildWithTag(gameObject, Tags.Chart);
            Assert.IsNotNull(chart);
            allCharts.Add(chart);
        }

        /// <summary>
        /// Unregisters the descendant of given <paramref name="gameObject"/> tagged by Tags.Chart
        /// in allCharts. Hightlighting and accentuation works only for elements of registered
        /// charts.
        /// </summary>
        /// <param name="gameObject">a game object containing a chart</param>
        public void UnregisterChart(GameObject gameObject)
        {
            GameObject chart = Tags.FindChildWithTag(gameObject, Tags.Chart);
            Assert.IsNotNull(chart);
            allCharts.Remove(chart);
        }

        public static void OnSelect(GameObject highlight)
        {
            SetCorrespondingMarkers(highlight, true);
        }

        public static void OnDeselect(GameObject highlight)
        {
            SetCorrespondingMarkers(highlight, false);
        }

        private static void SetCorrespondingMarkers(GameObject highlight, bool isSelected)
        {
            foreach (GameObject chart in Instance.AllCharts())
            {
                chart.GetComponent<ChartContent>()?.HighlightCorrespondingMarker(highlight, isSelected);
            }
        }

        /// <summary>
        /// Accentuates an object and all markers associated with it.
        /// </summary>
        /// <param name="highlight"></param>
        public static void Accentuate(GameObject highlight)
        {
            foreach (GameObject chart in Instance.AllCharts())
            {
                chart.GetComponent<ChartContent>()?.AccentuateCorrespondingMarker(highlight);
            }

            Transform highlightTransform = highlight.transform;

            for (int i = 0; i < highlightTransform.childCount; i++)
            {
                Transform child = highlightTransform.GetChild(i);
                if (child.gameObject.name.Equals(highlight.name + "(Clone)"))
                {
                    for (int x = 0; x < child.childCount; x++)
                    {
                        Transform secondChild = child.GetChild(x);
                        if (secondChild.gameObject.name.Equals("HighlightLine(Clone)"))
                        {
                            secondChild.GetComponent<HighlightLine>()?.ToggleAccentuation();
                            return;
                        }
                    }
                }
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
        public void CreateChartVr()
        {
            Transform cameraPosition = MainCamera.Camera.transform;
            Instantiate(chartPrefabVr, cameraPosition.position + 2 * cameraPosition.forward, Quaternion.identity, transform.GetChild(0));
        }

        /// <summary>
        /// Sets the properties of <see cref="buildingHighlightMaterial" /> to their original state.
        /// </summary>
        private void OnDestroy()
        {
            buildingHighlightMaterial.SetFloat("g_flOutlineWidth", highlightOutline);
            buildingHighlightMaterialAccentuated.SetFloat("g_flOutlineWidth", highlightOutline);
        }

        /// <summary>
        /// Unselects all currently selected markers of all existing charts.
        /// </summary>
        public void UnselectAll()
        {
            foreach (var chart in AllCharts())
            {
                chart.GetComponent<ChartContent>()?.UnselectAll();
            }
        }
    }
}