using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts.VR
{
	public class ChartSizeHandlerVr : ChartSizeHandler
	{
		private ChartContentVr _chartContent;
		private RectTransform _virtualRealityCanvas;
		private GameObject _physicalOpen;
		private GameObject _physicalClosed;
		private float _dropdownThickness = 100f;
		private float _physicalClosedPosition = 0.4575f * 600f;

		protected override void Awake()
		{
			base.Awake();
			_chartContent = transform.parent.GetComponent<ChartContentVr>();
			_virtualRealityCanvas = Chart.parent.GetComponent<RectTransform>();
			_physicalOpen = _chartContent.physicalOpen;
			_physicalClosed = _chartContent.physicalClosed;
		}

		public override void OnDrag(PointerEventData eventData)
		{
            RectTransform pos = GetComponent<RectTransform>();
            Vector3 oldPos = pos.position;
            pos.position = eventData.pointerCurrentRaycast.worldPosition;
            pos.anchoredPosition3D = new Vector3(pos.anchoredPosition.x, pos.anchoredPosition.y, 0);
            if (pos.anchoredPosition.x < _minimumSize || pos.anchoredPosition.y < _minimumSize) pos.position = oldPos;
            ChangeSize(pos.anchoredPosition.x, pos.anchoredPosition.y);
		}

		protected override void ChangeSize(float width, float height)
		{
			base.ChangeSize(width, height);
			_virtualRealityCanvas.sizeDelta =
				new Vector2(width + _dropdownThickness, height + _dropdownThickness);
            _physicalOpen.transform.localScale = new Vector2(width / 600f, height / 600f);
            _physicalClosed.transform.localPosition = new Vector2(width / _physicalClosedPosition, -(height / _physicalClosedPosition));
			//TODO: Change Physical Open Size and move Physical closed.
		}
	}
}