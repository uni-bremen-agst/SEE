using SEE.Audio;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Actions
{
    internal abstract class AbstractHideAction : ReversibleAction
    {
        /// <summary>
        /// The unique ID of an action.
        /// </summary>
        private readonly Guid id = Guid.NewGuid();

        /// <summary>
        /// The current state of the action as specified by <see cref="ReversibleAction.Progress"/>.
        /// </summary>
        protected ReversibleAction.Progress currentState = ReversibleAction.Progress.NoEffect;

        /// <summary>
        /// Returns the current state of the action indicating whether it has had an effect 
        /// that may need to be undone and whether it is still ongoing.
        /// Implements <see cref="ReversibleAction.CurrentProgress"/>.
        /// </summary>
        /// <returns>the current state of the action</returns>
        public ReversibleAction.Progress CurrentProgress() => currentState;

        /// <summary>
        /// Returns the ID of this action.
        /// </summary>
        /// <returns>The ID of this action as a string</returns>
        public string GetId() => id.ToString();

        /// <summary>
        /// The list of currently hidden objects.
        /// </summary>
        protected readonly ISet<GameObject> hiddenObjects = new HashSet<GameObject>();

        /// <summary>
        /// Time of the hiding/showing animation in seconds.
        /// </summary>
        private const float animationDuration = 1.0f;

        public virtual void Awake()
        {
            // Intentionally left blank. Can be overridden by subclasses.
        }
        public void Start()
        {
            // Intentionally left blank. Can be overridden by subclasses.
        }
        public void Stop()
        {
            // Intentionally left blank. Can be overridden by subclasses.
        }

        public abstract ActionStateType GetActionStateType();

        public HashSet<string> GetChangedObjects()
        {
            return new HashSet<String>(hiddenObjects.Select(o => o.name));
        }

        public abstract ReversibleAction NewInstance();

        public void Redo()
        {
            HideObjects();
        }

        private void HideObjects()
        {
            foreach (GameObject go in hiddenObjects)
            {
                if (go.TryGetComponent(out GraphElementOperator graphElementOperator))
                {
                    graphElementOperator.Show(Game.City.GraphElementAnimationKind.Fading, animationDuration);
                }
            }
        }

        public void Undo()
        {
            ShowObjects();
        }

        private void ShowObjects()
        {
            foreach (GameObject go in hiddenObjects)
            {
                if (go.TryGetComponent(out GraphElementOperator graphElementOperator))
                {
                    graphElementOperator.Hide(Game.City.GraphElementAnimationKind.Fading, animationDuration);
                }
            }
        }

        public virtual bool Update()
        {
            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
            {
                // TODO: new HideNetAction(selectedNode.name).Execute();
                hiddenObjects.UnionWith(Hide(raycastHit.collider.gameObject));
                HideObjects();
                currentState = ReversibleAction.Progress.Completed;
                AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.DROP_SOUND);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the nodes and/or edges that are to be hidden.
        /// </summary>
        /// <param name="selection">the currently selected node or edge</param>
        /// <returns>The set of game objects to be hidden.</returns>
        protected abstract ISet<GameObject> Hide(GameObject selection);
    }
}