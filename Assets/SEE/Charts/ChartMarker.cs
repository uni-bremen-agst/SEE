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

using SEE.Controls;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEE.Charts.Scripts
{
	/// <summary>
	/// Contains the logic for the markers representing entries linked to objects in the chart.
	/// </summary>
	public class ChartMarker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		//User Variables from ChartManager
		private float _cameraDistance;
		private bool _moveWithRotation;
		private float _cameraFlightTime;
		private float _clickDelay;
		private float _highlightDuration;

		/// <summary>
		/// The <see cref="GameObject" /> in the code city that is connected with this button.
		/// </summary>
		[HideInInspector] public GameObject linkedObject;

		/// <summary>
		/// The toggle linked to this marker.
		/// </summary>
		public ScrollViewToggle ScrollViewToggle { private get; set; }

		/// <summary>
		/// The active <see cref="Camera" /> in the scene.
		/// </summary>
		private Camera _activeCamera;

		/// <summary>
		/// The currently running camera movement <see cref="Coroutine" />.
		/// </summary>
		private Coroutine _cameraMoving;

		/// <summary>
		/// Determines if a second click happened during <see cref="_clickDelay" />.
		/// </summary>
		private bool _waiting;

		/// <summary>
		/// Determines if <see cref="WaitForDoubleClick" /> is currently running.
		/// </summary>
		private bool _runningClick;

		/// <summary>
		/// The currently running <see cref="TimedHighlightRoutine" />.
		/// </summary>
		public Coroutine TimedHighlight { get; private set; }

		/// <summary>
		/// Counts the time <see cref="TimedHighlight" /> has been running for.
		/// </summary>
		public float HighlightTime { get; private set; }

		/// <summary>
		/// Tells if <see cref="TimedHighlight" /> was running when script was deactivated due to minimization
		/// of the chart.
		/// </summary>
		private bool _reactivateHighlight;

		/// <summary>
		/// The <see cref="GameObject" /> making the marker look highlighted when active.
		/// </summary>
		[Header("Highlight Properties"), SerializeField]
		private GameObject markerHighlight;

		/// <summary>
		/// The prefab containing the <see cref="LineRenderer" /> that creates the beam above highlighted
		/// objects.
		/// </summary>
		[SerializeField] private GameObject highlightLine;

		/// <summary>
		/// The length of the beam appearing above highlighted objects.
		/// </summary>
		private float _highlightLineLength;

		/// <summary>
		/// True iff the marker is accentuated.
		/// </summary>
		private bool _accentuated;

		/// <summary>
		/// A text popup containing useful information about the marker and its <see cref="linkedObject" />.
		/// </summary>
		[Header("Other"), SerializeField] 
		private TextMeshProUGUI infoText;

		/// <summary>
		/// Calls methods for initialization.
		/// </summary>
		private void Awake()
		{
			GetSettingData();
		}

		/// <summary>
		/// Links the <see cref="ChartManager" /> and gets its setting data.
		/// </summary>
		private void GetSettingData()
		{
			ChartManager chartManager             = ChartManager.Instance;
			_cameraDistance                       = chartManager.cameraDistance;
			_moveWithRotation                     = chartManager.moveWithRotation;
			_cameraFlightTime                     = chartManager.cameraFlightTime;
			_clickDelay                           = chartManager.clickDelay;
			_highlightDuration                    = chartManager.highlightDuration;
			_highlightLineLength                  = chartManager.highlightLineLength;
		}

		/// <summary>
		/// Reactivates the highlight if a previous marker linked to the same <see cref="linkedObject" />
		/// highlighted it.
		/// </summary>
		private void Start()
		{
			for (var i = 0; i < linkedObject.transform.childCount; i++)
			{
				var child = linkedObject.transform.GetChild(i);
                if (child.name.Equals(linkedObject.name + "(Clone)"))
                {
				    TriggerTimedHighlight(ChartManager.Instance.highlightDuration, false, false);
				    break;
                }
			}
		}

		/// <summary>
		/// Adds the time that passed since the last <see cref="Update" /> to the <see cref="HighlightTime" />.
		/// </summary>
		private void Update()
		{
            if (TimedHighlight != null)
            {
                HighlightTime += Time.deltaTime;
            }
		}

		/// <summary>
		/// Called by Unity when the button assigned to the <see cref="ChartMarker" /> is pressed.
		/// </summary>
		public void ButtonClicked()
		{
			if (_runningClick)
            {
				_waiting = false;
            }
			else
            {
				StartCoroutine(WaitForDoubleClick());
            }
		}

		/// <summary>
		/// Checks if one or two clicks happen in a given interval.
		/// </summary>
		/// <returns></returns>
		private IEnumerator WaitForDoubleClick()
		{
			_runningClick = true;
			_waiting = true;
			yield return new WaitForSeconds(_clickDelay);
			if (_waiting)
			{
				ChartManager.HighlightObject(linkedObject, false);
			}
			else
			{
				ShowLinkedObject();
				HighlightLinkedObjectToggle(true);
			}

			_waiting = false;
			_runningClick = false;
		}

		/// <summary>
		/// Toggles the highlight of the <see cref="linkedObject" /> and this marker.
		/// </summary>
		private void HighlightLinkedObjectToggle(bool highlight)
		{
			if (highlight)
			{
                SetHighlighting(linkedObject, true);
			}
			else
            {
                SetHighlighting(linkedObject, false);
				_accentuated = false;
			}

			markerHighlight.SetActive(highlight);
			if (ScrollViewToggle)
			{
				ScrollViewToggle.SetHighlighted(highlight);
			}
		}

		/// <summary>
		/// Copy of the <see cref="linkedObject" /> with different material to make it look highlighted.
		/// </summary>
		private GameObject _highlightCopy;

		private void SetHighlighting(GameObject go, bool highlight)
        {
            if (go.TryGetComponent(out InteractableObject interactable))
            {
                interactable.SetSelect(highlight, true);
            }
		}

		/// <summary>
		/// Toggles the highlights this marker and its <see cref="linkedObject" /> for a given
        /// amount of time. Reactivates it after deactivation if
        /// <see cref="ChartManager.selectionMode"/> is toggled on.
		/// </summary>
		/// <param name="time">How long the highlight will last.</param>
		/// <param name="reenable">Overwrites the selection mode to reactivate the highlight
        /// after deactivation.
		/// </param>
		/// <param name="scrollView">If this is triggered by a <see cref="ScrollViewToggle" />
        /// or not.</param>
		public void TriggerTimedHighlight(float time, bool reenable, bool scrollView)
		{
			if (scrollView)
			{
				if (TimedHighlight == null)
                {
				    HighlightLinkedObjectToggle(!_highlightCopy);
                }
			}
			else
			{
				bool reactivate = false;

				if (TimedHighlight != null)
				{
					StopCoroutine(TimedHighlight);
					HighlightLinkedObjectToggle(false);
					TimedHighlight = null;
                    if (ChartManager.Instance.selectionMode || reenable)
                    {
                        reactivate = true;
                    }
				}
				else
				{
					reactivate = true;
				}

                if (reactivate)
                {
                    TimedHighlight = StartCoroutine(TimedHighlightRoutine(time));
                }
			}
		}

		/// <summary>
		/// The <see cref="Coroutine" /> stopping the highlight after the given time has passed.
		/// </summary>
		/// <param name="time">The time after which to stop the highlight.</param>
		/// <returns></returns>
		private IEnumerator TimedHighlightRoutine(float time)
		{
			if (time < _highlightDuration)
            {
				HighlightTime = _highlightDuration - time;
            }
			else
            {
                HighlightTime = 0.0f;
            }

			HighlightLinkedObjectToggle(true);
			yield return new WaitForSeconds(time);
            while (ChartManager.Instance.selectionMode)
            {
                yield return new WaitForEndOfFrame();
            }
			HighlightLinkedObjectToggle(false);
			TimedHighlight = null;
		}

		/// <summary>
		/// Moves the camera to view the <see cref="linkedObject />.
		/// </summary>
		private void ShowLinkedObject()
		{
			_activeCamera = Camera.main;
			Vector3 cameraPos = _activeCamera.transform.position;

			if (_moveWithRotation)
			{
				if (_cameraMoving != null)
				{
					StopCoroutine(_cameraMoving);
					_cameraMoving = null;
				}

				Vector3 linkedPos = linkedObject.transform.position;
				Vector3 lookPos = linkedPos - cameraPos;
				_cameraMoving = StartCoroutine(MoveCameraTo(
					Vector3.MoveTowards(cameraPos, linkedPos, lookPos.magnitude - _cameraDistance),
					Quaternion.LookRotation(lookPos)));
			}
			else
			{
				if (_cameraMoving != null)
				{
					StopCoroutine(_cameraMoving);
					_cameraMoving = null;
				}

				Vector3 pos = linkedObject.transform.position;
				_cameraMoving = StartCoroutine(MoveCameraTo(new Vector3(pos.x, cameraPos.y, pos.z - _cameraDistance)));
			}
		}

		/// <summary>
		/// Moves the <see cref="Camera"/> smoothly from one position to another and
        /// rotates it to look towards a specified position.
		/// </summary>
		/// <param name="newPos">The target position.</param>
		/// <param name="lookAt">The position to look at.</param>
		/// <returns></returns>
		private IEnumerator MoveCameraTo(Vector3 newPos, Quaternion lookAt)
		{
			Vector3 oldPos = _activeCamera.transform.position;
            if (newPos == linkedObject.transform.position)
            {
                yield break;
            }
			Quaternion oldRot = _activeCamera.transform.rotation;
			for (float time = 0.0f; time <= _cameraFlightTime; time += Time.deltaTime)
			{
				_activeCamera.transform.position = Vector3.Lerp(oldPos, newPos, time * (1.0f / _cameraFlightTime));
				_activeCamera.transform.rotation = Quaternion.Slerp(oldRot, lookAt, time * (1.0f / _cameraFlightTime));
				yield return new WaitForEndOfFrame();
			}

			Transform cameraPos = _activeCamera.transform;
			cameraPos.rotation = lookAt;
			cameraPos.transform.position = newPos;
		}

		/// <summary>
		/// Moves the camera smoothly from one position to another without rotation.
		/// </summary>
		/// <param name="newPos">The target position.</param>
		/// <returns></returns>
		private IEnumerator MoveCameraTo(Vector3 newPos)
		{
			Vector3 oldPos = _activeCamera.transform.position;
			for (float time = 0; time <= _cameraFlightTime; time += Time.deltaTime)
			{
				_activeCamera.transform.position = Vector3.Lerp(oldPos, newPos, time * (1 / _cameraFlightTime));
				yield return new WaitForEndOfFrame();
			}

			_activeCamera.transform.position = newPos;
		}

		/// <summary>
		/// Changes the color of the marker to the accentuation color.
		/// </summary>
		public void Accentuate()
        {
            if (markerHighlight.TryGetComponent(out Image image))
            {
                image.color = _accentuated ? ChartManager.Instance.standardColor : ChartManager.Instance.accentuationColor;
            }
			_accentuated = !_accentuated;
        }

		/// <summary>
		/// Changes the <see cref="infoText" /> of this marker.
		/// </summary>
		/// <param name="info">The new text.</param>
		public void SetInfoText(string info)
		{
			infoText.text = info;
		}

		/// <summary>
		/// Activates the <see cref="infoText" />.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerEnter(PointerEventData eventData)
		{
			infoText.gameObject.SetActive(true);
            if (TimedHighlight != null)
            {
                ChartManager.Accentuate(linkedObject);
            }
		}

		/// <summary>
		/// Deactivates the <see cref="infoText" />.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnPointerExit(PointerEventData eventData)
		{
			infoText.gameObject.SetActive(false);
            if (_accentuated)
            {
                ChartManager.Accentuate(linkedObject);
            }
		}

		/// <summary>
		/// If <see cref="TimedHighlight" /> was running, this will be saved to
		/// <see cref="_reactivateHighlight" />.
		/// </summary>
		private void OnDisable()
		{
            if (TimedHighlight != null)
            {
                _reactivateHighlight = true;
            }
		}

		/// <summary>
		/// Reactivates the highlight if it was running before disable.
		/// </summary>
		private void OnEnable()
		{
			if (_reactivateHighlight)
            {
			    TriggerTimedHighlight(_highlightDuration - HighlightTime, true, false);
			    _reactivateHighlight = false;
            }
		}

		/// <summary>
		/// Stops all co-routines and destroys the <see cref="_highlightCopy" /> if it exists.
		/// </summary>
		private void OnDestroy()
		{
            if (_highlightCopy != null)
            {
                Destroy(_highlightCopy);
            }
			StopAllCoroutines();
		}
	}
}