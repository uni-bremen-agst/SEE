using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts.VR
{
	/// <summary>
	/// Manages the pointer used to interact with canvases in VR.
	/// </summary>
	public class VrPointer : MonoBehaviour
	{
		/// <summary>
		/// Contains some settings used in this script.
		/// </summary>
		private ChartManager _chartManager;

		/// <summary>
		/// The length of the pointer attached to the controller.
		/// </summary>
		private float _pointerLength;

		/// <summary>
		/// Renders the line visualizing the pointer.
		/// </summary>
		private LineRenderer _lineRenderer;

		/// <summary>
		/// TODO
		/// </summary>
		private VrInputModule _inputModule;

		/// <summary>
		/// Visualizes the position at which the line of the pointer hits a target.
		/// </summary>
		[SerializeField] private GameObject hitDot;

		/// <summary>
		/// Attached to the players controller to create raycasts from it.
		/// </summary>
		public Camera Camera { get; private set; }

		/// <summary>
		/// Initializes some Attributes.
		/// </summary>
		private void Awake()
		{
			GetSettingData();
			Camera = GetComponent<Camera>();
			Camera.enabled = false;
			_lineRenderer = GetComponent<LineRenderer>();
		}

		/// <summary>
		/// Initializes some attributes.
		/// </summary>
		private void Start()
		{
			_inputModule = GameObject.FindGameObjectWithTag("VREventSystem")
				.GetComponent<VrInputModule>();
		}

		/// <summary>
		/// Links the <see cref="ChartManager" /> and gets its setting data.
		/// </summary>
		private void GetSettingData()
		{
			_chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
			_pointerLength = _chartManager.pointerLength;
		}

		/// <summary>
		/// Update is called once per frame.
		/// </summary>
		private void Update()
		{
			UpdateLine();
		}

		/// <summary>
		/// Checks the distance to the hit object and sets the length of the pointer accordingly.
		/// </summary>
		private void UpdateLine()
		{
			PointerEventData data = _inputModule.EventData;
			RaycastHit hit = CreateRaycast();
			float colliderDistance = hit.distance.Equals(0f) ? _pointerLength : hit.distance;
			float canvasDistance = data.pointerCurrentRaycast.distance.Equals(0f)
				? _pointerLength
				: data.pointerCurrentRaycast.distance;
			float targetLength = Mathf.Min(colliderDistance, canvasDistance);
			Vector3 hitPosition = transform.position + transform.forward * targetLength;
			hitDot.transform.position = hitPosition;
			_lineRenderer.SetPosition(0, transform.position);
			_lineRenderer.SetPosition(1, hitPosition);
		}

		/// <summary>
		/// Sends out a new Raycast and returns the hit data.
		/// </summary>
		/// <returns>Information about the hit.</returns>
		private RaycastHit CreateRaycast()
		{
			Ray ray = new Ray(transform.position, transform.forward);
			Physics.Raycast(ray, out RaycastHit hitData, _pointerLength);

			return hitData;
		}
	}
}