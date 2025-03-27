using System.Collections.Generic;
using System.Diagnostics;
using SEE.Controls.Actions;
using SEE.Utils.History;
using UnityEngine;

/// <summary>
/// The TracingHelper class provides static methods for tracking actions within the system. 
/// It aims to track every individual instance of objects and interactions that are involved in any action.
///
/// Eventually, each instance of the objects being tracked will have its own corresponding method in this class, making it a comprehensive solution 
/// for tracing SEE's activities and ensuring that all significant changes and interactions are monitored and logged.
/// </summary>
public static class TracingHelper
{
    private static readonly ActivitySource ActivitySource = new("SEE.Tracing");

    // Starts tracing for adding an action to history
    public static void TrackAddToHistory(IReversibleAction action, string actionID, HashSet<string> changedObjects)
    {
        // Check if the action is of type MoveAction
        if (action is MoveAction)
        {
            return; // If it's a MoveAction, skip tracing. MoveAction is handled separately
        }

        var tags = new Dictionary<string, object>
        {
            { "action.id", actionID }, // Add action ID as a tag
            { "action.type", action.GetType().FullName } // Add action type as a tag
        };

        int index = 0;
        // Iterate through all changed objects and add relevant tags for each
        foreach (string objectId in changedObjects)
        {
            GameObject foundObject = GameObject.Find(objectId);
            if (foundObject != null)
            {
                tags[$"action.affectedObjects[{index}].id"] = foundObject.name; // Add object ID tag
                tags[$"action.affectedObjects[{index}].instanceID"] =
                    foundObject.GetInstanceID(); // Add instance ID tag
                tags[$"action.affectedObjects[{index}].position"] =
                    foundObject.transform.position.ToString(); // Add position tag
                index++;
            }
        }

        // Start tracing the "AddToGlobalHistory" activity
        using (var activity = ActivitySource.StartActivity("AddToGlobalHistory"))
        {
            if (activity != null)
            {
                // Add all tags to the tracing activity
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }
    }

    // Starts tracing for removing an action from history
    public static void TrackRemoveFromHistory(ActionHistory.GlobalHistoryEntry action)
    {
        var tags = new Dictionary<string, object>
        {
            { "action.id", action.ActionID }, // Add action ID as a tag
            { "action.type", action.GetType().FullName } // Add action type as a tag
        };

        int index = 0;
        // Iterate through all changed objects and add relevant tags for each
        foreach (string objectId in action.ChangedObjects)
        {
            GameObject foundObject = GameObject.Find(objectId);
            if (foundObject != null)
            {
                tags[$"action.affectedObjects[{index}].id"] = foundObject.name; // Add object ID tag
                tags[$"action.affectedObjects[{index}].instanceID"] =
                    foundObject.GetInstanceID(); // Add instance ID tag
                tags[$"action.affectedObjects[{index}].position"] =
                    foundObject.transform.position.ToString(); // Add position tag
                index++;
            }
        }

        // Start tracing the "RemoveFromGlobalHistory" activity
        using (var activity = ActivitySource.StartActivity("RemoveFromGlobalHistory"))
        {
            if (activity != null)
            {
                // Add all tags to the tracing activity
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }
    }

    /// <summary>
    /// Tracks the MoveAction and logs the original and final position of the object.
    /// </summary>
    public static void TrackMoveAction(GameObject grabbedObject, Vector3 finalPosition,
        GameObject grabbedObjectNewParent)
    {
        if (grabbedObject == null)
        {
            return; // Skip if no object is grabbed
        }

        var tags = new Dictionary<string, object>
        {
            { "action.type", "MoveAction" }, // Type of action
            { "newParent", grabbedObjectNewParent.name }, // original position as tag
            { "finalPosition", finalPosition.ToString() } // Final position as tag
        };

        // Start tracing the "MoveAction" activity
        using (var activity = ActivitySource.StartActivity("MoveAction"))
        {
            if (activity != null)
            {
                // Add the tags to the tracing activity
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }

                activity.SetTag("action.objectId", grabbedObject.name); // Object ID for reference
                activity.SetTag("action.instanceID", grabbedObject.GetInstanceID()); // Object instance ID for reference
            }
        }
    }

    // Start tracing for a key press action 
    public static void TrackKeyPress(string actionName)
    {
        var tags = new Dictionary<string, object>
        {
            { "action.name", actionName } // Name of the action 
        };

        // Start tracing the "KeyPress" activity
        using (var activity = ActivitySource.StartActivity("KeyPress"))
        {
            if (activity != null)
            {
                // Add tags to the tracing activity
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }
    }

    /// <summary>
    /// Tracks the player's movement by recording the start and end positions along with the direction.
    /// </summary>
    public static void TrackMovement(Vector3 startPosition, Vector3 endPosition)
    {
        var tags = new Dictionary<string, object>
        {
            { "action.type", "PlayerMovement" }, // Action type
            { "startPosition", startPosition.ToString() }, // Starting position
            { "endPosition", endPosition.ToString() } // Ending position
        };

        // Start tracing the "PlayerMovement" activity
        using (var activity = ActivitySource.StartActivity("PlayerMovement"))
        {
            if (activity != null)
            {
                // Add all tags to the tracing activity
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }
    }
}