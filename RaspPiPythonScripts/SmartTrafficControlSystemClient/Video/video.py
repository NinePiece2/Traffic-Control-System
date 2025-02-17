import cv2
import subprocess
import threading
import time
from collections import deque
import requests
import Config.config as config
from jwt.exceptions import DecodeError
from datetime import datetime, timezone
import urllib.parse
import jwt
import platform
import numpy as np  # Needed for YOLO processing

# Attempt to import Picamera2 on Linux
try:
    if platform.system() == 'Linux':
        from picamera2 import Picamera2
except ImportError:
    pass


class UploadClip:
    def __init__(self, filename):
        self.filename = filename
        self.token = None

    def _get_new_token(self):
        token_url = config.Config().get("Video_URL") + "Token/GetToken?userID="
        key = config.Config().get("API_KEY")
        response_token = requests.get(token_url + urllib.parse.quote(key, safe=''))
        if response_token.status_code == 200:
            self.token = response_token.text
        else:
            print(f"Failed to get token. Status code: {response_token.status_code}")
            print(f"Response Content: {response_token.text}")
            self.token = None

    def _ensure_valid_token(self):
        if not self.token:
            self._get_new_token()

        try:
            decoded_token = jwt.decode(self.token, options={"verify_signature": False})
            exp = decoded_token.get("exp")
            expiration_time = datetime.fromtimestamp(exp, tz=timezone.utc)
            current_time = datetime.now(tz=timezone.utc)

            if current_time >= expiration_time:
                self._get_new_token()
            elif (expiration_time - current_time).total_seconds() <= 300:
                self._get_new_token()

        except DecodeError as e:
            print(f"Invalid token format: {e}")
            return False
        except Exception as e:
            print(f"Error while decoding token in _ensure_valid_token: {e}")
            return False

    def upload_clip(self, device_id):
        self._ensure_valid_token()
        url = f"{config.Config().get('Video_URL')}Clip/UploadFile"

        with open(self.filename, 'rb') as file:
            headers = {'Authorization': f'Bearer {self.token}'}
            files = {'file': (self.filename.split('/')[-1], file)}
            data = {'deviceID': device_id}

            response = requests.post(url, headers=headers, files=files, data=data)

            if response.status_code == 200:
                print("File uploaded successfully!")
                print("Response:", response.json())
            else:
                print("Failed to upload file.")
                print("Status Code:", response.status_code)
                print("Response:", response.text)


class Streamer:
    def __init__(self, rtmp_url, width, height, fps):
        self.rtmp_url = rtmp_url
        self.width = width
        self.height = height
        self.process = None
        self.fps = fps

    def start_stream(self):
        ffmpeg_path = './ffmpeg'
        system_name = platform.system()
        if system_name == 'Linux':
            ffmpeg_path = 'ffmpeg'
        elif system_name == 'Windows':
            ffmpeg_path = 'ffmpeg.exe'

        command = [
            ffmpeg_path,
            '-y',
            '-f', 'rawvideo',
            '-pixel_format', 'bgr24',  # Expecting 3-channel BGR
            '-video_size', f"{self.width}x{self.height}",
            '-framerate', str(self.fps),
            '-i', '-',
            '-c:v', 'libx264',
            '-preset', 'veryfast',
            '-f', 'flv',
            self.rtmp_url
        ]
        self.process = subprocess.Popen(command, stdin=subprocess.PIPE)

    def send_frame(self, frame):
        if self.process and self.process.stdin:
            self.process.stdin.write(frame.tobytes())

    def stop_stream(self):
        if self.process:
            if self.process.stdin:
                self.process.stdin.close()
            self.process.wait()


