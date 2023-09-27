using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RootMotion.FinalIK.HitReaction;
using Assets.SEE.Net.Actions.Whiteboard;
using SEE.Net.Actions;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using static SimpleFileBrowser.FileBrowser;
using SimpleFileBrowser;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Allows to create drawings by the mouse cursor.
    /// It serves as an example for a continuous action that modifies the
    /// scene while active.
    /// </summary>
    class LoadAction : AbstractPlayerAction
    {
        

        /// <summary>
        /// Undo and Redo only used when a object is clicked! Don't support the color picker. Because the color picker would make to many states.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit))// && // Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    //GameDrawableFinder.hasDrawableParent(raycastHit.collider.gameObject))
                {

                }

            }
            return result;
        }

        public override void Awake()
        {
            GameObject.Find("UI Canvas").AddComponent<Test>();
        }



        public override void Stop()
        {
            Destroyer.Destroy(GameObject.Find("UI Canvas").GetComponent<Test>());
        }


        /// <summary>
        /// A new instance of <see cref="DrawOnAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawOnAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new LoadAction();
        }

        /// <summary>
        /// A new instance of <see cref="DrawOnAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawOnAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.DrawOnWhiteboard"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Load;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>an empty set</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>();
        }
    }
}
