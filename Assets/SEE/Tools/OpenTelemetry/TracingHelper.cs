using System;
using System.Collections.Generic;
using System.Diagnostics;
using SEE.Controls.Actions;
using SEE.Utils.History;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SEE.Tools.OpenTelemetry
{
    /// <summary>
    /// Contains tools and helpers for tracing user and system actions in SEE via OpenTelemetry.
    /// </summary>
    public class TracingHelper
    {
        /// <summary>
        /// The OpenTelemetry activity source used for creating spans.
        /// </summary>
        private readonly ActivitySource activitySource;

        /// <summary>
        /// The name of the player for which actions are traced.
        /// </summary>
        private readonly string playerName;

        /// <summary>
        /// Action types that should not be traced.
        /// </summary>
        private readonly HashSet<Type> excludedActionTypes = new()
        {
            typeof(MoveAction)
        };

        /// <summary>
        /// Maps player names to the start time of boost actions.
        /// </summary>
        private readonly Dictionary<string, DateTimeOffset> boostKeyStartTimes = new();

        /// <summary>
        /// Last tracked timestamp for head transform in VR.
        /// </summary>
        private DateTimeOffset lastHeadTrackingTime = DateTimeOffset.MinValue;

        /// <summary>
        /// Initializes a new instance of <see cref="TracingHelper"/>.
        /// Preconditions: <paramref name="sourceName"/> and <paramref name="playerName"/> must not be null.
        /// </summary>
        /// <param name="sourceName">Name for the activity source (must not be null).</param>
        /// <param name="playerName">The player's name to be traced (must not be null).</param>
        public TracingHelper(string sourceName = "SEE.Tracing", string playerName = "UnknownPlayer")
        {
            activitySource = new ActivitySource(sourceName);
            this.playerName = playerName;
        }

        /// <summary>
        /// Tracks the addition of an action to the global history, including affected objects.
        /// Preconditions: <paramref name="action"/> and <paramref name="changedObjects"/> must not be null.
        /// </summary>
        /// <param name="action">The reversible action that was added. Must not be null.</param>
        /// <param name="changedObjects">The set of object IDs that were affected by the action. Must not be null.</param>
        public void TrackAddToHistory(IReversibleAction action, HashSet<string> changedObjects)
        {
            if (action == null || changedObjects == null)
            {
                throw new ArgumentNullException("action and changedObjects must not be null.");
            }

            if (excludedActionTypes.Contains(action.GetType()))
            {
                return;
            }

            Dictionary<string, object> tags = new Dictionary<string, object>
            {
                { "action.id", action.GetId() },
                { "player.name", playerName },
                { "action.type", action.GetType().FullName }
            };

            int index = 0;
            foreach (string objectId in changedObjects)
            {
                GameObject obj = GameObject.Find(objectId);
                if (obj != null)
                {
                    tags[$"action.affectedObjects[{index}].id"] = obj.name;
                    tags[$"action.affectedObjects[{index}].instanceID"] = obj.GetInstanceID();
                    tags[$"action.affectedObjects[{index}].position"] = obj.transform.position.ToString();
                    index++;
                }
            }

            using Activity activity = activitySource.StartActivity("AddToGlobalHistory");
            if (activity != null)
            {
                foreach (KeyValuePair<string, object> tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Tracks the removal of an action from the global history.
        /// Preconditions: <paramref name="action"/> must not be null.
        /// </summary>
        /// <param name="action">The action entry that was removed. Must not be null.</param>
        public void TrackRemoveFromHistory(ActionHistory.GlobalHistoryEntry action)
        {
            Dictionary<string, object> tags = new()
            {
                { "action.type", action.GetType().FullName },
                { "player.name", playerName },
                { "action.id", action.ActionID },
                { "isOwner", action.IsOwner }
            };

            int index = 0;
            foreach (string objectId in action.ChangedObjects)
            {
                GameObject obj = GameObject.Find(objectId);
                if (obj != null)
                {
                    tags[$"action.affectedObjects[{index}].id"] = obj.name;
                    tags[$"action.affectedObjects[{index}].instanceID"] = obj.GetInstanceID();
                    tags[$"action.affectedObjects[{index}].position"] = obj.transform.position.ToString();
                    index++;
                }
            }

            using Activity activity = activitySource.StartActivity("RemoveFromGlobalHistory");
            if (activity != null)
            {
                foreach (KeyValuePair<string, object> tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Tracks the result of a move action, including final position and new parent.
        /// Preconditions: <paramref name="grabbedObject"/> must not be null.
        /// </summary>
        /// <param name="grabbedObject">The object that was moved. Must not be null.</param>
        /// <param name="finalPosition">The final position of the object after movement.</param>
        /// <param name="grabbedObjectNewParent">The new parent object, if any. May be null.</param>
        public void TrackMoveAction(GameObject grabbedObject, Vector3 finalPosition, GameObject grabbedObjectNewParent)
        {
            if (grabbedObject == null)
            {
                throw new ArgumentNullException(nameof(grabbedObject));
            }

            Dictionary<string, object> tags = new()
            {
                { "action.type", "MoveAction" },
                { "player.name", playerName },
                { "newParent", grabbedObjectNewParent?.name ?? "None" },
                { "finalPosition", finalPosition.ToString() },
                { "action.objectId", grabbedObject.name },
                { "action.instanceID", grabbedObject.GetInstanceID() }
            };

            using Activity activity = activitySource.StartActivity("MoveAction");
            if (activity != null)
            {
                foreach (KeyValuePair<string, object> tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Tracks a discrete key press action (e.g. user toggling a view or activating a feature).
        /// Preconditions: <paramref name="actionName"/> must not be null.
        /// </summary>
        /// <param name="actionName">The name of the key-bound action. Must not be null.</param>
        public void TrackKeyPress(string actionName)
        {
            if (actionName == null)
            {
                throw new ArgumentNullException(nameof(actionName));
            }

            Dictionary<string, object> tags = new()
            {
                { "action.type", "KeyPress" },
                { "player.name", playerName },
                { "action.name", actionName }
            };

            using Activity activity = activitySource.StartActivity("KeyPress");
            if (activity != null)
            {
                foreach (KeyValuePair<string, object> tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Tracks a desktop movement event, logging the start and end position as well as duration.
        /// </summary>
        /// <param name="start">The position at the start of the movement.</param>
        /// <param name="end">The position at the end of the movement.</param>
        /// <param name="durationSeconds">The duration of the movement in seconds.</param>
        public void TrackDesktopMovement(Vector3 start, Vector3 end, float durationSeconds)
        {
            Dictionary<string, object> tags = new()
            {
                { "action.type", "PlayerDesktopMovement" },
                { "player.name", playerName },
                { "startPosition", start.ToString() },
                { "endPosition", end.ToString() },
                { "movement.duration", (double)durationSeconds }
            };

            DateTimeOffset startTime = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(durationSeconds);
            ActivityContext parentContext = default;

            Activity activity = activitySource.StartActivity(
                "PlayerMovement",
                ActivityKind.Internal,
                parentContext,
                tags,
                null,
                startTime
            );

            activity?.Stop();
        }

        /// <summary>
        /// Tracks a camera yaw rotation event when the mouse is released.
        /// </summary>
        /// <param name="previousYaw">Yaw before the rotation.</param>
        /// <param name="currentYaw">Yaw after the rotation.</param>
        public void TrackRotation(float previousYaw, float currentYaw)
        {
            float rotationDelta = Mathf.Abs(Mathf.DeltaAngle(previousYaw, currentYaw));

            using Activity activity = activitySource.StartActivity("RotationChange");
            if (activity != null)
            {
                activity.SetTag("action.type", "RotationChange");
                activity.SetTag("player.name", playerName);
                activity.SetTag("rotation.previousYaw", previousYaw.ToString("F2"));
                activity.SetTag("rotation.currentYaw", currentYaw.ToString("F2"));
                activity.SetTag("rotation.delta", rotationDelta.ToString("F2"));
            }
        }

        /// <summary>
        /// Tracks how long the player hovered over a specific object,
        /// if the duration exceeds the specified threshold.
        /// Preconditions: <paramref name="hoveredObject"/> must not be null.
        /// </summary>
        /// <param name="hoveredObject">The GameObject that was hovered over. Must not be null.</param>
        /// <param name="duration">The duration of the hover in seconds.</param>
        /// <param name="minimumDuration">The minimum duration required to track the hover (in seconds). Default is 5.0s.</param>
        public void TrackHoverDuration(GameObject hoveredObject, float duration, float minimumDuration)
        {
            if (hoveredObject == null)
            {
                throw new ArgumentNullException(nameof(hoveredObject));
            }

            if (duration < minimumDuration)
            {
                return;
            }

            Dictionary<string, object> tags = new()
            {
                { "action.type", "Hover" },
                { "player.name", playerName },
                { "hovered.objectId", hoveredObject.name },
                { "hovered.instanceID", hoveredObject.GetInstanceID() },
                { "hovered.position", hoveredObject.transform.position.ToString() },
                { "hover.duration", (double)duration }
            };

            DateTimeOffset startTime = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(duration);
            ActivityContext parentContext = default;

            Activity activity = activitySource.StartActivity(
                "HoverDuration",
                ActivityKind.Internal,
                parentContext,
                tags,
                null,
                startTime
            );

            if (activity != null)
            {
                activity.Stop();
            }
        }

        /// <summary>
        /// Updates the camera boost tracking state based on the current key press status.
        /// Starts tracking when boost begins and logs the duration when boost ends.
        /// </summary>
        /// <param name="isPressed">Whether the boost key is currently pressed.</param>
        public void UpdateBoostCameraTracking(bool isPressed)
        {
            if (isPressed)
            {
                if (!boostKeyStartTimes.ContainsKey(playerName))
                {
                    boostKeyStartTimes[playerName] = DateTimeOffset.UtcNow;
                }
            }
            else
            {
                if (boostKeyStartTimes.TryGetValue(playerName, out DateTimeOffset startTime))
                {
                    DateTimeOffset endTime = DateTimeOffset.UtcNow;
                    double duration = (endTime - startTime).TotalSeconds;

                    Dictionary<string, object> tags = new()
                    {
                        { "action.type", "BoostCamera" },
                        { "player.name", playerName },
                        { "boost.duration", duration }
                    };

                    ActivityContext parentContext = default;

                    Activity activity = activitySource.StartActivity(
                        "BoostCamera",
                        ActivityKind.Internal,
                        parentContext,
                        tags,
                        null,
                        startTime
                    );

                    activity?.Stop();

                    boostKeyStartTimes.Remove(playerName);
                }
            }
        }

        /// <summary>
        /// Periodically tracks the transform (position and rotation) of the player's head in the VR environment.
        /// A new span is only recorded if at least 5 seconds have passed since the last tracking event.
        /// Preconditions: <paramref name="headTransform"/> must not be null.
        /// </summary>
        /// <param name="headTransform">The transform of the player's head (e.g. VRIK head target). Must not be null.</param>
        /// <param name="timestamp">Game time in seconds since game start.</param>
        public void TrackHeadTransformPeriodically(Transform headTransform, float timestamp)
        {
            if (headTransform == null)
            {
                throw new ArgumentNullException(nameof(headTransform));
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if ((now - lastHeadTrackingTime).TotalSeconds < 5)
            {
                return; // Skip if interval has not passed
            }

            lastHeadTrackingTime = now;

            headTransform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
            Dictionary<string, object> tags = new()
            {
                { "action.type", "HeadTransformTracking" },
                { "player.name", playerName },
                { "head.position", position.ToString("F3") },
                { "head.position.x", position.x },
                { "head.position.y", position.y },
                { "head.position.z", position.z },
                { "head.rotation", rotation.eulerAngles.ToString("F2") },
                { "head.timestamp", timestamp }
            };

            using Activity activity = activitySource.StartActivity("HeadTransformTracking");
            if (activity != null)
            {
                foreach (KeyValuePair<string, object> tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Sends a final trace span indicating that the session has ended.
        /// This allows external tools to detect when a session is complete.
        /// </summary>
        public void TrackSessionEnd()
        {
            using Activity activity = activitySource.StartActivity("SessionEnd");
            if (activity != null)
            {
                activity.SetTag("event.type", "session.end");
                activity.SetTag("player.name", playerName);
                activity.SetTag("message", "Trace Export Session Ended. Tracer shut down cleanly.");
            }

            Debug.Log("SessionEnd span sent.\n");
        }
    }
}