class Recorder:
    def __init__(self, video_capture, width, height, fps):
        """
        video_capture should be an instance of VideoCapture (below),
        which provides a `read()` method returning (ret, frame).
        """
        self.video_capture = video_capture
        self.width = width
        self.height = height
        self.fps = fps
        self.buffer = deque(maxlen=abs(fps) * 20)  # Buffer last 20 seconds
        self.recording = False
        self.writer = None
        self.filename = None

    def add_frame_to_buffer(self, frame):
        """Buffers the frame for up to 20s."""
        self.buffer.append(frame)

    def start_recording(self, filename):
        """Save 20s of buffered frames + 10s of live frames."""
        self.filename = filename
        self.recording = True
        fourcc = cv2.VideoWriter_fourcc(*'mp4v')
        self.writer = cv2.VideoWriter(self.filename, fourcc, self.fps, (self.width, self.height))

        # Write the last 20 seconds first
        for frame in self.buffer:
            self.writer.write(frame)

        # Continue recording live frames for 10 seconds
        threading.Thread(target=self.record_live_frames, daemon=True).start()

    def record_live_frames(self):
        start_time = time.time()
        while self.recording and (time.time() - start_time) < 10:
            ret, frame = self.video_capture.read()
            if ret:
                self.writer.write(frame)
        self.stop_recording()

    def stop_recording(self):
        self.recording = False
        if self.writer:
            self.writer.release()
            self.writer = None
            # Perform upload after done writing
            upload = UploadClip(self.filename)
            upload.upload_clip(config.Config().get("Device_ID"))


