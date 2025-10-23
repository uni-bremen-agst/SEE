import mediapipe as mp
import logging
from typing import Callable, Optional, Dict, List, Any
import collections.abc

logger = logging.getLogger(__name__)

# MediaPipe constants for easier access
BaseOptions = mp.tasks.BaseOptions
FaceLandmarker = mp.tasks.vision.FaceLandmarker
FaceLandmarkerOptions = mp.tasks.vision.FaceLandmarkerOptions
FaceLandmarkerResult = mp.tasks.vision.FaceLandmarkerResult
VisionRunningMode = mp.tasks.vision.RunningMode


class FaceAnalyzer:
    """
    Manages MediaPipe's Face Landmarker detection.

    Processes frames asynchronously and invokes a callback with detected blendshape data
    and a subset of landmarks.
    """
    MODEL_PATH = 'face_landmarker.task'

    def __init__(self, on_new_result_callback: Optional[Callable[[Dict[str, float], Dict[int, Any]], None]] = None):
        """
        Initializes the FaceAnalyzer.

        Args:
            on_new_result_callback:
                A callback function to be invoked with the detected blendshape data (as a dict)
                and a list of selected landmark coordinates.
                If `None`, no action is taken upon detection.
        """
        self._landmarker: FaceLandmarker | None = None
        self.latest_detection_result: FaceLandmarkerResult | None = None
        self._on_new_result_callback = on_new_result_callback

        # A subset of landmarks to send for head rotation and eye tracking
        self._selected_landmark_indices = [
            152,    # Chin
            362,    # Left eye inner corner
            133,    # Right eye inner corner
            263,    # Left eye outer corner
            33,     # Right eye outer corner
            473,    # Left iris center
            468,    # Right iris center
            446,    # Left upper eyelid
            226,    # Right upper eyelid
        ]

    def _result_callback(self, result: FaceLandmarkerResult, output_image: mp.Image, timestamp_ms: int):
        """
        Callback function invoked by MediaPipe when a new detection result is available.
        """
        self.latest_detection_result = result

        blendshape_data = {}
        selected_landmarks = {}

        if result and result.face_landmarks and result.face_blendshapes:
            # 1. Process blendshapes
            current_face_blendshapes = result.face_blendshapes[0]
            blendshape_data = {
                bs.category_name: round(bs.score, 4) for bs in current_face_blendshapes
            }

            # 2. Extract only the required landmarks
            if result.face_landmarks and result.face_landmarks[0]:
                for i in self._selected_landmark_indices:
                    selected_landmarks[i] = {  # Use index as the key
                        "x": round(result.face_landmarks[0][i].x, 4),
                        "y": round(result.face_landmarks[0][i].y, 4),
                        "z": round(result.face_landmarks[0][i].z, 4)
                    }

        # 3. Invoke the combined callback with the processed data
        if self._on_new_result_callback:
            try:
                self._on_new_result_callback(blendshape_data, selected_landmarks)
            except Exception as e:
                logger.error(f"Error executing combined callback: {e}")

    def start(self):
        """
        Starts the Face Landmarker.

        Creates a MediaPipe Face Landmarker instance configured for live stream mode.
        """
        try:
            options = FaceLandmarkerOptions(
                base_options=BaseOptions(model_asset_path=self.MODEL_PATH),
                running_mode=VisionRunningMode.LIVE_STREAM,
                result_callback=self._result_callback,
                output_face_blendshapes=True,
                num_faces=1,
                output_facial_transformation_matrixes=False,
            )
            self._landmarker = FaceLandmarker.create_from_options(options)
            logger.info("FaceAnalyzer started.")
        except Exception as e:
            logger.error(f"Failed to start FaceAnalyzer: {e}")
            self._landmarker = None

    def detect_async(self, mp_image: mp.Image, timestamp_ms: int):
        """
        Submits an image for asynchronous face detection.
        """
        if self._landmarker:
            self._landmarker.detect_async(mp_image, timestamp_ms)

    def stop(self):
        """
        Stops the Face Landmarker and releases its resources.
        """
        if self._landmarker:
            self._landmarker.close()
            self._landmarker = None
        logger.info("FaceAnalyzer stopped.")
