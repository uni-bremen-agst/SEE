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
		/// Contains some settings used in this script.
		/// </summary>
		private ChartManager _chartManager;

		/// <summary>
		/// The minimum size a chart can have for width and height.
		/// </summary>
		private int _minimumSize;

		[Header("For resizing"), SerializeField]
		private Transform _dragButton = null;

		[SerializeField] private Transform _topRight = null;
		[SerializeField] private Transform _topLeft = null;
		[SerializeField] private Transform _bottomRight = null;
		[SerializeField] private Transform _bottomLeft = null;

		/// <summary>
		/// The script attached to the chart.
		/// </summary>
		private ChartContent _chartContent;

		/// <summary>
		/// Contains the size of the chart.
		/// </summary>
		private RectTransform _chart;

		private void Awake()
		{
			GetSettingData();
			GameObject chart = GameObject.FindGameObjectWithTag("Chart");
			_chartContent = chart.GetComponent<ChartContent>();
			_chart = chart.GetComponent<RectTransform>();
		}

		/// <summary>
		/// Links the <see cref="ChartManager" /> and gets its setting data.
		/// </summary>
		private void GetSettingData()
		{
			_chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
			_minimumSize = _chartManager.MinimumSize;
		}

		/// <summary>
		/// Checks the current <see cref="Input.mousePosition" /> and calls
		/// <see cref="ChangeSize" /> to resize the chart accordingly.
		/// </summary>
		/// <param name="eventData">Event payload associated with pointer (mouse / touch) events.</param>
		public void OnDrag(PointerEventData eventData)
		{
			RectTransform pos = GetComponent<RectTransform>();
			Vector2 oldPos = new Vector2(pos.position.x, pos.position.y);
			pos.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
			if (pos.anchoredPosition.x / pos.lossyScale.x < _minimumSize ||
			    pos.anchoredPosition.y / pos.lossyScale.y < _minimumSize) pos.position = oldPos;
			ChangeSize(pos.anchoredPosition.x, pos.anchoredPosition.y);
		}

		/// <summary>
		/// Changes the width and height of the chart.
		/// </summary>
		/// <param name="width">The new width of the chart.</param>
		/// <param name="height">The new height of the chart.</param>
		public void ChangeSize(float width, float height)
		{
			RectTransform dataPanel = _chartContent.DataPanel;
			dataPanel.sizeDelta = new Vector2(width - 100, height - 100);
			dataPanel.anchoredPosition = new Vector2(width / 2, height / 2);
			RectTransform _labelsPanel = _chartContent.LabelsPanel;
			_labelsPanel.sizeDelta = new Vector2(width, height);
			_labelsPanel.anchoredPosition = new Vector2(width / 2, height / 2);
			RectTransform xDropdown = _chartContent.AxisDropdownX.GetComponent<RectTransform>();
			xDropdown.anchoredPosition = new Vector2(width / 2, xDropdown.anchoredPosition.y);
			xDropdown.sizeDelta = new Vector2(width / 3, xDropdown.sizeDelta.y);
			RectTransform yDropdown = _chartContent.AxisDropdownY.GetComponent<RectTransform>();
			yDropdown.anchoredPosition = new Vector2(yDropdown.anchoredPosition.x, height / 2);
			yDropdown.sizeDelta = new Vector2(height / 3, yDropdown.sizeDelta.y);
			_chart.sizeDelta = new Vector2(width, height);
			_topRight.localPosition = new Vector2(width / 2, height / 2);
			_topLeft.localPosition = new Vector2(-width / 2, height / 2);
			_bottomRight.localPosition = new Vector2(width / 2, -height / 2);
			_bottomLeft.localPosition = new Vector2(-width / 2, -height / 2);
			_dragButton.localPosition = _bottomRight.localPosition - new Vector3(25f, -25f);

			_chartContent.DrawData();
		}
	}
}