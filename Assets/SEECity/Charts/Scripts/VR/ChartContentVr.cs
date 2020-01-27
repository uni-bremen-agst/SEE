using UnityEngine;

namespace SEECity.Charts.Scripts.VR
{
	public class ChartContentVr : ChartContent
	{
		public GameObject physicalOpen;
		public GameObject physicalClosed;

		public override void AreaSelection(Vector2 min, Vector2 max, bool direction)
		{
			float highlightDuration = _chartManager.highlightDuration;
			if (direction)
				foreach (GameObject marker in _activeMarkers)
				{
					Vector2 markerPos = marker.GetComponent<RectTransform>().anchoredPosition;
					if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y &&
						markerPos.y < max.y)
						marker.GetComponent<ChartMarker>().TriggerTimedHighlight(highlightDuration);
				}
			else
				foreach (GameObject marker in _activeMarkers)
				{
					Vector2 markerPos = marker.GetComponent<RectTransform>().anchoredPosition;
					if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y < min.y &&
						markerPos.y > max.y)
						marker.GetComponent<ChartMarker>().TriggerTimedHighlight(highlightDuration);
				}
		}
	}
}