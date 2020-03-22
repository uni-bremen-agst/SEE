using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Charts.Scripts
{
	/// <summary>
	/// Handles the dragging of charts.
	/// </summary>
	public class ChartDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
	{
		/// <summary>
		/// Contains position information of the chart.
		/// </summary>
		protected RectTransform chart;

		/// <summary>
		/// The size of the screen.
		/// </summary>
		private RectTransform _screenSize;

		/// <summary>
		/// The distance between the pointer and the middle of the chart.
		/// </summary>
		private Vector2 _distance;

		/// <summary>
		/// Gets information about the chart and the screen it is on.
		/// </summary>
		protected virtual void Awake()
		{
			chart = transform.parent.GetComponent<RectTransform>();
			_screenSize = chart.transform.parent.parent.GetComponent<RectTransform>();
		}

		/// <summary>
		/// Repositions the chart at the pointers position if it is on the screen.
		/// </summary>
		/// <param name="eventData">Contains information about the pointers position.</param>
		public virtual void OnDrag(PointerEventData eventData)
		{
			if (eventData.position.x > 0 &&
			    eventData.position.x < _screenSize.sizeDelta.x * _screenSize.lossyScale.x &&
			    eventData.position.y > 0 &&
			    eventData.position.y < _screenSize.sizeDelta.y * _screenSize.lossyScale.y)
				chart.position = new Vector2(eventData.position.x - _distance.x,
					eventData.position.y - _distance.y);
		}

		/// <summary>
		/// Saves the distance to the middle of the chart to keep it when dragging the chart.
		/// </summary>
		/// <param name="eventData">Contains information about the pointers position.</param>
		public virtual void OnPointerDown(PointerEventData eventData)
		{
			_distance = eventData.position - (Vector2) chart.position;
		}
	}
}