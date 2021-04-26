using UnityEngine;

namespace Lean.Common
{
	/// <summary>This component causes the current Rigidbody to chase the specified position.</summary>
	[RequireComponent(typeof(Rigidbody))]
	[HelpURL(LeanHelper.PlusHelpUrlPrefix + "LeanChaseRigidbody")]
	[AddComponentMenu(LeanHelper.ComponentPathPrefix + "Chase Rigidbody")]
	public class LeanChaseRigidbody : LeanChase
	{
		/*
		public bool Rotation;

		[Tooltip("How sharp the position value changes update (-1 = instant)")]
		public float RotationDamping = -1.0f;
		*/

		[System.NonSerialized]
		private Rigidbody cachedRigidbody;

		[System.NonSerialized]
		protected bool fixedUpdateCalled;

		/// <summary>This method will override the Position value based on the specified value.</summary>
		public override void SetPosition(Vector3 newPosition)
		{
			base.SetPosition(newPosition);

			fixedUpdateCalled = false;
		}

		protected virtual void OnEnable()
		{
			cachedRigidbody = GetComponent<Rigidbody>();
		}

		protected override void UpdatePosition(float damping, float linear)
		{
			if (positionSet == true || continuous == true)
			{
				if (destination != null)
				{
					position = destination.TransformPoint(destinationOffset);
				}

				var currentPosition = transform.position;
				var targetPosition  = position + offset;

				if (ignoreZ == true)
				{
					targetPosition.z = currentPosition.z;
				}

				var direction = targetPosition - currentPosition;
				var velocity  = direction / Time.fixedDeltaTime;

				// Apply the velocity
				velocity *= LeanHelper.GetDampenFactor(damping, Time.fixedDeltaTime);
				velocity  = Vector3.MoveTowards(velocity, Vector3.zero, linear * Time.fixedDeltaTime);

				cachedRigidbody.velocity = velocity;
				Debug.Log(velocity);

				/*
				if (Rotation == true && direction != Vector3.zero)
				{
					var angle           = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
					var directionB      = (Vector2)transform.up;
					var angleB          = Mathf.Atan2(directionB.x, directionB.y) * Mathf.Rad2Deg;
					var delta           = Mathf.DeltaAngle(angle, angleB);
					var angularVelocity = delta / Time.fixedDeltaTime;

					angularVelocity *= LeanHelper.GetDampenFactor(RotationDamping, Time.fixedDeltaTime);

					//cachedRigidbody.angularVelocity = angularVelocity;
				}
				*/
				fixedUpdateCalled = true;
			}
		}

		protected virtual void LateUpdate()
		{
			if (fixedUpdateCalled == true)
			{
				positionSet       = false;
				fixedUpdateCalled = false;
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;

	[CustomEditor(typeof(LeanChaseRigidbody))]
	public class LeanChaseRigidbody_Editor : LeanChase_Editor
	{
		protected override void OnInspector()
		{
			base.OnInspector();
		}
	}
}
#endif