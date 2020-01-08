using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEECity.Charts.Scripts
{
	public class VRPointer : MonoBehaviour
	{
		private GameManager _gameManager;

		private float _pointerLength;
		private LineRenderer _lineRenderer;
		private VRInputModule _inputModule;
		[SerializeField] private GameObject _hitDot;

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
			_inputModule = EventSystem.current.gameObject.GetComponent<VRInputModule>();
		}

		private void GetSettingData()
		{
			_gameManager = GameObject.FindGameObjectWithTag("GameManager")
				.GetComponent<GameManager>();
			_pointerLength = _gameManager.PointerLength;
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