using SEE.Game;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using SEE.Net.Actions.Drawable;
using Assets.SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using TextConf = SEE.Game.Drawable.Configurations.TextConf;
using SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using HighlightPlus;
using SEE.Game.UI.Notification;
using UnityEngine.Events;
using Assets.SEE.Game.UI.Drawable;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Adds the <see cref="DrawableType"/> to the scene from one or more drawable configs saved in a file on the disk.
    /// </summary>
    public class LoadAction : AbstractPlayerAction
    {
        /// <summary>
        /// Represents how the file was loaded.
        /// LoadState.Regular is load the drawable(s) from file one-to-one into the same drawable.
        /// LoadState.Specific load the drawable(s) from the given file to one specific drawable.
        /// </summary>
        public enum LoadState
        {
            Regular,
            Specific
        }
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="LoadAction"/>
        /// </summary>
        private class Memento
        {
            /// <summary>
            /// The load state of the action
            /// </summary>
            public readonly LoadState state;
            /// <summary>
            /// The specific chosen drawable (needed for LoadState.Specific)
            /// </summary>
            public GameObject specificDrawable;
            /// <summary>
            /// The drawable configurations.
            /// </summary>
            public DrawablesConfigs configs;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="state">The kind how the file was loaded.</param>
            public Memento(LoadState state)
            {
                this.state = state;
                this.specificDrawable = null;
                this.configs = null;
            }
        }

        /// <summary>
        /// Ensures that per click is only saved once.
        /// </summary>
        private bool clicked = false;

        /// <summary>
        /// The selected drawable for specific loading.
        /// </summary>
        private GameObject selectedDrawable;

        /// <summary>
        /// The instance for the drawable file browser
        /// </summary>
        private DrawableFileBrowser browser;

        /// <summary>
        /// Creates the load menu and adds the neccressary Handler for the buttons.
        /// </summary>
        public override void Awake()
        {
            UnityAction loadButtonCall = () =>
            {
                if (browser == null || (browser != null && !browser.IsOpen()))
                {
                    browser = GameObject.Find("UI Canvas").AddOrGetComponent<DrawableFileBrowser>();
                    browser.LoadDrawableConfiguration(LoadState.Regular);
                    memento = new Memento(LoadState.Regular);
                }
            };
            UnityAction loadSpecificButtonCall = () =>
            {
                if (browser == null || (browser != null && !browser.IsOpen()))
                {
                    if (selectedDrawable != null)
                    {
                        browser = GameObject.Find("UI Canvas").AddOrGetComponent<DrawableFileBrowser>();
                        browser.LoadDrawableConfiguration(LoadState.Specific);
                        memento = new Memento(LoadState.Specific);
                    } else
                    {
                        ShowNotification.Warn("No drawable selected.", "Select a drawable to load specific.");
                    }
                }
            };

            LoadMenu.Enable(loadButtonCall, loadSpecificButtonCall);
        }

        /// <summary>
        /// Stops the <see cref="LoadAction"/>.
        /// Destroys the load menu and if there are still an activ
        /// highlight effect
        /// </summary>
        public override void Stop()
        {
            LoadMenu.Disable();

            if (selectedDrawable != null && selectedDrawable.GetComponent<HighlightEffect>() != null)
            {
                Destroyer.Destroy(selectedDrawable.GetComponent<HighlightEffect>());
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Load"/>.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                /// This block marks the selected drawable and adds it to a list. If it has already been selected, it is removed from the list, and the marking is cleared.
                /// For execution, no open file browser should exist.
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                    && !clicked &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit hit) &&
                    (GameFinder.hasDrawable(hit.collider.gameObject) || hit.collider.gameObject.CompareTag(Tags.Drawable))
                    && (browser == null || (browser != null && !browser.IsOpen())))
                {
                    clicked = true;
                    GameObject drawable = hit.collider.gameObject.CompareTag(Tags.Drawable) ?
                        hit.collider.gameObject : GameFinder.FindDrawable(hit.collider.gameObject);

                    if (drawable.GetComponent<HighlightEffect>() == null)
                    {
                        if (selectedDrawable != null && selectedDrawable.GetComponent<HighlightEffect>() != null)
                        {
                            Destroyer.Destroy(selectedDrawable.GetComponent<HighlightEffect>());
                        }
                        selectedDrawable = drawable;
                        HighlightEffect effect = selectedDrawable.AddComponent<HighlightEffect>();
                        effect.highlighted = true;
                        effect.previewInEditor = false;
                        effect.outline = 0;
                        effect.glowQuality = HighlightPlus.QualityLevel.Highest;
                        effect.glow = 1.0f;
                        effect.glowHQColor = Color.yellow;
                        effect.overlay = 1.0f;
                        effect.overlayColor = Color.magenta;
                    }
                    else
                    {
                        Destroyer.Destroy(drawable.GetComponent<HighlightEffect>());
                        selectedDrawable = null;
                    }
                }

                /// Needed for select more drawables to save.
                if (Input.GetMouseButtonUp(0))
                {
                    clicked = false;
                }
                /// This block will be executed when a file was successfully chosen.
                if (browser != null && browser.TryGetFilePath(out string filePath) && memento != null)
                {
                    switch (memento.state)
                    {
                        /// This block loads one drawable onto the specific chosen drawable.
                        case LoadState.Specific:
                            memento.specificDrawable = selectedDrawable;
                            DrawablesConfigs configsSpecific = DrawableConfigManager.LoadDrawables(new FilePath(filePath));
                            foreach (DrawableConfig drawableConfig in configsSpecific.Drawables)
                            {
                                Restore(memento.specificDrawable, drawableConfig);
                            }
                            memento.configs = configsSpecific;
                            currentState = ReversibleAction.Progress.Completed;
                            result = true;
                            break;
                        /// This block loads one or more drawables onto the drawables of the configuration.
                        case LoadState.Regular:
                            DrawablesConfigs configs = DrawableConfigManager.LoadDrawables(new FilePath(filePath));
                            foreach (DrawableConfig drawableConfig in configs.Drawables)
                            {
                                GameObject drawableOfFile = GameFinder.Find(drawableConfig.DrawableName, drawableConfig.DrawableParentName);
                                /// If the drawable does not exist it will be spawned as a sticky note.
                                if (drawableOfFile == null)
                                {
                                    GameObject stickyNote = GameStickyNoteManager.Spawn(drawableConfig);
                                    drawableOfFile = GameFinder.FindDrawable(stickyNote);
                                    new StickyNoteSpawnNetAction(drawableConfig).Execute();
                                }
                                Restore(drawableOfFile, drawableConfig);
                            }
                            memento.configs = configs;
                            currentState = ReversibleAction.Progress.Completed;
                            result = true;
                            break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Restores all the objects of the configuration.
        /// </summary>
        /// <param name="drawable"></param>
        /// <param name="config"></param>
        private void Restore(GameObject drawable, DrawableConfig config)
        {
            RestoreLines(drawable, config.LineConfigs);
            RestoreTexts(drawable, config.TextConfigs);
            RestoreImages(drawable, config.ImageConfigs);
        }

        /// <summary>
        /// This method restores the lines of the drawable configuration to the given drawable.
        /// </summary>
        /// <param name="drawable">The drawable on which the lines should be restored.</param>
        /// <param name="lines">The lines to be restored.</param>
        private void RestoreLines(GameObject drawable, List<LineConf> lines)
        {
            GameObject attachedObject = GameFinder.GetAttachedObjectsObject(drawable);
            foreach (LineConf line in lines)
            {
                if (attachedObject != null)
                {
                    CheckAndChangeID(line, attachedObject, ValueHolder.LinePrefix);
                }
                GameDrawer.ReDrawLine(drawable, line);
                new DrawOnNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), line).Execute();
            }
        }

        /// <summary>
        /// This method restores the texts of the drawable configuration to the given drawable.
        /// </summary>
        /// <param name="drawable">The drawable on which the texts should be restored.</param>
        /// <param name="texts">The texts to be restored.</param>
        private void RestoreTexts(GameObject drawable, List<TextConf> texts)
        {
            GameObject attachedObject = GameFinder.GetAttachedObjectsObject(drawable);
            foreach (TextConf text in texts)
            {
                if (attachedObject != null)
                {
                    CheckAndChangeID(text, attachedObject, ValueHolder.TextPrefix);
                }
                GameTexter.ReWriteText(drawable, text);
                new WriteTextNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), text).Execute();
            }
        }

        /// <summary>
        /// This method restores the images of the drawable configuration to the given drawable.
        /// </summary>
        /// <param name="drawable">The drawable on which the texts should be restored.</param>
        /// <param name="images">The images to be restored.</param>
        private void RestoreImages(GameObject drawable, List<ImageConf> images)
        {
            GameObject attachedObject = GameFinder.GetAttachedObjectsObject(drawable);
            foreach (ImageConf image in images)
            {
                if (attachedObject != null)
                {
                    CheckAndChangeID(image, attachedObject, ValueHolder.ImagePrefix);
                }
                GameImage.RePlaceImage(drawable, image);
                new AddImageNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), image).Execute();
            }
        }

        /// <summary>
        /// When the id of the given config already exist on the drawable, the id will be changed.
        /// </summary>
        /// <param name="conf">The configuration to restore.</param>
        /// <param name="attachedObjects">The objects that are attached on a drawable</param>
        /// <param name="prefix">The prefix for the drawable type object.</param>
        private void CheckAndChangeID (DrawableType conf, GameObject attachedObjects, string prefix)
        {
            if (GameFinder.FindChild(attachedObjects, conf.id) != null)
            {
                string newName = prefix + "-" + DrawableHolder.GetRandomString(8);
                while (GameFinder.FindChild(attachedObjects, newName) != null)
                {
                    newName = prefix + "-" + DrawableHolder.GetRandomString(8);
                }
                conf.id = newName;
            }
        }


        /// <summary>
        /// Destroyes the objects, which was loaded from the configuration.
        /// </summary>
        /// <param name="attachedObjects"></param>
        /// <param name="config"></param>
        private void DestroyLoadedObjects(GameObject attachedObjects, DrawableConfig config)
        {
            if (attachedObjects != null)
            {
                GameObject drawable = GameFinder.FindDrawable(attachedObjects);
                string drawableParentName = GameFinder.GetDrawableParentName(drawable);
                List<DrawableType> allConfigs = new(config.LineConfigs);
                allConfigs.AddRange(config.TextConfigs);
                allConfigs.AddRange(config.ImageConfigs);
                // TODO ADD new drawable config types

                foreach (DrawableType type in allConfigs)
                {
                    GameObject typeObj = GameFinder.FindChild(attachedObjects, type.id);
                    new EraseNetAction(drawable.name, drawableParentName, typeObj.name).Execute();
                    Destroyer.Destroy(typeObj);
                }
            }
        }

        /// <summary>
        /// Reverts this instance of the action, i.e., deletes the attached object that was loaded from the file.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            switch (memento.state)
            {
                case LoadState.Specific:
                    GameObject attachedObjs = GameFinder.GetAttachedObjectsObject(memento.specificDrawable);
                    foreach (DrawableConfig config in memento.configs.Drawables)
                    {
                        DestroyLoadedObjects(attachedObjs, config);
                    }
                    break;
                case LoadState.Regular:
                    foreach (DrawableConfig config in memento.configs.Drawables)
                    {
                        GameObject drawable = GameFinder.Find(config.DrawableName, config.DrawableParentName);
                        GameObject attachedObj = GameFinder.GetAttachedObjectsObject(drawable);
                        DestroyLoadedObjects(attachedObj, config);
                    }
                    break;
            }
        }

        /// <summary>
        /// Repeats this action, i.e., loades the configuration again.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            switch (memento.state)
            {
                case LoadState.Specific:
                    foreach (DrawableConfig config in memento.configs.Drawables)
                    {
                        Restore(memento.specificDrawable, config);
                    }
                    break;
                case LoadState.Regular:
                    foreach (DrawableConfig config in memento.configs.Drawables)
                    {
                        GameObject d = GameFinder.Find(config.DrawableName, config.DrawableParentName);
                        Restore(d, config);
                    }
                    break;
            }
        }

        /// <summary>
        /// A new instance of <see cref="LoadAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LoadAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new LoadAction();
        }

        /// <summary>
        /// A new instance of <see cref="LoadAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="LoadAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Load"/></returns>
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
