using UnityEngine;

namespace Assets.Charts
{
	/// <summary>
	/// Contains all options the user has for content on the two axes.
	/// </summary>
	public enum AxisContent
	{
		LocalScaleX,
		LocalScaleY
	}

	/// <summary>
	/// Contains all the information needed to create the next chart.
	/// </summary>
	public class ChartCreator : MonoBehaviour
	{
		private AxisContent _xAxisContent;
		private AxisContent _yAxisContent;
		[SerializeField] private GameObject _chartPrefab;
		[SerializeField] private Transform _chartsCanvas;

		public void CreateChart()
		{
			ChartContent content =
				Instantiate(_chartPrefab, _chartsCanvas).GetComponent<ChartContent>();
			content.Initialize(_xAxisContent, _yAxisContent);
			gameObject.SetActive(false);
		}

		public void SetXAxisContent(int content)
		{
			switch (content)
			{
				case 1:
					_xAxisContent = AxisContent.LocalScaleX;
					break;
				case 2:
					_xAxisContent = AxisContent.LocalScaleY;
					break;
			}
		}
	}
}