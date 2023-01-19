using System;
using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using SEE.Game.UI.HolisticMetrics;
using SEE.Net.Actions.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This action manages the creation of a new metrics board.
    /// </summary>
    internal class NewBoardAction : AbstractPlayerAction
    {
        /// <summary>
        /// This field saves the information needed for reverting/recovering this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This field saves the current progress of this instance.
        /// </summary>
        private ProgressState progress = ProgressState.Initial;

        /// <summary>
        /// The progress of this action.
        /// </summary>
        private enum ProgressState
        {
            Initial,
            WaitingForName,
            WaitingForPosition,
            WaitingForRotation,
            Finished
        }
        
        /// <summary>
        /// All the information needed for reverting/recovering this action.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The configuration of the board to create/that has been created.
            /// </summary>
            public readonly BoardConfig boardConfig;

            /// <summary>
            /// The constructor that can be used to create a new instance of this struct.
            /// </summary>
            /// <param name="boardConfig">The <see cref="BoardConfig"/> of the new board.</param>
            public Memento(BoardConfig boardConfig)
            {
                this.boardConfig = boardConfig;
            }
        }
        
        /// <summary>
        /// Allows the player to create a new metrics board.
        /// </summary>
        /// <returns>Whether or not this action is done.</returns>
        /// <exception cref="Exception">This should be impossible.</exception>
        public override bool Update()
        {
            switch (progress)
            {
                case ProgressState.Initial:
                    GameObject.Find("/DemoWorld/Plane").AddComponent<BoardAdder>();
                    progress = ProgressState.WaitingForName;
                    return false;
                case ProgressState.WaitingForName:
                    
                case ProgressState.WaitingForPosition:
                    if (BoardAdder.GetBoardConfig(out BoardConfig boardConfig))  // We get the config
                    {
                        memento = new Memento(boardConfig);
                        PrefabInstantiator
                            .InstantiatePrefab(
                                "Prefabs/UI/MetricsBoardRotation",
                                GameObject.Find("UI Canvas").transform,
                                instantiateInWorldSpace: false)
                            .GetComponent<AddBoardSliderController>()
                            .Setup(boardConfig);
                        progress = ProgressState.WaitingForRotation;
                    }
                    return false;
                case ProgressState.WaitingForRotation:
                    if (AddBoardSliderController.GetRotation(out Quaternion rotation))  // We get the rotation
                    {
                        memento.boardConfig.Rotation = rotation;
                        Redo();
                        progress = ProgressState.Finished;
                        return true;
                    }
                    return false;
                case ProgressState.Finished:
                    return true;
                default:
                    throw new Exception("This should not be possible");
            }
        }

        /// <summary>
        /// Creates a new instance of this.
        /// </summary>
        /// <returns>The new instance of this class.</returns>
        internal static ReversibleAction CreateReversibleAction()
        {
            return new NewBoardAction();
        }

        /// <summary>
        /// Creates a new instance of this.
        /// </summary>
        /// <returns>The new instance of this class.</returns>
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
        /// This method recovers the changes made by this action.
        /// </summary>
        public override void Redo()
        {
            BoardsManager.Create(memento.boardConfig);
            new CreateBoardNetAction(memento.boardConfig).Execute();
        }

        /// <summary>
        /// Returns a <see cref="HashSet{T}"/> containing one element, i.e., the board that was created by this action.
        /// </summary>
        /// <returns>A <see cref="HashSet{T}"/> containing one element, i.e., the board that was created by this action.
        /// </returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.boardConfig.Title };
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this class, i.e., <see cref="ActionStateType.NewBoard"/>.
        /// </summary>
        /// <returns>The <see cref="ActionStateType"/> of this class, i.e., <see cref="ActionStateType.NewBoard"/>.
        /// </returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.NewBoard;
        }
    }
}