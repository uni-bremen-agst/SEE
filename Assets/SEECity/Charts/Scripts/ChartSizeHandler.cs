using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts
{
	/// <summary>
	/// Handles the resizing of charts.
	/// </summary>
	public class ChartSizeHandler : MonoBehaviour, IDragHandler
	{
		/// <summary>
		/// Contains some settings used in this script.
		/// </summary>
		private ChartManager _chartManager;

		/// <summary>
		/// The minimum size a chart can have for width and height.
		/// </summary>
		protected int MinimumSize;

		/// <summary>
		/// The objects that have to be moved individually when resizing the chart.
		/// </summary>
		[Header("For resizing"), SerializeField]
		private Transform dragButton;

		[SerializeField] private Transform topRight;
		[SerializeField] private Transform topLeft;
		[SerializeField] private Transform bottomRight;
		[SerializeField] private Transform bottomLeft;

		/// <summary>
		/// The script attached to the chart.
		/// </summary>
		private ChartContent _chartContent;

		/// <summary>
		/// Contains the size of the chart.
		/// </summary>
		protected RectTransform Chart;

		/// <summary>
		/// Initializes some attributes.
		/// </summary>
		protected virtual void Awake()
		{
			GetSettingData();
			Transform parent = transform.parent;
			_chartContent = parent.GetComponent<ChartContent>();
			Chart = parent.GetComponent<RectTransform>();
		}

		/// <summary>
		/// Links the <see cref="ChartManager" /> and gets its setting data.
		/// </summary>
		private void GetSettingData()
		{
			_chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
			MinimumSize = _chartManager.minimumSize;
		}

		/// <summary>
		/// Checks the current <see cref="PointerEventData.position" /> and calls
		/// <see cref="ChangeSize" /> to resize the chart accordingly.
		/// </summary>
		/// <param name="eventData">Contains the position data.</param>
		public virtual void OnDrag(PointerEventData eventData)
		{
			RectTransform pos = GetComponent<RectTransform>();
			Vector2 oldPos = pos.position;
			pos.position = eventData.position;
			if (pos.anchoredPosition.x / pos.lossyScale.x < MinimumSize ||
			    pos.anchoredPosition.y / pos.lossyScale.y < MinimumSize) pos.position = oldPos;
			ChangeSize(pos.anchoredPosition.x, pos.anchoredPosition.y);
		}

		/// <summary>
		/// Changes the width and height of the chart and its contents.
		/// </summary>
		/// <param name="width">The new width of the chart.</param>
		/// <param name="height">The new height of the chart.</param>
		protected virtual void ChangeSize(float width, float height)
		{
			RectTransform dataPanel = _chartContent.dataPanel;
			dataPanel.sizeDelta = new Vector2(width - 100, height - 100);
			dataPanel.anchoredPosition = new Vector2(width / 2, height / 2);
			RectTransform labelsPanel = _chartContent.labelsPanel;
			labelsPanel.sizeDelta = new Vector2(width, height);
			labelsPanel.anchoredPosition = new Vector2(width / 2, height / 2);
			RectTransform xDropdown = _chartContent.axisDropdownX.GetComponent<RectTransform>();
			xDropdown.anchoredPosition = new Vector2(width / 2, xDropdown.anchoredPosition.y);
			xDropdown.sizeDelta = new Vector2(width / 3, xDropdown.sizeDelta.y);
			RectTransform yDropdown = _chartContent.axisDropdownY.GetComponent<RectTransform>();
			yDropdown.anchoredPosition = new Vector2(yDropdown.anchoredPosition.x, height / 2);
			yDropdown.sizeDelta = new Vector2(height / 3, yDropdown.sizeDelta.y);
			Chart.sizeDelta = new Vector2(width, height);
			topRight.localPosition = new Vector2(width / 2, height / 2);
			topLeft.localPosition = new Vector2(-width / 2, height / 2);
			bottomRight.localPosition = new Vector2(width / 2, -height / 2);
			bottomLeft.localPosition = new Vector2(-width / 2, -height / 2);
			dragButton.localPosition = bottomRight.localPosition - new Vector3(25f, -25f);

			_chartContent.DrawData(false);
		}
	}
}