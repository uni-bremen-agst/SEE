using SEE.Controls;
using UnityEngine;

namespace SEE.Charts.Scripts
{
	/// <summary>
	/// Contains most settings and some methods needed across all charts.
	/// </summary>
	public class ChartManager : MonoBehaviour
	{
		/// <summary>
		/// The instance of the <see cref="ChartManager" />, to ensure there will be only one.
		/// </summary>
		private static ChartManager _instance;

		/// <summary>
		/// If true, highlighted objects will stay highlighted until this is deactivated.
		/// </summary>
		[HideInInspector] public bool selectionMode;

		/// <summary>
		/// The distance the camera will keep to the <see cref="GameObject" /> to focus on.
		/// </summary>
		[Header("Settings"), Header("Camera Controls")]
		public float cameraDistance = 40f;

		/// <summary>
		/// When checked, the <see cref="Camera" /> will rotate while moving.
		/// </summary>
		public bool moveWithRotation = true;

		/// <summary>
		/// The time the <see cref="Camera" /> needs to reach it's destination when moving from one
		/// <see cref="GameObject" /> to another.
		/// </summary>
		public float cameraFlightTime = 0.5f;

		/// <summary>
		/// The minimum size a chart can have for width and height.
		/// </summary>
		public int minimumSize = 400;

		/// <summary>
		/// The maximum time between two clicks to recognize them as double click.
		/// </summary>
		[Header("User Inputs"), Range(0.1f, 1f)]
		public float clickDelay = 0.5f;

		/// <summary>
		/// The minimum time for a drag to be recognized as a drag and not a click.
		/// </summary>
		[Range(0.1f, 1f)] public float dragDelay = 0.2f;

		/// <summary>
		/// The <see cref="Material" /> making the object look highlighted.
		/// </summary>
		[Header("Highlights")] public Material buildingHighlightMaterial;

		/// <summary>
		/// The <see cref="Material" /> making the object look accentuated.
		/// </summary>
		public Material buildingHighlightMaterialAccentuated;

		/// <summary>
		/// The thickness of the highlight outline of <see cref="buildingHighlightMaterial" />.
		/// </summary>
		[SerializeField] private float highlightOutline = 0.005f;

		/// <summary>
		/// The color highlighted objects will have.
		/// </summary>
		public Color standardColor;

		/// <summary>
		/// The color accentuated highlighted objects will have.
		/// </summary>
		public Color accentuationColor;

		/// <summary>
		/// The length of the beam appearing above highlighted objects.
		/// </summary>
		public float highlightLineLength = 20f;

		/// <summary>
		/// The time an object will be highlighted for.
		/// </summary>
		public float highlightDuration = 5f;

		/// <summary>
		/// Determines if the scene is being played in VR or not.
		/// </summary>
		private bool _isVirtualReality;

		/// <summary>
		/// The length of the pointer attached to the controller.
		/// </summary>
		[Header("Virtual Reality")] public float pointerLength = 5f;

		/// <summary>
		/// The speed at which charts will be moved in or out when the player scrolls.
		/// </summary>
		public float chartScrollSpeed = 10f;

		/// <summary>
		/// The minimum distance between the players head and the <see cref="GameObject" /> the charts are
		/// attached to to trigger it to follow the players head.
		/// </summary>
		public float distanceThreshold = 1f;

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
		/// Enforces the singleton pattern.
		/// </summary>
		private void Awake()
		{
			if (_instance == null)
				_instance = this;
			else if (_instance == this) Destroy(gameObject);
		}

		/// <summary>
		/// Checks if the scene is started in VR and initializes it accordingly.
		/// </summary>
		private void Start()
		{
			_isVirtualReality =
				GameObject.Find("Player Settings").GetComponent<PlayerSettings>().playerInputType ==
				PlayerSettings.PlayerInputType.VR;
			if (!_isVirtualReality)
				_chartsOpen = GameObject.Find("ChartCanvas") != null
					? GameObject.Find("ChartCanvas").transform.Find("ChartsOpen").gameObject
					: Instantiate(chartsPrefab).transform.Find("ChartsOpen").gameObject;
			else
				foreach (var nonVrObject in nonVrObjects)
					Destroy(nonVrObject);
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
			var cameraPosition = Camera.main.transform;
			var charts = GameObject.FindGameObjectsWithTag("ChartContainer");
			var offset = 0f;
			foreach (var chart in charts)
			{
				chart.transform.position =
					cameraPosition.position + (2 + offset) * cameraPosition.forward;
				offset += 0.01f;
			}
		}

		/// <summary>
		/// Highlights an object and all markers associated with it.
		/// </summary>
		/// <param name="highlight"></param>
		/// <param name="scrollView">If this is triggered by a <see cref="ScrollViewToggle" /> or not.</param>
		public static void HighlightObject(GameObject highlight, bool scrollView)
		{
			var charts = GameObject.FindGameObjectsWithTag("Chart");
			foreach (var chart in charts)
			{
				chart.TryGetComponent<ChartContent>(out var content);
				content.HighlightCorrespondingMarker(highlight, scrollView);
			}
		}

		/// <summary>
		/// Accentuates an object and all markers associated with it.
		/// </summary>
		/// <param name="highlight"></param>
		public static void Accentuate(GameObject highlight)
		{
			var charts = GameObject.FindGameObjectsWithTag("Chart");
			foreach (var chart in charts)
			{
				chart.TryGetComponent<ChartContent>(out var content);
				content.AccentuateCorrespondingMarker(highlight);
			}

			var highlightTransform = highlight.transform;

			for (var i = 0; i < highlightTransform.childCount; i++)
			{
				var child = highlightTransform.GetChild(i);
				if (!child.gameObject.name.Equals(highlight.name + "(Clone)")) continue;
				for (var x = 0; x < child.childCount; x++)
				{
					var secondChild = child.GetChild(x);
					if (!secondChild.gameObject.name.Equals("HighlightLine(Clone)")) continue;
					secondChild.TryGetComponent<HighlightLine>(out var line);
					line.ToggleAccentuation();
					return;
				}
			}
		}

		/// <summary>
		/// Toggles the chart UI.
		/// </summary>
		public void ToggleCharts()
		{
			_chartsOpen.SetActive(!_chartsOpen.activeInHierarchy);
		}

		/// <summary>
		/// Toggles the selection mode (Objects stay highlighted).
		/// </summary>
		public void ToggleSelectionMode()
		{
			selectionMode = !selectionMode;
		}

		/// <summary>
		/// Initializes a new chart in front of the player in VR.
		/// </summary>
		public void CreateChartVr()
		{
			var cameraPosition = Camera.main.transform;

			Instantiate(chartPrefabVr, cameraPosition.position + 2 * cameraPosition.forward,
				Quaternion.identity, transform.GetChild(0));
		}

		/// <summary>
		/// Sets the properties of <see cref="buildingHighlightMaterial" /> to their original state.
		/// </summary>
		private void OnDestroy()
		{
			buildingHighlightMaterial.SetFloat("g_flOutlineWidth", highlightOutline);
			buildingHighlightMaterialAccentuated.SetFloat("g_flOutlineWidth", highlightOutline);
		}
	}
}