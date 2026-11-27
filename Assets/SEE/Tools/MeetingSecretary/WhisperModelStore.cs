using System;
using System.IO;
using Cysharp.Threading.Tasks;
using SEE.UI;
using SEE.UI.Notification;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Provides the Whisper model files on the local disk. A model that is not
    /// yet there is downloaded on first use.
    /// </summary>
    /// <remarks>The models are stored in the persistent data folder of the application,
    /// never in the project.</remarks>
    public static class WhisperModelStore
    {
        /// <summary>
        /// The name of the folder (below <see cref="Application.persistentDataPath"/>)
        /// in which the models are stored.
        /// </summary>
        private const string folderName = "Whisper";

        /// <summary>
        /// Appended to the name of a model file while it is being downloaded.
        /// A file with this extension is never used as a model.
        /// </summary>
        private const string partialExtension = ".part";

        /// <summary>
        /// The title of the notifications shown to the user.
        /// </summary>
        private const string notificationTitle = "Transcription";

        /// <summary>
        /// The folder in which the models are stored.
        /// </summary>
        public static string Folder => Path.Combine(Application.persistentDataPath, folderName);

        /// <summary>
        /// Returns the path of the model file <paramref name="fileName"/>. If the model is
        /// not yet on the local disk, it is downloaded and verified first. If that fails,
        /// the user is notified and nothing is left on the disk.
        /// </summary>
        /// <param name="fileName">The file name of the model, e.g., "ggml-small-q5_1.bin".</param>
        /// <returns>The absolute path of the model file, or null if it is not available.</returns>
        public static async UniTask<string> GetModelPathAsync(string fileName)
        {
            if (!WhisperModels.IsValidFileName(fileName))
            {
                ShowNotification.Error(notificationTitle,
                                       $"'{fileName}' is not a valid file name for a speech model.");
                return null;
            }

            string path = Path.Combine(Folder, fileName);
            if (File.Exists(path))
            {
                return path;
            }

            // The model is downloaded under a different name and renamed only after it
            // has been verified. Thus, an incomplete file can never be taken for a model.
            string partialPath = path + partialExtension;
            try
            {
                Directory.CreateDirectory(Folder);
                // A leftover of an earlier download that was interrupted.
                FileIO.DeleteIfExists(partialPath);

                string error = await DownloadAsync(WhisperModels.GetDownloadUrl(fileName), partialPath, fileName);
                if (error != null)
                {
                    FileIO.DeleteIfExists(partialPath);
                    ShowNotification.Error(notificationTitle,
                                           $"The speech model {fileName} could not be downloaded ({error}). "
                                           + "An internet connection is needed the first time the transcription is used.");
                    return null;
                }

                if (WhisperModels.TryGetChecksum(fileName, out string checksum))
                {
                    // Computing the checksum of a large file takes a while.
                    bool verified = await UniTask.RunOnThreadPool(() => WhisperModels.HasChecksum(partialPath, checksum));
                    if (!verified)
                    {
                        FileIO.DeleteIfExists(partialPath);
                        ShowNotification.Error(notificationTitle,
                                               $"The downloaded speech model {fileName} is damaged (wrong checksum). "
                                               + "It was deleted. Please try again.");
                        return null;
                    }
                }
                else
                {
                    Debug.LogWarning($"No checksum is known for the speech model {fileName}. "
                                     + "The downloaded file was not verified.\n");
                }

                File.Move(partialPath, path);
                return path;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                FileIO.DeleteIfExists(partialPath);
                ShowNotification.Error(notificationTitle,
                                       $"The speech model {fileName} could not be stored in {Folder}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Downloads <paramref name="url"/> to the file <paramref name="targetPath"/>
        /// while showing the progress to the user.
        /// </summary>
        /// <param name="url">The URL to be downloaded.</param>
        /// <param name="targetPath">The path of the file to be written.</param>
        /// <param name="fileName">The name of the model shown to the user.</param>
        /// <returns>Null if the download succeeded; otherwise the reason of the failure.</returns>
        private static async UniTask<string> DownloadAsync(string url, string targetPath, string fileName)
        {
            // The request is disposed at the end of this method. Only then, the
            // downloaded file is closed and can be verified and renamed by the caller.
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerFile(targetPath)
            {
                removeFileOnAbort = true
            };

            using (LoadingSpinner.ShowDeterminate($"Downloading speech model {fileName}...", out Action<float> updateProgress))
            {
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    updateProgress(request.downloadProgress);
                    await UniTask.Yield();
                }
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return request.error;
            }
            return null;
        }
    }
}
