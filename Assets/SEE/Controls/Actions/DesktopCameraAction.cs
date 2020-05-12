using UnityEngine;

namespace SEE.Controls
{
	/// <summary>
	/// Moves and rotates the game object this component is attached to as a component.
	/// </summary>
	public class DesktopCameraAction : CameraAction
	{
		[Header("Movement Settings"),
		 Tooltip("Exponential boost factor for acceleration when throttle is pressed.")]
		public float boost = 3.5f;

		[Tooltip("Time it takes to interpolate camera position 99% of the way to the target."),
		 Range(0.001f, 1f)]
		public float positionLerpTime = 0.2f;

		[Header("Rotation Settings"),
		 Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
		public AnimationCurve SensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f),
			new Keyframe(1f, 2.5f, 0f, 0f));

		[Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."),
		 Range(0.001f, 1f)]
		public float rotationLerpTime = 0.01f;

		[Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
		public bool invertY;

		/// <summary>
		/// If true, we are currently looking around rather than moving.
		/// </summary>
		private bool looking;

		/// <summary>
		/// If <paramref name="activated" /> is true, we are entering the looking mode,
		/// in which the mouse cursor is locked and invisible. If <paramref name="activated" />
		/// is false, the mouse cursor is unlocked and visible again.
		/// </summary>
		/// <param name="activated">whether the looking mode is activated</param>
		private void Look(bool activated)
		{
			looking = activated;

			// Hide and lock cursor while looking.
			if (looking)
			{
				Cursor.lockState = CursorLockMode.Locked;
			}
			else
			{
				// Unlock and show cursor when no longer looking.
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
			}
		}

		private class CameraState
		{
			public float yaw;
			public float pitch;
			public float roll;
			public float x;
			public float y;
			public float z;

			public void SetFromTransform(Transform t)
			{
				pitch = t.eulerAngles.x;
				yaw = t.eulerAngles.y;
				roll = t.eulerAngles.z;
				x = t.position.x;
				y = t.position.y;
				z = t.position.z;
			}

			public void Translate(Vector3 translation)
			{
				Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;

				x += rotatedTranslation.x;
				y += rotatedTranslation.y;
				z += rotatedTranslation.z;
			}

			public void LerpTowards(CameraState target, float positionLerpPct,
				float rotationLerpPct)
			{
				yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
				pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
				roll = Mathf.Lerp(roll, target.roll, rotationLerpPct);

				x = Mathf.Lerp(x, target.x, positionLerpPct);
				y = Mathf.Lerp(y, target.y, positionLerpPct);
				z = Mathf.Lerp(z, target.z, positionLerpPct);
			}

			/// <summary>
			/// Moves the object <paramref name="t" /> according to the current settings of pitch, yaw, roll
			/// and x, y, z.
			/// </summary>
			/// <param name="t">the transform to be set</param>
			public void UpdateTransform(Transform t)
			{
				t.eulerAngles = new Vector3(pitch, yaw, roll);
				t.position = new Vector3(x, y, z);
			}
		}

		private CameraState m_TargetCameraState = new CameraState();
		private CameraState m_InterpolatingCameraState = new CameraState();

		private void OnEnable()
		{
			m_TargetCameraState.SetFromTransform(transform);
			m_InterpolatingCameraState.SetFromTransform(transform);
		}

		private void Update()
		{
			boost += BoostDevice.Value * 0.2f;

			// Camera rotation for looking around
			if (ViewpointDevice.Activated)
			{
				Look(true);
				Vector2 viewpoint = ViewpointDevice.Value;
				Vector2 lookMovement = new Vector2(viewpoint.x, viewpoint.y * (invertY ? 1 : -1));

				float sensitivityFactor = SensitivityCurve.Evaluate(lookMovement.magnitude);

				m_TargetCameraState.yaw += lookMovement.x * sensitivityFactor;
				m_TargetCameraState.pitch += lookMovement.y * sensitivityFactor;
			}
			else
			{
				Look(false);
			}

			// Translation
			Vector3 translation = DirectionDevice.Value * Time.deltaTime;

			// Speed up movement.
			if (ThrottleDevice.Value > 0) translation *= 10.0f;

			translation *= Mathf.Pow(2.0f, boost);

			m_TargetCameraState.Translate(translation);

			// Framerate-independent interpolation
			// Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
			var positionLerpPct =
				1f - Mathf.Exp(Mathf.Log(1f - 0.99f) / positionLerpTime * Time.deltaTime);
			var rotationLerpPct =
				1f - Mathf.Exp(Mathf.Log(1f - 0.99f) / rotationLerpTime * Time.deltaTime);
			m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct,
				rotationLerpPct);

			// Finally move the object.
			m_InterpolatingCameraState.UpdateTransform(transform);
		}
	}
}