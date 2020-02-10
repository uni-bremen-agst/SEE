using UnityEngine;
using UnityEngine.UI;

namespace SEECity.Charts.Scripts.VR
{
	public class ChartContentVr : ChartContent
	{
		public GameObject physicalOpen;
		public GameObject physicalClosed;
		[SerializeField] private Toggle selectionToggle;

		protected override void Start()
		{
			base.Start();
			selectionToggle.gameObject.SetActive(true);
		}

		/// <summary>
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="direction"></param>
		public override void AreaSelection(Vector2 min, Vector2 max, bool direction)
		{
			if (direction)
				foreach (GameObject marker in ActiveMarkers)
				{
					Vector2 markerPos = marker.GetComponent<RectTransform>().anchoredPosition;
					if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y &&
					    markerPos.y < max.y)
						ChartManager.HighlightObject(
							marker.GetComponent<ChartMarker>().linkedObject);
				}
			else
				foreach (GameObject marker in ActiveMarkers)
				{
					Vector2 markerPos = marker.GetComponent<RectTransform>().anchoredPosition;
					if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y < min.y &&
					    markerPos.y > max.y)
						ChartManager.HighlightObject(
							marker.GetComponent<ChartMarker>().linkedObject);
				}
		}

		/// <summary>
		/// Activates or deactivates the selection mode. TODO: Not synced across charts.
		/// </summary>
		public void SetSelectionMode()
		{
			ChartManager.selectionMode = selectionToggle.isOn;
		}
	}
}