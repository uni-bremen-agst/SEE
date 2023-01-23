using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using SEE.Game.UI.HolisticMetrics;
using SEE.Game.UI.PropertyDialog.HolisticMetrics;
using SEE.Net.Actions.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This action manages the creation of a specific metrics board.
    /// </summary>
    internal class AddBoardAction : AbstractPlayerAction
    {
        private const string boardRotatorPath = "Prefabs/UI/MetricsBoardRotation";
        
        private ProgressState progress;
        
        private Memento memento;
        
        private enum ProgressState
        {
            GettingPosition,
            GettingRotation,
            GettingName
        }
        
        private struct Memento
        {
            /// <summary>
            /// The configuration of the board to create/that has been created.
            /// </summary>
            internal readonly BoardConfig boardConfig;

            /// <summary>
            /// Creates this action. That does not execute it, it only prepares it.
            /// </summary>
            /// <param name="boardConfig">The configuration of the board to create.</param>
            internal Memento(BoardConfig boardConfig)
            {
                this.boardConfig = boardConfig;
            }    
        }
        
        public override void Start()
        {
            BoardAdder.Init();
        }

        public override bool Update()
        {
            switch (progress)
            {
                case ProgressState.GettingPosition:
                    if (BoardAdder.GetPosition(out Vector3 position))
                    {
                        progress = ProgressState.GettingRotation;
                        memento = new Memento(new BoardConfig { Position = position });
                        GameObject slider = PrefabInstantiator.InstantiatePrefab(boardRotatorPath, 
                            GameObject.Find("UI Canvas").transform, false);
                        slider.GetComponent<AddBoardSliderController>().Setup(position);
                    }

                    return false;
                case ProgressState.GettingRotation:
                    if (AddBoardSliderController.GetRotation(out Quaternion rotation))
                    {
                        memento.boardConfig.Rotation = rotation;
                        progress = ProgressState.GettingName;
                        new AddBoardDialog().Open();
                    }
                    
                    return false;
                case ProgressState.GettingName:
                    if (AddBoardDialog.GetName(out string name))
                    {
                        memento.boardConfig.Title = name;
                        Redo();
                        return true;
                    }

                    return false;
                default:
                    return false;
            }
        }

        public override void Stop()
        {
            BoardAdder.Stop();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddBoardAction"/>.
        /// </summary>
        /// <returns>The new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new AddBoardAction();
        }
        
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Deletes the board that was created.
        /// </summary>
        public override void Undo()
        {
            BoardsManager.Delete(memento.boardConfig.Title);
            new DeleteBoardNetAction(memento.boardConfig.Title).Execute();
        }

        /// <summary>
        /// This method (re-)executes the action, i.e. creates the board from the given configuration.
        /// </summary>
        public override void Redo()
        {
            BoardsManager.Create(memento.boardConfig);
            new CreateBoardNetAction(memento.boardConfig).Execute();
        }
        
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.boardConfig.Title };
        }

        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.AddBoard;
        }
    }
}