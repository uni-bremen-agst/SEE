import os
os.environ["OPENCV_VIDEOIO_MSMF_ENABLE_HW_TRANSFORMS"] = "0"

import mediapipe as mp
import time
import argparse
import logging
import cv2
import numpy as np
from face_analyzer import FaceAnalyzer
from face_data_sender import FaceDataSender
from video_io import (
    BaseStream,
    WebcamStream,
    VideoStream,
    PlaybackClock,
    FrameViewer,
)
from helpers import draw_landmarks_on_image


logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    datefmt="%H:%M:%S",
)
logger = logging.getLogger(__name__)


class EchoFaceClient:
    """
    Orchestrates stream capture, face analysis, data sending, and display.
    GUI layout is handled by FrameViewer, the client only supplies callbacks.
    """

    def __init__(
        self,
        ip: str,
        port: int,
        headless_mode: bool = False,
        camera_index: int = 0,
        video_path: str | None = None,
        playback_fps: float | None = None,
    ):
        """
        Initializes the client, sets up the network sender, selects the video source,
        and initializes the frame viewer.

        Args:
            ip: The UDP server IP address to send data to.
            port: The UDP server port.
            headless_mode: If True, runs without any display window (no OpenCV output).
            camera_index: The numerical index of the webcam device to use (e.g., 0).
                          This is ignored if video_path is provided.
            video_path: The filesystem path to a video file to process. If provided,
                        it is used instead of the webcam.
            playback_fps: Optional playback FPS override. If provided, this value is
                used to drive the real-time playback clock instead of the source FPS.

        Raises:
            RuntimeError: If the selected video source (webcam or file) fails to start.
        """
        self.headless_mode = headless_mode
        self.landmarks_enabled = False
        self.paused = False
        self._running = False
        self.last_frame = None

        self.playback_clock = None
        self.frame_viewer = None
        self.face_data_sender = FaceDataSender(ip, port)
        self.face_analyzer = FaceAnalyzer(
            on_new_result_callback=self.face_data_sender.send_face_data
        )

        if video_path:
            logger.info(f"Initializing VideoStream for file: {video_path}")
            self.video_source: BaseStream = VideoStream(video_path=video_path)
        else:
            logger.info(f"Initializing WebcamStream for index: {camera_index}")
            self.video_source: BaseStream = WebcamStream(camera_index=camera_index)

        if not self.video_source.start():
            raise RuntimeError("Failed to start video source.")

        if playback_fps is not None and playback_fps > 0:
            logger.info(f"Overriding playback FPS to {playback_fps}")
            self.playback_clock = PlaybackClock(playback_fps)

        if not self.headless_mode:
            self.frame_viewer = FrameViewer(
                window_name="EchoFace Client",
                on_pause=self.toggle_pause,
                on_landmarks=self.toggle_landmarks,
                on_quit=self._quit_from_ui,
                paused_getter=lambda: self.paused,
                landmarks_getter=lambda: self.landmarks_enabled,
            )

        logger.info(f"Headless mode is {'ON' if self.headless_mode else 'OFF'}.")

    def run(self):
        """
        Starts the networking and face analysis components and enters the main
        processing loop. The loop continues until the user quits or the stream ends.
        """
        self.face_data_sender.start()
        self.face_analyzer.start()

        if self.headless_mode:
            logger.info("Headless mode: processing without display.")

        self._running = True

        try:
            while self._running:
                if not self.paused:
                    self._process_frame()
                    if self.playback_clock is not None:
                        self.playback_clock.wait_for_next()
                else:
                    self._display_paused_frame()
                    time.sleep(0.3)
        except StopIteration:
            logger.info("Stream ended or failed. Exiting loop.")
        except KeyboardInterrupt:
            logger.info("Keyboard interrupt received. Exiting...")
        finally:
            self._cleanup()

    def _quit_from_ui(self):
        """
        Handles the Quit action triggered from the GUI and exits the main loop.
        """
        logger.info("Quit pressed in GUI.")
        self._running = False

    def _process_frame(self):
        """
        Reads a frame from the video source, runs face landmark detection,
        sends the result, and displays the (optionally annotated) frame.
        """
        ret, frame_bgr = self.video_source.read_frame()
        if not ret:
            logger.warning("Could not read frame from source.")
            raise StopIteration("End of stream or read error.")

        self.last_frame = frame_bgr

        rgb_frame = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)
        frame_timestamp_ms = time.time_ns() // 1_000_000
        self.face_analyzer.detect_async(mp_image, frame_timestamp_ms)

        if not self.headless_mode:
            frame_to_display = frame_bgr
            if self.landmarks_enabled and self.face_analyzer.latest_detection_result:
                annotated_rgb_frame = draw_landmarks_on_image(
                    rgb_frame, self.face_analyzer.latest_detection_result
                )
                frame_to_display = cv2.cvtColor(annotated_rgb_frame, cv2.COLOR_RGB2BGR)

            self.frame_viewer.display_frame(frame_to_display)

    def _display_paused_frame(self):
        """
        Displays the last captured frame to keep the
        OpenCV window responsive while the stream is paused.
        """
        if self.headless_mode:
            return

        if self.last_frame is None:
            w, h = self.video_source.get_resolution()
            frame = np.zeros((h, w, 3), dtype=np.uint8)
            self.frame_viewer.display_frame(frame)
            return

        self.frame_viewer.display_frame(self.last_frame)

    def toggle_pause(self):
        """
        Toggles the paused state of the client. When paused, frames are not processed
        but the GUI remains active.
        """
        self.paused = not self.paused
        logger.info(f"Stream {'PAUSED' if self.paused else 'RESUMED'}")

        if not self.paused and self.playback_clock is not None:
            self.playback_clock.reset()

    def toggle_landmarks(self):
        """
        Toggles drawing of face landmarks on the displayed frames.
        """
        self.landmarks_enabled = not self.landmarks_enabled
        logger.info(f"Landmarks {'ENABLED' if self.landmarks_enabled else 'DISABLED'}")

    def _cleanup(self):
        """
        Releases all resources (video source, analyzer, sender, GUI windows)
        before exiting the application.
        """
        self.video_source.release()
        self.face_analyzer.stop()
        self.face_data_sender.close()
        if self.frame_viewer:
            self.frame_viewer.cleanup()
        logger.info("Client closed cleanly.")