class VideoCapture:
    def __init__(self, rtmp_url):
        self.rtmp_url = rtmp_url
        self.running = True

        # Decide between Picamera2 (Linux) and OpenCV VideoCapture (other OS).
        if platform.system() == 'Linux':
            self.use_picamera2 = True
            self.picam2 = Picamera2()
            # Set desired resolution / read from config if needed
            self.width = 640
            self.height = 480
            self.fps = 30  # PiCamera2 approximate

            config_params = self.picam2.create_video_configuration(
                main={"format": 'XRGB8888', "size": (self.width, self.height)}
            )
            self.picam2.configure(config_params)
            self.picam2.start()
        else:
            self.use_picamera2 = False
            self.cap = cv2.VideoCapture(int(config.Config().get("WebcamID")))
            if not self.cap.isOpened():
                raise Exception("Error: Could not open webcam.")
            self.width = int(self.cap.get(cv2.CAP_PROP_FRAME_WIDTH))
            self.height = int(self.cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
            cam_fps = self.cap.get(cv2.CAP_PROP_FPS)
            self.fps = int(cam_fps) if cam_fps > 0 else 30

        self.streamer = Streamer(self.rtmp_url, self.width, self.height, self.fps)
        self.recorder = Recorder(self, self.width, self.height, self.fps)

        # Initialize YOLO for object detection
        try:
            yolo_config_path = config.Config().get("YOLO_CONFIG")  # e.g., "yolov3.cfg"
            yolo_weights_path = config.Config().get("YOLO_WEIGHTS")  # e.g., "yolov3.weights"
            yolo_labels_path = config.Config().get("YOLO_LABELS")      # e.g., "coco.names"            
            self.yolo_net = cv2.dnn.readNetFromDarknet(yolo_config_path, yolo_weights_path)
            self.yolo_net.setPreferableBackend(cv2.dnn.DNN_BACKEND_OPENCV)
            self.yolo_net.setPreferableTarget(cv2.dnn.DNN_TARGET_CPU)
            layer_names = self.yolo_net.getLayerNames()
            layer_names = self.yolo_net.getLayerNames()
            unconnected = self.yolo_net.getUnconnectedOutLayers()

            # Handle different possible return types for getUnconnectedOutLayers()
            if isinstance(unconnected, np.ndarray):
                if unconnected.ndim == 2:
                    self.yolo_output_layers = [layer_names[i[0]-1] for i in unconnected]
                else:  # 1D array
                    self.yolo_output_layers = [layer_names[i-1] for i in unconnected]
            else:
                # If not a numpy array, assume it's a scalar
                self.yolo_output_layers = [layer_names[unconnected-1]]

            with open(yolo_labels_path, "r") as f:
                self.yolo_labels = [line.strip() for line in f.readlines()]
            
            try:
                self.yolo_confidence_threshold = float(config.Config().get("YOLO_CONFIDENCE"))
            except Exception:
                self.yolo_confidence_threshold = 0.5

            try:
                self.yolo_nms_threshold = float(config.Config().get("YOLO_NMS"))
            except Exception:
                self.yolo_nms_threshold = 0.4

            print("YOLO model loaded successfully.")
        except Exception as e:
            print("YOLO not configured or error loading YOLO model:", e)
            self.yolo_net = None

    def read(self):
        """
        Returns (ret, frame).
        If using picamera2 with 'XRGB8888' format, convert BGRA → BGR.
        """
        if self.use_picamera2:
            frame = self.picam2.capture_array()
            frame = cv2.cvtColor(frame, cv2.COLOR_BGRA2BGR)
            return True, frame
        else:
            return self.cap.read()

    def release_capture(self):
        """Stop the camera capture."""
        if self.use_picamera2:
            self.picam2.stop()
        else:
            self.cap.release()

    def record_clip(self, filename):
        self.recorder.start_recording(filename)

    def start(self):
        self.streamer.start_stream()

        while self.running:
            ret, frame = self.read()
            if not ret or frame is None:
                print("Error: Failed to capture frame.")
                break

            # Run YOLO object detection (if available)
            if self.yolo_net is not None:
                # Create a blob from the frame and perform a forward pass
                blob = cv2.dnn.blobFromImage(frame, 1/255.0, (416, 416), swapRB=True, crop=False)
                self.yolo_net.setInput(blob)
                layer_outputs = self.yolo_net.forward(self.yolo_output_layers)

                boxes = []
                confidences = []
                class_ids = []
                (H, W) = frame.shape[:2]

                for output in layer_outputs:
                    for detection in output:
                        scores = detection[5:]
                        class_id = int(np.argmax(scores))
                        confidence = scores[class_id]
                        if confidence > self.yolo_confidence_threshold:
                            box = detection[0:4] * np.array([W, H, W, H])
                            (centerX, centerY, width, height) = box.astype("int")
                            x = int(centerX - (width / 2))
                            y = int(centerY - (height / 2))
                            boxes.append([x, y, int(width), int(height)])
                            confidences.append(float(confidence))
                            class_ids.append(class_id)

                # Apply non-max suppression to suppress weak, overlapping bounding boxes
                idxs = cv2.dnn.NMSBoxes(boxes, confidences, self.yolo_confidence_threshold, self.yolo_nms_threshold)

                if len(idxs) > 0:
                    for i in idxs.flatten():
                        (x, y) = (boxes[i][0], boxes[i][1])
                        (w, h) = (boxes[i][2], boxes[i][3])
                        color = (0, 255, 0)  # Green bounding box
                        cv2.rectangle(frame, (x, y), (x + w, y + h), color, 2)
                        label = self.yolo_labels[class_ids[i]] if class_ids[i] < len(self.yolo_labels) else str(class_ids[i])
                        text = f"{label}: {confidences[i]:.2f}"
                        cv2.putText(frame, text, (x, y - 5), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 2)

            # Send annotated frame to streamer and add to recorder buffer
            self.streamer.send_frame(frame)
            self.recorder.add_frame_to_buffer(frame)

            # Optional: Display local preview (if desired)
            # cv2.imshow("Webcam", frame)
            # key = cv2.waitKey(1)
            # if key & 0xFF == ord('r'):
            #     print("Recording last 20s + next 10s...")
            #     self.record_clip("recorded_clip.mp4")
            # if key & 0xFF == ord('q'):
            #     self.running = False

        self.stop()

    def stop(self):
        self.running = False
        self.release_capture()
        self.streamer.stop_stream()
        cv2.destroyAllWindows()
