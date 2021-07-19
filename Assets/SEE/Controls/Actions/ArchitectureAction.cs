using System;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.ModernUIPack;
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
        /// Maps the <see cref="AbstractArchitectureAction"/> name to their respective instance.
        /// </summary>
        private readonly Dictionary<string, AbstractArchitectureAction> nameToAction =
            new Dictionary<string, AbstractArchitectureAction>();

        /// <summary>
        /// Maps the <see cref="AbstractArchitectureAction"/> name to their respective ui button element.
        /// </summary>
        private readonly Dictionary<string, GameObject> nameToUI = new Dictionary<string, GameObject>();


        /// <summary>
        /// The global UI Canvas to place UI elements on.
        /// </summary>
        private GameObject UICanvas;

        private GameObject IndicatorUI;
        
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
            // Find the UI Canvas to place the AbstractArchitectureAction buttons on.
            if (UICanvas == null)
            {
                UICanvas = GameObject.Find("UI Canvas");
            }
            //Initialize the action instances
            InitializeActions();
            //Start the active action.
            activeAction.Start();
        }


        public override void Stop()
        {
            Debug.Log("Stop");
            OnArchitectureActionDisabled?.Invoke();
            activeAction.Stop();
            nameToUI.Values.ForEach(e => e.SetActive(false));
            IndicatorUI.SetActive(false);
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
                GameObject uiElement = PrefabInstantiator.InstantiatePrefab(type.PrefabPath, UICanvas.transform, false);
                ButtonManagerBasicIcon bmi = uiElement.GetComponent<ButtonManagerBasicIcon>();
                bmi.clickEvent.AddListener((() => SwitchMode(type.Name)));
                nameToUI.Add(type.Name, uiElement);
                nameToAction.Add(type.Name, action);
            }
            nameToAction.Values.ForEach(a => a.Awake());
            activeAction = nameToAction.Values.First();
            IndicatorUI = PrefabInstantiator.InstantiatePrefab("Prefabs/Architecture/UI/Indicator");
        }

        
        /// <summary>
        /// Switches between the separate <see cref="ArchitectureAction"/> sub modes e.g Draw or Select.
        /// </summary>
        /// <param name="actionName">The name of the exception defined in <see cref="ArchitectureActionType"/></param>
        /// <exception cref="Exception">Thrown when an <paramref name="actionName"/>
        /// was passed that has no <see cref="AbstractArchitectureAction"/> implementation</exception>
        private void SwitchMode(string actionName)
        {
            Debug.Log("Switch Action");
            if (nameToAction.TryGetValue(actionName, out AbstractArchitectureAction action))
            {
                activeAction.Stop();
                activeAction = action;
                activeAction.Start();
                return;
            }
            throw new Exception($"There is no implementation of AbstractArchitectureAction for action {actionName}");
        }
    }
}