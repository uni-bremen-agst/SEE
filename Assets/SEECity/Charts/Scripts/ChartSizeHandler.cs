using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Charts.Scripts
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
		/// The script attached to the chart.
		/// </summary>
		private ChartContent _chartContent;

		/// <summary>
		/// The minimum size a chart can have for width and height.
		/// </summary>
		protected int minimumSize;

		/// <summary>
		/// The objects that have to be moved individually when resizing the chart.
		/// </summary>
		[Header("For resizing"), SerializeField]
		private Transform dragButton;

		/// <summary>
		/// The warning displayed when no data is displayed in the chart.
		/// </summary>
		[SerializeField] private RectTransform noDataWarning;

		/// <summary>
		/// Top right area of the chart.
		/// </summary>
		[SerializeField] private Transform topRight;

		/// <summary>
		/// Top left area of the chart.
		/// </summary>
		[SerializeField] private Transform topLeft;

		/// <summary>
		/// Bottom right area of the chart.
		/// </summary>
		[SerializeField] private Transform bottomRight;

		/// <summary>
		/// Bottom left area of the chart.
		/// </summary>
		[SerializeField] private Transform bottomLeft;

		/// <summary>
		/// Area on the right of the chart to select the nodes to be displayed in the chart.
		/// </summary>
		[SerializeField] private RectTransform contentSelection;

		/// <summary>
		/// The content of the <see cref="contentSelection" />.
		/// </summary>
		[SerializeField] private RectTransform scrollView;

		/// <summary>
		/// The text above the <see cref="scrollView" />.
		/// </summary>
		[SerializeField] private RectTransform contentSelectionHeader;

		/// <summary>
		/// Contains the size of the chart.
		/// </summary>
		protected RectTransform chart;

		/// <summary>
		/// Initializes some attributes.
		/// </summary>
		protected virtual void Awake()
		{
			GetSettingData();
			var parent = transform.parent;
			_chartContent = parent.GetComponent<ChartContent>();
			chart = parent.GetComponent<RectTransform>();
		}

		/// <summary>
		/// Links the <see cref="ChartManager" /> and gets its setting data.
		/// </summary>
		private void GetSettingData()
		{
			_chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
			minimumSize = _chartManager.minimumSize;
		}

		/// <summary>
		/// Checks the current <see cref="PointerEventData.position" /> and calls
		/// <see cref="ChangeSize" /> to resize the chart accordingly.
		/// </summary>
		/// <param name="eventData">Contains the position data.</param>
		public virtual void OnDrag(PointerEventData eventData)
		{
			var pos = GetComponent<RectTransform>();
			Vector2 oldPos = pos.position;
			pos.position = eventData.position;
			var anchoredPos = pos.anchoredPosition;
			if (anchoredPos.x / pos.lossyScale.x < minimumSize ||
			    anchoredPos.y / pos.lossyScale.y < minimumSize) pos.position = oldPos;
			anchoredPos = pos.anchoredPosition;
			ChangeSize(anchoredPos.x, anchoredPos.y);
		}

		/// <summary>
		/// Changes the width and height of the chart and its contents.
		/// </summary>
		/// <param name="width">The new width of the chart.</param>
		/// <param name="height">The new height of the chart.</param>
		protected virtual void ChangeSize(float width, float height)
		{
			var dataPanel = _chartContent.dataPanel;
			dataPanel.sizeDelta = new Vector2(width - 80, height - 80);
			dataPanel.anchoredPosition = new Vector2(width / 2, height / 2);
			noDataWarning.sizeDelta = new Vector2(width - 150, height - 150);
			var labelsPanel = _chartContent.labelsPanel;
			labelsPanel.sizeDelta = new Vector2(width, height);
			labelsPanel.anchoredPosition = new Vector2(width / 2, height / 2);
			var xDropdown = _chartContent.axisDropdownX.GetComponent<RectTransform>();
			xDropdown.anchoredPosition = new Vector2(width / 2, xDropdown.anchoredPosition.y);
			xDropdown.sizeDelta = new Vector2(width / 2, xDropdown.sizeDelta.y);
			var yDropdown = _chartContent.axisDropdownY.GetComponent<RectTransform>();
			yDropdown.anchoredPosition = new Vector2(yDropdown.anchoredPosition.x, height / 2);
			yDropdown.sizeDelta = new Vector2(height / 2, yDropdown.sizeDelta.y);
			chart.sizeDelta = new Vector2(width, height);
			topRight.localPosition = new Vector2(width / 2, height / 2);
			topLeft.localPosition = new Vector2(-width / 2, height / 2);
			bottomRight.localPosition = new Vector2(width / 2, -height / 2);
			bottomLeft.localPosition = new Vector2(-width / 2, -height / 2);
			dragButton.localPosition = bottomRight.localPosition - new Vector3(20f, -20f);
			contentSelection.anchoredPosition =
				new Vector2(width / 2 + contentSelection.sizeDelta.x / 2, 0);
			contentSelection.sizeDelta = new Vector2(contentSelection.sizeDelta.x, height);
			scrollView.sizeDelta = new Vector2(scrollView.sizeDelta.x, height - 50);
			contentSelectionHeader.anchoredPosition = new Vector2(0, height / 2 - 20);

			if (_chartContent.CitySize > 50)
			{
				if (_chartContent.Drawing == null)
					_chartContent.Drawing = StartCoroutine(_chartContent.QueueDraw());
			}
			else
			{
				_chartContent.DrawData(false);
			}
		}

		/// <summary>
		/// Toggles the active state of <see cref="contentSelection" />. Called by Unity.
		/// </summary>
		public void ToggleContentSelection()
		{
			contentSelection.gameObject.SetActive(!contentSelection.gameObject.activeInHierarchy);
		}
	}
}