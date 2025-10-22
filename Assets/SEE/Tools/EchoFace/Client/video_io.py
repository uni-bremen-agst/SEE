import cv2
import logging
import numpy as np
from abc import ABC, abstractmethod

logger = logging.getLogger(__name__)


class BaseStream(ABC):
    """
    Abstract Base Class for video and webcam stream I/O.
    Responsible ONLY for capturing and reading frames.

    This design ensures that the data source logic is separate from
    the application's main loop and display/UI logic.
    """

    def __init__(self, target_fps: int):
        """
        Initializes the base stream properties.

        Args:
            target_fps: The requested frame rate for the stream.
        """
        self.target_fps = target_fps
        self.cap: cv2.VideoCapture | None = None
        self.is_running: bool = False
        self.actual_fps: float = 0.0

    @abstractmethod
    def start(self) -> bool:
        """Initializes and opens the capture source (webcam or file)."""
        pass

    @abstractmethod
    def read_frame(self) -> tuple[bool, np.ndarray | None]:
        """Reads a single frame from the source."""
        pass

    def release(self):
        """Releases the capture resource."""
        if self.cap:
            self.cap.release()
            logger.info("Capture source released.")
            self.cap = None
        self.is_running = False

    def get_actual_fps(self) -> float:
        """
        Retrieves the actual FPS reported by the video source.

        Returns:
            float: The actual measured or reported FPS, or the target_fps
                   if the actual rate is unknown or zero.
        """
        return self.actual_fps if self.actual_fps > 0 else self.target_fps

    def __del__(self):
        self.release()


class WebcamStream(BaseStream):
    """Manages live webcam capture, implementing BaseStream."""

    def __init__(self, camera_index: int = 0, target_fps: int = 30):
        """
        Initializes the webcam stream.

        Args:
            camera_index: The numerical index of the camera device (e.g., 0 for default).
            target_fps: The requested frame rate for the webcam.
        """
        super().__init__(target_fps)
        self.camera_index = camera_index
        logger.info(f"WebcamStream initialized for index {self.camera_index}, target FPS: {self.target_fps}")

    def start(self) -> bool:
        """Opens the webcam device and sets the requested FPS."""
        self.cap = cv2.VideoCapture(self.camera_index)
        if not self.cap.isOpened():
            logger.error(f"Could not open webcam with index {self.camera_index}. Check connection.")
            return False

        self.cap.set(cv2.CAP_PROP_FPS, self.target_fps)
        self.actual_fps = self.cap.get(cv2.CAP_PROP_FPS)

        if self.actual_fps <= 0:
            logger.warning(f"Webcam did not report actual FPS. Falling back to requested FPS {self.target_fps}.")
            self.actual_fps = self.target_fps
        else:
            logger.info(f"Webcam reports actual FPS: {self.actual_fps:.2f} (Requested: {self.target_fps})")

        self.is_running = True
        logger.info("Webcam stream started.")
        return True

    def read_frame(self) -> tuple[bool, np.ndarray | None]:
        """
        Reads a single frame from the webcam.

        Returns:
            tuple[bool, np.ndarray | None]: (Success flag, Frame data in BGR format)
        """
        if not self.is_running or self.cap is None:
            return False, None
        return self.cap.read()


class VideoStream(BaseStream):
    """Manages video file capture (with looping), implementing BaseStream."""

    def __init__(self, video_path: str, target_fps: int = 30):
        """
        Initializes the video file stream.

        Args:
            video_path: The filesystem path to the video file.
            target_fps: The requested frame rate. This is usually overridden by the file's native FPS.
        """
        super().__init__(target_fps)
        self.video_path = video_path
        logger.info(f"VideoStream initialized for file: {self.video_path}, requested FPS: {self.target_fps}")

    def start(self) -> bool:
        """Opens the video file and reads its native FPS."""
        self.cap = cv2.VideoCapture(self.video_path)
        if not self.cap.isOpened():
            logger.error(f"Could not open video file: {self.video_path}. Check the path.")
            return False

        self.actual_fps = self.cap.get(cv2.CAP_PROP_FPS)

        if self.actual_fps <= 0:
            logger.warning(f"Video file did not report a valid FPS. Falling back to requested FPS {self.target_fps}.")
            self.actual_fps = self.target_fps
        else:
            logger.info(f"Video file FPS: {self.actual_fps:.2f}")

        self.is_running = True
        logger.info("Video stream started.")
        return True

    def read_frame(self) -> tuple[bool, np.ndarray | None]:
        """
        Reads a single frame from the video file.

        The method will NOT loop and will return (False, None) after the last frame.

        Returns:
            tuple[bool, np.ndarray | None]: (Success flag, Frame data in BGR format)
        """
        if not self.is_running or self.cap is None:
            return False, None

        # Attempt to read the next frame
        ret, frame = self.cap.read()

        if not ret:
            # If 'ret' is False, the end of the video file has been reached.
            # We log the event and stop the stream.
            logger.info("End of video file reached. Stopping stream.")
            self.release()  # Call the release method to clean up resources
            return False, None

        return ret, frame


class FrameViewer:
    """
    Handles all visual presentation and user input management (UI and Control)
    for displaying individual frames using OpenCV's windowing system.

    It uses the stream's FPS to calculate the appropriate delay for smooth playback.
    """

    def __init__(self, window_name: str, stream_fps: float):
        """
        Initializes the frame viewer.

        Args:
            window_name: The title for the OpenCV window.
            stream_fps: The actual or target frame rate of the video source, used to calculate the display delay.
        """
        self.window_name = window_name
        # Calculate delay (in ms) based on the actual/target FPS of the stream
        self.display_delay_ms = max(1, int(1000 / stream_fps))
        logger.info(f"FrameViewer initialized. Frame display delay: {self.display_delay_ms} ms.")

    def display_frame(self, frame_bgr: np.ndarray):
        """
        Displays the frame in the dedicated OpenCV window.

        Args:
            frame_bgr: The frame data in BGR format (NumPy array).
        """
        if frame_bgr is not None:
            cv2.imshow(self.window_name, frame_bgr)

    def check_key(self) -> int:
        """
        Waits for a key press based on the calculated inter-frame delay.

        Returns:
            int: The ASCII value of the pressed key, or -1 if no key was pressed.
        """
        # The key check is tied to the delay calculated from the stream's FPS
        return cv2.waitKey(self.display_delay_ms) & 0xFF

    def cleanup(self):
        """Destroys all OpenCV windows."""
        cv2.destroyAllWindows()
        logger.info("OpenCV windows destroyed.")
