using System;
using System.Collections.Generic;
using System.Diagnostics;
using SEE.Controls.Actions;
using SEE.Utils.History;
using UnityEngine;

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
        private readonly Dictionary<string, DateTimeOffset> boostKeyStartTimes = new Dictionary<string, DateTimeOffset>();

        public TracingHelper(string sourceName = "SEE.Tracing", string playerName = "UnknownPlayer")
        {
            activitySource = new ActivitySource(sourceName);
            this.playerName = playerName;
        }

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

        public void TrackMovement(Vector3 start, Vector3 end, float durationSeconds)
        {
            Dictionary<string, object> tags = new Dictionary<string, object>
            {
                { "action.type", "PlayerMovement" },
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

        public void StartBoostCameraTracking()
        {
            boostKeyStartTimes[playerName] = DateTimeOffset.UtcNow;
        }

        public void StopBoostCameraTracking()
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
}
