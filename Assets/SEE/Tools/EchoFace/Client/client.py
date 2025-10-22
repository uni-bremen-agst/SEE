import mediapipe as mp
import time
import argparse
import logging
import cv2

from face_analysis_engine import FaceAnalysisEngine
from face_data_sender import FaceDataSender
from video_io import BaseStream, WebcamStream, VideoStream, FrameViewer
from helpers import draw_landmarks_on_image, draw_centered_text

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    datefmt="%H:%M:%S"
)
logger = logging.getLogger(__name__)


class EchoFaceClient:
    """
    EchoFaceClient orchestrates stream capture, face analysis, data sending, and display.
    """

    def __init__(self, ip: str, port: int, show_landmarks: bool = False,
                 camera_index: int = 0, video_path: str = None, target_fps: int = 30):
        """
        Initializes the client, sets up the network sender, selects the video source,
        and initializes the frame viewer.

        Args:
            ip: The UDP server IP address to send data to.
            port: The UDP server port.
            show_landmarks: A flag to determine whether to overlay
                            detected face landmarks on the stream display window.
            camera_index: The numerical index of the webcam device to use (e.g., 0).
                          This is ignored if video_path is provided.
            video_path: The filesystem path to a video file to process. If provided,
                        it is used instead of the webcam.
            target_fps: The requested frame rate (FPS) for the stream and processing.

        Raises:
            RuntimeError: If the selected video source (webcam or file) fails to start.
        """

        self.face_data_sender = FaceDataSender(ip, port)
        self.show_landmarks = show_landmarks

        # 1. Initialize I/O Stream Source
        if video_path:
            logger.info(f"Initializing VideoStream for file: {video_path}")
            self.video_source: BaseStream = VideoStream(
                video_path=video_path,
                target_fps=target_fps
            )
        else:
            logger.info(f"Initializing WebcamStream for index: {camera_index}")
            self.video_source: BaseStream = WebcamStream(
                camera_index=camera_index,
                target_fps=target_fps
            )

        # Must start the source to get the actual FPS for display timing
        if not self.video_source.start():
            raise RuntimeError("Failed to start video source.")

        # 2. Initialize Display/UI Controller using the actual stream FPS
        self.frame_viewer = FrameViewer(
            window_name="Stream Feed (Client)",
            stream_fps=self.video_source.get_actual_fps()
        )

        self.engine = FaceAnalysisEngine(on_new_result_callback=self.face_data_sender.send_face_data)

        # Pause-related state
        self.paused = False
        self.last_frame = None
        self.paused_frame_displayed = False

        logger.info(f"Stream running at {self.video_source.get_actual_fps():.2f} FPS.")

    def toggle_pause(self):
        """Toggles the paused state of the client."""
        self.paused = not self.paused
        self.paused_frame_displayed = False
        logger.info(f"Application {'PAUSED' if self.paused else 'RESUMED'}")

    def run(self):
        """
        The main execution loop of the client.
        It reads frames, processes them, sends data, and manages the display until terminated.
        """

        self.face_data_sender.start()
        self.engine.start()
        logger.info("Press 'q' to quit, 'p' to pause/resume...")

        try:
            while True:
                key = self.frame_viewer.check_key()
                if key == ord("q"):
                    logger.info(" 'q' pressed. Exiting...")
                    break
                elif key == ord("p"):
                    self.toggle_pause()

                if not self.paused:
                    self._process_frame()
                else:
                    self._display_paused_frame()
                    time.sleep(1)  # Reduce CPU usage when paused
        except StopIteration:
            logger.info("Stream ended or failed. Breaking main loop.")
        except KeyboardInterrupt:  # Handle Ctrl+C for headless operation
            logger.info("Keyboard interrupt received. Exiting...")
        finally:
            self._cleanup()

    def _process_frame(self):
        """Reads a frame from the source, runs face detection, and displays the result."""
        ret, frame_bgr = self.video_source.read_frame()
        if not ret:
            logger.warning("Failed to grab frame.")
            raise StopIteration("Stream read failed or video end reached.")

        self.last_frame = frame_bgr
        self.paused_frame_displayed = False

        # Conversion and Detection
        rgb_frame = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)
        frame_timestamp_ms = time.time_ns() // 1_000_000
        self.engine.detect_async(mp_image, frame_timestamp_ms)

        # Display Logic
        frame_to_display = frame_bgr
        if self.show_landmarks and self.engine.latest_detection_result:
            annotated_rgb_frame = draw_landmarks_on_image(rgb_frame, self.engine.latest_detection_result)
            frame_to_display = cv2.cvtColor(annotated_rgb_frame, cv2.COLOR_RGB2BGR)

        self.frame_viewer.display_frame(frame_to_display)

    def _display_paused_frame(self):
        """Displays the last captured frame with a "PAUSED" overlay."""
        if self.last_frame is None:
            return

        if not self.paused_frame_displayed:
            frame_to_display = self.last_frame.copy()
            frame_to_display = draw_centered_text(frame_to_display, "PAUSED")
            self.frame_viewer.display_frame(frame_to_display)
            self.paused_frame_displayed = True

    def _cleanup(self):
        """Releases all resources cleanly, including the stream, engine, sender, and display window."""
        self.video_source.release()
        self.engine.stop()
        self.face_data_sender.close()
        self.frame_viewer.cleanup()
        logger.info("Client application closed cleanly.")


def parse_arguments() -> argparse.Namespace:
    """
    Parses command-line arguments for the client application.
    Provides defaults for local testing.
    """
    parser = argparse.ArgumentParser(
        description="Face Landmarker client streams blendshape and landmark data via UDP."
    )

    # Network Arguments
    parser.add_argument("--ip", type=str, default="127.0.0.1",
                        help="The UDP server IP address to send data to (default: 127.0.0.1).")
    parser.add_argument("--port", type=int, default=12345,
                        help="The UDP server port (default: 12345).")

    # Source Arguments
    parser.add_argument("--camera-index", type=int, default=0,
                        help="The index of the webcam device to use (default: 0). This is ignored if --video-path is specified.")
    parser.add_argument("--video-path", type=str, default=None,
                        help="Path to a video file to process instead of a live webcam feed. The video will loop.")

    # Display/Processing Arguments
    parser.add_argument("--show-landmarks", action="store_true",
                        help="If set, overlays the detected face landmarks on the stream display window.")
    parser.add_argument("--target-fps", type=int, default=30,
                        help="The target frame rate (FPS) for the stream and processing. Used as a fallback if the video source FPS is unknown.")

    return parser.parse_args()


def main():
    args = parse_arguments()
    try:
        client = EchoFaceClient(
            ip=args.ip,
            port=args.port,
            show_landmarks=args.show_landmarks,
            camera_index=args.camera_index,
            video_path=args.video_path,
            target_fps=args.target_fps
        )
        client.run()
    except RuntimeError as e:
        logger.error(f"Application failed to start or run: {e}")
        cv2.destroyAllWindows()


if __name__ == "__main__":
    main()
