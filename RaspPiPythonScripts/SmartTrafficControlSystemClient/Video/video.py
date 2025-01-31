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

# ------------------------------
# Picamera2 is only needed if running on Linux
try:
    if platform.system() == 'Linux':
        from picamera2 import Picamera2
except ImportError:
    # If picamera2 isn't installed, you might want to handle it here
    pass
# ------------------------------

class UploadClip:
    def __init__(self, filename):
        self.filename = filename
        self.token = None
    
    def _get_new_token(self):
        """Fetch a new token from the token URL and update expiration time."""
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
            # Set up the headers with Bearer token
            headers = {
                'Authorization': f'Bearer {self.token}'
            }
            # Set up the form data (multipart)
            files = {
                'file': (self.filename.split('/')[-1], file),
            }
            # Include the deviceID as part of the form data
            data = {
                'deviceID': device_id
            }
            # Make the POST request
            response = requests.post(url, headers=headers, files=files, data=data)

            if response.status_code == 200:
                print("File uploaded successfully!")
                print("Response:", response.json())
            else:
                print("Failed to upload file.")
                print("Status Code:", response.status_code)
                print("Response:", response.text)


class Streamer:
    def __init__(self, rtmp_url, width, height):
        self.rtmp_url = rtmp_url
        self.width = width
        self.height = height
        self.process = None

    def start_stream(self):
        # FFmpeg command to stream the video
        ffmpeg_path = './ffmpeg'
        system_name = platform.system()
        if system_name == 'Linux':
            ffmpeg_path = 'ffmpeg'
        elif system_name == 'Windows':
            ffmpeg_path = 'ffmpeg.exe'

        command = [
            f'{ffmpeg_path}',
            '-y',
            '-f', 'rawvideo',
            '-pixel_format', 'bgr24',
            '-video_size', f"{self.width}x{self.height}",
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
        which provides a `read()` method.
        """
        self.video_capture = video_capture
        self.width = width
        self.height = height
        self.fps = fps
        self.buffer = deque(maxlen=abs(fps) * 20)  # Buffer for last 20 seconds
        self.recording = False
        self.writer = None
        self.filename = None

    def add_frame_to_buffer(self, frame):
        """Adds a frame to the buffer for the last 20 seconds."""
        self.buffer.append(frame)

    def start_recording(self, filename):
        """Starts recording using buffered frames and live frames."""
        self.filename = filename  # Store the filename for later use
        self.recording = True
        fourcc = cv2.VideoWriter_fourcc(*'mp4v')
        self.writer = cv2.VideoWriter(self.filename, fourcc, self.fps, (self.width, self.height))

        # Write buffered frames (last 20 seconds)
        for frame in self.buffer:
            self.writer.write(frame)

        # Record live frames for 10 seconds
        threading.Thread(target=self.record_live_frames, daemon=True).start()

    def record_live_frames(self):
        """Records live frames for 10 seconds after the trigger."""
        start_time = time.time()
        while self.recording and (time.time() - start_time) < 10:
            ret, frame = self.video_capture.read()
            if ret:
                self.writer.write(frame)

        self.stop_recording()

    def stop_recording(self):
        """Stops recording and releases the video writer."""
        self.recording = False
        if self.writer:
            self.writer.release()
            self.writer = None

            # Upload the clip after we're done writing
            upload = UploadClip(self.filename)
            upload.upload_clip('device1')


class VideoCapture:
    def __init__(self, rtmp_url):
        self.rtmp_url = rtmp_url
        self.running = True

        # Decide between picamera2 (Linux) and regular OpenCV capture (others).
        if platform.system() == 'Linux':
            # Use Picamera2
            self.use_picamera2 = True
            self.picam2 = Picamera2()
            # Set a desired resolution or adapt from config
            self.width = 640
            self.height = 480
            self.fps = 30  # PiCamera2 doesn't always provide exact FPS, but we'll use 30 as a reference.

            # Configure Picamera2
            video_config = self.picam2.create_video_configuration(
                main={"format": 'XRGB8888', "size": (self.width, self.height)}
            )
            self.picam2.configure(video_config)
            self.picam2.start()
        else:
            # Use OpenCV VideoCapture
            self.use_picamera2 = False
            self.cap = cv2.VideoCapture(int(config.Config().get("WebcamID")))
            if not self.cap.isOpened():
                raise Exception("Error: Could not open webcam.")

            self.width = int(self.cap.get(cv2.CAP_PROP_FRAME_WIDTH))
            self.height = int(self.cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
            # Some cameras may return 0 for FPS, so we default to 30 if that's the case.
            cam_fps = self.cap.get(cv2.CAP_PROP_FPS)
            self.fps = int(cam_fps) if cam_fps > 0 else 30

        # Create the streamer and recorder
        self.streamer = Streamer(self.rtmp_url, self.width, self.height)
        self.recorder = Recorder(self, self.width, self.height, self.fps)

    def read(self):
        """
        Unified read method to return (ret, frame) 
        regardless of whether we use picamera2 or OpenCV.
        """
        if self.use_picamera2:
            frame = self.picam2.capture_array()
            # Emulate the OpenCV read() convention: (True, frame)
            return True, frame
        else:
            return self.cap.read()

    def release_capture(self):
        """Stop capture depending on the mode."""
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

            # Send frame to streamer and add to recorder buffer
            self.streamer.send_frame(frame)
            self.recorder.add_frame_to_buffer(frame)

            # OPTIONAL: If you want to see a local preview
            # cv2.imshow("Webcam", frame)
            # key = cv2.waitKey(1)
            # if key & 0xFF == ord('r'):
            #     print("Recording last 20 and next 10 seconds...")
            #     self.record_clip("recorded_clip.mp4")
            # if key & 0xFF == ord('q'):
            #     self.running = False

        self.stop()

    def stop(self):
        self.running = False
        self.release_capture()
        self.streamer.stop_stream()
        cv2.destroyAllWindows()
