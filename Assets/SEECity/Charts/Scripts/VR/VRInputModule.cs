using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR;

namespace Assets.SEECity.Charts.Scripts.VR
{
	public class VRInputModule : BaseInputModule
	{
		private GameManager _gameManager;

		private VRPointer _pointer;

		private SteamVR_Input_Sources _source;
		private SteamVR_Action_Boolean _click;

		public PointerEventData EventData { get; private set; }

		protected override void Awake()
		{
			base.Awake();
			GetSettingData();
			_pointer = GameObject.FindGameObjectWithTag("Pointer").GetComponent<VRPointer>();
		}

		private void GetSettingData()
		{
			_gameManager = GameObject.FindGameObjectWithTag("GameManager")
				.GetComponent<GameManager>();
			_source = _gameManager.Source;
			_click = _gameManager.Click;
		}

		protected override void Start()
		{
			EventData = new PointerEventData(eventSystem)
			{
				position = new Vector2(_pointer.Camera.pixelWidth / 2,
					_pointer.Camera.pixelHeight / 2)
			};
		}

		public override void Process()
		{
			eventSystem.RaycastAll(EventData, m_RaycastResultCache);
			EventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
			HandlePointerExitAndEnter(EventData, EventData.pointerCurrentRaycast.gameObject);
			ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.dragHandler);
			if (_click.GetStateDown(_source)) Press();
			if (_click.GetStateUp(_source)) Release();
		}

		private void Press()
		{
			EventData.pointerPressRaycast = EventData.pointerCurrentRaycast;

			EventData.pointerPress =
				ExecuteEvents.GetEventHandler<IPointerClickHandler>(EventData.pointerPressRaycast
					.gameObject);
			EventData.pointerDrag =
				ExecuteEvents.GetEventHandler<IDragHandler>(
					EventData.pointerPressRaycast.gameObject);

			ExecuteEvents.Execute(EventData.pointerPress, EventData,
				ExecuteEvents.pointerDownHandler);
			ExecuteEvents.Execute(EventData.pointerDrag, EventData, ExecuteEvents.beginDragHandler);
		}

		private void Release()
		{
			GameObject pointerRelease =
				ExecuteEvents.GetEventHandler<IPointerClickHandler>(EventData.pointerCurrentRaycast
					.gameObject);

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