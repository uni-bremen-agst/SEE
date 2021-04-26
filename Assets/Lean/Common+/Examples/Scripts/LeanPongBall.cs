using UnityEngine;

namespace Lean.Common
{
	/// <summary>This script moves the ball left or right and resets it if it goes out of bounds.</summary>
	[RequireComponent(typeof(Rigidbody))]
	[HelpURL(LeanHelper.PlusHelpUrlPrefix + "LeanPongBall")]
	[AddComponentMenu(LeanHelper.ComponentPathPrefix + "Pong Ball")]
	public class LeanPongBall : MonoBehaviour
	{
		[Tooltip("Starting horizontal speed of the ball")]
		public float StartSpeed = 1.0f;

		[Tooltip("Starting vertical speed of the ball")]
		public float Spread = 1.0f;

		[Tooltip("The acceleration per second")]
		public float Acceleration = 0.1f;

		[Tooltip("If the ball goes this many units from the center, it will reset")]
		public float Bounds = 10.0f;

		// The current rigidbody
		private Rigidbody body;

		// The current speed of the ball
		private float speed;
		
		protected virtual void Awake()
		{
			// Store the rigidbody component attached to this GameObject
			body = GetComponent<Rigidbody>();

			// Reset the ball
			ResetPositionAndVelocity();
		}

		protected virtual void FixedUpdate()
		{
			// Is the position out of bounds?
			if (transform.localPosition.magnitude > Bounds)
			{
				ResetPositionAndVelocity();
			}

			// Increase speed value
			speed += Acceleration * Time.deltaTime;

			// Reset velocity magnitude to new speed
			body.velocity = body.velocity.normalized * speed;
		}

		private void ResetPositionAndVelocity()
		{
			// Reset position
			transform.localPosition = Vector3.zero;

			// Reset speed value
			speed = StartSpeed;

			// If moving right, reset velocity to the left
			if (body.velocity.x > 0.0f)
			{
				body.velocity = new Vector3(-speed, Random.Range(-Spread, Spread), 0.0f);
			}
			// If moving left, reset velocity to the right
			else
			{
				body.velocity = new Vector3(speed, Random.Range(-Spread, Spread), 0.0f);
			}
		}
	}
}