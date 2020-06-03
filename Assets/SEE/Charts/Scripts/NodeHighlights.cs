using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace SEE.Charts.Scripts
{
	/// <summary>
	/// Manages the highlighting and visibility of <see cref="DataModel.Node" />s.
	/// </summary>
	public class NodeHighlights : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
		IPointerClickHandler
	{
		/// <summary>
		/// Determines if this objects node will be displayed in charts.
		/// </summary>
		public IDictionary showInChart = new Dictionary<ChartContent, bool>();

		/// <summary>
		/// A toggle linked to this object.
		/// </summary>
		[FormerlySerializedAs("ScrollViewToggle")]
		public ScrollViewToggle scrollViewToggle;

		/// <summary>
		/// Accentuates this object and all linked markers when the user starts hovering over it if it was
		/// highlighted.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerEnter(PointerEventData eventData)
		{
			for (var i = 0; i < transform.childCount; i++)
				if (transform.GetChild(i).gameObject.name.Equals(gameObject.name + "(Clone)"))
				{
					ChartManager.Accentuate(gameObject);
					return;
				}
		}

		/// <summary>
		/// Deactivates accentuation of this object and all linked markers when the user stops hovering over
		/// it.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerExit(PointerEventData eventData)
		{
			for (var i = 0; i < transform.childCount; i++)
				if (transform.GetChild(i).gameObject.name.Equals(gameObject.name + "(Clone)"))
				{
					ChartManager.Accentuate(gameObject);
					return;
				}
		}

		/// <summary>
		/// Highlights this object and all linked markers.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerClick(PointerEventData eventData)
		{
			ChartManager.HighlightObject(gameObject, false);
			StartCoroutine(Accentuate());
		}

		/// <summary>
		/// Calls <see cref="Accentuate" /> in the next frame.
		/// </summary>
		/// <returns></returns>
		private IEnumerator Accentuate()
		{
			yield return new WaitForEndOfFrame();
			ChartManager.Accentuate(gameObject);
		}
	}
}