def parse_arguments() -> argparse.Namespace:
    """
    Parses command-line arguments for configuring the client.

    Returns:
        An argparse.Namespace containing the parsed arguments.
    """
    parser = argparse.ArgumentParser(
        description="Face Landmarker client streams blendshape and landmark data via UDP."
    )

    parser.add_argument("--ip", type=str, default="127.0.0.1",
                        help="IP address of the UDP server to send face landmarks/blendshape data to.")
    parser.add_argument("--port", type=int, default=12345, help="Port number of the UDP server for sending face data.")
    parser.add_argument("--camera-index", type=int, default=0,
                        help="Index of the webcam to use as input (default is 0).")
    parser.add_argument("--video-path", type=str, default=None,
                        help="Path to a video file to use as input instead of a webcam.")
    parser.add_argument("--headless", action="store_true",
                        help="Run in headless mode (no display window); data is streamed but not rendered.")
    parser.add_argument("--fps", type=float, default=None,
                        help="Override playback FPS used for timing. Defaults to input source FPS if unset.")
    return parser.parse_args()


def main():
    """
    Entry point for the client application. Creates and runs the EchoFaceClient
    instance with arguments parsed from the command line.
    """
    args = parse_arguments()
    try:
        client = EchoFaceClient(
            ip=args.ip,
            port=args.port,
            headless_mode=args.headless,
            camera_index=args.camera_index,
            video_path=args.video_path,
            playback_fps=args.fps,
        )
        client.run()
    except RuntimeError as e:
        logger.error(f"Failed to start application: {e}")
        cv2.destroyAllWindows()


if __name__ == "__main__":
    main()
