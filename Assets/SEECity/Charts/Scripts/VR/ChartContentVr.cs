using UnityEngine;
using UnityEngine.UI;

namespace SEECity.Charts.Scripts.VR
{
	/// <summary>
	/// The virtual reality version of <see cref="ChartContent" />.
	/// </summary>
	public class ChartContentVr : ChartContent
	{
		/// <summary>
		/// A cube behind the chart to make it look three dimensional.
		/// </summary>
		public GameObject physicalOpen;

		/// <summary>
		/// The minimized chart displayed as a cube.
		/// </summary>
		public GameObject physicalClosed;

		/// <summary>
		/// A checkbox to toggle the <see cref="ChartManager.selectionMode" />.
		/// </summary>
		[SerializeField] private Toggle selectionToggle;

		/// <summary>
		/// Activates the <see cref="selectionToggle" />.
		/// </summary>
		protected override void Start()
		{
			base.Start();
			selectionToggle.gameObject.SetActive(true);
		}

		/// <summary>
		/// VR version of <see cref="ChartContent.AreaSelection" />.
		/// </summary>
		/// <param name="min">The starting edge of the rectangle.</param>
		/// <param name="max">The ending edge of the rectangle.</param>
		/// <param name="direction">If <see cref="max" /> lies above or below <see cref="min" /></param>
		public override void AreaSelection(Vector2 min, Vector2 max, bool direction)
		{
			if (direction)
				foreach (var marker in activeMarkers)
				{
					var markerPos = marker.GetComponent<RectTransform>().anchoredPosition;
					if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y &&
					    markerPos.y < max.y)
						chartManager.HighlightObject(
							marker.GetComponent<ChartMarker>().linkedObject);
				}
			else
				foreach (var marker in activeMarkers)
				{
					var markerPos = marker.GetComponent<RectTransform>().anchoredPosition;
					if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y < min.y &&
					    markerPos.y > max.y)
						chartManager.HighlightObject(
							marker.GetComponent<ChartMarker>().linkedObject);
				}
		}

		/// <summary>
		/// Activates or deactivates the selection mode. TODO: Not synced across charts.
		/// </summary>
		public void SetSelectionMode()
		{
			chartManager.selectionMode = selectionToggle.isOn;
		}
	}
}