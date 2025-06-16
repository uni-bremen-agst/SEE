using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using SEE.Controls.Actions;
using SEE.Utils.History;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SEE.Tools.OpenTelemetry
{
    /// <summary>
    /// Provides methods for tracing key user and system actions within SEE.
    /// Each method records metadata supporting observability and behavioral analysis via OpenTelemetry.
    /// </summary>
    public class TracingHelper
    {
        private readonly ActivitySource activitySource;
        private readonly string playerName;

        private readonly HashSet<Type> excludedActionTypes = new HashSet<Type>
        {
            typeof(MoveAction)
        };

        private readonly Dictionary<string, DateTimeOffset> boostKeyStartTimes =
            new Dictionary<string, DateTimeOffset>();

        public TracingHelper(string sourceName = "SEE.Tracing", string playerName = "UnknownPlayer")
        {
            activitySource = new ActivitySource(sourceName);
            this.playerName = playerName;
        }

        /// <summary>
        /// Tracks the addition of an action to the global history, including affected objects.
        /// </summary>
        /// <param name="action">The reversible action that was added.</param>
        /// <param name="changedObjects">The set of object IDs that were affected by the action.</param>
        public void TrackAddToHistory(IReversibleAction action, HashSet<string> changedObjects)
        {
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
        /// </summary>
        /// <param name="action">The action entry that was removed.</param>
        public void TrackRemoveFromHistory(ActionHistory.GlobalHistoryEntry action)
        {
            Dictionary<string, object> tags = new Dictionary<string, object>
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
        /// </summary>
        /// <param name="grabbedObject">The object that was moved.</param>
        /// <param name="finalPosition">The final position of the object after movement.</param>
        /// <param name="grabbedObjectNewParent">The new parent object, if any.</param>
        public void TrackMoveAction(GameObject grabbedObject, Vector3 finalPosition, GameObject grabbedObjectNewParent)
        {
            if (grabbedObject == null)
            {
                return;
            }

            Dictionary<string, object> tags = new Dictionary<string, object>
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
        /// </summary>
        /// <param name="actionName">The name of the key-bound action.</param>
        public void TrackKeyPress(string actionName)
        {
            Dictionary<string, object> tags = new Dictionary<string, object>
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
            Dictionary<string, object> tags = new Dictionary<string, object>
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

            if (activity != null)
            {
                activity.Stop();
            }
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
        /// Tracks how long the player hovered over a specific object.
        /// </summary>
        /// <param name="hoveredObject">The GameObject that was hovered over.</param>
        /// <param name="duration">The duration of the hover in seconds.</param>
        public void TrackHoverDuration(GameObject hoveredObject, float duration)
        {
            if (hoveredObject == null)
            {
                return;
            }

            Dictionary<string, object> tags = new Dictionary<string, object>
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

                    Dictionary<string, object> tags = new Dictionary<string, object>
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

                    if (activity != null)
                    {
                        activity.Stop();
                    }

                    boostKeyStartTimes.Remove(playerName);
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

            Debug.Log("SessionEnd span sent.");
        }
    }
}