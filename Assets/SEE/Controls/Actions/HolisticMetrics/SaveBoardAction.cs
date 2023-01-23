using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using SEE.Game.UI.HolisticMetrics;
using SEE.Game.UI.PropertyDialog.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.HolisticMetrics
{
    public class SaveBoardAction : AbstractPlayerAction
    {
        private const string buttonPath = "Prefabs/HolisticMetrics/SceneComponents/SaveBoardButton";

        private GameObject button;

        private LoadBoardButtonController buttonController;

        private bool buttonClicked;

        private Memento memento;

        private struct Memento
        {
            internal readonly string filename; 
                
            internal readonly WidgetsManager widgetsManager;

            internal Memento(string filename, WidgetsManager widgetsManager)
            {
                this.filename = filename;
                this.widgetsManager = widgetsManager;
            }
        }
        
        public override void Start()
        {
            button = PrefabInstantiator.InstantiatePrefab(buttonPath, GameObject.Find("UI Canvas").transform,
                false);
            buttonController = button.GetComponent<LoadBoardButtonController>();
        }
        
        public override bool Update()
        {
            if (!buttonClicked && buttonController.GetClick())
            {
                new SaveBoardConfigurationDialog().Open();
                buttonClicked = true;
                return false;
            }

            if (buttonClicked && SaveBoardConfigurationDialog.GetUserInput(out string filename, out WidgetsManager widgetsManager))
            {
                memento = new Memento(filename, widgetsManager);
                Redo();
                return true;
            }

            return false;
        }

        public override void Stop()
        {
            Destroyer.Destroy(button);
        }

        public override void Undo()
        {
            ConfigManager.DeleteBoard(memento.filename);
        }
        
        public override void Redo()
        {
            ConfigManager.SaveBoard(memento.widgetsManager, memento.filename);
        }

        public static ReversibleAction CreateReversibleAction()
        {
            return new SaveBoardAction();
        }
        
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }
        
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.SaveBoard;
        }
        
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.filename };
        }
    }
}