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
    /// This class add's an image to a drawable.
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
        /// Setup the image game object with all the needed components.
        /// Either the file path is empty or the file data.
        /// </summary>
        /// <param name="drawable">The drawable where the image should be displayed.</param>
        /// <param name="name">the name of the image game object</param>
        /// <param name="imageFilePath">the path where the image is located</param>
        /// <param name="data">the file data of the image.</param>
        /// <param name="position">The position where the image should be</param>
        /// <param name="order">The order in layer for the image</param>
        /// <param name="image">The output image game object.</param>
        private static void Setup(GameObject drawable, string name, string imageFilePath, 
            byte[] data, Vector3 position, int order,
            out GameObject image)
        {
            /// If the object has been created earlier, it already has a name, 
            /// and this name is taken from the parameters <paramref name="name"/>.
            if (name.Length > 4)
            {
                image = new(name);
            }
            else
            {
                /// Otherwise, a name for the image will be generated.
                /// For this, the <see cref="ValueHolder.ImagePrefix"/> is concatenated with 
                /// the object ID along with a random string consisting of four characters.
                image = new("");
                name = ValueHolder.ImagePrefix + image.GetInstanceID() + DrawableHolder.GetRandomString(4);
                /// Check if the name is already in use. If so, generate a new name.
                while (GameFinder.FindChild(drawable, name) != null)
                {
                    name = ValueHolder.ImagePrefix + image.GetInstanceID() + DrawableHolder.GetRandomString(4);
                }
                image.name = name;
            }

            /// Setups the drawable holder <see cref="DrawableHolder"/>.
            DrawableHolder.Setup(drawable, out GameObject highestParent, out GameObject attachedObjects);

            /// Assign the image tag to the image object.
            image.tag = Tags.Image;
            /// Add the image object to the hierarchy below the attached objects - object of the drawable.
            image.transform.SetParent(attachedObjects.transform);

            /// Adds the needed canvas to the image object and sets the order to it.
            Canvas canvas = image.AddComponent<Canvas>();
            canvas.sortingOrder = order;

            /// Adds the image to the object.
            Image i = image.AddComponent<Image>();
            /// Sets the size initial to 1, 1. (Otherwise the image were to big for the game)
            i.rectTransform.sizeDelta = new Vector2(1, 1);

            /// Block for the case that the path is not empty and the file exists. Then load the image from the file.
            if (imageFilePath != "" && File.Exists(imageFilePath))
            {
                byte[] fileData = File.ReadAllBytes(imageFilePath);
                ImageValueHolder holder = image.AddComponent<ImageValueHolder>();
                holder.SetFileData(fileData);
                /// Saves the loaded file to the app data folder.
                CreateImageFile(image, fileData, Path.GetFileName(imageFilePath), out string filePath);

                Texture2D texture = new(2, 2)
                {
                    anisoLevel = 5
                };
                texture.LoadImage(fileData);
                i.sprite = TextureToSprite(texture);
            }
            else if (data != null)
            {
                /// Block for the case that the byte data is not empty. Load the image based on the byte data.
                image.AddComponent<ImageValueHolder>().SetFileData(data);
                Texture2D texture = new(2, 2)
                {
                    anisoLevel = 5
                };
                texture.LoadImage(data);
                i.sprite = TextureToSprite(texture);
            }
            /// Add the box collider to the image object.
            BoxCollider collider = image.AddComponent<BoxCollider>();

            /// Adjust the size of the collider to match the size of the canvas.
            collider.size = new Vector3(1, 1, 0.01f);

            /// Set the position of the line and ensure the correct order in the layer. 
            /// Additionally, adopt the rotation of the attached object.
            image.transform.rotation = attachedObjects.transform.rotation;
            image.transform.position = position - image.transform.forward * ValueHolder.distanceToDrawable.z * order;

            /// Adds the order in layer value holder component to the line object and sets the order.
            image.AddComponent<OrderInLayerValueHolder>().SetOrderInLayer(order);
        }

        /// <summary>
        /// Adds an image to a drawable.
        /// The current order in layer will be increased.
        /// </summary>
        /// <param name="drawable">The drawable where the image should be displayed.</param>
        /// <param name="imageFilePath">The file path where the image is located.</param>
        /// <param name="position">The position where the image should be placed.</param>
        /// <param name="order">The order in layer for the image</param>
        /// <returns>The created image game object.</returns>
        public static GameObject PlaceImage(GameObject drawable, string imageFilePath, Vector3 position, int order)
        {
            Setup(drawable, "", imageFilePath, null, position, order, out GameObject image);
            ValueHolder.currentOrderInLayer++;
            return image;
        }

        /// <summary>
        /// Re-adds an image to a drawable.
        /// </summary>
        /// <param name="drawable">The drawable where the image should be displayed.</param>
        /// <param name="name">The name of the image game object</param>
        /// <param name="fileData">The file data that contains the image</param>
        /// <param name="position">The position where the image should be placed.</param>
        /// <param name="scale">The scale of the image</param>
        /// <param name="eulerAngles">The rotation in euler angles of the image</param>
        /// <param name="order">The order in layer for the image</param>
        /// <param name="imageColor">The image color</param>
        /// <param name="fileName">The file name of the image</param>
        /// <returns>The created image game object.</returns>
        public static GameObject RePlaceImage(GameObject drawable, string name, byte[] fileData, Vector3 position, 
            Vector3 scale, Vector3 eulerAngles, int order, Color imageColor, string fileName)
        {
            /// Adjusts the current order in the layer if the 
            /// order in layer for the line is greater than or equal to it.
            if (order >= ValueHolder.currentOrderInLayer)
            {
                ValueHolder.currentOrderInLayer = order + 1;
            }
            GameObject imageObj;

            /// Block for update an existing image with the given name.
            if (GameFinder.FindChild(drawable, name) != null)
            {
                imageObj = GameFinder.FindChild(drawable, name);
                imageObj.GetComponent<Canvas>().sortingOrder = order;
            }
            else
            {
                /// Block for create a new image.
                Setup(drawable, name, "", fileData, position, order, out GameObject image);
                imageObj = image;
            }
            /// Saves the loaded image to the app data folder.
            CreateImageFile(imageObj, fileData, fileName, out string filePath);
            /// Sets the old values:
            imageObj.transform.localScale = scale;
            imageObj.transform.localEulerAngles = eulerAngles;
            imageObj.transform.localPosition = position;
            imageObj.GetComponent<Image>().color = imageColor;
            imageObj.GetComponent<OrderInLayerValueHolder>().SetOrderInLayer(order);

            return imageObj;
        }

        /// <summary>
        /// Re-adds the given image of the <paramref name="conf"/> to the <paramref name="drawable"/>.
        /// It calls the RePlaceImage - Method with all the attributes.
        /// </summary>
        /// <param name="drawable">The drawable where the image should be displayed.</param>
        /// <param name="conf">The image configuration to restore the old image</param>
        /// <returns>The created image game object.</returns>
        public static GameObject RePlaceImage(GameObject drawable, ImageConf conf)
        {
            string fileName = Path.GetFileName(conf.path);
            return RePlaceImage(
                drawable,
                conf.id,
                conf.fileData,
                conf.position,
                conf.scale,
                conf.eulerAngles,
                conf.orderInLayer,
                conf.imageColor,
                fileName
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
            DrawableConfigManager.EnsureDrawableDirectoryExists(ValueHolder.imagePath);
            string path = ValueHolder.imagePath + fileName;
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
                        imageObj.GetComponent<ImageValueHolder>().SetPath(path);
                    }
                } else
                {
                    /// For the case if the file in the path dont exists and the file data is empty.
                    ShowNotification.Warn("Cannot be restored.", "The image cannot be restored.");
                    Destroyer.Destroy(imageObj);
                }
            }
            else
            {
                /// If a file with the same name exists, it checks whether they are the same images. 
                /// This is done by comparing the byte count. 
                /// To do this, it is necessary to create a temporary image file. 
                /// This is done in a separate subfolder 'temp.' 
                /// After the examinations, this file and the folder are deleted.
                string tmpPath = ValueHolder.imagePath + "/temp/";
                DrawableConfigManager.EnsureDrawableDirectoryExists(tmpPath);
                string tmpFileName = fileNameWithoutExtension + "-" + 
                    DrawableHolder.GetRandomStringForFile(10) + fileExtension;

                /// If the file already exists, find a new name.
                while (File.Exists(tmpPath + tmpFileName))
                {
                    tmpFileName = fileNameWithoutExtension + "-" + 
                        DrawableHolder.GetRandomStringForFile(10) + fileExtension;
                }
                File.WriteAllBytes(tmpPath + tmpFileName, fileData);
                FileInfo fileInfo = new (tmpPath + tmpFileName);
                FileInfo existsInfo = new (path);

                if (fileInfo.Length != existsInfo.Length)
                {
                    /// If the byte counts are not equal, indicating different images, 
                    /// it checks whether another file with a similar name already exists and 
                    /// whether there is a file among them that matches the image to be saved. 
                    /// If this is not the case, a new file with the highest index + 1 is created.
                    for (int i = 1; i <= Directory.GetFiles(ValueHolder.imagePath).ToList().Count; i++)
                    {
                        string newPath = ValueHolder.imagePath + fileNameWithoutExtension 
                            + "(" + i + ")" + fileExtension;
                        if (!File.Exists(newPath))
                        {
                            Directory.Delete(tmpPath, true);
                            CreateImageFile(imageObj, fileData, Path.GetFileName(newPath), out filePath);
                            break;
                        }
                        else
                        {
                            FileInfo info = new FileInfo(newPath);
                            if (fileInfo.Length == info.Length)
                            {
                                Directory.Delete(tmpPath, true);
                                if (imageObj != null)
                                {
                                    imageObj.GetComponent<ImageValueHolder>().SetPath(newPath);
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Directory.Delete(tmpPath, true);
                    if (imageObj != null)
                    {
                        imageObj.GetComponent<ImageValueHolder>().SetPath(path);
                    }
                }
            }
        }
    }
}