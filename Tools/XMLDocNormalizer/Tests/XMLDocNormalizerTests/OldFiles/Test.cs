using System;
using SEE.GameObjects;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This action shows/animates edges connecting authors spheres and nodes when
    /// the user hovers over them.
    /// This script can be added to both <see cref="AuthorSphere"/>s and game objects
    /// representing graph nodes (aka game nodes).
    /// </summary>
    internal class Test : InteractableObjectAction, IDisposable
    {
        /// <summary>
        /// Toggles the visibility of author edges for a node.
        /// <code>
        ///     true
        /// </code>
        /// </summary>
        /// <param name="isHovered"><paramref name="authorRef"/> whether the game object is currently hovered.</param>
        /// <param name="authorRef"><see cref="AuthorRef"/> instance which the user hovered over.</param>
        /// <remarks>This will be executed at the hovering of file nodes.</remarks>
        private void ToggleAuthorEdgesForNode(bool isHovered, AuthorRef authorRef)
        {
            foreach (AuthorEdge authorEdge in authorRef)
            {
                authorEdge.ShowOrHide(isHovered);
            }
        }

        /// <summary>
        /// Returns the interactable object of given <paramref name="id"/>
        /// or <code>null</code>, if it does not exist.
        /// </summary>
        /// <param name="id">The id of the interactable object.</param>
        /// <returns>The interactable with the given <paramref name="id"/>;
        /// null if none exists.</returns>
        public static InteractableObject Get(string id)
        {
            idToInteractableObjectDict.TryGetValue(id, out InteractableObject result);
            return result;
        }

        /// <summary>
        /// Whether the given hover flag is set.
        /// </summary>
        /// <param name="flag">The flag to check.</param>
        /// <returns><code>true</code> if the given flag is set, <code>false</code>
        /// otherwise.</returns>
        public bool IsHoverFlagSet(HoverFlag flag) => (HoverFlags & (uint)flag) != 0;

        /// <summary>
        /// Whether the object is currently interactable (at the given hit point).
        /// An object without a portal is always interactable.
        /// <para>
        /// Otherwise, if the object has a portal:
        /// <list type="bullet">
        /// <item>
        /// If a hit point is given, it is checked against the portal bounds.
        /// </item><item>
        /// Else, the object boundaries are checked against the portal bounds.
        /// </item></list>
        /// If either given point or the object's bounds are outside the portal bounds, the object is not interactable.
        /// </para>
        /// </summary>
        /// <param name="point">The hit point on the object.</param>
        /// <returns><c>true</c> if the user can interact with the object, <c>false</c> otherwise.</returns>
        public bool IsInteractable(Vector3? point = null)
        {
            if (Portal.GetPortal(gameObject, out Vector2 leftFront, out Vector2 rightBack))
            {
                Bounds2D portalBounds = Bounds2D.FromPortal(leftFront, rightBack);

                // Check interaction point
                if (point != null)
                {
                    if (!portalBounds.Contains(point.Value))
                    {
                        return false;
                    }
                }
                // Check if the object is (partially) within the portal bounds.
                else
                {
                    Bounds2D bounds = new(gameObject);
                    if (!portalBounds.Contains(bounds))
                    {
                        return false;
                    }
                }
            }
            // Potential further checks go here
            return true;
        }

        /// <summary>
        /// Returns the <see cref="GitBranchesGraphProvider"/> from the <see cref="DataProvider"/>'s pipeline.
        ///
        /// We currently assume that the pipeline has exactly one element which is of type <see cref="GitBranchesGraphProvider"/>.
        /// </summary>
        /// <param name="dataProvider">the graph provider pipeline from which to derive the <see cref="GitBranchesGraphProvider"/></param>
        /// <returns>the <see cref="GitBranchesGraphProvider"/></returns>
        /// <exception cref="ArgumentException">thrown in case our assumptions are invalid</exception>
        private static GitBranchesGraphProvider GetGitBranchesGraphProvider(SingleGraphPipelineProvider dataProvider)
        {
            if (dataProvider == null)
            {
                throw new ArgumentException("Data provider is null.");
            }

            if (dataProvider.Pipeline.Count == 0)
            {
                throw new ArgumentException("Data provider pipeline is empty.");
            }
            if (dataProvider.Pipeline.Count > 1)
            {
                throw new ArgumentException($"Data provider pipeline has more than one element. That is currently not supported for {nameof(BranchCity)}.");
            }

            SingleGraphProvider graphProvider = dataProvider.Pipeline[0];
            if (graphProvider == null)
            {
                throw new ArgumentException("Data provider in pipeline is null.");
            }
            if (graphProvider is GitBranchesGraphProvider result)
            {
                return result;
            }
            throw new ArgumentException($"Data provider is not a {nameof(GitBranchesGraphProvider)}.");
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The Name of this ActionStateType. Must be unique.</param>
        /// <param name="description">Description for this ActionStateType.</param>
        /// <param name="color">Color for this ActionStateType.</param>
        /// <param name="icon">The icon which shall be displayed alongside this entry,
        /// given as a FontAwesome codepoint.</param>
        /// <param name="group">The group this action state type belongs to; may be null.</param>
        /// <param name="register">If true, this action state type will be registered in <see cref="ActionStateTypes"/>.</param>
        protected AbstractActionStateType(string name, string description, Color color, char icon,
                                          ActionStateTypeGroup group, bool register)
        {
            Name = name;
            Description = description;
            Color = color;
            Icon = icon;
            group?.Add(this);
            if (register)
            {
                ActionStateTypes.Add(this);
            }
        }

        /// <summary>
        /// Gets the <paramref name="descriptor"/> bound to the specified <paramref name="keyAction"/>.
        /// </summary>
        /// <param name="keyAction">The <see cref="KeyAction"/> whose <paramref name="descriptor"/> is requested.</param>
        /// <param name="descriptor">The resulting <see cref="KeyActionDescriptor"/> bound
        /// to <paramref name="keyAction"/; defined only if this method returns <c>true</c></param>
        /// <returns><c>true</c> if this <see cref="KeyMap"/> contains an descriptor bound to <paramref name="keyAction"/>;
        /// otherwise, <c>false</c>.</returns>
        internal bool TryGetValue(KeyAction keyAction, out KeyActionDescriptor descriptor)
        {
            return keyBindings.TryGetValue(keyAction, out descriptor);
        }

        /// <summary>
        /// Removes removed <see cref="AudioGameObject">s from the AudioManager's AudioObject collection.
        /// </summary>
        /// <param name="removedElements">A list of <see cref="AudioGameObject">s that were removed in the current frame.</param>
        private void DeleteRemovedAudioObjects(ISet<AudioGameObject> removedElements)
        {
            soundEffectGameObjects.ExceptWith(removedElements);
        }

        /// <summary>
        /// Returns true if there is at least one attribute A in <paramref name="attributes"/>
        /// meeting any of the following conditions:
        /// (1) <paramref name="left"/> has A, but <paramref name="right"/> does not have it, or vice versa
        /// (2) <paramref name="left"/> and <paramref name="right"/> have A, but their values for A differ
        /// </summary>
        /// <typeparam name="T">the type of an attributes value</typeparam>
        /// <param name="left">element to be compared to <paramref name="right"/></param>
        /// <param name="right">element to be compared to <paramref name="left"/></param>
        /// <param name="attributes">the list of relevant attributes that are to be compared</param>
        /// <param name="tryGetValue">a delegate to retrieve the value of an attribute; <seealso cref="TryGetValue{T}"/></param>
        /// <returns>true if the attributes of <paramref name="left"/> and <paramref name="right"/> differ
        /// as specified above</returns>
        private static bool AttributesDiffer<T>(GraphElement left, GraphElement right, ICollection<string> attributes, TryGetValue<T> tryGetValue)
        {
            foreach (string attribute in attributes)
            {
                if (tryGetValue(left, attribute, out T leftValue))
                {
                    // left has attribute
                    if (tryGetValue(right, attribute, out T rightValue))
                    {
                        // and right has attribute; if they are different, we have found one difference
                        if (!leftValue.Equals(rightValue))
                        {
                            return true;
                        }
                        // the two attribute values are the same => we need to continue
                    }
                    else
                    {
                        // right does not have attribute
                        return true;
                    }
                }
                else
                {
                    // left does not have the attribute => right must neither have it;
                    // if right has the attribute, we must return true because there
                    // is a difference
                    if (tryGetValue(right, attribute, out T _))
                    {
                        return true;
                    }
                    // Neither of the two nodes have the attribute => we need to continue
                }
            }
            return false;
        }

        /// <summary>
        /// Checks whether the action with the given <paramref name="actionId"/> present
        /// in <see cref="globalHistory"/> has a conflict to another action in the
        /// <see cref="globalHistory"/>. Two actions have a conflict if their two
        /// sets of modified game objects overlap.
        /// If the action is not contained in <see cref="globalHistory"/>, it
        /// cannot have a conflict. <code>True</code>.
        /// <code>
        /// if (x > 0)
        /// {
        ///    return x;
        /// }
        /// </code>
        /// </summary>
        /// <param name="affectedGameObjects">The gameObjects modified by the action.</param>
        /// <param name="actionId">The ID of the action.</param>
        /// <returns>True if there are conflicts.</returns>
        private bool ActionHasConflicts(HashSet<string> affectedGameObjects, string actionId)
        {
            if (affectedGameObjects.Count == 0)
            {
                return false;
            }
            int index = GetIndexOfAction(actionId);
            if (index == -1)
            {
                return false;
            }
            ++index;
            for (int i = index; i < globalHistory.Count; i++)
            {
                if (!globalHistory[i].IsOwner && affectedGameObjects.Overlaps(globalHistory[i].ChangedObjects))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Runs the given <paramref name="task"/> with a <paramref name="timeout"/>.
        /// Note that a timeout of <see cref="TimeSpan.Zero"/> will cause no timeout to be applied.
        ///
        /// If the task does not complete within the given timeout and <paramref name="throwOnTimeout"/> is set to true,
        /// a <see cref="TimeoutException"/> is thrown.
        /// </summary>
        /// <param name="task">The task to run. It should accept a <see cref="CancellationToken"/> as an argument,
        /// which will be used to cancel the task if it exceeds the timeout.</param>
        /// <param name="timeout">The maximum time to wait for the task to complete.</param>
        /// <param name="throwOnTimeout">Whether to throw a <see cref="TimeoutException"/> if the task times out.</param>
        /// <exception cref="TimeoutException">Thrown if the task does not complete within the given timeout and
        /// <paramref name="throwOnTimeout"/> is set to <c>true</c>.</exception>
        /// <returns><c>true</c> if the task completed within the timeout, <c>false</c> otherwise.</returns>
        public static async UniTask<bool> RunWithTimeoutAsync(Func<CancellationToken, UniTask> task, TimeSpan timeout,
                                                        bool throwOnTimeout = true)
        {
            return await RunWithTimeoutAsync(async token =>
            {
                await task(token);
                return true;
            }, timeout, throwOnTimeout: throwOnTimeout, defaultValue: false);
        }

        /// <summary>
        /// Disables the given <paramref name="monoBehaviour"/>, if the given
        /// <paramref name="condition"/> is <code>true</code>. Returns the evaluated
        /// condition.
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour to disable on condition. null <c>null</c> <code>null</code></param>
        /// <param name="condition">The <c>boolean</c> condition to check <see langword="true"/>.</param>
        /// <returns>Returns True, <c>True</c>, <code>True</code>whether the given monoBehaviour was disabled or not (The
        /// evaluated condition).</returns>
        public static bool DisableOnCondition(UnityEngine.MonoBehaviour monoBehaviour, bool condition)
        {
            if (condition)
            {
                monoBehaviour.enabled = false;
            }
            return condition;
        }
    }
}
