using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEECity.Charts.Scripts.VR
{
	/// <summary>
	/// Manages the pointer used to interact with canvases in VR.
	/// </summary>
	public class VRPointer : MonoBehaviour
	{
		private ChartManager _chartManager;

		private float _pointerLength;
		private LineRenderer _lineRenderer;
		private VRInputModule _inputModule;
		[SerializeField] private GameObject _hitDot = null;

		public Camera Camera { get; private set; }

		private void Awake()
		{
			GetSettingData();
			Camera = GetComponent<Camera>();
			Camera.enabled = false;
			_lineRenderer = GetComponent<LineRenderer>();
		}

		private void Start()
		{
			_inputModule = GameObject.FindGameObjectWithTag("VREventSystem").GetComponent<VRInputModule>();
		}

		/// <summary>
		/// Links the <see cref="ChartManager" /> and gets its setting data.
		/// </summary>
		private void GetSettingData()
		{
			_chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
			_pointerLength = _chartManager.PointerLength;
		}

		private void Update()
		{
			UpdateLine();
		}

		private void UpdateLine()
		{
			PointerEventData data = _inputModule.EventData;
			RaycastHit hit = CreateRaycast();
			float colliderDistance = hit.distance == 0 ? _pointerLength : hit.distance;
			float canvasDistance = data.pointerCurrentRaycast.distance == 0
				? _pointerLength
				: data.pointerCurrentRaycast.distance;
			float targetLength = Mathf.Min(colliderDistance, canvasDistance);
			Vector3 hitPosition = transform.position + transform.forward * targetLength;
			_hitDot.transform.position = hitPosition;
			_lineRenderer.SetPosition(0, transform.position);
			_lineRenderer.SetPosition(1, hitPosition);
		}

		private RaycastHit CreateRaycast()
		{
			Ray ray = new Ray(transform.position, transform.forward);
			Physics.Raycast(ray, out RaycastHit hitData, _pointerLength);

			return hitData;
		}
	}
}