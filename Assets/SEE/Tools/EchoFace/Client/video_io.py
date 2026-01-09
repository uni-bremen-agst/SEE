import cv2
import logging
import numpy as np
from abc import ABC, abstractmethod
from enum import Enum, auto
import time

logger = logging.getLogger(__name__)

def resize_letterbox(
    frame: np.ndarray,
    target_size: tuple[int, int],
    pad_color: tuple[int, int, int] = (0, 0, 0),
) -> np.ndarray:
    """
    Resizes an image to fit inside target_size while preserving aspect ratio.
    Pads remaining area with pad_color (letterboxing).

    Args:
        frame: Input image (BGR).
        target_size: (width, height).
        pad_color: BGR color for padding.

    Returns:
        np.ndarray: Letterboxed image of exact target_size.
    """
    target_w, target_h = target_size
    h, w = frame.shape[:2]

    scale = min(target_w / w, target_h / h)
    new_w = int(w * scale)
    new_h = int(h * scale)

    resized = cv2.resize(frame, (new_w, new_h), interpolation=cv2.INTER_LINEAR)

    canvas = np.full((target_h, target_w, 3), pad_color, dtype=np.uint8)

    x_offset = (target_w - new_w) // 2
    y_offset = (target_h - new_h) // 2

    canvas[y_offset:y_offset + new_h, x_offset:x_offset + new_w] = resized
    return canvas

