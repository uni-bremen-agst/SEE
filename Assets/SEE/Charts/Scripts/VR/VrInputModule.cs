using SEE.Controls;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Charts.Scripts.VR
{
	/// <summary>
	/// Extends the standard <see cref="BaseInputModule" /> for usage in VR.
	/// </summary>
	public class VrInputModule : BaseInputModule
	{
		/// <summary>
		/// Contains some settings used in this script.
		/// </summary>
		private ChartManager _chartManager;

		/// <summary>
		/// Manages the VR pointer used to interact with canvases.
		/// </summary>
		private VrPointer _pointer;

		/// <summary>
		/// Contains the users clicking information.
		/// </summary>
		private ChartAction _chartAction;

		public PointerEventData EventData { get; private set; }

		/// <summary>
		/// Calls methods for initialization and links the <see cref="VrPointer" /> script.
		/// </summary>
		protected override void Awake()
		{
			base.Awake();
			GetSettingData();
			_pointer = GameObject.FindGameObjectWithTag("Pointer").GetComponent<VrPointer>();
		}

		/// <summary>
		/// Links the <see cref="ChartManager" /> and gets its setting data.
		/// </summary>
		private void GetSettingData()
		{
			_chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
			_chartAction = GameObject.Find("VRPlayer").GetComponent<Actor>().ChartAction;
		}

		/// <summary>
		/// Initializes <see cref="EventData" />.
		/// </summary>
		protected override void Start()
		{
			EventData = new PointerEventData(eventSystem)
			{
				position = new Vector2(_pointer.Camera.pixelWidth / 2,
					_pointer.Camera.pixelHeight / 2)
			};
		}

		/// <summary>
		/// Sends a raycast and triggers actions depending on the users input.
		/// </summary>
		public override void Process()
		{
			eventSystem.RaycastAll(EventData, m_RaycastResultCache);
			EventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
			var ray = new Ray(_pointer.transform.position, _pointer.transform.forward);
			Physics.Raycast(ray, out var hitData, _chartManager.pointerLength);
			var colliderDistance = hitData.distance.Equals(0f)
				? _chartManager.pointerLength
				: hitData.distance;
			var canvasDistance = EventData.pointerCurrentRaycast.distance.Equals(0f)
				? _chartManager.pointerLength
				: EventData.pointerCurrentRaycast.distance;

			if (colliderDistance.CompareTo(canvasDistance) < 0)
				ExecutePhysical(hitData);
			else
				ExecuteCanvas();
		}

		private void ExecutePhysical(RaycastHit hitData)
		{
			HandlePointerExitAndEnter(EventData, hitData.transform.gameObject);
			ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.dragHandler);
			if (_chartAction.clickDown) Press(hitData.transform.gameObject);
			if (_chartAction.clickUp) Release(hitData.transform.gameObject);
		}

		private void ExecuteCanvas()
		{
			HandlePointerExitAndEnter(EventData, EventData.pointerCurrentRaycast.gameObject);
			ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.dragHandler);
			if (_chartAction.clickDown) Press(EventData.pointerCurrentRaycast.gameObject);
			if (_chartAction.clickUp) Release(EventData.pointerCurrentRaycast.gameObject);
		}

		/// <summary>
		/// Finds handlers on the object the user pressed on, that should react to the press and triggers them.
		/// </summary>
		private void Press(GameObject hitObject)
		{
			EventData.pointerPressRaycast = EventData.pointerCurrentRaycast;

			EventData.pointerPress =
				ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject);
			EventData.pointerDrag =
				ExecuteEvents.GetEventHandler<IDragHandler>(hitObject);

			ExecuteEvents.Execute(EventData.pointerPress, EventData,
				ExecuteEvents.pointerDownHandler);
			ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.beginDragHandler);
		}

		/// <summary>
		/// Finds handlers on the object the user released on, that should react to a release and triggers
		/// them.
		/// </summary>
		private void Release(GameObject hitObject)
		{
			var pointerRelease = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject);

			if (EventData.pointerPress == pointerRelease)
				ExecuteEvents.Execute(EventData.pointerPress, EventData,
					ExecuteEvents.pointerClickHandler);

			ExecuteEvents.Execute(EventData.pointerPress, EventData,
				ExecuteEvents.pointerUpHandler);
			ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.endDragHandler);

			EventData.pointerPress = null;
			EventData.pointerDrag = null;
			EventData.pointerCurrentRaycast.Clear();
		}
	}
}