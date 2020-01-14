using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

namespace Assets.SEECity.Charts.Scripts
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
		public float CameraDistance = 40f;

		/// <summary>
		/// When checked, the <see cref="Camera" /> will rotate while moving.
		/// </summary>
		public bool MoveWithRotation = true;

		/// <summary>
		/// The time the <see cref="Camera" /> needs to reach it's destination when moving from one
		/// <see cref="GameObject" /> to another.
		/// </summary>
		public float CameraFlightTime = 0.5f;

		/// <summary>
		/// The minimum size a chart can have for width and height.
		/// </summary>
		public int MinimumSize = 400;

		/// <summary>
		/// The maximum time between two clicks to recognize them as double click.
		/// </summary>
		[Header("User Inputs"), Range(0.1f, 1f)]
		public float ClickDelay = 0.5f;

		[Range(0.1f, 1f)] public float DragDelay = 0.2f;

		/// <summary>
		/// The <see cref="Material" /> making the object look highlighted.
		/// </summary>
		[Header("Highlights")] public Material BuildingHighlightMaterial = null;

		/// <summary>
		/// The thickness of the highlight outline of <see cref="BuildingHighlightMaterial" />.
		/// </summary>
		[SerializeField] private float _highlightOutline = 0.005f;

		public float HighlightDuration = 5f;

		/// <summary>
		/// Determines if the scene is being played in VR or not.
		/// </summary>
		private bool _isVirtualReality;

		[Header("Virtual Reality")] public float PointerLength = 5f;

		public SteamVR_Input_Sources Source = SteamVR_Input_Sources.RightHand;
		public SteamVR_Action_Boolean Click;

		[Header("Prefabs"), SerializeField] private GameObject _chartsPrefab;

		[SerializeField] private GameObject _chartsVirtualRealityPrefab;

		/// <summary>
		/// The current thickness of the highlight outline of <see cref="BuildingHighlightMaterial" /> used in
		/// animations.
		/// </summary>
		[Header("DO NOT CHANGE THIS"), SerializeField]
		private float _highlightOutlineAnim = 0.001f;

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
		}

		/// <summary>
		/// Update is called once per frame.
		/// </summary>
		private void Update()
		{
			AnimateHighlight();
			Debug.Log(XRDevice.isPresent);
		}

		/// <summary>
		/// Animates the highlight material.
		/// </summary>
		private void AnimateHighlight()
		{
			BuildingHighlightMaterial.SetFloat("g_flOutlineWidth", _highlightOutlineAnim);
		}

		/// <summary>
		/// Sets the properties of <see cref="BuildingHighlightMaterial" /> to their original state.
		/// </summary>
		private void OnDestroy()
		{
			BuildingHighlightMaterial.SetFloat("g_flOutlineWidth", _highlightOutline);
		}
	}
}