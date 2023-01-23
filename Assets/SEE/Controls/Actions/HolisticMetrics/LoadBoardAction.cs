using System;
using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.Game.UI.HolisticMetrics;
using SEE.Game.UI.PropertyDialog.HolisticMetrics;
using SEE.Net.Actions.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.HolisticMetrics
{
    public class LoadBoardAction : AbstractPlayerAction
    {
        private const string buttonPath = "Prefabs/HolisticMetrics/SceneComponents/LoadBoardButton";

        private GameObject button;

        private LoadBoardButtonController buttonController;

        private bool buttonClicked;

        private Memento memento;

        private struct Memento
        {
            
            internal readonly BoardConfig config;

            internal Memento(BoardConfig config)
            {
                this.config = config;
            }
        }
        
        public override void Start()
        {
            button = PrefabInstantiator.InstantiatePrefab(buttonPath, GameObject.Find("UI Canvas").transform,
                false);
        }
        
        public override bool Update()
        {
            if (!buttonClicked && buttonController.GetClick())
            {
                new LoadBoardConfigurationDialog().Open();
                buttonClicked = true;
                return false;
            }

            if (buttonClicked && LoadBoardConfigurationDialog.GetConfig(out string filename))
            {
                try
                {
                    BoardConfig config = ConfigManager.LoadBoard(filename);
                    memento = new Memento(config);
                    Redo();
                    return true;
                }
                catch (Exception exception)
                {
                    Debug.LogError(exception);
                }

                buttonClicked = false;
            }

            return false;
        }

        public override void Stop()
        {
            Destroyer.Destroy(button);
        }

        public override void Undo()
        {
            BoardsManager.Delete(memento.config.Title);
            new DeleteBoardNetAction(memento.config.Title).Execute();
        }
        
        public override void Redo()
        {
            BoardsManager.Create(memento.config);
            new CreateBoardNetAction(memento.config).Execute();
        }

        public static ReversibleAction CreateReversibleAction()
        {
            return new LoadBoardAction();
        }
        
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }
        
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.LoadBoard;
        }
        
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.config.Title };
        }
    }
}