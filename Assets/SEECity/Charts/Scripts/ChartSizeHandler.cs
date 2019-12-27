using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Handles the resizing of charts.
	/// </summary>
	public class ChartSizeHandler : MonoBehaviour, IDragHandler
	{
		/// <summary>
		/// The script attached to the chart.
		/// </summary>
		[SerializeField] private ChartContent _chart;

		/// <summary>
		/// The minimum size a chart can have for width and height.
		/// </summary>
		private const int MinimumSize = 400;

		/// <summary>
		/// Checks the current <see cref="Input.mousePosition" /> and calls
		/// <see cref="ChartContent.ChangeSize" /> to resize the chart accordingly.
		/// </summary>
		/// <param name="eventData">Event payload associated with pointer (mouse / touch) events.</param>
		public void OnDrag(PointerEventData eventData)
		{
			RectTransform pos = GetComponent<RectTransform>();
			Vector2 oldPos = new Vector2(pos.position.x, pos.position.y);
			pos.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
			if (pos.anchoredPosition.x / pos.lossyScale.x < MinimumSize ||
			    pos.anchoredPosition.y / pos.lossyScale.y < MinimumSize) pos.position = oldPos;
			_chart.ChangeSize(pos.anchoredPosition.x, pos.anchoredPosition.y);
		}
	}
}