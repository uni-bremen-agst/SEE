using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts.VR
{
	public class ChartSizeHandlerVR : ChartSizeHandler
	{
		private ChartContentVR _chartContent;
		private RectTransform _virtualRealityCanvas;
		private GameObject _physicalOpen;
		private GameObject _physicalClosed;
		private float _dropdownThickness = 100f;
		private float _physicalClosedPosition;

		protected override void Awake()
		{
			base.Awake();
			_chartContent = transform.parent.GetComponent<ChartContentVR>();
			_virtualRealityCanvas = Chart.parent.GetComponent<RectTransform>();
			_physicalOpen = _chartContent.physicalOpen;
			_physicalClosed = _chartContent.physicalClosed;
		}

		public override void OnDrag(PointerEventData eventData)
		{
		}

		protected override void ChangeSize(float width, float height)
		{
			base.ChangeSize(width, height);
			_virtualRealityCanvas.sizeDelta =
				new Vector2(width + _dropdownThickness, height + _dropdownThickness);
			//TODO: Change Physical Open Size and move Physical closed.
		}
	}
}