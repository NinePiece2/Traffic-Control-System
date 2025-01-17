import cv2
import subprocess
import threading
import time
from collections import deque
import requests

class UploadClip:
    def __init__(self, filename):
        self.filename = filename

    def upload_clip(self, device_id):
        url = "http://localhost:5130/Clip/UploadFile"
        # Open the file in binary mode
        with open(self.filename, 'rb') as file:
            # Set up the headers with Bearer token
            headers = {
                'Authorization': f'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6Ik9SbUVxR1VZeUphQzJHVmdHT0tiNXlJUVI5dUdlMUNhN0FLYldoZzc3Mi91L1Jxam1lK0NPTkJkNnpVWFNsbU1wQzkrU3JMSUZYeXJmOVhoTUVLakZhcEROZ2N3ampIcjNBQmFvdTQ1MXRyYVdGd2V2d1BzVTlKS3dyaFV5TWQ5T1FySytYUFlScFVXK2pOZE9yQUxYaWRGN2prR0lTZnhMUHVuUWxkLzVOaz0iLCJuYmYiOjE3MzcwNzA0MzUsImV4cCI6MTczNzA3NDAzNSwiaWF0IjoxNzM3MDcwNDM1LCJpc3MiOiJodHRwczovL2xvY2FsaG9zdDo0NDM4NCIsImF1ZCI6Imh0dHBzOi8vbG9jYWxob3N0OjQ0Mzg0In0.Mz6QK3egl0CB1Qe-ZfXSgohSnmFYxOYbwXJcv4J2saU'
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

            # Check the response
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
        command = [
            './ffmpeg',
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
        if self.process:
            self.process.stdin.write(frame.tobytes())

    def stop_stream(self):
        if self.process:
            self.process.stdin.close()
            self.process.wait()


class Recorder:
    def __init__(self, width, height, fps):
        self.width = width
        self.height = height
        self.fps = fps
        self.buffer = deque(maxlen=fps * 20)  # Buffer for last 20 seconds
        self.recording = False
        self.writer = None

    def add_frame_to_buffer(self, frame):
        """Adds a frame to the buffer for the last 20 seconds."""
        self.buffer.append(frame)

    def start_recording(self, filename):
        """Starts recording using buffered frames and live frames."""
        self.recording = True
        fourcc = cv2.VideoWriter_fourcc(*'mp4v')
        self.writer = cv2.VideoWriter(filename, fourcc, self.fps, (self.width, self.height))

        # Write buffered frames (last 20 seconds)
        for frame in self.buffer:
            self.writer.write(frame)

        # Record live frames for 10 seconds
        threading.Thread(target=self.record_live_frames, daemon=True).start()

    def record_live_frames(self):
        """Records live frames for 10 seconds after the trigger."""
        start_time = time.time()
        while self.recording and (time.time() - start_time) < 10:
            ret, frame = video_capture.cap.read()
            if ret:
                self.writer.write(frame)

        self.stop_recording()

    def stop_recording(self):
        """Stops recording and releases the video writer."""
        self.recording = False
        if self.writer:
            self.writer.release()
            self.writer = None
            upload = UploadClip("recorded_clip.mp4")
            upload.upload_clip('device1')

class VideoCapture:
    def __init__(self, rtmp_url):
        self.rtmp_url = rtmp_url

        self.cap = cv2.VideoCapture(1)
        if not self.cap.isOpened():
            raise Exception("Error: Could not open webcam.")

        self.width = int(self.cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        self.height = int(self.cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        self.fps = int(self.cap.get(cv2.CAP_PROP_FPS) or 30)

        self.streamer = Streamer(self.rtmp_url, self.width, self.height)
        self.recorder = Recorder(self.width, self.height, self.fps)

        self.running = True

    def start(self):
        self.streamer.start_stream()

        while self.running:
            ret, frame = self.cap.read()
            if not ret:
                print("Error: Failed to capture frame.")
                break

            # Send frame to streamer and add to recorder buffer
            self.streamer.send_frame(frame)
            self.recorder.add_frame_to_buffer(frame)

            # Display the frame (optional)
            cv2.imshow("Webcam", frame)

            # Trigger recording on key press 'r'
            key = cv2.waitKey(1)
            if key & 0xFF == ord('r'):
                print("Recording last 20 and next 10 seconds...")
                self.recorder.start_recording("recorded_clip.mp4")

            # Break the loop if 'q' is pressed
            if key & 0xFF == ord('q'):
                self.running = False

        self.stop()

    def stop(self):
        self.running = False
        self.cap.release()
        self.streamer.stop_stream()
        cv2.destroyAllWindows()


if __name__ == "__main__":
    rtmp_url = "rtmp://stream1-trafficcontrolsystem.romitsagu.com/live/device1?key=F17461C168C25A5B63F97ADB677B9A9D3797C357BEB46E417E1E8B788B"
    video_capture = VideoCapture(rtmp_url)
    video_capture.start()
