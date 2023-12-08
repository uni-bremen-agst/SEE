using Assets.SEE.Game.Drawable;
using Assets.SEE.Game.Drawable.ActionHelpers;
using Assets.SEE.Game.UI.Drawable;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.UI.Notification;
using SEE.Game.UI.PropertyDialog.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides an action to add an image to a drawable.
    /// </summary>
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
        private string filePath = "";

        /// <summary>
        /// The file name of the downloaded image.
        /// </summary>
        private string fileName = "";

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
        /// This struct can store all the information needed to 
        /// revert or repeat a <see cref="AddImageAction"/>
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The drawable on that the text should be displayed
            /// </summary>
            public DrawableConfig drawable;

            /// <summary>
            /// The written text.
            /// </summary>
            public ImageConf image;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="drawable">The drawable on that the text should be displayed.</param>
            /// <param name="image">The image configuration</param>
            public Memento(GameObject drawable, ImageConf image)
            {
                this.drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                this.image = image;
            }
        }

        /// <summary>
        /// Destroys the image source menu if it's still active.
        /// </summary>
        public override void Stop()
        {
            ImageSourceMenu.Disable();
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.AddImage"/>.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// The block for the selection of the position and a query from which source the image should be loaded.
                SelectPosition();

                /// When a source was chosen this block opens the file browser (for local) or the web dialog (for web).
                SelectSource();

                /// The following blocks are for the web source case.
                WebSource();

                /// The following block is for the local source case.
                /// When the player chose a file path it will be loaded into the attribut.
                LocalSource();

                /// When a file path was chosen, it loads the image on the chosen position.
                return Finish();
            }
            return false;
        }

        /// <summary>
        /// Selects a position for the image and opens the menu for querying the image source.
        /// </summary>
        private void SelectPosition()
        {
            if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) 
                && Raycasting.RaycastAnything(out RaycastHit raycastHit) 
                && (GameFinder.hasDrawable(raycastHit.collider.gameObject) 
                    || raycastHit.collider.gameObject.CompareTag(Tags.Drawable))
                && !ImageSourceMenu.IsOpen()
                && (browser == null 
                    || (browser != null && !browser.IsOpen())) &&
                        (webImageDialog == null || (webImageDialog != null && !isDialogOpen)))
            {
                drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                    raycastHit.collider.gameObject : GameFinder.GetDrawable(raycastHit.collider.gameObject);
                position = raycastHit.point;
                ImageSourceMenu.Enable();
            }
        }

        /// <summary>
        /// When the user has chosen a source, the corresponding menu for loading the image is opened.
        /// For local source a file browser will be opened.
        /// For web source a web image dialog will be opened.
        /// </summary>
        private void SelectSource()
        {
            if (ImageSourceMenu.TryGetSource(out ImageSourceMenu.Source source))
            {
                switch (source)
                {
                    case ImageSourceMenu.Source.Local:
                        browser = GameObject.Find("UI Canvas").AddComponent<DrawableFileBrowser>();
                        browser.LoadImage();
                        break;
                    case ImageSourceMenu.Source.Web:
                        webImageDialog = new WebImageDialog();
                        isDialogOpen = true;
                        webImageDialog.Open();
                        break;
                }
                currentState = ReversibleAction.Progress.InProgress;
            }
        }

        /// <summary>
        /// Provides the process for displaying an image from the internet. 
        /// The image is downloaded from the URL specified in the WebImageDialog. 
        /// If no desired filename has been given, a random string is provided along with the PNG extension. 
        /// If the desired filename is already associated with a different (but not the same) image, numbering is added.
        /// </summary>
        private void WebSource()
        {
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
                    (Path.GetExtension(fileName) != Filenames.PNGExtension 
                        && Path.GetExtension(fileName) != Filenames.JPGExtension))
                {
                    fileName += Filenames.PNGExtension;
                }
                GameImage.CreateImageFile(null, tex.EncodeToPNG(), fileName, out string path);
                ShowNotification.Info("Download successful", "Image has been saved to: " + path);
                Destroyer.Destroy(download);
                filePath = path;
            }
        }

        /// <summary>
        /// Provides the process for displaying an image from the local source.
        /// </summary>
        private void LocalSource()
        {
            if (browser != null && browser.TryGetFilePath(out string chosenPath))
            {
                filePath = chosenPath;
            }
        }

        /// <summary>
        /// If a valid file path to an image (from Web/Local source) has been provided, 
        /// the image will be added to the desired position on the drawable. 
        /// Subsequently, a memento is created, and the action process is completed.
        /// </summary>
        /// <returns>true, if the action is completed. Otherwise false.</returns>
        private bool Finish()
        {
            if (filePath != "")
            {
                imageObj = GameImage.PlaceImage(drawable, filePath, position, 
                    ValueHolder.currentOrderInLayer);
                new AddImageNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), 
                    ImageConf.GetImageConf(imageObj)).Execute();
                memento = new Memento(drawable, ImageConf.GetImageConf(imageObj));
                currentState = ReversibleAction.Progress.Completed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reverts this action, i.e., deletes the image that was add to the drawable.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            GameObject obj = GameFinder.FindChild(memento.drawable.GetDrawable(), 
                memento.image.id);
            new EraseNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                memento.image.id).Execute();
            Destroyer.Destroy(obj);
        }

        /// <summary>
        /// Repeats this action, i.e., adds the image again to the drawable.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameImage.RePlaceImage(memento.drawable.GetDrawable(), memento.image);
            new AddImageNetAction(memento.drawable.ID, memento.drawable.ParentID, 
                memento.image).Execute();
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