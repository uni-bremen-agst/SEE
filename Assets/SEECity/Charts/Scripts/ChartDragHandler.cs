using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Handles the dragging of charts.
	/// </summary>
	public class ChartDragHandler : MonoBehaviour, IDragHandler
	{
		[SerializeField] private RectTransform chart;

		/// <summary>
		/// Checks the current mouse position and moves the chart to the corresponding position on the canvas.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnDrag(PointerEventData eventData)
		{
			RectTransform pos = GetComponent<RectTransform>();
			chart.position =
				new Vector2(Input.mousePosition.x - pos.anchoredPosition.x * pos.lossyScale.x,
					Input.mousePosition.y - pos.anchoredPosition.y * pos.lossyScale.y);
		}
	}
}