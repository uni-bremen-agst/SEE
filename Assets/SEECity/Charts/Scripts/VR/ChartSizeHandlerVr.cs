using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts.VR
{
	public class ChartSizeHandlerVr : ChartSizeHandler
	{
		private ChartContentVr _chartContentVr;
		private RectTransform _virtualRealityCanvas;
		private GameObject _physicalOpen;
		private GameObject _physicalClosed;
		[SerializeField] private GameObject _contentSelectionBackground;
		private float _dropdownThickness = 100f;
		private float _physicalClosedPosition = 0.4575f * 600f;

		protected override void Awake()
		{
			base.Awake();
			_chartContentVr = transform.parent.GetComponent<ChartContentVr>();
			_virtualRealityCanvas = chart.parent.GetComponent<RectTransform>();
			_physicalOpen = _chartContentVr.physicalOpen;
			_physicalClosed = _chartContentVr.physicalClosed;
		}

		public override void OnDrag(PointerEventData eventData)
		{
			if (eventData.pointerCurrentRaycast.worldPosition != Vector3.zero)
			{
				RectTransform pos = GetComponent<RectTransform>();
				Vector3 oldPos = pos.position;
				pos.position = eventData.pointerCurrentRaycast.worldPosition;
				pos.anchoredPosition3D =
					new Vector3(pos.anchoredPosition.x, pos.anchoredPosition.y, 0);
				if (pos.anchoredPosition.x < minimumSize || pos.anchoredPosition.y < minimumSize)
					pos.position = oldPos;
				ChangeSize(pos.anchoredPosition.x, pos.anchoredPosition.y);
			}
		}

		protected override void ChangeSize(float width, float height)
		{
			base.ChangeSize(width, height);
			_virtualRealityCanvas.sizeDelta =
				new Vector2(width + _dropdownThickness, height + _dropdownThickness);
			_physicalOpen.transform.localScale = new Vector2(width / 600f, height / 600f);
			_physicalClosed.transform.localPosition = new Vector2(width / _physicalClosedPosition,
				-(height / _physicalClosedPosition));
			_contentSelectionBackground.transform.localScale = new Vector2(_contentSelectionBackground.transform.localScale.x, height);
		}
	}
}