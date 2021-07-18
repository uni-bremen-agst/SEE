using System.Collections.Generic;
using System.Linq;
using SEE.Controls.Actions.Architecture;
using SEE.Utils;
using Sirenix.Utilities;
using UnityEngine;

namespace SEE.Controls.Actions
{
    
   
    /// <summary>
    /// Action to model the architecture for the reflexion analysis
    /// </summary>
    public class ArchitectureAction : AbstractPlayerAction
    {
        
        #region Delegates

        public delegate void ArchitectureActionEnabled();

        public delegate void ArchitectureActionDisabled();

        #endregion
        
        #region Static Events
        
        /// <summary>
        /// Notifies the subscriber about the activation of this <see cref="AbstractPlayerAction"/> action.
        /// </summary>
        public static event ArchitectureActionEnabled OnArchitectureActionEnabled;
        
        /// <summary>
        /// Notifies the subscriber about the deactivation of this <see cref="AbstractPlayerAction"/> action.
        /// </summary>
        public static event ArchitectureActionDisabled OnArchitectureActionDisabled;

        #endregion
        
        /// <summary>
        /// The currently selected action.
        /// </summary>
        private IArchitectureAction activeAction;
        
        /// <summary>
        /// 
        /// </summary>
        private readonly Dictionary<string, AbstractArchitectureAction> nameToAction =
            new Dictionary<string, AbstractArchitectureAction>();
        
        public override HashSet<string> GetChangedObjects()
        {
            // We handle the undo process within this action itself
            return new HashSet<string>();
        }

        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Architecture;
        }

        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        public static ReversibleAction CreateReversibleAction()
        {
            return new ArchitectureAction();
        }

        public override bool Update()
        {
            activeAction.Update();
            return false;
        }

        public override void Start()
        {
            Debug.Log("Start");
            OnArchitectureActionEnabled?.Invoke();
            InitializeActions();
            activeAction.Start();
        }


        public override void Stop()
        {
            Debug.Log("Stop");
            OnArchitectureActionDisabled?.Invoke();
            activeAction.Stop();
        }

        public override void Awake()
        {
            Debug.Log("Awake");
        }

        
        /// <summary>
        /// Initializes the available <see cref="AbstractArchitectureAction"/> instances.
        /// </summary>
        private void InitializeActions()
        {
            foreach (ArchitectureActionType type in ArchitectureActionType.AllTypes)
            {
                AbstractArchitectureAction action = type.CreateAbstractArchitectureAction();
                nameToAction.Add(type.Name, action);
            }

            nameToAction.Values.ForEach(a => a.Awake());
            activeAction = nameToAction.FirstOrDefault().Value;
        }
    }
}