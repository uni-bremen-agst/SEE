using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Components;
using SEE.Net.Actions.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// This class is responsible for executing or reverting widget deletions. (holistic metric widgets)
    /// </summary>
    internal class DeleteWidgetAction : AbstractPlayerAction
    {
        private Memento memento;

        private struct Memento
        {
            /// <summary>
            /// The name of the board from which to delete the widget.
            /// </summary>
            internal readonly string boardName;

            /// <summary>
            /// The configuration of the widget, so it can be restored.
            /// </summary>
            internal readonly WidgetConfig widgetConfig;

            /// <summary>
            /// Writes the two parameter values into fields of the class.
            /// </summary>
            /// <param name="boardName">The name of the board from which to delete the widget</param>
            /// <param name="widgetConfig">The configuration of the widget, so it can be restored</param>
            internal Memento(string boardName, WidgetConfig widgetConfig)
            {
                this.boardName = boardName;
                this.widgetConfig = widgetConfig;
            }
        }

        public override void Start()
        {
            BoardsManager.AddWidgetDeleters();
        }
        
        public override bool Update()
        {
            if (BoardsManager.GetWidgetDeletion(out string boardName, out WidgetConfig widgetConfig))
            {
                memento = new Memento(boardName, widgetConfig);
                Redo();
                return true;
            }
            return false;
        }

        public override void Stop()
        {
            WidgetDeleter.Stop();
        }

        /// <summary>
        /// Returns a new instance of <see cref="DeleteBoardAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DeleteWidgetAction();
        }
        
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// This method executes the action (deletes the widget from the board on all clients).
        /// </summary>
        public override void Redo()
        {
            // The widgets manager that manages the widget we want to delete
            WidgetsManager widgetsManager = BoardsManager.Find(memento.boardName);

            if (widgetsManager != null)
            {
                // Delete the widget locally
                widgetsManager.Delete(memento.widgetConfig.ID);
            
                // Delete the widget on the other clients
                new DeleteWidgetNetAction(memento.boardName, memento.widgetConfig.ID).Execute();   
            }
            else
            {
                Debug.LogError($"Tried to delete a widget from a board named {memento.boardName} that " +
                               $"could not be found.\n");
            }
        }
        
        /// <summary>
        /// This method creates the widget again from the saved configuration (on all clients).
        /// </summary>
        public override void Undo()
        {
            WidgetsManager widgetsManager = BoardsManager.Find(memento.boardName);

            if (widgetsManager != null)
            {
                widgetsManager.Create(memento.widgetConfig);
                new CreateWidgetNetAction(memento.boardName, memento.widgetConfig).Execute();    
            }
            else
            {
                Debug.LogError($"Tried to create a widget on a board named {memento.boardName} that " +
                               $"could not be found.\n");
            }
        }

        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.widgetConfig.ID.ToString() };
        }

        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.DeleteWidget;
        }
    }
}