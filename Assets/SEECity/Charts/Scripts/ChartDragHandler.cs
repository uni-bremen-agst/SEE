using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Handles the dragging of charts in non VR.
	/// </summary>
	public class ChartDragHandler : MonoBehaviour, IDragHandler
	{
		/// <summary>
		/// Contains the position of the chart on the <see cref="Canvas" />.
		/// </summary>
		[SerializeField] private RectTransform _chart;

		/// <summary>
		/// Checks the current mouse position and moves the chart to the corresponding position on the canvas.
		/// </summary>
		/// <param name="eventData">Event payload associated with pointer (mouse / touch) events.</param>
		public void OnDrag(PointerEventData eventData)
		{
			RectTransform pos = GetComponent<RectTransform>();
			_chart.position =
				new Vector2(Input.mousePosition.x - pos.anchoredPosition.x * pos.lossyScale.x,
					Input.mousePosition.y - pos.anchoredPosition.y * pos.lossyScale.y);
		}
	}
}