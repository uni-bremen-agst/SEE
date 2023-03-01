using SEE.Game.Operator;
using SEE.Utils;
using System;
using System.Collections.Generic;
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
        public abstract HashSet<string> GetChangedObjects();

        public abstract ReversibleAction NewInstance();

        public void Redo()
        {
            HideObjects();
        }

        private void HideObjects()
        {
            foreach (GameObject go in hiddenObjects)
            {
                if (go.TryGetComponent(out EdgeOperator edgeOperator))
                {
                    edgeOperator.Show(Game.City.EdgeAnimationKind.Fading, animationDuration);
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
                if (go.TryGetComponent(out EdgeOperator edgeOperator))
                {
                    edgeOperator.Hide(Game.City.EdgeAnimationKind.Fading, animationDuration);
                }
            }
        }

        public abstract bool Update();
    }
}