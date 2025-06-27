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

namespace Mediapipe.Unity.Tutorial
{
    public class HandLandmarkerRunner : MonoBehaviour
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
            if (WebCamTexture.devices.Length == 0)
            {
                throw new System.Exception("Web Camera devices are not found");
            }
            var webCamDevice = WebCamTexture.devices[0];
            webCamTexture = new WebCamTexture(webCamDevice.name, width, height, fps);
            webCamTexture.Play();

            yield return new WaitUntil(() => webCamTexture.width > 16);

            var outputTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var outputPixelData = new Color32[width * height];

            screen.rectTransform.sizeDelta = new Vector2(width, height);
            screen.texture = outputTexture;

            var options = new HandLandmarkerOptions(
                baseOptions: new Tasks.Core.BaseOptions(
                    Tasks.Core.BaseOptions.Delegate.CPU,
                    modelAssetBuffer: modelAsset.bytes),
                runningMode: Tasks.Vision.Core.RunningMode.VIDEO);

            using var handLandmarker = HandLandmarker.CreateFromOptions(options);

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

            using var textureFrame = new Experimental.TextureFrame(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32);

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

                var taskLandmarks = multiHandLandmarksStream.WaitNextAsync();
                yield return new WaitUntil(() => taskLandmarks.IsCompleted);
                if (!taskLandmarks.Result.ok)
                {
                    throw new System.Exception("Something went wrong");
                }

                var landmarksOutputPacket = taskLandmarks.Result.packet;
                if (landmarksOutputPacket != null)
                {
                     //List<NormalizedLandmarkList> handLandmarks = landmarksOutputPacket.Get();
                }

                using var image = textureFrame.BuildCPUImage();
                var result = handLandmarker.DetectForVideo(image, stopwatch.ElapsedMilliseconds);
                //Debug.Log(result);
                if (result.handLandmarks?.Count > 0)
                {
                    var landmarks = result.handLandmarks[0].landmarks;
                    var palmCenter = landmarks[0];
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











/**using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Vision.PoseLandmarker;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Mediapipe.Unity.Tutorial
{
  public class HandLandmarkerRunner : MonoBehaviour
  {
    [SerializeField] private RawImage screen;
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int fps;

    [SerializeField] private TextAsset modelAsset;


        private OutputStream<ImageFrame> outputVideoStream;

        private WebCamTexture webCamTexture;

    private IEnumerator Start()
    {
      if (WebCamTexture.devices.Length == 0)
      {
        throw new System.Exception("Web Camera devices are not found");
      }
      var webCamDevice = WebCamTexture.devices[0];
      webCamTexture = new WebCamTexture(webCamDevice.name, width, height, fps);
      webCamTexture.Play();

      // NOTE: On macOS, the contents of webCamTexture may not be readable immediately, so wait until it is readable
      yield return new WaitUntil(() => webCamTexture.width > 16);

      var outputTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
      var outputPixelData = new Color32[width * height];

      screen.rectTransform.sizeDelta = new Vector2(width, height);
      screen.texture = webCamTexture;

      var options = new PoseLandmarkerOptions(
                baseOptions: new Tasks.Core.BaseOptions(
                    Tasks.Core.BaseOptions.Delegate.CPU,
                    modelAssetBuffer: modelAsset.bytes),
                runningMode: Tasks.Vision.Core.RunningMode.VIDEO);    

      using var poseLandmarker = PoseLandmarker.CreateFromOptions(options);

    
      var stopwatch = new Stopwatch();
      stopwatch.Start();

      var waitForEndOfFrame = new WaitForEndOfFrame();
      var textureFrame = new Experimental.TextureFrame(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32);
      

            //var tmpTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

            while (true)
            {
                ///tmpTexture.SetPixels32(webCamTexture.GetPixels32());
                //tmpTexture.Apply();
                //using var image = new Image(tmpTexture);
                textureFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: false, flipVertically: true);
                using var image = textureFrame.BuildCPUImage();

                var result = poseLandmarker.DetectForVideo(image, stopwatch.ElapsedMilliseconds);
                //Debug.Log(result);
                if (result.poseLandmarks?.Count > 0)
                {
                    var landmarks = result.poseLandmarks[0].landmarks;
                    var head = landmarks[0];
                    var leftHand = landmarks[15];
                    var rightHand = landmarks[16];

                }
               

                    yield return waitForEndOfFrame;
            }
            
    }

    private void OnDestroy()
    {
      if (webCamTexture != null)
      {
        webCamTexture.Stop();
      }
    }
  }
}*/
