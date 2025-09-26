import mediapipe as mp
import time
import argparse
import logging
import cv2

from face_analysis_engine import FaceAnalysisEngine
from face_data_sender import FaceDataSender
from webcam_stream import WebcamStream
from helpers import draw_landmarks_on_image, draw_centered_text

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    datefmt="%H:%M:%S"
)
logger = logging.getLogger(__name__)


class EchoFaceClient:
    """
    EchoFaceClient encapsulates a webcam-based face tracking client
    that streams blendshape and landmark data over UDP.

    The client performs the core tasks: real-time face landmark and blendshape detection using MediaPipe,
    continuous streaming of this processed data to a specified UDP server,
    and a local webcam display that includes an optional landmark
    overlay and a user-controlled pause/resume function.
    """

    def __init__(self, ip: str, port: int, show_landmarks: bool = True,
                 camera_index: int = 0, target_fps: int = 30):
        """
        Initializes the client, including configuration of UDP sender, webcam, and face engine.

        Args:
            ip: The UDP server IP address.
            port: The UDP server port.
            show_landmarks: A flag to determine whether to overlay
                landmarks on the webcam feed.
            camera_index: The index of the webcam device to use.
            target_fps: The requested frame rate for the webcam.
        """
        self.face_data_sender = FaceDataSender(ip, port)
        self.show_landmarks = show_landmarks

        self.webcam_stream = WebcamStream(
            camera_index=camera_index,
            window_name="Webcam Feed (Client)",
            target_fps=target_fps
        )

        self.engine = FaceAnalysisEngine(on_new_result_callback=self.face_data_sender.send_face_data)

        # Pause-related state
        self.paused = False
        self.last_frame = None
        self.paused_frame_displayed = False

    def toggle_pause(self):
        """
        Toggles the paused state and resets the paused frame display flag.
        """
        self.paused = not self.paused
        self.paused_frame_displayed = False
        # logger.info(f"Toggled pause state. Current state: {'PAUSED' if self.paused else 'RUNNING'}")

    def run(self):
        """
        Main loop of the client.

        Starts the UDP socket, webcam, and face engine,
        and handles frame capture, face detection, UDP streaming,
        pause/resume logic, and frame display.
        """
        # Start UDP socket
        self.face_data_sender.start()
        logger.info("FaceDataSender started.")

        if not self.webcam_stream.start():
            logger.error("Failed to start webcam. Exiting.")
            self.face_data_sender.close()
            return

        self.engine.start()
        logger.info("Press 'q' to quit, 'p' to pause/resume...")
        logger.info(f"Webcam reports actual FPS: {self.webcam_stream.get_actual_fps():.2f}.")

        try:
            while True:
                key = self.webcam_stream.check_key()
                if key == ord("q"):
                    logger.info(" 'q' pressed. Exiting...")
                    break
                elif key == ord("p"):
                    self.toggle_pause()

                if not self.paused:
                    self._process_frame()
                else:
                    self._display_paused_frame()
        finally:
            self._cleanup()

    def _process_frame(self):
        """
        Processes a single webcam frame.

        The steps include capturing the frame, converting it to RGB format,
        submitting it to the face engine for asynchronous detection, sending the results via UDP,
        and finally displaying the processed frame in the window.
        """
        ret, frame_bgr = self.webcam_stream.read_frame()
        if not ret:
            logger.warning("Failed to grab frame. Exiting...")
            raise RuntimeError("Webcam frame read failed")

        self.last_frame = frame_bgr
        self.paused_frame_displayed = False

        rgb_frame = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)
        frame_timestamp_ms = time.time_ns() // 1_000_000
        self.engine.detect_async(mp_image, frame_timestamp_ms)

        frame_to_display = frame_bgr
        if self.show_landmarks and self.engine.latest_detection_result:
            annotated_rgb_frame = draw_landmarks_on_image(rgb_frame, self.engine.latest_detection_result)
            frame_to_display = cv2.cvtColor(annotated_rgb_frame, cv2.COLOR_RGB2BGR)

        self.webcam_stream.display_frame(frame_to_display)

    def _display_paused_frame(self):
        """
        Displays the last captured frame with a "PAUSED" overlay.
        Ensures the paused frame is drawn only once.
        """
        if self.last_frame is None:
            return

        if not self.paused_frame_displayed:
            frame_to_display = self.last_frame.copy()
            self.engine.latest_detection_result = None
            frame_to_display = draw_centered_text(frame_to_display, "PAUSED")
            self.webcam_stream.display_frame(frame_to_display)
            self.paused_frame_displayed = True

        time.sleep(1)  # Reduce CPU usage while paused

    def _cleanup(self):
        """
        Releases all resources cleanly: webcam, face engine, and UDP sender.
        """
        self.webcam_stream.release()
        self.engine.stop()
        self.face_data_sender.close()
        logger.info("Client application closed cleanly.")


def parse_arguments() -> argparse.Namespace:
    """
    Parses command-line arguments for the client application.
    Provides defaults for local testing.
    """
    parser = argparse.ArgumentParser(description="Face Landmarker client streaming blendshapes and landmarks via UDP.")
    parser.add_argument("--ip", type=str, default="127.0.0.1",
                        help="UDP server IP address (default: 127.0.0.1).")
    parser.add_argument("--port", type=int, default=12345,
                        help="UDP server port (default: 12345).")
    parser.add_argument("--show-landmarks", action="store_true",
                        help="Display face landmarks on the webcam feed.")
    return parser.parse_args()


def main():
    args = parse_arguments()
    client = EchoFaceClient(ip=args.ip, port=args.port, show_landmarks=args.show_landmarks)
    client.run()


if __name__ == "__main__":
    main()
