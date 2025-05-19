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
    /// Each method is tailored to record metadata about specific types of actions,
    /// supporting observability and behavioral analysis via OpenTelemetry.
    /// </summary>
    public class TracingHelper
    {
        private readonly ActivitySource activitySource;

        /// <summary>
        /// The name of the player whose actions are being tracked.
        /// </summary>
        private readonly string playerName;

        /// <summary>
        /// List of action types that should not be tracked.
        /// Add action types to this set to exclude them from tracing.
        /// </summary>
        private readonly HashSet<Type> excludedActionTypes = new()
        {
            typeof(MoveAction) // MoveAction is tracked separately
        };

        public TracingHelper(string sourceName = "SEE.Tracing", string playerName = "UnknownPlayer")
        {
            activitySource = new ActivitySource(sourceName);
            this.playerName = playerName;
        }

        /// <summary>
        /// Traces the addition of a reversible action to the global action history.
        /// Ignores actions listed in <see cref="excludedActionTypes"/>.
        /// </summary>
        public void TrackAddToHistory(IReversibleAction action, HashSet<string> changedObjects)
        {
            if (excludedActionTypes.Contains(action.GetType()))
            {
                return;
            }

            var tags = new Dictionary<string, object>
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

            using var activity = activitySource.StartActivity("AddToGlobalHistory");
            if (activity != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Traces the removal of an action from the global history.
        /// </summary>
        public void TrackRemoveFromHistory(ActionHistory.GlobalHistoryEntry action)
        {
            var tags = new Dictionary<string, object>
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

            using var activity = activitySource.StartActivity("RemoveFromGlobalHistory");
            if (activity != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Traces a MoveAction, capturing details about the moved object and its new parent and position.
        /// </summary>
        public void TrackMoveAction(GameObject grabbedObject, Vector3 finalPosition, GameObject grabbedObjectNewParent)
        {
            if (grabbedObject == null) return;

            var tags = new Dictionary<string, object>
            {
                { "action.type", "MoveAction" },
                { "player.name", playerName },
                { "newParent", grabbedObjectNewParent?.name ?? "None" },
                { "finalPosition", finalPosition.ToString() },
                { "action.objectId", grabbedObject.name },
                { "action.instanceID", grabbedObject.GetInstanceID() }
            };

            using var activity = activitySource.StartActivity("MoveAction");
            if (activity != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Traces a key press event.
        /// </summary>
        public void TrackKeyPress(string actionName)
        {
            var tags = new Dictionary<string, object>
            {
                { "action.type", "KeyPress" },
                { "player.name", playerName },
                { "action.name", actionName }
            };

            using var activity = activitySource.StartActivity("KeyPress");
            if (activity != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        /// <summary>
        /// Traces a player's movement by recording both the start and end positions.
        /// </summary>
        public void TrackMovement(Vector3 start, Vector3 end)
        {
            var tags = new Dictionary<string, object>
            {
                { "action.type", "PlayerMovement" },
                { "player.name", playerName },
                { "startPosition", start.ToString() },
                { "endPosition", end.ToString() }
            };

            using var activity = activitySource.StartActivity("PlayerMovement");
            if (activity != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }
        
        /// <summary>
        /// Traces a hover event if the user hovers over an object for more than a threshold duration (e.g., 5 seconds).
        /// </summary>
        /// <param name="hoveredObject">The object that was hovered.</param>
        /// <param name="duration">The duration in seconds the object was hovered over.</param>
        public void TrackHoverDuration(GameObject hoveredObject, float duration)
        {
            if (hoveredObject == null) return;

            var tags = new Dictionary<string, object>
            {
                { "action.type", "Hover" },
                { "player.name", playerName },
                { "hovered.objectId", hoveredObject.name },
                { "hovered.instanceID", hoveredObject.GetInstanceID() },
                { "hovered.position", hoveredObject.transform.position.ToString() },
                { "hover.duration", duration }
            };

            using var activity = activitySource.StartActivity("HoverDuration");
            if (activity != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

    }
}
