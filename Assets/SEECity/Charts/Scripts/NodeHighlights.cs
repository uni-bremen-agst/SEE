using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEECity.Charts.Scripts
{
	/// <summary>
	/// Manages the highlighting and visibility of <see cref="SEE.DataModel.Node" />s.
	/// </summary>
	public class NodeHighlights : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
		IPointerClickHandler
	{
		private ChartManager _chartManager;
		public IDictionary showInChart = new Dictionary<ChartContent, bool>();
		public ScrollViewToggle scrollViewToggle;

		private void Awake()
		{
			_chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			for (var i = 0; i < transform.childCount; i++)
				if (transform.GetChild(i).gameObject.name.Equals(gameObject.name + "(Clone)"))
				{
					_chartManager.Accentuate(gameObject);
					return;
				}
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			for (var i = 0; i < transform.childCount; i++)
				if (transform.GetChild(i).gameObject.name.Equals(gameObject.name + "(Clone)"))
				{
					_chartManager.Accentuate(gameObject);
					return;
				}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			_chartManager.HighlightObject(gameObject);
			StartCoroutine(Accentuate());
		}

		private IEnumerator Accentuate()
		{
			yield return new WaitForEndOfFrame();
			_chartManager.Accentuate(gameObject);
		}
	}
}