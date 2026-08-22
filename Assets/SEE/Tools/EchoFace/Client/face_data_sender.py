import socket
import json
import logging
from typing import Dict, Optional, List

logger = logging.getLogger(__name__)

# Blendshape order must match BlendshapeOrder.Names in Unity (alphabetical).
BLENDSHAPE_ORDER = (
    "_neutral",
    "browDownLeft",
    "browDownRight",
    "browInnerUp",
    "browOuterUpLeft",
    "browOuterUpRight",
    "cheekPuff",
    "cheekSquintLeft",
    "cheekSquintRight",
    "eyeBlinkLeft",
    "eyeBlinkRight",
    "eyeLookDownLeft",
    "eyeLookDownRight",
    "eyeLookInLeft",
    "eyeLookInRight",
    "eyeLookOutLeft",
    "eyeLookOutRight",
    "eyeLookUpLeft",
    "eyeLookUpRight",
    "eyeSquintLeft",
    "eyeSquintRight",
    "eyeWideLeft",
    "eyeWideRight",
    "jawForward",
    "jawLeft",
    "jawOpen",
    "jawRight",
    "mouthClose",
    "mouthDimpleLeft",
    "mouthDimpleRight",
    "mouthFrownLeft",
    "mouthFrownRight",
    "mouthFunnel",
    "mouthLeft",
    "mouthLowerDownLeft",
    "mouthLowerDownRight",
    "mouthPressLeft",
    "mouthPressRight",
    "mouthPucker",
    "mouthRight",
    "mouthRollLower",
    "mouthRollUpper",
    "mouthShrugLower",
    "mouthShrugUpper",
    "mouthSmileLeft",
    "mouthSmileRight",
    "mouthStretchLeft",
    "mouthStretchRight",
    "mouthUpperUpLeft",
    "mouthUpperUpRight",
    "noseSneerLeft",
    "noseSneerRight",
)

# Landmark IDs used by EchoFace, sorted by numeric size:
# 152 (Chin), 226 (RightUpperEyelid), 446 (LeftUpperEyelid)
LM_ORDER = (152, 226, 446)


class FaceDataSender:
    """
    Handles sending blendshape, landmark, and timestamp data over UDP.

    Data is encoded as a compact JSON object:
      {
        "bs": [ ... ],      # blendshapes in fixed alphabetical order
        "lm": [[x,y,z],...],# 3 landmark vectors (Chin, RightUpperEyelid, LeftUpperEyelid)
        "ts": 123456789     # timestamp in ms
      }
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
        self.sock: Optional[socket.socket] = None
        logger.info(f"FaceDataSender configured for {target_ip}:{target_port}")

    def start(self):
        """
        Creates the UDP socket.
        """
        if self.sock is None:
            self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            logger.info(f"UDP socket created for {self.target_ip}:{self.target_port}")

    def _serialize_face_data(
        self,
        blendshape_data: Dict[str, float],
        landmarks: Dict[int, Dict[str, float]],
        timestamp_ms: int,
    ) -> Optional[bytes]:
        """
        Serializes blendshape, landmark, and timestamp data into a compact JSON byte string.

        Args:
            blendshape_data: Dictionary of blendshape names to float scores.
            landmarks: Dictionary mapping landmark indices to their 'x', 'y', 'z' coordinates.
            timestamp_ms: Unix timestamp in milliseconds.

        Returns:
            UTF-8 encoded JSON bytes, or None if there is no data to send.
        """
        if not blendshape_data and not landmarks:
            return None

        # 1) Build ordered list of blendshape values (alphabetical order)
        blendshape_values = [float(blendshape_data.get(name, 0.0)) for name in BLENDSHAPE_ORDER]

        # 2) Build landmark list: [[x,y,z], [x,y,z], [x,y,z]]
        landmark_list = []
        for lm_id in LM_ORDER:
            coords = landmarks.get(lm_id)
            if coords is not None:
                x = float(coords.get("x", 0.0))
                y = float(coords.get("y", 0.0))
                z = float(coords.get("z", 0.0))
            else:
                x = y = z = 0.0
            landmark_list.append([x, y, z])

        combined_data = {
            "bs": blendshape_values,
            "lm": landmark_list,
            "ts": timestamp_ms,
        }

        try:
            json_data = json.dumps(combined_data, separators=(",", ":"))
            return json_data.encode("utf-8")
        except Exception as e:
            logger.error(f"Failed to serialize face data: {e}")
            return None

    def send_face_data(
        self,
        blendshape_data: Dict[str, float],
        landmarks: Dict[int, Dict[str, float]],
        timestamp_ms: int,
    ):
        """
        Sends blendshape, landmark, and timestamp data as compact JSON over UDP.

        Args:
            blendshape_data: Dictionary of blendshape names to float scores.
            landmarks: Dictionary mapping landmark indices to their 'x', 'y', 'z' coordinates.
            timestamp_ms: Unix timestamp in milliseconds.
        """
        if self.sock is None:
            logger.warning("UDP socket not started; call start() before sending data.")
            return

        payload = self._serialize_face_data(blendshape_data, landmarks, timestamp_ms)
        if payload is None:
            logger.debug("No data to send; skipping UDP packet.")
            return

        try:
            self.sock.sendto(payload, (self.target_ip, self.target_port))
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