class BaseStream(ABC):
    """
    Abstract base class for webcam and video file input.
    Handles initialization, frame reading, resizing, and cleanup.
    """

    def __init__(self, fps: int = 30, resolution: tuple[int, int] = (640, 480)):
        """
        Initializes the base stream configuration.

        Args:
            fps: Desired frame rate (frames per second).
            resolution: Desired frame resolution as (width, height).
        """
        self.req_fps = fps
        self.req_resolution = resolution
        self.cap: cv2.VideoCapture | None = None
        self.is_running: bool = False

    def release(self):
        """
        Releases the video capture resource and marks the stream as stopped.
        """
        if self.cap:
            self.cap.release()
            self.cap = None
        self.is_running = False

    def get_fps(self) -> float:
        """
        Returns the current FPS of the capture source, if available.

        Returns:
            float: The actual FPS reported by the source, or the
                   requested req_fps if no FPS information is available.
        """
        if not self.cap:
            return float(self.req_fps)
        fps = self.cap.get(cv2.CAP_PROP_FPS)
        return fps if fps and fps > 0 else float(self.req_fps)

    def get_resolution(self) -> tuple[int, int]:
        """
        Returns the current resolution (width, height).

        Returns:
            tuple[int, int]: The actual resolution reported by the source,
                             or the requested resolution if not available.
        """
        if not self.cap:
            return self.req_resolution
        width = int(self.cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        height = int(self.cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        if width > 0 and height > 0:
            return (width, height)
        return self.req_resolution

    def log_properties(self):
        """
        Logs the current capture properties (width, height, FPS) for debugging.

        This method queries OpenCV capture parameters and prints them via
        the logger, showing both actual and requested values.

        Example output:
            Capture properties: 640x480 @ 29.97 FPS
            (requested 640x480, 30 FPS)
        """
        if not self.cap or not self.cap.isOpened():
            logger.info("Capture not opened.")
            return

        width = int(self.cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        height = int(self.cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        fps = self.cap.get(cv2.CAP_PROP_FPS)

        logger.info(
            f"Capture properties: {width}x{height} @ {fps:.2f} FPS "
            f"(requested {self.req_resolution[0]}x{self.req_resolution[1]}, {self.req_fps} FPS)"
        )

    @abstractmethod
    def start(self) -> bool:
        """
        Opens and initializes the video source (webcam or file).

        Returns:
            bool: True if the capture source was successfully opened,
                  False otherwise.
        """
        pass

    def read_frame(self) -> tuple[bool, np.ndarray | None]:
        """
        Reads a single frame from the capture source.

        The frame is automatically resized to the requested resolution
        (req_resolution).

        Returns:
            tuple[bool, np.ndarray | None]:
                - True and a valid frame (as a NumPy array in BGR format)
                  if reading succeeded.
                - False and None if reading failed or the stream ended.
        """
        if not self.is_running or self.cap is None:
            return False, None

        ret, frame = self.cap.read()
        if not ret or frame is None:
            # self.release()
            return False, None

        frame = resize_letterbox(frame, self.req_resolution)
        return True, frame

    def __del__(self):
        """Ensures resources are released when the object is deleted."""
        self.release()


class WebcamStream(BaseStream):
    """
    Video stream class for live webcam input.
    """

    def __init__(self, camera_index: int = 0, fps: int = 30, resolution: tuple[int, int] = (640, 480)):
        """
        Initializes a webcam video stream.

        Args:
            camera_index: Index of the webcam device (0 = default).
            fps: Desired frame rate (FPS) to request from the device.
            resolution: Desired frame resolution as (width, height).
        """
        super().__init__(fps, resolution)
        self.camera_index = camera_index

    def start(self) -> bool:
        """
        Opens the webcam device and applies the requested FPS and resolution.

        Returns:
            bool: True if the webcam was successfully opened,
                  False otherwise.
        """
        self.cap = cv2.VideoCapture(self.camera_index)
        if not self.cap.isOpened():
            logger.error(f"Failed to open webcam index {self.camera_index}")
            return False

        self.cap.set(cv2.CAP_PROP_FRAME_WIDTH, self.req_resolution[0])
        self.cap.set(cv2.CAP_PROP_FRAME_HEIGHT, self.req_resolution[1])
        self.cap.set(cv2.CAP_PROP_FPS, self.req_fps)

        self.is_running = True
        self.log_properties()
        return True


class VideoStream(BaseStream):
    """
    Video stream class for reading from video files.
    """

    def __init__(self, video_path: str, fps: int = 30, resolution: tuple[int, int] = (640, 480)):
        """
        Initializes a video file stream.

        Args:
            video_path: Path to the video file to read.
            fps: Desired frame rate (FPS) to request from the device.
            resolution: Desired frame resolution as (width, height).
        """
        super().__init__(fps, resolution)
        self.video_path = video_path

    def start(self) -> bool:
        """
        Opens the video file for reading.

        Returns:
            bool: True if the file was successfully opened,
                  False otherwise.
        """
        self.cap = cv2.VideoCapture(self.video_path)
        if not self.cap.isOpened():
            logger.error(f"Failed to open video file: {self.video_path}")
            return False

        self.is_running = True
        self.log_properties()
        return True



class PlaybackClock:
    """
    Maintains a real-time playback schedule for frame processing.

    The clock tracks a start time and frame index and provides a method to
    block until the presentation time of the next frame, based on a target FPS.
    If the target FPS is not valid, the clock becomes a no-op.
    """

    def __init__(self, target_fps: float | None):
        """
        Initializes the playback clock.

        Args:
            target_fps: Desired playback rate in frames per second. If None
                or non-positive, the clock will not enforce any timing.
        """
        if target_fps and target_fps > 0:
            self._frame_duration = 1.0 / float(target_fps)
        else:
            self._frame_duration = None

        self._playback_fps = target_fps
        self._start_time: float | None = None
        self._frame_index: int = 0

    def reset(self):
        """
        Resets the internal time reference and frame index.

        This is useful when restarting a stream or seeking to a new
        position in the video source.
        """
        self._start_time = None
        self._frame_index = 0

    def wait_for_next(self):
        """
        Blocks until the presentation time of the next frame.

        If no valid frame duration is configured, this method returns
        immediately without sleeping.
        """
        if self._frame_duration is None:
            return

        if self._start_time is None:
            self._start_time = time.perf_counter()
            self._frame_index = 0
            return

        self._frame_index += 1
        target = self._start_time + self._frame_index * self._frame_duration

        now = time.perf_counter()
        remaining = target - now

        if remaining > 0.005:
            time.sleep(remaining - 0.002)

    def get_playback_fps(self) -> float | None:
        """Returns the configured playback FPS, or None if disabled."""
        return self._playback_fps


class ToolbarButton(Enum):
    PAUSE = auto()
    LANDMARKS = auto()
    QUIT = auto()


class FrameViewer:
    """
    Displays frames in an OpenCV window with a minimal, auto-sized toolbar
    rendered below the frame. All button behavior (labels, state, actions, colors)
    is defined through internal mappings.
    """

    def __init__(
        self,
        window_name: str,
        on_pause=None,
        on_landmarks=None,
        on_quit=None,
        paused_getter=None,
        landmarks_getter=None,
    ):
        """
        Creates a viewer window and prepares the toolbar.

        Args:
            window_name: The title of the OpenCV window.
            on_pause: Callback to invoke when the pause button is pressed.
            on_landmarks: Callback to invoke when the landmarks button is pressed.
            on_quit: Callback to invoke when the quit button is pressed.
            paused_getter: Callable returning True if the client is currently paused.
            landmarks_getter: Callable returning True if landmarks are currently enabled.
        """
        self.window_name = window_name
        self.wait_delay_ms = 1

        self._action_map = {
            ToolbarButton.PAUSE: on_pause,
            ToolbarButton.LANDMARKS: on_landmarks,
            ToolbarButton.QUIT: on_quit,
        }
        self._state_getters = {
            ToolbarButton.PAUSE: paused_getter,
            ToolbarButton.LANDMARKS: landmarks_getter,
        }
        self._label_map = {
            ToolbarButton.PAUSE: "Pause",
            ToolbarButton.LANDMARKS: "Landmarks",
            ToolbarButton.QUIT: "Quit",
        }
        self._color_map = {
            ToolbarButton.QUIT: np.array([40, 40, 140]),
        }

        # visual parameters
        self._font = cv2.FONT_HERSHEY_SIMPLEX
        self._font_scale = 0.55
        self._font_thickness = 1
        self._padding_x = 16
        self._padding_y = 8
        self._button_gap = 12
        self._toolbar_bg = (40, 35, 35)

        # runtime layout
        self._buttons: list[dict] = []
        self._toolbar_height = 60
        self._layout_computed = False
        self._last_frame_width = 0
        self._last_frame_height = 0

        cv2.namedWindow(self.window_name)
        cv2.setMouseCallback(self.window_name, self._on_mouse)

        logger.info("FrameViewer initialized.")

    def display_frame(self, frame_bgr: np.ndarray):
        """
        Renders the given frame and draws the toolbar below it.

        Args:
            frame_bgr: The image (BGR) to be shown in the window.
        """
        if frame_bgr is None:
            return

        h, w = frame_bgr.shape[:2]
        self._last_frame_height = h
        self._last_frame_width = w

        if not self._layout_computed:
            self._compute_layout(w)
            self._layout_computed = True

        toolbar = np.zeros((self._toolbar_height, w, 3), dtype=np.uint8)
        toolbar[:] = self._toolbar_bg

        for btn in self._buttons:
            btn_id = btn["id"]

            getter = self._state_getters.get(btn_id)
            is_active = bool(getter()) if getter else False

            label = self._label_map[btn_id]
            self._draw_button(toolbar, btn["rect"], label, is_active, btn_id)

        combined = np.vstack([frame_bgr, toolbar])
        cv2.imshow(self.window_name, combined)
        cv2.waitKey(self.wait_delay_ms)

    def cleanup(self):
        """
        Closes the OpenCV window and releases window resources.
        """
        cv2.destroyAllWindows()
        logger.info("OpenCV windows destroyed.")

    def _compute_layout(self, width: int):
        """
        Calculates button sizes from the longest label (across all states)
        and centers them horizontally.

        Args:
            width: The width of the current video frame.
        """
        all_labels = list(self._label_map.values())

        max_text_w, max_text_h = 0, 0
        for text in all_labels:
            (tw, th), _ = cv2.getTextSize(
                text, self._font, self._font_scale, self._font_thickness
            )
            max_text_w = max(max_text_w, tw)
            max_text_h = max(max_text_h, th)

        btn_w = max_text_w + 2 * self._padding_x
        btn_h = max_text_h + 2 * self._padding_y

        num_buttons = len(self._label_map)
        total_buttons_w = btn_w * num_buttons
        total_gaps_w = self._button_gap * (num_buttons - 1)
        total_w = total_buttons_w + total_gaps_w

        start_x = max(10, (width - total_w) // 2)

        top_pad, bottom_pad = 10, 10
        y1, y2 = top_pad, top_pad + btn_h
        self._toolbar_height = btn_h + top_pad + bottom_pad

        self._buttons = []
        x = start_x
        for btn_id in self._label_map.keys():
            rect = (x, y1, x + btn_w, y2)
            self._buttons.append(
                {
                    "id": btn_id,
                    "rect": rect,
                }
            )
            x += btn_w + self._button_gap

    def _on_mouse(self, event, x, y, flags, param):
        """
        Mouse callback to detect clicks on toolbar buttons.

        Args:
            event: The OpenCV mouse event.
            x: X coordinate of the mouse in the window.
            y: Y coordinate of the mouse in the window.
            flags: Event flags (unused).
            param: Extra data (unused).
        """
        if event != cv2.EVENT_LBUTTONDOWN or y < self._last_frame_height:
            return

        toolbar_x = x
        toolbar_y = y - self._last_frame_height

        for btn in self._buttons:
            x1, y1, x2, y2 = btn["rect"]
            if x1 <= toolbar_x <= x2 and y1 <= toolbar_y <= y2:
                self._trigger(btn["id"])
                break

    def _trigger(self, btn_id: ToolbarButton):
        """
        Dispatches a toolbar button click to the corresponding callback.

        Args:
            btn_id: The identifier of the pressed toolbar button.
        """
        action = self._action_map.get(btn_id)
        if action:
            action()

    def _draw_button(
        self,
        panel: np.ndarray,
        rect: tuple[int, int, int, int],
        label: str,
        active: bool,
        btn_id: ToolbarButton,
    ):
        """Draws a single rectangular button."""
        x1, y1, x2, y2 = rect
        w, h = x2 - x1, y2 - y1

        base_color = np.array([70, 70, 75])
        active_color = np.array([0, 160, 0])
        text_color = (230, 230, 230)

        color = self._color_map.get(btn_id, active_color if active else base_color)

        # shadow
        # cv2.rectangle(panel, (x1 + 2, y1 + 2), (x2 + 2, y2 + 2), (20, 20, 20), -1)

        # main rect
        cv2.rectangle(panel, (x1, y1), (x2, y2), color.tolist(), -1)

        # outline
        cv2.rectangle(panel, (x1, y1), (x2, y2), (200, 200, 200), 1)

        (tw, th), _ = cv2.getTextSize(
            label, self._font, self._font_scale, self._font_thickness
        )
        tx = x1 + (w - tw) // 2
        ty = y1 + (h + th) // 2
        cv2.putText(
            panel,
            label,
            (tx, ty),
            self._font,
            self._font_scale,
            text_color,
            self._font_thickness,
            lineType=cv2.LINE_AA,
        )
