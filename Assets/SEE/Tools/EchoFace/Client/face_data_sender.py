import socket
import json
import logging
from typing import List, Dict, Any

logger = logging.getLogger(__name__)


class FaceDataSender:
    """
    Handles sending blendshape and landmark data over UDP.

    Data is encoded as a JSON string containing both blendshapes and landmarks.
    """

    def __init__(self, target_ip: str, target_port: int):
        """
        Initializes the sender configuration but does not open the UDP socket yet.

        Args:
            target_ip: The IP address of the UDP server.
            target_port: The port of the UDP server.
        """
        self.target_ip = target_ip
        self.target_port = target_port
        self.sock = None
        logger.info(f"FaceDataSender configured for {target_ip}:{target_port}")

    def start(self):
        """
        Creates the UDP socket.
        """
        if self.sock is None:
            self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            logger.info(f"UDP socket created for {self.target_ip}:{self.target_port}")

    def send_face_data(self, blendshape_data: Dict[str, float], landmarks: Dict[int, Dict[str, float]]):
        """
        Sends blendshape and landmark data as JSON over UDP.

        Args:
            blendshape_data: Dictionary of blendshape names to float scores.
            landmarks: Dictionary mapping landmark indices to their 'x', 'y', 'z' coordinates.
        """
        if self.sock is None:
            logger.warning("UDP socket not started; call start() before sending data.")
            return

        if not blendshape_data and not landmarks:
            logger.debug("No data to send; skipping UDP packet.")
            return

        combined_data = {
            "blendshapes": blendshape_data,
            "landmarks": landmarks
        }

        try:
            json_data = json.dumps(combined_data)
            self.sock.sendto(json_data.encode('utf-8'), (self.target_ip, self.target_port))
        except Exception as e:
            logger.error(f"Failed to send UDP packet: {e}")

    def close(self):
        """
        Closes the UDP socket, releasing system resources.
        """
        if self.sock:
            self.sock.close()
            self.sock = None
            logger.info("UDP socket closed.")
