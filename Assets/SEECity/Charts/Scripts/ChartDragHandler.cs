using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts
{
	public class ChartDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
	{
		protected RectTransform _chart;
		private RectTransform _screenSize;
		private Vector2 _distance;

		protected virtual void Awake()
		{
			_chart = transform.parent.GetComponent<RectTransform>();
			_screenSize = _chart.transform.parent.parent.GetComponent<RectTransform>();
		}

		public virtual void OnDrag(PointerEventData eventData)
		{
			if (eventData.position.x > 0 &&
			    eventData.position.x < _screenSize.sizeDelta.x * _screenSize.lossyScale.x &&
			    eventData.position.y > 0 &&
			    eventData.position.y < _screenSize.sizeDelta.y * _screenSize.lossyScale.y)
				_chart.position = new Vector2(eventData.position.x - _distance.x,
					eventData.position.y - _distance.y);
		}

		public virtual void OnPointerDown(PointerEventData eventData)
		{
			_distance = eventData.position - (Vector2) _chart.position;
		}
	}
}