import cv2
import logging
import numpy as np  # For type hinting ndarray

logger = logging.getLogger(__name__)


class WebcamStream:
    """
    Manages webcam capture, frame reading, and display using OpenCV.

    Frames read via `read_frame()` are returned in BGR format (OpenCV's default).
    For RGB (e.g., for Matplotlib or MediaPipe.Image(SRGB)), convert using cv2.cvtColor(frame, cv2.COLOR_BGR2RGB).
    """

    def __init__(self, camera_index: int = 0, window_name: str = "Webcam Feed", target_fps: int = 30):
        """
        Initializes the webcam stream.

        Args:
            camera_index: The index of the camera to use (default is 0 for the primary webcam).
            window_name: The name of the OpenCV window to display the webcam feed.
            target_fps: The desired frames per second for the webcam capture.
        """
        self.camera_index = camera_index
        self.window_name = window_name
        self.target_fps = target_fps
        self.cap: cv2.VideoCapture | None = None
        self.is_running: bool = False
        self.display_delay_ms: int = 1  # Calculated based on actual FPS, ensures at least 1ms

        logger.info(f"WebcamStream initialized for camera index {self.camera_index}, target FPS: {self.target_fps}")

    def start(self) -> bool:
        """
        Starts the webcam capture and attempts to set the target FPS.

        Returns:
            bool: True if the webcam was successfully opened, False otherwise.
        """
        self.cap = cv2.VideoCapture(self.camera_index)
        if not self.cap.isOpened():
            logger.error(
                f"Could not open webcam with index {self.camera_index}. Check if the camera is connected and not in use.")
            return False

        # Attempt to set the desired FPS property
        self.cap.set(cv2.CAP_PROP_FPS, self.target_fps)

        # Get the actual FPS the camera is reporting (might differ from target_fps)
        actual_fps = self.cap.get(cv2.CAP_PROP_FPS)

        if actual_fps == 0:
            logger.warning(
                f"Webcam did not report its actual FPS (returned 0). Falling back to requested FPS {self.target_fps} for display timing.")
            actual_fps = self.target_fps  # Fallback if camera doesn't report FPS
        else:
            logger.info(f"Webcam reports actual FPS: {actual_fps:.2f} (Requested: {self.target_fps})")

        # Calculate the delay for cv2.waitKey() to synchronize with the camera's FPS
        self.display_delay_ms = max(1, int(1000 / actual_fps))

        self.is_running = True
        logger.info(f"Webcam stream started for '{self.window_name}'. Display delay set to {self.display_delay_ms} ms.")
        return True

    def read_frame(self) -> tuple[bool, np.ndarray | None]:
        """
        Reads a single frame from the webcam. The frame is returned in BGR format
        (OpenCV's default).

        Returns:
            tuple: A tuple containing (ret, frame).
                   ret (bool): True if the frame was read successfully, False otherwise.
                   frame (numpy.ndarray): The captured frame (BGR format) if successful, None otherwise.
        """
        if not self.is_running or self.cap is None:
            return False, None

        ret, frame = self.cap.read()
        return ret, frame

    def display_frame(self, frame_bgr: np.ndarray):
        """
        Displays the given frame in the OpenCV window. This method expects a BGR frame.

        Args:
            frame_bgr: The frame to display (BGR format).
        """
        if frame_bgr is not None and self.is_running:
            cv2.imshow(self.window_name, frame_bgr)

    def check_key(self) -> int:
        """
        Waits for a key press for a duration equal to the inter-frame delay.
        This method is a replacement for calling `cv2.waitKey()` directly
        from the main loop.

        Returns:
            int: The ASCII value of the key pressed, or -1 if no key was pressed.
        """
        if not self.is_running:
            return -1
        return cv2.waitKey(self.display_delay_ms) & 0xFF

    def get_actual_fps(self) -> float:
        """
        Retrieves the actual frames per second (FPS) reported by the webcam hardware.
        This is the FPS the camera is currently attempting to deliver.

        Returns:
            float: The actual FPS of the webcam, or 0.0 if not available or webcam not opened.
        """
        if self.cap and self.is_running:
            return self.cap.get(cv2.CAP_PROP_FPS)
        return 0.0

    def release(self):
        """
        Releases the webcam resource and destroys the OpenCV window.
        """
        if self.cap:
            self.cap.release()
            logger.info(f"Webcam with index {self.camera_index} released.")
            self.cap = None

        if self.is_running:
            try:
                cv2.destroyWindow(self.window_name)
                logger.info(f"OpenCV window '{self.window_name}' destroyed.")
            except cv2.error as e:
                logger.debug(f"Could not destroy window '{self.window_name}': {e}")
        self.is_running = False

    def __del__(self):
        """
        Ensures resources are released when the object is garbage collected.
        """
        self.release()
