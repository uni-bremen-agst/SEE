using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

namespace SEECity.Charts.Scripts
{
	/// <summary>
	/// Contains most settings and some methods needed across all charts.
	/// </summary>
	public class ChartManager : MonoBehaviour
	{
		private static ChartManager _instance;

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

		[Range(0.1f, 1f)] public float dragDelay = 0.2f;

		/// <summary>
		/// The <see cref="Material" /> making the object look highlighted.
		/// </summary>
		[Header("Highlights")] public Material buildingHighlightMaterial;

		/// <summary>
		/// The thickness of the highlight outline of <see cref="buildingHighlightMaterial" />.
		/// </summary>
		[SerializeField] private float highlightOutline = 0.005f;

		public float highlightDuration = 5f;

		/// <summary>
		/// Determines if the scene is being played in VR or not.
		/// </summary>
		private bool _isVirtualReality;

		[Header("Virtual Reality")] public float pointerLength = 5f;
		public float chartScrollSpeed = 10f;
		public float distanceThreshold = 0.5f;

		public SteamVR_Input_Sources source = SteamVR_Input_Sources.RightHand;
		public SteamVR_Action_Boolean click;
		public SteamVR_Action_Vector2 moveInOut;

		[Header("Prefabs"), SerializeField] private GameObject chartsPrefab;

		[SerializeField] private GameObject nonVRCamera;

		[SerializeField] private GameObject[] virtualRealityObjects;

		public Sprite maximizedSprite;

		public Sprite minimizedSprite;

		/// <summary>
		/// The current thickness of the highlight outline of <see cref="buildingHighlightMaterial" /> used in
		/// animations.
		/// </summary>
		[Header("DO NOT CHANGE THIS"), SerializeField]
		private float highlightOutlineAnim = 0.001f;

		/// <summary>
		/// Enforces singleton pattern.
		/// </summary>
		private void Awake()
		{
			if (_instance == null)
				_instance = this;
			else if (_instance == this) Destroy(gameObject);
		}

		private void Start()
		{
			_isVirtualReality = XRDevice.isPresent;
			if (!_isVirtualReality)
			{
				foreach (GameObject vrObject in virtualRealityObjects) Destroy(vrObject);
				Instantiate(chartsPrefab);
			}
			else
			{
				Destroy(nonVRCamera);
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
		}

		/// <summary>
		/// Sets the properties of <see cref="buildingHighlightMaterial" /> to their original state.
		/// </summary>
		private void OnDestroy()
		{
			buildingHighlightMaterial.SetFloat("g_flOutlineWidth", highlightOutline);
		}
	}
}