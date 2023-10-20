using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using SEE.Controls.Actions;
using SEE.Controls.Actions.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.SEE.Game.UI.Drawable;
using SEE.Game.UI.Notification;
using SEE.Game.UI.PropertyDialog.Drawable;
using SEE.Game;
using SEE.Game.Drawable.Configurations;
using Assets.SEE.Game.Drawable.ActionHelpers;
using System;
using System.IO;

namespace SEE.Controls.Actions.Drawable
{
    public class AddImageAction : AbstractPlayerAction
    {
        /// <summary>
        /// The game object that holds the image component.
        /// </summary>
        private GameObject imageObj;

        /// <summary>
        /// The drawable on that the image should be displayed.
        /// </summary>
        private GameObject drawable;

        /// <summary>
        /// The position on the drawable where the image should be displayed.
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// The file path of the image.
        /// </summary>
        public static string filePath = "";

        /// <summary>
        /// The file name of the downloaded image.
        /// </summary>
        private static string fileName = "";

        /// <summary>
        /// The instance for the drawable file browser
        /// </summary>
        private DrawableFileBrowser browser;

        /// <summary>
        /// The dialog for insert a web adress.
        /// </summary>
        private WebImageDialog webImageDialog;

        /// <summary>
        /// Attribut for opened WebImageDialog.
        /// </summary>
        private bool isDialogOpen = false;

        /// <summary>
        /// The component for downloading the image.
        /// </summary>
        private DownloadImage download;

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="AddImageAction"/>
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The drawable on that the text should be displayed
            /// </summary>
            public GameObject drawable;

            /// <summary>
            /// The written text.
            /// </summary>
            public ImageConf image;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="drawable">The drawable on that the text should be displayed.</param>
            /// <param name="text">The written text</param>
            public Memento(GameObject drawable, ImageConf image)
            {
                this.drawable = drawable;
                this.image = image;
            }
        }

        public override void Stop()
        {
            base.Stop();
            filePath = "";
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.AddImage"/>.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// Block for 
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                     Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) &&
                    (GameDrawableFinder.hasDrawable(raycastHit.collider.gameObject) || raycastHit.collider.gameObject.CompareTag(Tags.Drawable))
                    && (browser == null || (browser != null && !browser.IsOpen())) && (webImageDialog == null || (webImageDialog != null && !isDialogOpen)))
                {
                    drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                        raycastHit.collider.gameObject : GameDrawableFinder.FindDrawable(raycastHit.collider.gameObject);
                    position = raycastHit.point;
                    if (!Input.GetKey(KeyCode.LeftControl))
                    {
                        browser = GameObject.Find("UI Canvas").AddComponent<DrawableFileBrowser>();
                        browser.LoadImage();
                    } else
                    {
                        webImageDialog = new WebImageDialog();
                        isDialogOpen = true;
                        webImageDialog.Open();
                    }
                    currentState = ReversibleAction.Progress.InProgress;
                }
                if (webImageDialog != null && webImageDialog.WasCanceled())
                {
                    isDialogOpen = false;
                }

                if (webImageDialog != null && webImageDialog.GetUserInput(out string http, out string fileNameOut))
                {
                    isDialogOpen = false;
                    download = drawable.AddComponent<DownloadImage>();
                    download.Download(http);
                    fileName = fileNameOut;
                }

                if (download != null && download.GetTexture() != null)
                {
                    Texture2D tex = download.GetTexture();
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = DrawableHolder.GetRandomStringForFile(10) + Filenames.PNGExtension;
                    }
                    if (string.IsNullOrEmpty(Path.GetExtension(fileName)) || 
                        (Path.GetExtension(fileName) != Filenames.PNGExtension && Path.GetExtension(fileName) != Filenames.JPGExtension)) {
                        fileName += Filenames.PNGExtension;
                    }
                    GameImage.CreateImageFile(null, tex.EncodeToPNG(), fileName, out string path);
                    ShowNotification.Info("Download successful", "Image has been saved to: " + path);
                    Destroyer.Destroy(download);
                    filePath = path;
                }

                if (filePath != "" && (browser == null || (browser != null && !browser.IsOpen())))
                {
                    imageObj = GameImage.PlaceImage(drawable, filePath, position, ValueHolder.currentOrderInLayer);
                    new AddImageNetAction(drawable.name, GameDrawableFinder.GetDrawableParentName(drawable), ImageConf.GetImageConf(imageObj)).Execute();
                    memento = new Memento(drawable, ImageConf.GetImageConf(imageObj));
                    currentState = ReversibleAction.Progress.Completed;
                    return true;
                }                
                return false;
            }
            return false;
        }

        /// <summary>
        /// Reverts this action, i.e., deletes the image that was add to the drawable.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            GameObject obj = GameDrawableFinder.FindChild(memento.drawable, memento.image.id);
            new EraseNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.image.id).Execute();
            Destroyer.Destroy(obj);
        }

        /// <summary>
        /// Repeats this action, i.e., adds the image again to the drawable.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameImage.RePlaceImage(memento.drawable, memento.image);
            string drawableParent = GameDrawableFinder.GetDrawableParentName(memento.drawable);
            new AddImageNetAction(memento.drawable.name, drawableParent, memento.image).Execute();
        }

        /// <summary>
        /// A new instance of <see cref="AddImageAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="AddImageAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new AddImageAction();
        }

        /// <summary>
        /// A new instance of <see cref="AddImageAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="AddImageAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.AddImage"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.AddImage;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>the id of the created drawable text</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.image.id };
        }
    }
}