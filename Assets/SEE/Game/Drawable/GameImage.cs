using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.UI.Notification;
using SEE.Utils;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class adds an image to a drawable.
    /// </summary>
    public static class GameImage
    {
        /// <summary>
        /// Converts a texture to a sprite.
        /// @author Jaimin, April 2022
        /// https://stackoverflow.com/questions/71898310/how-to-change-the-texture-type-of-a-png-image-to-sprite-through-a-script-in-uni
        /// </summary>
        /// <param name="texture">The texture that should be converted.</param>
        /// <returns>The converted sprite</returns>
        public static Sprite TextureToSprite(Texture2D texture) =>
            Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 50f, 0, SpriteMeshType.FullRect);

        /// <summary>
        /// Setsup the image game object with all the needed components.
        /// Either the file path is empty or the file data.
        /// </summary>
        /// <param name="surface">The drawable surface where the image should be displayed.</param>
        /// <param name="name">the name of the image game object</param>
        /// <param name="imageFilePath">the path where the image is located</param>
        /// <param name="data">the file data of the image.</param>
        /// <param name="position">the position where the image should be</param>
        /// <param name="order">the order in layer for the image</param>
        /// <param name="associatedPage">The assoiated surface page for this object.</param>
        /// <param name="image">the output image game object.</param>
        private static void Setup(GameObject surface, string name, string imageFilePath,
            byte[] data, Vector3 position, int order, int associatedPage,
            out GameObject image)
        {
            /// If the object has been created earlier, it already has a name,
            /// and this name is taken from the parameters <paramref name="name"/>.
            if (name.Length > Tags.Image.Length)
            {
                image = new(name);
            }
            else
            {
                /// Otherwise, a name for the image will be generated.
                /// For this, the <see cref="ValueHolder.ImagePrefix"/> is concatenated with
                /// the object ID along with a random string consisting of four characters.
                image = new("");

                name = ValueHolder.ImagePrefix + image.GetInstanceID() + RandomStrings.GetRandomString(4);
                /// Check if the name is already in use. If so, generate a new name.
                while (GameFinder.FindChild(surface, name) != null)
                {
                    name = ValueHolder.ImagePrefix + image.GetInstanceID() + RandomStrings.GetRandomString(4);
                }
                image.name = name;
            }

            /// Sets up the drawable holder <see cref="DrawableSetupManager"/>.
            DrawableSetupManager.Setup(surface, out GameObject _, out GameObject attachedObjects);

            /// Assign the image tag to the image object.
            image.tag = Tags.Image;
            /// Add the image object to the hierarchy below the attached objects - object of the drawable.
            image.transform.SetParent(attachedObjects.transform);

            /// Adds the needed canvas to the image object and sets the order to it.
            Canvas canvas = image.AddComponent<Canvas>();
            canvas.sortingOrder = order;

            /// Adds the image to the object.
            Image theImage = image.AddComponent<Image>();
            /// Sets the size initial to 1, 1. (Otherwise the image were to big for the game)
            theImage.rectTransform.sizeDelta = new Vector2(1, 1);

            /// Block for the case that the path is not empty and the file exists. Then load the image from the file.
            if (imageFilePath != "" && File.Exists(imageFilePath))
            {
                byte[] fileData = File.ReadAllBytes(imageFilePath);
                ImageValueHolder holder = image.AddComponent<ImageValueHolder>();
                holder.FileData = fileData;
                /// Saves the loaded file to the app data folder.
                CreateImageFile(image, fileData, Path.GetFileName(imageFilePath), out string _);

                Texture2D texture = new(2, 2)
                {
                    anisoLevel = 5
                };
                texture.LoadImage(fileData);
                theImage.sprite = TextureToSprite(texture);
            }
            else if (data != null)
            {
                /// Block for the case that the byte data is not empty. Load the image based on the byte data.
                image.AddComponent<ImageValueHolder>().FileData = data;
                Texture2D texture = new(2, 2)
                {
                    anisoLevel = 5
                };
                texture.LoadImage(data);
                theImage.sprite = TextureToSprite(texture);
            }
            /// Add the box collider to the image object.
            BoxCollider collider = image.AddComponent<BoxCollider>();

            /// Adjust the size of the collider to match the size of the canvas.
            collider.size = new Vector3(1, 1, 0.01f);

            /// Set the position of the line and ensure the correct order in the layer.
            /// Additionally, adopt the rotation of the attached object.
            image.transform.rotation = attachedObjects.transform.rotation;
            image.transform.position = position - image.transform.forward * ValueHolder.DistanceToDrawable.z * order;
            /// Adds the order in layer value holder component to the line object and sets the order.
            image.AddComponent<OrderInLayerValueHolder>().OrderInLayer = order;

            /// Adds a <see cref="AssociatedPageHolder"/> component.
            /// And sets the associated page to the used page.
            image.AddComponent<AssociatedPageHolder>().AssociatedPage = associatedPage;
            if (associatedPage != surface.GetComponent<DrawableHolder>().CurrentPage)
            {
                image.SetActive(false);
            }
        }

        /// <summary>
        /// Adds an image to a drawable.
        /// The current order in layer will be increased.
        /// </summary>
        /// <param name="surface">The drawable surface where the image should be displayed.</param>
        /// <param name="imageFilePath">The file path where the image is located.</param>
        /// <param name="position">The position where the image should be placed.</param>
        /// <param name="order">The order in layer for the image</param>
        /// <returns>The created image game object.</returns>
        public static GameObject PlaceImage(GameObject surface, string imageFilePath, Vector3 position, int order)
        {
            Setup(surface, "", imageFilePath, null, position, order, surface.GetComponent<DrawableHolder>().CurrentPage, out GameObject image);
            surface.GetComponent<DrawableHolder>().Inc();
            ValueHolder.MaxOrderInLayer++;
            return image;
        }

        /// <summary>
        /// Re-adds an image to a drawable.
        /// </summary>
        /// <param name="surface">The drawable surface where the image should be displayed.</param>
        /// <param name="name">The name of the image game object</param>
        /// <param name="fileData">The file data that contains the image</param>
        /// <param name="position">The position where the image should be placed.</param>
        /// <param name="scale">The scale of the image</param>
        /// <param name="eulerAngles">The rotation in euler angles of the image</param>
        /// <param name="order">The order in layer for the image</param>
        /// <param name="imageColor">The image color</param>
        /// <param name="fileName">The file name of the image</param>
        /// <returns>The created image game object.</returns>
        public static GameObject RePlaceImage(GameObject surface, string name, byte[] fileData, Vector3 position,
            Vector3 scale, Vector3 eulerAngles, int order, Color imageColor, string fileName, int associatedPage)
        {
            /// Adjusts the current order in the layer if the
            /// order in layer for the line is greater than or equal to it.
            DrawableHolder holder = surface.GetComponent<DrawableHolder>();
            if (order >= holder.OrderInLayer && associatedPage == holder.CurrentPage)
            {
                holder.OrderInLayer = order + 1;
            }
            if (associatedPage >= holder.MaxPageSize)
            {
                holder.MaxPageSize = associatedPage + 1;
            }
            if (order >= ValueHolder.MaxOrderInLayer)
            {
                ValueHolder.MaxOrderInLayer = order + 1;
            }
            GameObject imageObj;

            /// Block to update an existing image with the given name.
            if (GameFinder.FindChild(surface, name) != null)
            {
                imageObj = GameFinder.FindChild(surface, name);
                imageObj.GetComponent<Canvas>().sortingOrder = order;
            }
            else
            {
                /// Block to create a new image.
                Setup(surface, name, "", fileData, position, order, associatedPage, out GameObject image);
                imageObj = image;
            }
            /// Saves the loaded image to the app data folder.
            CreateImageFile(imageObj, fileData, fileName, out string _);
            /// Sets the old values:
            imageObj.transform.localScale = scale;
            imageObj.transform.localEulerAngles = eulerAngles;
            imageObj.transform.localPosition = position;
            imageObj.GetComponent<Image>().color = imageColor;
            imageObj.GetComponent<OrderInLayerValueHolder>().OrderInLayer = order;
            imageObj.GetComponent<AssociatedPageHolder>().AssociatedPage = associatedPage;

            return imageObj;
        }

        /// <summary>
        /// Re-adds the given image of the <paramref name="conf"/> to the <paramref name="surface"/>.
        /// It calls the RePlaceImage - Method with all the attributes.
        /// </summary>
        /// <param name="surface">The drawable surface where the image should be displayed.</param>
        /// <param name="conf">The image configuration to restore the old image</param>
        /// <returns>The created image game object.</returns>
        public static GameObject RePlaceImage(GameObject surface, ImageConf conf)
        {
            string fileName = Path.GetFileName(conf.Path);
            return RePlaceImage(
                surface,
                conf.Id,
                conf.FileData,
                conf.Position,
                conf.Scale,
                conf.EulerAngles,
                conf.OrderInLayer,
                conf.ImageColor,
                fileName,
                conf.AssociatedPage
                );
        }

        /// <summary>
        /// Creates a local backup of the image in the AppData directory.
        /// </summary>
        /// <param name="imageObj">The image game object, can be null when the method is used to save a downloaded file.</param>
        /// <param name="fileData">The fila data of the image.</param>
        /// <param name="fileName">The file name of the image.</param>
        public static void CreateImageFile(GameObject imageObj, byte[] fileData, string fileName,
            out string filePath)
        {
            DrawableConfigManager.EnsureDrawableDirectoryExists(ValueHolder.ImagePath);
            string path = ValueHolder.ImagePath + fileName;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string fileExtension = Path.GetExtension(fileName);
            filePath = path;

            /// If no file with the exact name exists, create it.
            if (!File.Exists(path))
            {
                if (fileData != null && fileData.Length > 0)
                {
                    File.WriteAllBytes(path, fileData);
                    if (imageObj != null)
                    {
                        imageObj.GetComponent<ImageValueHolder>().Path = path;
                    }
                }
                else
                {
                    /// For the case if the file in the path does not exists and the file data is empty.
                    ShowNotification.Warn("Cannot be restored.", "The image cannot be restored.");
                    Destroyer.Destroy(imageObj);
                }
            }
            else
            {
                /// If a file with the same name exists, it checks whether they are the same images.
                /// This is done by comparing the byte count.
                /// To do this, it is necessary to create a temporary file.
                /// After the examinations, this file will be deleted.
                string tmpPath = Path.GetTempFileName();
                File.WriteAllBytes(tmpPath, fileData);
                FileInfo fileInfo = new(tmpPath);
                FileInfo existsInfo = new (path);

                if (fileInfo.Length != existsInfo.Length)
                {
                    int numberOfFiles = Directory.GetFiles(ValueHolder.ImagePath).ToList().Count;
                    /// If the byte counts are not equal, indicating different images,
                    /// it checks whether another file with a similar name already exists and
                    /// whether there is a file among them that matches the image to be saved.
                    /// If this is not the case, a new file with the highest index + 1 is created.
                    for (int i = 1; i <= numberOfFiles; i++)
                    {
                        string newPath = ValueHolder.ImagePath + fileNameWithoutExtension
                            + "(" + i + ")" + fileExtension;
                        if (!File.Exists(newPath))
                        {
                            File.Delete(tmpPath);
                            CreateImageFile(imageObj, fileData, Path.GetFileName(newPath), out filePath);
                            break;
                        }
                        else
                        {
                            FileInfo info = new(newPath);
                            if (fileInfo.Length == info.Length)
                            {
                                Directory.Delete(tmpPath, true);
                                if (imageObj != null)
                                {
                                    imageObj.GetComponent<ImageValueHolder>().Path = newPath;
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    File.Delete(tmpPath);
                    if (imageObj != null)
                    {
                        imageObj.GetComponent<ImageValueHolder>().Path = path;
                    }
                }
            }
        }
    }
}
