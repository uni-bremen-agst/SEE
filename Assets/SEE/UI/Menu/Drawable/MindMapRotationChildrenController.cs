using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Controls whether child nodes of a Mind Map node are included while rotating
    /// the selected node.
    /// </summary>
    internal sealed class MindMapRotationChildrenController
    {
        /// <summary>
        /// The instantiated rotation menu containing the child inclusion controls.
        /// </summary>
        private readonly GameObject menu;

        /// <summary>
        /// The object that is currently rotated.
        /// </summary>
        private readonly GameObject selectedObject;

        /// <summary>
        /// Whether child nodes should be included in the current rotation.
        /// </summary>
        internal bool IncludeChildren { get; private set; }

        /// <summary>
        /// Creates a controller for the child inclusion option of the rotation menu.
        /// </summary>
        /// <param name="menu">The instantiated rotation menu.</param>
        /// <param name="selectedObject">The object that is currently rotated.</param>
        internal MindMapRotationChildrenController(GameObject menu, GameObject selectedObject)
        {
            this.menu = menu;
            this.selectedObject = selectedObject;
        }

        /// <summary>
        /// Initializes the child inclusion controls for the selected object.
        /// The controls are hidden for objects that are not Mind Map nodes.
        /// </summary>
        internal void SetUp()
        {
            GameObject childrenArea = GameFinder.FindAttachedOrLocalDescendant(
                menu, "Content").transform.Find("Children").gameObject;

            if (!selectedObject.CompareTag(Tags.MindMapNode))
            {
                childrenArea.SetActive(false);
                IncludeChildren = false;
                return;
            }

            childrenArea.SetActive(true);

            GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            /// The old rotation of the object is needed if child inclusion is
            /// activated after the rotation has already started.
            Vector3 oldRotation = selectedObject.transform.localEulerAngles;

            MMNodeValueHolder valueHolder = selectedObject.GetComponent<MMNodeValueHolder>();

            /// Save the original positions and rotations of the children. They are
            /// restored if child inclusion is disabled again during the rotation.
            Dictionary<GameObject, (Vector3 Position, Vector3 Rotation)> oldTransforms = new();
            foreach (KeyValuePair<GameObject, GameObject> pair in valueHolder.GetAllChildren())
            {
                oldTransforms[pair.Key] = (
                    pair.Key.transform.localPosition,
                    pair.Key.transform.localEulerAngles);
            }

            SwitchManager childrenSwitch = GameFinder.FindAttachedOrLocalDescendant(
                menu, "ChildrenSwitch").GetComponent<SwitchManager>();

            bool inclusionWasEnabled = false;

            childrenSwitch.OnEvents.RemoveAllListeners();
            childrenSwitch.OnEvents.AddListener(() =>
            {
                IncludeChildren = true;

                /// Rotate the children to the current rotation of the parent node.
                /// The parent node is first returned to its original rotation before
                /// being rotated with the children to the new degree. This preserves
                /// the node arrangement.
                Vector3 newRotation = selectedObject.transform.localEulerAngles;

                GameMoveRotator.SetRotate(selectedObject, oldRotation.z, false);
                new RotatorNetAction(surface.name, surfaceParentName, selectedObject.name,
                    oldRotation.z, false).Execute();

                GameMoveRotator.SetRotate(selectedObject, newRotation.z, true);
                new RotatorNetAction(surface.name, surfaceParentName, selectedObject.name,
                    newRotation.z, true).Execute();

                inclusionWasEnabled = true;
            });

            childrenSwitch.OffEvents.RemoveAllListeners();
            childrenSwitch.OffEvents.AddListener(() =>
            {
                IncludeChildren = false;

                if (inclusionWasEnabled)
                {
                    /// Restore the original position and rotation of every child node.
                    foreach (KeyValuePair<GameObject, (Vector3 Position, Vector3 Rotation)> pair in oldTransforms)
                    {
                        Vector3 position = pair.Value.Position;
                        Vector3 rotation = pair.Value.Rotation;

                        GameMoveRotator.SetPosition(pair.Key, position, false);
                        new MoveNetAction(surface.name, surfaceParentName, pair.Key.name,
                            position, false).Execute();

                        GameMoveRotator.SetRotate(pair.Key, rotation.z, false);
                        new RotatorNetAction(surface.name, surfaceParentName, pair.Key.name,
                            rotation.z, false).Execute();
                    }
                }
            });
        }
    }
}
