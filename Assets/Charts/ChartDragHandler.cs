using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Charts
{
	/// <summary>
	/// Handles the dragging of charts.
	/// </summary>
	public class ChartDragHandler : MonoBehaviour, IDragHandler
	{
		[SerializeField] private RectTransform chart;
		[SerializeField] private RectTransform canvas;

		/// <summary>
		/// Checks the current mouse position and moves the chart to the corresponding position on the canvas.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnDrag(PointerEventData eventData)
		{
			RectTransform pos = GetComponent<RectTransform>();
			chart.position = new Vector2(Input.mousePosition.x - pos.anchoredPosition.x * canvas.localScale.x,
				Input.mousePosition.y - pos.anchoredPosition.y * canvas.localScale.y);
		}
	}
}