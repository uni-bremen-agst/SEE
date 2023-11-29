using SEE.Game.Drawable.Configurations;
using ICSharpCode.SharpZipLib.Core;
using SEE.Game;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Assets.SEE.Game.Drawable.GameDrawer;
using System.IO;
using OpenAI.Files;
using TMPro;
using SEE.Utils;
using System.Linq;
using SEE.GO;
using SEE.Game.Drawable;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This class add's an image to a drawable.
    /// </summary>
    public static class GameImage
    {
        /// <summary>
        /// Method to convert a texture to a sprite
        /// @author Jaimin, April 2022
        /// https://stackoverflow.com/questions/71898310/how-to-change-the-texture-type-of-a-png-image-to-sprite-through-a-script-in-uni
        /// </summary>
        /// <param name="texture">The texture that should be convertet.</param>
        /// <returns>The convertet sprite</returns>
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
        /// <param name="image">the output game object.</param>
        private static void Setup(GameObject drawable, string name, string imageFilePath, byte[] data, Vector3 position, int order,
            out GameObject image)
        {
            if (name.Length > 4)
            {
                image = new(name);
            }
            else
            {
                image = new("");
                name = ValueHolder.ImagePrefix + image.GetInstanceID() + DrawableHolder.GetRandomString(4);
                while (GameFinder.FindChild(drawable, name) != null)
                {
                    name = ValueHolder.ImagePrefix + image.GetInstanceID() + DrawableHolder.GetRandomString(4);
                }
                image.name = name;
            }

            GameObject highestParent, attachedObjects;
            DrawableHolder.Setup(drawable, out highestParent, out attachedObjects);

            image.tag = Tags.Image;
            image.transform.SetParent(attachedObjects.transform);

            Canvas canvas = image.AddComponent<Canvas>();
            canvas.sortingOrder = order;

            Image i = image.AddComponent<Image>();
            i.rectTransform.sizeDelta = new Vector2(1, 1);  

            if (imageFilePath != "" && File.Exists(imageFilePath))
            {
                byte[] fileData = File.ReadAllBytes(imageFilePath);
                ImageValueHolder holder = image.AddComponent<ImageValueHolder>();
                holder.SetFileData(fileData);
                CreateImageFile(image, fileData, Path.GetFileName(imageFilePath), out string filePath);

                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);
                i.sprite = TextureToSprite(texture);
            }
            else if (data != null)
            {
                image.AddComponent<ImageValueHolder>().SetFileData(data);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(data);
                i.sprite = TextureToSprite(texture);
            }

            BoxCollider collider = image.AddComponent<BoxCollider>();
            collider.size = new Vector3(1, 1, 0.01f);

            image.transform.rotation = attachedObjects.transform.rotation;
            image.transform.position = position - image.transform.forward * ValueHolder.distanceToDrawable.z * order;

            image.AddComponent<OrderInLayerValueHolder>().SetOrderInLayer(order);
        }

        /// <summary>
        /// Method to add an image to a drawable.
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
        /// Method to readd an image to a drawable.
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
        public static GameObject RePlaceImage(GameObject drawable, string name, byte[] fileData, Vector3 position, Vector3 scale, Vector3 eulerAngles, int order, Color imageColor, string fileName)
        {
            if (order >= ValueHolder.currentOrderInLayer)
            {
                ValueHolder.currentOrderInLayer = order + 1;
            }
            GameObject imageObj;
            if (GameFinder.FindChild(drawable, name) != null)
            {
                imageObj = GameFinder.FindChild(drawable, name);
                imageObj.GetComponent<Canvas>().sortingOrder = order;
            }
            else
            {
                Setup(drawable, name, "", fileData, position, order, out GameObject image);
                imageObj = image;
            }
            CreateImageFile(imageObj, fileData, fileName, out string filePath);
            imageObj.transform.localScale = scale;
            imageObj.transform.localEulerAngles = eulerAngles;
            imageObj.transform.localPosition = position;
            imageObj.GetComponent<Image>().color = imageColor;
            imageObj.GetComponent<OrderInLayerValueHolder>().SetOrderInLayer(order);
            return imageObj;
        }

        /// <summary>
        /// Method to readd an image to a drawable.
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
        /// Method to create a local backup of the image in the AppData directory.
        /// </summary>
        /// <param name="imageObj">The image game object, can be null when the method is used to save a downloaded file.</param>
        /// <param name="fileData">The fila data of the image.</param>
        /// <param name="fileName">The file name of the image.</param>
        public static void CreateImageFile(GameObject imageObj, byte[] fileData, string fileName, out string filePath)
        {
            DrawableConfigManager.EnsureDrawableDirectoryExists(ValueHolder.imagePath);
            string path = ValueHolder.imagePath + fileName;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string fileExtension = Path.GetExtension(fileName);
            filePath = path;

            /// If no file with the exact name exists, create it.
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, fileData);
                if (imageObj != null)
                {
                    imageObj.GetComponent<ImageValueHolder>().SetPath(path);
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
                string tmpFileName = fileNameWithoutExtension + "-" + DrawableHolder.GetRandomStringForFile(10) + fileExtension;

                while (File.Exists(tmpPath + tmpFileName))
                {
                    tmpFileName = fileNameWithoutExtension + "-" + DrawableHolder.GetRandomStringForFile(10) + fileExtension;
                }
                File.WriteAllBytes(tmpPath + tmpFileName, fileData);
                FileInfo fileInfo = new FileInfo(tmpPath + tmpFileName);
                FileInfo existsInfo = new FileInfo(path);

                if (fileInfo.Length != existsInfo.Length)
                {
                    /// If the byte counts are not equal, indicating different images, 
                    /// it checks whether another file with a similar name already exists and 
                    /// whether there is a file among them that matches the image to be saved. 
                    /// If this is not the case, a new file with the highest index + 1 is created.
                    for (int i = 1; i <= Directory.GetFiles(ValueHolder.imagePath).ToList().Count; i++)
                    {
                        string newPath = ValueHolder.imagePath + fileNameWithoutExtension + "(" + i + ")" + fileExtension;
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