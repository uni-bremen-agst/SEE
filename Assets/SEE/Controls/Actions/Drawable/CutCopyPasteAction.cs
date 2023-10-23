using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game.UI.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions;
using SEE.Controls.Actions.Drawable;
using SEE.Game;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This action provides the cut and copy functionality for drawable types.
    /// </summary>
    public class CutCopyPasteAction : AbstractPlayerAction
    {
        /// <summary>
        /// Holds the current progress state.
        /// </summary>
        private ProgressState progressState = ProgressState.SelectObject;

        /// <summary>
        /// The progress states of the <see cref="CutCopyPasteAction"/>
        /// </summary>
        private enum ProgressState
        {
            SelectObject,
            CutCopyPaste,
            Finish
        }

        /// <summary>
        /// Holds the current state.
        /// </summary>
        private CutCopy state = CutCopy.None;

        /// <summary>
        /// The state of cut or copy is chosen.
        /// </summary>
        private enum CutCopy
        {
            None,
            Cut,
            Copy
        }

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="CutCopyPasteAction"/>
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="CutCopyPasteAction"/>
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The old values of the drawable type object.
            /// </summary>
            public readonly DrawableType oldValueHolder;
            /// <summary>
            /// The new values of the drawable type object.
            /// </summary>
            public readonly DrawableType newValueHolder;
            /// <summary>
            /// The drawable on that the drawable type object was displayed.
            /// </summary>
            public readonly GameObject oldDrawable;
            /// <summary>
            /// The drawable on that the drawable type object is now displayed.
            /// </summary>
            public readonly GameObject newDrawable;

            /// <summary>
            /// The state if it was copied or cutted.
            /// </summary>
            public readonly CutCopy state;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="oldValueHolder">The old values of the drawable type object.</param>
            /// <param name="newValueHolder">The new edited values of the drawable type object.</param>
            /// <param name="oldDrawable">The drawable on that the drawable type object was displayed.</param>
            /// <param name="newDrawable">The drawable on that the drawable type object is displayed.</param>
            /// <param name="cutCopy">The stat if it was cutted or copied.</param>
            public Memento(DrawableType oldValueHolder,
                DrawableType newValueHolder, GameObject oldDrawable, GameObject newDrawable, CutCopy cutCopy)
            {
                this.oldValueHolder = oldValueHolder;
                this.newValueHolder = newValueHolder;
                this.oldDrawable = oldDrawable;
                this.newDrawable = newDrawable;
                this.state = cutCopy;
            }
        }

        /// <summary>
        /// The prefab of the cut copy paste menu.
        /// </summary>
        private const string cutCopyPasteMenuPrefab = "Prefabs/UI/Drawable/CutCopyPaste";
        /// <summary>
        /// The instance of the cut copy paste menu
        /// </summary>
        private GameObject cutCopyPasteMenu;

        /// <summary>
        /// The new created object.
        /// </summary>
        private GameObject newObject;

        /// <summary>
        /// The selected drawable type object that should be cut or copied.
        /// </summary>
        private GameObject selectedObj;

        /// <summary>
        /// The id of the old selected object of the last run.
        /// </summary>
        private static string oldSelectedId;

        /// <summary>
        /// Bool that represents that the left mouse button was released after finish.
        /// It is necessary to prevent the previously selected object from being accidentally selected again. 
        /// After the action has successfully completed, it starts again, allowing for the selection of a new object. 
        /// This option enables the immediate selection of another object while holding down the mouse button.
        /// </summary>
        private static bool mouseWasReleased = true;

        /// <summary>
        /// Bool that represents that the paste button was pressed.
        /// </summary>
        private bool pastePressed = false;

        /// <summary>
        /// The old values of the selected drawable type.
        /// </summary>
        private DrawableType oldValueHolder;

        /// <summary>
        /// The new values of the selected drawable type.
        /// </summary>
        private DrawableType newValueHolder;

        /// <summary>
        /// The drawable where the selected object was displayed.
        /// </summary>
        private GameObject oldDrawable;

        /// <summary>
        /// The drawable where the new object is displayed.
        /// </summary>
        private GameObject newDrawable;

        /// <summary>
        /// Resets the old selected object, if the action state will leave.
        /// </summary>
        public static void Reset()
        {
            oldSelectedId = "";
            mouseWasReleased = true;
        }

        /// <summary>
        /// Deactivates the blink effect if, it is still active and destroys cut copy paste menu.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            if (selectedObj != null && selectedObj.GetComponent<BlinkEffect>() != null)
            {
                selectedObj.GetComponent<BlinkEffect>().Deactivate();
            }

            if (cutCopyPasteMenu != null)
            {
                Destroyer.Destroy(cutCopyPasteMenu);
            }
        }

        /// <summary>
        /// Adds the necressary Handler for the cut, copy and paste button.
        /// </summary>
        /// <param name="menu">The instance of the cut copy paste menu</param>
        private void SetupButtons(GameObject menu)
        {
            Transform content = menu.transform.Find("Content");
            ButtonManagerBasic cut = content.Find("Cut").GetComponent<ButtonManagerBasic>();
            ButtonManagerBasic copy = content.Find("Copy").GetComponent<ButtonManagerBasic>();

            cut.clickEvent.AddListener(()=>
            {
                state = CutCopy.Cut;
            });

            copy.clickEvent.AddListener(() =>
            {
                state = CutCopy.Copy;
            });
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.CutCopyPaste"/>.
        /// It allows to cut or copy drawable type objects and paste them on a specific position on a specific drawbale.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                switch (progressState)
                {
                    /// Block for selecting the drawable type object to copy/cut.
                    case ProgressState.SelectObject:
                        if (Input.GetMouseButtonUp(0) && selectedObj == null)
                        {
                            mouseWasReleased = true;
                        }
                        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && selectedObj == null &&
                            Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                            (oldSelectedId == "" || oldSelectedId != raycastHit.collider.gameObject.name || (oldSelectedId == raycastHit.collider.gameObject.name && mouseWasReleased)) &&
                            Tags.DrawableTypes.Contains(raycastHit.collider.gameObject.tag))
                        {
                            selectedObj = raycastHit.collider.gameObject;
                            oldDrawable = GameDrawableFinder.FindDrawable(selectedObj);
                            oldSelectedId = selectedObj.name;
                            oldValueHolder = new DrawableType().Get(selectedObj);

                            BlinkEffect effect = selectedObj.AddOrGetComponent<BlinkEffect>();
                            effect.SetAllowedActionStateType(GetActionStateType());

                            if (GameObject.Find("UI Canvas").GetComponent<ValueResetter>() == null)
                            {
                                GameObject.Find("UI Canvas").AddComponent<ValueResetter>().SetAllowedState(GetActionStateType());
                            }
                            cutCopyPasteMenu = PrefabInstantiator.InstantiatePrefab(cutCopyPasteMenuPrefab,
                                        GameObject.Find("UI Canvas").transform, false);
                            SetupButtons(cutCopyPasteMenu);
                            
                        }
                        if (Input.GetMouseButtonUp(0) && selectedObj != null)
                        {
                            progressState = ProgressState.CutCopyPaste;
                        }
                        break;

                    /// Block in which the object is duplicated at the desired location, and the original may be deleted if necessary.
                    case ProgressState.CutCopyPaste:
                        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && selectedObj != null && 
                            selectedObj.GetComponent<BlinkEffect>() != null && state != CutCopy.None &&
                            Raycasting.RaycastAnything(out RaycastHit hit) && 
                            (hit.collider.gameObject.CompareTag(Tags.Drawable) ||
                            GameDrawableFinder.hasDrawable(hit.collider.gameObject)))
                        {
                            Destroyer.Destroy(cutCopyPasteMenu);
                            selectedObj.GetComponent<BlinkEffect>().Deactivate();
                            Vector3 newPosition = hit.point;
                            newDrawable = hit.collider.gameObject.CompareTag(Tags.Drawable) ?
                                    hit.collider.gameObject : GameDrawableFinder.FindDrawable(hit.collider.gameObject);
                            switch(new DrawableType().Get(selectedObj)) 
                            {
                                case LineConf:
                                    LineConf lineConf = LineConf.GetLine(selectedObj);
                                    lineConf.id = "";
                                    newObject = GameDrawer.ReDrawLine(newDrawable, lineConf);
                                    newObject.transform.position = newPosition - newObject.transform.forward * ValueHolder.distanceToDrawable.z * lineConf.orderInLayer;
                                    new DrawOnNetAction(newDrawable.name, GameDrawableFinder.GetDrawableParentName(newDrawable), LineConf.GetLine(newObject)).Execute();
                                    break;
                                case TextConf:
                                    TextConf textConf = TextConf.GetText(selectedObj);
                                    textConf.id = "";
                                    newObject = GameTexter.ReWriteText(newDrawable, textConf);
                                    newObject.transform.position = newPosition - newObject.transform.forward * ValueHolder.distanceToDrawable.z * textConf.orderInLayer;
                                    new WriteTextNetAction(newDrawable.name, GameDrawableFinder.GetDrawableParentName(newDrawable), TextConf.GetText(newObject)).Execute();
                                    break;
                                case ImageConf:
                                    ImageConf imageConf = ImageConf.GetImageConf(selectedObj);
                                    imageConf.id = "";
                                    newObject = GameImage.RePlaceImage(newDrawable, imageConf);
                                    newObject.transform.position = newPosition - newObject.transform.forward * ValueHolder.distanceToDrawable.z * imageConf.orderInLayer;
                                    new AddImageNetAction(newDrawable.name, GameDrawableFinder.GetDrawableParentName(newDrawable), ImageConf.GetImageConf(newObject)).Execute();
                                    break;
                            }
                            newValueHolder = new DrawableType().Get(newObject);
                            if (state == CutCopy.Cut)
                            {
                                Destroyer.Destroy(selectedObj);
                                new EraseNetAction(oldDrawable.name, GameDrawableFinder.GetDrawableParentName(oldDrawable), selectedObj.name).Execute();
                            }
                        }
                        if (Input.GetMouseButtonUp(0) && state != CutCopy.None && newObject != null)
                        {
                            progressState = ProgressState.Finish;
                        }

                        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && state == CutCopy.None) {
                            Destroyer.Destroy(cutCopyPasteMenu);
                            selectedObj.GetComponent<BlinkEffect>().Deactivate();
                            selectedObj = null;
                            mouseWasReleased = false;
                            state = CutCopy.None;
                            progressState = ProgressState.SelectObject;
                        }
                        break;

                    /// Block to finish this action.
                    case ProgressState.Finish:
                        mouseWasReleased = false;
                        memento = new Memento(oldValueHolder, newValueHolder, oldDrawable, newDrawable, state);
                        currentState = ReversibleAction.Progress.Completed;
                        return true;

                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Reverts this action, i.e., restores the old object and destroyes the new one.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.oldValueHolder is LineConf lineConf)
            {
                GameDrawer.ReDrawLine(memento.oldDrawable, lineConf);
                new DrawOnNetAction(memento.oldDrawable.name, GameDrawableFinder.GetDrawableParentName(memento.oldDrawable), lineConf).Execute();
            }

            if (memento.oldValueHolder is TextConf textConf)
            {
                GameTexter.ReWriteText(memento.oldDrawable, textConf);
                new WriteTextNetAction(memento.oldDrawable.name, GameDrawableFinder.GetDrawableParentName(memento.oldDrawable), textConf).Execute();
            }

            if (memento.oldValueHolder is ImageConf imageConf)
            {
                GameImage.RePlaceImage(memento.oldDrawable, imageConf);
                new AddImageNetAction(memento.oldDrawable.name, GameDrawableFinder.GetDrawableParentName(memento.oldDrawable), imageConf).Execute();
            }

            GameObject newObject = GameDrawableFinder.FindChild(memento.newDrawable, memento.newValueHolder.id);
            new EraseNetAction(memento.newDrawable.name, GameDrawableFinder.GetDrawableParentName(memento.newDrawable), newObject.name);
            Destroyer.Destroy(newObject);
        }

        /// <summary>
        /// Repeats this action, i.e., create again the new object and deletes the old one if it was cutted.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.newValueHolder is LineConf lineConf)
            {
                GameDrawer.ReDrawLine(memento.newDrawable, lineConf);
                new DrawOnNetAction(memento.newDrawable.name, GameDrawableFinder.GetDrawableParentName(memento.newDrawable), lineConf).Execute();
            }

            if (memento.newValueHolder is TextConf textConf)
            {
                GameTexter.ReWriteText(memento.newDrawable, textConf);
                new WriteTextNetAction(memento.newDrawable.name, GameDrawableFinder.GetDrawableParentName(memento.newDrawable), textConf).Execute();
            }

            if (memento.newValueHolder is ImageConf imageConf)
            {
                GameImage.RePlaceImage(memento.newDrawable, imageConf);
                new AddImageNetAction(memento.newDrawable.name, GameDrawableFinder.GetDrawableParentName(memento.newDrawable), imageConf).Execute();
            }

            if (memento.state == CutCopy.Cut)
            {
                GameObject selected = GameDrawableFinder.FindChild(memento.oldDrawable, memento.oldValueHolder.id);
                new EraseNetAction(memento.oldDrawable.name, GameDrawableFinder.GetDrawableParentName(memento.oldDrawable), selected.name);
                Destroyer.Destroy(selected);
            }
        }


        /// <summary>
        /// A new instance of <see cref="CutCopyPasteAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="CutCopyPasteAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new CutCopyPasteAction();
        }

        /// <summary>
        /// A new instance of <see cref="CutCopyPasteAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="CutCopyPasteAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.CutCopyPaste"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.CutCopyPaste;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>The object id of the changed object.</returns>
        public override HashSet<string> GetChangedObjects()
        {
                return new HashSet<string>
                {
                    memento.oldValueHolder.id,
                    memento.newValueHolder.id
                };
        }
    }
}