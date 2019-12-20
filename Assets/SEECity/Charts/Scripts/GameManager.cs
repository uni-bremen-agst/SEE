using UnityEngine;

namespace Assets.SEECity.Charts.Scripts
{
	public class GameManager : MonoBehaviour
	{
		/// <summary>
		/// The distance the camera will keep to the <see cref="GameObject" /> to focus on.
		/// </summary>
		[Header("Settings"), Header("Camera Controls")]
		public float CameraDistance = 40f;

		/// <summary>
		/// When checked, the <see cref="Camera" /> will rotate while moving.
		/// </summary>
		public bool MoveWithRotation = true;

		/// <summary>
		/// The time the <see cref="Camera" /> needs to reach it's destination when moving from one
		/// <see cref="GameObject" /> to another.
		/// </summary>
		public float CameraFlightTime = 0.5f;

		/// <summary>
		/// The maximum time between two clicks to recognize them as double click.
		/// </summary>
		[Header("User Inputs"), Range(0.1f, 1f)]
		public float ClickDelay = 0.5f;

		/// <summary>
		/// The <see cref="Material" /> making the object look highlighted.
		/// </summary>
		[Header("Highlights"), SerializeField] private Material _highlightMaterial;

		/// <summary>
		/// The thickness of the highlight outline of <see cref="_highlightMaterial" />.
		/// </summary>
		[SerializeField] private float _highlightOutline = 0.005f;

		/// <summary>
		/// The current thickness of the highlight outline of <see cref="_highlightMaterial" /> used in
		/// animations.
		/// </summary>
		[Header("DO NOT CHANGE THIS"), SerializeField]
		private float _highlightOutlineAnim = 0.001f;

		/// <summary>
		/// Update is called once per frame.
		/// </summary>
		private void Update()
		{
			AnimateHighlight();
		}

		/// <summary>
		/// Animates the highlight material.
		/// </summary>
		private void AnimateHighlight()
		{
			_highlightMaterial.SetFloat("g_flOutlineWidth", _highlightOutlineAnim);
		}

		/// <summary>
		/// Sets the properties of <see cref="_highlightMaterial" /> to their original state.
		/// </summary>
		private void OnDestroy()
		{
			_highlightMaterial.SetFloat("g_flOutlineWidth", _highlightOutline);
		}
	}
}