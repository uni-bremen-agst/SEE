using SEE.Layout;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;
using Valve.VR;

namespace SEECity.Charts.Scripts
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
		[FormerlySerializedAs("HighlightLineLength")]
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
		public float distanceThreshold = 0.5f;

		/// <summary>
		/// The input source for controlling charts in VR.
		/// </summary>
		public SteamVR_Input_Sources source = SteamVR_Input_Sources.RightHand;

		public SteamVR_Input_Sources movementSource = SteamVR_Input_Sources.LeftHand;

		public SteamVR_Action_Single movement;

		/// <summary>
		/// The action boolean assigned to interacting with canvases in VR.
		/// </summary>
		public SteamVR_Action_Boolean click;

		/// <summary>
		/// Contains the scrolling information for moving charts in or out.
		/// </summary>
		public SteamVR_Action_Vector2 moveInOut;

		[SerializeField] private SteamVR_Action_Boolean resetPosition;

		/// <summary>
		/// The canvas setup for charts that is used in non VR.
		/// </summary>
		[Header("Prefabs"), SerializeField] private GameObject chartsPrefab;

		/// <summary>
		/// All objects in the scene that are used by the non VR representation of charts before the game
		/// starts.
		/// </summary>
		[SerializeField] private GameObject[] nonVrObjects;

		/// <summary>
		/// All objects in the scene that are used by the VR representation of charts before the game starts.
		/// </summary>
		[SerializeField] private GameObject[] vrObjects;

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
			_isVirtualReality = XRDevice.isPresent;
			if (!_isVirtualReality)
			{
				foreach (GameObject vrObject in vrObjects) Destroy(vrObject);
				Instantiate(chartsPrefab);
			}
			else
			{
				foreach (GameObject nonVrObject in nonVrObjects) Destroy(nonVrObject);
			}
		}

		/// <summary>
		/// Update is called once per frame.
		/// </summary>
		private void Update()
		{
			AnimateHighlight();
			if (_isVirtualReality)
			{
				if (resetPosition.GetChanged(source)) ResetPosition();
			}
			else
			{
				if (Input.GetMouseButtonDown(0))
				{
					Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

					if (Physics.Raycast(ray, out RaycastHit hit, 100f) &&
					    hit.transform.gameObject.TryGetComponent(out NodeRef _))
						HighlightObject(hit.transform.gameObject);
				}

				if (Input.GetButtonDown("SelectionMode")) selectionMode = true;

				if (Input.GetButtonUp("SelectionMode")) selectionMode = false;
			}
		}

		/// <summary>
		/// Animates the highlight material.
		/// </summary>
		private void AnimateHighlight()
		{
			buildingHighlightMaterial.SetFloat("g_flOutlineWidth", highlightOutlineAnim);
			buildingHighlightMaterialAccentuated.SetFloat("g_flOutlineWidth", highlightOutlineAnim);
		}

		private void ResetPosition()
		{
			Transform cameraPosition = Camera.main.transform;
			GameObject[] charts = GameObject.FindGameObjectsWithTag("ChartContainer");
			float offset = 0f;
			foreach (GameObject chart in charts)
			{
				chart.transform.position =
					cameraPosition.position + (2 + offset) * cameraPosition.forward;
				offset += 0.01f;
			}
		}

		public void HighlightObject(GameObject highlight)
		{
			GameObject[] charts = GameObject.FindGameObjectsWithTag("Chart");
			foreach (GameObject chart in charts)
				chart.GetComponent<ChartContent>().HighlightCorrespondingMarker(highlight);
		}

		public void Accentuate(GameObject highlight)
		{
			GameObject[] charts = GameObject.FindGameObjectsWithTag("Chart");
			foreach (GameObject chart in charts)
				chart.GetComponent<ChartContent>().AccentuateCorrespondingMarker(highlight);

			Transform highlightTransform = highlight.transform;

			for (int i = 0; i < highlightTransform.childCount; i++)
			{
				Transform child = highlightTransform.GetChild(i);
				if (!child.gameObject.name.Equals(highlight.name + "(Clone)")) continue;
				for (int x = 0; x < child.childCount; x++)
				{
					Transform secondChild = child.GetChild(x);
					if (!secondChild.gameObject.name.Equals("HighlightLine(Clone)")) continue;
					secondChild.GetComponent<HighlightLine>().ToggleAccentuation();
					return;
				}
			}
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