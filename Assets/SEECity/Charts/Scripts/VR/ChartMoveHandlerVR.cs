using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using Valve.VR;

namespace Assets.SEECity.Charts.Scripts.VR
{
	public class ChartMoveHandlerVR : ChartMoveHandler
	{
        private ChartContent _chartContent;
        private Camera _mainCamera;
        private SteamVR_Input_Sources _source;
        private SteamVR_Action_Vector2 _moveInOut;
        private float _chartSpeed;
        private Camera _pointerCamera;
        
        [SerializeField] private GameObject _physicalOpen = null;
		[SerializeField] private GameObject _physicalClosed = null;
        private Vector3 _chartOffset = new Vector3(0, 0, -0.03f);

        protected override void Awake()
        {
            base.Awake();
            _chartContent = transform.parent.GetComponent<ChartContent>();
            _mainCamera = Camera.main;
            _pointerCamera = GameObject.FindGameObjectWithTag("Pointer").GetComponent<Camera>();
        }

        protected override void GetSettingData()
        {
            base.GetSettingData();
            _chartSpeed = _chartManager.ChartSpeed;
            _source = _chartManager.Source;
            _moveInOut = _chartManager.MoveInOut;
        }

        protected override void Update()
        {
            base.Update();
            Transform chart = _chartContent.Parent.transform;
            chart.LookAt(chart.position - (_mainCamera.transform.position - chart.position));
            if (_pointerDown)
            {
                //TODO: Specify source
                if (_moveInOut.axis.y != 0)
                {
                    Debug.Log(_moveInOut.axis.y);
                    Vector3 direction = _pointerCamera.transform.position - GetComponent<RectTransform>().position;
                    _chartContent.Parent.transform.position -= direction * _moveInOut.axis.y * _chartSpeed * Time.deltaTime;
                }
            }
        }

        public override void OnDrag(PointerEventData eventData)
		{
            Vector3 diff = transform.position - (_chartContent.Parent.transform.position + _chartOffset);
            _chartContent.Parent.transform.position = eventData.pointerCurrentRaycast.worldPosition - diff - _chartOffset;
        }

        protected override void ToggleMinimize()
		{
			_physicalOpen.SetActive(_minimized);
			_physicalClosed.SetActive(!_minimized);
            base.ToggleMinimize();
		}
	}
}