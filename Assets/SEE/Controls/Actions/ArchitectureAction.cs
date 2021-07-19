using System;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Architecture;
using SEE.Controls.Architecture;
using SEE.DataModel;
using SEE.Game;
using SEE.Game.UI.Architecture;
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

        /// <summary>
        /// UI element that shows the currently selected <see cref="AbstractArchitectureAction"/>.
        /// Components attached:
        /// <see cref="Game.UI.Architecture.ModeIndicator"/>
        /// </summary>
        private GameObject ModeIndicator;

        /// <summary>
        /// Modal Dialog that asks for delete permission.
        /// Prefab instantiated from Prefabs/Architecture/UI/DeleteElementDialog
        /// Components attached:
        /// <see cref="DeleteElementDialog"/>
        /// </summary>
        private GameObject DeleteElementDialog;
        
        /// <summary>
        /// UI Context menu for node selection. Contains context action such as delete or edit for nodes.
        /// Prefab instantiated from Prefabs/Architecture/UI/NodeContextMenuHolder
        /// Components attached:
        /// <see cref="NodeContextMenu"/>
        /// </summary>
        private GameObject NodeContextMenu;

        /// <summary>
        /// UI context menu for edge selection. Contains context actions such as delete.
        /// Prefab instantiated from Prefabs/Architecture/UI/EdgeContextMenuHolder
        /// Components attached:
        /// <see cref="EdgeContextMenu"/>
        /// </summary>
        private GameObject EdgeContextMenu;
        /// <summary>
        /// UI element that holds the graph element tooltip for displaying the names.
        /// Prefab instantiated from Prefabs/Architecture/UI/ObjectTooltip.
        /// Components attached:
        /// <see cref="TooltipHolder"/>
        /// <see cref="Tooltip"/>
        /// </summary>
        private GameObject ToolTipDisplay;


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
            //Get the PenInteractionController from the SEECityArchitecture
            SEECityArchitecture city = SceneQueries.FindArchitectureCity();
            if (!city.TryGetComponent(out PenInteractionController penInteractionController))
            {
                throw new Exception(
                    "City game object does not have the PenInteractionController component attached! Check your setup");
            }
            //Initialize the action instances
            InitializeActions(penInteractionController);
            PrepareUI(penInteractionController);
            //Start the active action.
            activeAction.Start();
        }


        public override void Stop()
        {
            OnArchitectureActionDisabled?.Invoke();
            activeAction.Stop();
            nameToUI.Values.ForEach(e => e.SetActive(false));
            Destroyer.DestroyGameObject(ModeIndicator);
        }

        public override void Awake()
        {
            Debug.Log("Awake");
        }

        /// <summary>
        /// Instantiates the UI elements for the <see cref="ArchitectureAction"/>.
        /// </summary>
        private void PrepareUI(PenInteractionController penInteractionController)
        {
            ModeIndicator = PrefabInstantiator.InstantiatePrefab("Prefabs/Architecture/UI/Indicator", UICanvas.transform, false);
            ModeIndicator.GetComponent<ModeIndicator>().ChangeStateIndicator(activeAction.GetActionType().Name);
            DeleteElementDialog = PrefabInstantiator.InstantiatePrefab("Prefabs/Architecture/UI/DeleteElementDialog", UICanvas.transform, false);
            NodeContextMenu = PrefabInstantiator.InstantiatePrefab("Prefabs/Architecture/UI/NodeContextMenuHolder",
                UICanvas.transform, false);
            EdgeContextMenu = PrefabInstantiator.InstantiatePrefab("Prefabs/Architecture/UI/EdgeContextMenuHolder",
                UICanvas.transform, false);
            // Initialize Tooltip Display
            ToolTipDisplay = PrefabInstantiator.InstantiatePrefab("Prefabs/Architecture/UI/ObjectTooltip", UICanvas.transform, false);
            ToolTipDisplay.GetComponent<TooltipHolder>().Controller = penInteractionController;
        }
        

        
        /// <summary>
        /// Initializes the available <see cref="AbstractArchitectureAction"/> instances.
        /// </summary>
        private void InitializeActions(PenInteractionController penInteractionController)
        {
           
            foreach (ArchitectureActionType type in ArchitectureActionType.AllTypes)
            {
                AbstractArchitectureAction action = type.CreateAbstractArchitectureAction();
                action.PenInteractionController = penInteractionController;
                GameObject uiElement = PrefabInstantiator.InstantiatePrefab(type.PrefabPath, UICanvas.transform, false);
                ButtonManagerBasicIcon bmi = uiElement.GetComponent<ButtonManagerBasicIcon>();
                bmi.clickEvent.AddListener((() => SwitchMode(type.Name)));
                nameToUI.Add(type.Name, uiElement);
                nameToAction.Add(type.Name, action);
            }
            nameToAction.Values.ForEach(a => a.Awake());
            activeAction = nameToAction.Values.First();
            
            

        }

        
        /// <summary>
        /// Switches between the separate <see cref="ArchitectureAction"/> sub modes e.g Draw or Select.
        /// Updates the <see cref="ModeIndicator"/> with the new <see cref="AbstractArchitectureAction"/> name.
        /// </summary>
        /// <param name="actionName">The name of the exception defined in <see cref="ArchitectureActionType"/></param>
        /// <exception cref="Exception">Thrown when an <paramref name="actionName"/>
        /// was passed that has no <see cref="AbstractArchitectureAction"/> implementation</exception>
        private void SwitchMode(string actionName)
        {
            if (nameToAction.TryGetValue(actionName, out AbstractArchitectureAction action))
            {
                activeAction.Stop();
                activeAction = action;
                activeAction.Start();
                ModeIndicator.GetComponent<ModeIndicator>().ChangeStateIndicator(action.GetActionType().Name);
                return;
            }
            throw new Exception($"There is no implementation of AbstractArchitectureAction for action {actionName}");
        }
    }
}