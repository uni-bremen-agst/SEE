using SEE.Controls.Actions;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Controls.Actions.Drawable.LoadAction;
using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Game.UI.Notification;
using SEE.Game;

namespace SEE.Controls.Actions.Drawable
{
    public class StickyNoteAction : AbstractPlayerAction
    {
        /// <summary>
        /// The prefab of the sticky note menu.
        /// </summary>
        private const string stickyNoteMenuPrefab = "Prefabs/UI/Drawable/StickyNoteMenu";

        /// <summary>
        /// The instance for the sticky note menu.
        /// </summary>
        private GameObject stickyNoteMenu;

        /// <summary>
        /// The prefab of the sticky note menu.
        /// </summary>
        private const string rotationMenuPrefab = "Prefabs/UI/Drawable/StickyNoteRotation";

        /// <summary>
        /// The instance for the sticky note chose rotation menu.
        /// </summary>
        private GameObject rotationMenu;

        private ProgressState progress = ProgressState.SelectAction;
        private enum ProgressState
        {
            SelectAction,
            DoAction,
            FinishAction
        }
        private ActionStates selectedAction = ActionStates.None;
        private enum ActionStates
        {
            None,
            Spawn,
            Move,
            Edit,
            Delete
        }

        private bool selectRotation = false;

        public override void Awake()
        {
            stickyNoteMenu = PrefabInstantiator.InstantiatePrefab(stickyNoteMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            ButtonManagerBasic spawn = GameFinder.FindChild(stickyNoteMenu, "Spawn").GetComponent<ButtonManagerBasic>();
            spawn.clickEvent.AddListener(() =>
            {
                progress = ProgressState.DoAction;
                selectedAction = ActionStates.Spawn;
            });
            ButtonManagerBasic move = GameFinder.FindChild(stickyNoteMenu, "Move").GetComponent<ButtonManagerBasic>();
            move.clickEvent.AddListener(() =>
            {
                progress = ProgressState.DoAction;
                selectedAction = ActionStates.Move;
            });
            ButtonManagerBasic edit = GameFinder.FindChild(stickyNoteMenu, "Edit").GetComponent<ButtonManagerBasic>();
            edit.clickEvent.AddListener(() =>
            {
                progress = ProgressState.DoAction;
                selectedAction = ActionStates.Edit;
            });
            ButtonManagerBasic delete = GameFinder.FindChild(stickyNoteMenu, "Delete").GetComponent<ButtonManagerBasic>();
            delete.clickEvent.AddListener(() =>
            {
                progress = ProgressState.DoAction;
                selectedAction = ActionStates.Delete;
            });
        }

        /// <summary>
        /// Destroys the menu's.
        /// </summary>
        public override void Stop()
        {
            Destroyer.Destroy(stickyNoteMenu);
        }

        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                if (Input.GetMouseButtonDown(0) && selectedAction == ActionStates.None && progress == ProgressState.SelectAction)
                {
                    ShowNotification.Info("Select an operation", "First you need to select an operation from the menu.");
                }

                if (Input.GetMouseButtonDown(0) && selectedAction == ActionStates.Spawn && progress == ProgressState.DoAction
                && Raycasting.RaycastAnything(out RaycastHit raycastHit))
                {
                    GameObject w = PrefabInstantiator.InstantiatePrefab("Prefabs/Whiteboard/StickyNote");
                    w.transform.rotation = raycastHit.collider.gameObject.transform.rotation;
                    w.transform.position = raycastHit.point - w.transform.forward * ValueHolder.distanceToDrawable.z * ValueHolder.currentOrderInLayer;
                    ValueHolder.currentOrderInLayer++;
                    w.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    w.transform.Find("Front").GetComponent<MeshRenderer>().material.color = Random.ColorHSV().Darker();

                    if (!raycastHit.collider.gameObject.CompareTag(Tags.Drawable) &&
                        !GameFinder.hasDrawable(raycastHit.collider.gameObject))
                    {
                        SEEInput.KeyboardShortcutsEnabled = false;
                        stickyNoteMenu.SetActive(false);
                        rotationMenu = PrefabInstantiator.InstantiatePrefab(rotationMenuPrefab,
                            GameObject.Find("UI Canvas").transform, false);
                        GameFinder.FindChild(rotationMenu, "Laying").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
                        {
                            SEEInput.KeyboardShortcutsEnabled = true;
                            w.transform.eulerAngles = new Vector3(90, w.transform.eulerAngles.y, w.transform.eulerAngles.z);
                            Destroyer.Destroy(rotationMenu);
                            stickyNoteMenu.SetActive(true);
                            if (raycastHit.collider.gameObject.name.Equals("Floor"))
                            {
                                w.transform.position -= w.transform.forward * ValueHolder.distanceToDrawable.z * ValueHolder.currentOrderInLayer; ; 
                            }
                            selectRotation = true;
                        });

                        GameFinder.FindChild(rotationMenu, "Hanging").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
                        {
                            SEEInput.KeyboardShortcutsEnabled = true;
                            Destroyer.Destroy(rotationMenu);
                            stickyNoteMenu.SetActive(true);
                            selectRotation = true;
                        });
                    } else
                    {
                        return true;
                    }
                }

                if (selectRotation)
                {
                    return true;
                }

                if (Input.GetMouseButtonDown(0) && selectedAction == ActionStates.Delete && progress == ProgressState.DoAction
                    && Raycasting.RaycastAnything(out RaycastHit hit) &&
                    (hit.collider.gameObject.CompareTag(Tags.Drawable) ||
                    GameFinder.hasDrawable(hit.collider.gameObject)))
                {
                    GameObject drawable = hit.collider.gameObject.CompareTag(Tags.Drawable) ?
                                    hit.collider.gameObject : GameFinder.FindDrawable(hit.collider.gameObject);
                    if (GameFinder.GetDrawableParentName(drawable).Contains("StickyNote"))
                    {
                        Destroyer.Destroy(GameFinder.GetHighestParent(drawable));
                        return true;
                    }
                    else
                    {
                        ShowNotification.Info("Operation canceled", "You don't selected a sticky note.");
                        return false;
                    }

                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Undo()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public override void Redo()
        {

        }

        /// <summary>
        /// A new instance of <see cref="StickyNoteAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="StickyNoteAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new StickyNoteAction();
        }

        /// <summary>
        /// A new instance of <see cref="StickyNoteAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="StickyNoteAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.StickyNote"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.StickyNote;
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