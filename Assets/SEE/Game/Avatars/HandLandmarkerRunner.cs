using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediapipe;
using Mediapipe.Unity;
using UnityEngine.UI;

using Stopwatch = System.Diagnostics.Stopwatch;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.CoordinateSystem;
using Mediapipe.Tasks.Vision.HandLandmarker;

namespace SEE.Game.Avatars
{
    internal class HandLandmarkerRunner : MonoBehaviour
    {
        [SerializeField] private TextAsset configAsset;
        [SerializeField] private RawImage screen;
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private int fps;

        [SerializeField] private TextAsset modelAsset;

        private CalculatorGraph graph;
        private OutputStream<ImageFrame> outputVideoStream;

        private WebCamTexture webCamTexture;

        private IEnumerator Start()
        {
            if (webCamTexture == null)
            {
                WebcamManager.Initialize();
                Debug.Log("WebCamTexture is not initialized yet.");
            }

            webCamTexture = WebcamManager.SharedWebCamTexture;

            yield return new WaitUntil(() => webCamTexture.width > 16);

            var outputTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var outputPixelData = new Color32[width * height];

            screen.rectTransform.sizeDelta = new Vector2(width, height);
            screen.texture = outputTexture;

            IResourceManager resourceManager = new LocalResourceManager();
            yield return resourceManager.PrepareAssetAsync("palm_detection_full.bytes");
            yield return resourceManager.PrepareAssetAsync("hand_landmark_full.bytes");
            yield return resourceManager.PrepareAssetAsync("handedness.txt");

            graph = new CalculatorGraph(configAsset.text);
            outputVideoStream = new OutputStream<ImageFrame>(graph, "output_video");
            outputVideoStream.StartPolling();
            var multiHandLandmarksStream = new OutputStream<List<NormalizedLandmarkList>>(graph, "multi_hand_landmarks");
            multiHandLandmarksStream.StartPolling();
            graph.StartRun();
                
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            using var textureFrame = new Mediapipe.Unity.Experimental.TextureFrame(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32);

            while (true)
            {
                textureFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: true, flipVertically: true);
                using var imageFrame = textureFrame.BuildImageFrame();

                var currentTimestamp = stopwatch.ElapsedTicks / ((double)System.TimeSpan.TicksPerMillisecond / 1000);
                graph.AddPacketToInputStream("input_video", Packet.CreateImageFrameAt(imageFrame, (long)currentTimestamp));

                var task = outputVideoStream.WaitNextAsync();
                yield return new WaitUntil(() => task.IsCompleted);

                if (!task.Result.ok)
                {
                    throw new System.Exception("Something went wrong");
                }

                var outputPacket = task.Result.packet;
                if (outputPacket != null)
                {
                    var outputVideo = outputPacket.Get();

                    if (outputVideo.TryReadPixelData(outputPixelData))
                    {
                        outputTexture.SetPixels32(outputPixelData);
                        outputTexture.Apply();
                    }
                }

            }
        }

        private void OnDestroy()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
            }

            outputVideoStream?.Dispose();
            outputVideoStream = null;

            if (graph != null)
            {
                try
                {
                    graph.CloseInputStream("input_video");
                    graph.WaitUntilDone();
                }
                finally
                {
                    graph.Dispose();
                    graph = null;
                }
            }
        }
    }
}