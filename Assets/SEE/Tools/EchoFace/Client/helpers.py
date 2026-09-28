import mediapipe as mp
import numpy as np  # For np.copy
import cv2

# Import MediaPipe drawing utilities and protobuf for landmark conversion
from mediapipe.framework.formats import landmark_pb2


def draw_landmarks_on_image(rgb_image: np.ndarray, detection_result: mp.tasks.vision.FaceLandmarkerResult) -> np.ndarray:
    """
    Draws face landmarks (tesselation, contours, irises) on the given RGB image.

    Args:
        rgb_image (np.ndarray): The input image in RGB format (NumPy array).
        detection_result (mp.tasks.vision.FaceLandmarkerResult): The detection result
                                                                 containing face landmarks.

    Returns:
        np.ndarray: The annotated image in RGB format.
    """
    if not detection_result or not detection_result.face_landmarks:
        return rgb_image  # Return original image if no landmarks detected

    annotated_image = np.copy(rgb_image)
    for face_landmarks in detection_result.face_landmarks:
        # Convert MediaPipe landmarks to NormalizedLandmarkList proto for drawing_utils
        face_landmarks_proto = landmark_pb2.NormalizedLandmarkList()
        face_landmarks_proto.landmark.extend([
            landmark_pb2.NormalizedLandmark(x=lm.x, y=lm.y, z=lm.z) for lm in face_landmarks
        ])

        # Draw face tesselation
        mp.solutions.drawing_utils.draw_landmarks(
            image=annotated_image,
            landmark_list=face_landmarks_proto,
            connections=mp.solutions.face_mesh.FACEMESH_TESSELATION,
            landmark_drawing_spec=None,  # Use default style for landmarks
            connection_drawing_spec=mp.solutions.drawing_styles.get_default_face_mesh_tesselation_style()
        )

        # Draw face contours
        mp.solutions.drawing_utils.draw_landmarks(
            image=annotated_image,
            landmark_list=face_landmarks_proto,
            connections=mp.solutions.face_mesh.FACEMESH_CONTOURS,
            landmark_drawing_spec=None,
            connection_drawing_spec=mp.solutions.drawing_styles.get_default_face_mesh_contours_style()
        )

        # Draw face irises
        mp.solutions.drawing_utils.draw_landmarks(
            image=annotated_image,
            landmark_list=face_landmarks_proto,
            connections=mp.solutions.face_mesh.FACEMESH_IRISES,
            landmark_drawing_spec=None,
            connection_drawing_spec=mp.solutions.drawing_styles.get_default_face_mesh_iris_connections_style()
        )
    return annotated_image


def draw_centered_text(frame, text: str,
                       color=(0, 0, 255),
                       font_scale=1.5,
                       thickness=3):
    font = cv2.FONT_HERSHEY_SIMPLEX
    text_size, _ = cv2.getTextSize(text, font, font_scale, thickness)
    text_x = (frame.shape[1] - text_size[0]) // 2
    text_y = (frame.shape[0] + text_size[1]) // 2
    cv2.putText(frame, text, (text_x, text_y), font,
                font_scale, color, thickness, cv2.LINE_AA)
    return frame
