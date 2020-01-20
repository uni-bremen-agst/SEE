using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR;

namespace Assets.SEECity.Charts.Scripts.VR
{
	/// <summary>
	/// Handles the dragging and minimization of charts in VR.
	/// </summary>
	public class ChartMoveHandlerVR : ChartMoveHandler
	{
		private ChartContentVR _chartContent;

		/// <summary>
		/// The transform of the ChartCanvasVRContainer (Contains world space <see cref="Canvas" /> and 3D
		/// objects for the canvas to sit on).
		/// </summary>
		private Transform _parent;

		/// <summary>
		/// The camera the player sees through.
		/// </summary>
		private Camera _mainCamera;

		/// <summary>
		/// The source of which to take the inputs for scrolling with <see cref="_moveInOut" /> from.
		/// </summary>
		private SteamVR_Input_Sources _source;

		/// <summary>
		/// Contains the scrolling information for moving charts in or out.
		/// </summary>
		private SteamVR_Action_Vector2 _moveInOut;

        private float _minimumDistance = 1f;

        private float _maximumDistance;

		/// <summary>
		/// The speed to charts with when scrolling.
		/// </summary>
		private float _chartScrollSpeed;

		/// <summary>
		/// The <see cref="Camera" /> attached to the pointer.
		/// </summary>
		private Camera _pointerCamera;

		/// <summary>
		/// 3D representation of the chart when not minimized.
		/// </summary>
		private GameObject _physicalOpen;

		/// <summary>
		/// 3D representation of the chart when minimized.
		/// </summary>
		private GameObject _physicalClosed;

		/// <summary>
		/// The offset of the <see cref="Canvas" /> to <see cref="_physicalOpen" /> so the two don't clip.
		/// </summary>
		private readonly Vector3 _chartOffset = new Vector3(0, 0, -0.03f);

		protected override void Awake()
		{
			base.Awake();
			_parent = transform.parent.GetComponent<ChartContent>().Parent.transform;
			_mainCamera = Camera.main;
			_pointerCamera = GameObject.FindGameObjectWithTag("Pointer").GetComponent<Camera>();
			_chartContent = transform.parent.GetComponent<ChartContentVR>();
			_physicalOpen = _chartContent.PhysicalOpen;
			_physicalClosed = _chartContent.PhysicalClosed;
		}

		protected override void GetSettingData()
		{
			base.GetSettingData();
			_chartScrollSpeed = ChartManager.ChartScrollSpeed;
			_source = ChartManager.Source;
			_moveInOut = ChartManager.MoveInOut;
            //minDist
            _maximumDistance = ChartManager.PointerLength;
		}


		protected override void Update()
		{
			base.Update();
			_parent.LookAt(_parent.position - (_mainCamera.transform.position - _parent.position));
			ScrollInOut();
		}

		/// <summary>
		/// Checks if the player scrolled while moving a chart and if so, moves it towards or away from the
		/// player.
		/// </summary>
		private void ScrollInOut()
		{
			if (PointerDown)
				//TODO: Specify source
				if (_moveInOut.GetChanged(_source))
				{
					Vector3 direction = _pointerCamera.transform.position -
					                    GetComponent<RectTransform>().position;
                    float moveBy = _moveInOut.GetAxis(_source).y * _chartScrollSpeed * Time.deltaTime;
                    if (!(_moveInOut.GetAxis(_source).y < 0 && direction.magnitude < _minimumDistance + moveBy || _moveInOut.GetAxis(_source).y > 0 && direction.magnitude > _maximumDistance - moveBy))
                    {
                        _parent.position -= direction * moveBy;
                    }
				}
		}

		public override void OnDrag(PointerEventData eventData)
		{
			_parent.position = eventData.pointerCurrentRaycast.worldPosition -
			                   (transform.position - (_parent.position + _chartOffset)) -
			                   _chartOffset;
		}

		protected override void ToggleMinimize()
		{
			_physicalOpen.SetActive(Minimized);
			_physicalClosed.SetActive(!Minimized);
			base.ToggleMinimize();
		}
	}
}