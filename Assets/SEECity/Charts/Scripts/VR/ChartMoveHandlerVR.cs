using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEECity.Charts.Scripts.VR
{
	public class ChartMoveHandlerVR : ChartMoveHandler
	{
        private ChartContent _chartContent;
        private Camera _mainCamera = Camera.main;
        [SerializeField] private GameObject _physicalOpen = null;
		[SerializeField] private GameObject _physicalClosed = null;
        private float _chartOffset = -0.03f;

        protected override void Awake()
        {
            base.Awake();
            _chartContent = transform.parent.GetComponent<ChartContent>();
        }

        protected override void Update()
        {
            base.Update();
            FacePlayer();
        }

        private void FacePlayer()
        {
            _chartContent.Parent.transform.LookAt(_mainCamera.transform);
        }

        public override void OnDrag(PointerEventData eventData)
		{
			RectTransform pos = GetComponent<RectTransform>();
            Vector3 xPos = eventData.pointerCurrentRaycast.worldPosition;
            _chartContent.Parent.transform.position = xPos - new Vector3(pos.anchoredPosition.x * pos.lossyScale.x, pos.anchoredPosition.y * pos.lossyScale.y, 0) - new Vector3(0, 0, _chartOffset);
        }

		protected override void ToggleMinimize()
		{
			_physicalOpen.SetActive(_minimized);
			_physicalClosed.SetActive(!_minimized);
            base.ToggleMinimize();
		}
	}
}