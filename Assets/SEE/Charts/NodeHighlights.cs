// Copyright 2020 Robert Bohnsack
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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