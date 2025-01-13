import cv2
import subprocess
import threading
import time
from collections import deque


class Streamer:
    def __init__(self, rtmp_url, width, height):
        self.rtmp_url = rtmp_url
        self.width = width
        self.height = height
        self.process = None

    def start_stream(self):
        # FFmpeg command to stream the video
        command = [
            'ffmpeg',
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
    def __init__(self, filename, fps, width, height):
        self.filename = filename
        self.fps = fps
        self.width = width
        self.height = height
        self.writer = None

    def start_recording(self):
        fourcc = cv2.VideoWriter_fourcc(*'mp4v')
        self.writer = cv2.VideoWriter(self.filename, fourcc, self.fps, (self.width, self.height))

    def write_frame(self, frame):
        if self.writer:
            self.writer.write(frame)

    def stop_recording(self):
        if self.writer:
            self.writer.release()


class VideoCapture:
    def __init__(self, rtmp_url, record_filename):
        self.rtmp_url = rtmp_url
        self.record_filename = record_filename

        self.cap = cv2.VideoCapture(0)
        if not self.cap.isOpened():
            raise Exception("Error: Could not open webcam.")

        self.width = int(self.cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        self.height = int(self.cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        self.fps = int(self.cap.get(cv2.CAP_PROP_FPS) or 30)

        self.streamer = Streamer(self.rtmp_url, self.width, self.height)
        self.recorder = Recorder(self.record_filename, self.fps, self.width, self.height)

        self.running = True

    def start(self):
        self.streamer.start_stream()
        self.recorder.start_recording()

        while self.running:
            ret, frame = self.cap.read()
            if not ret:
                print("Error: Failed to capture frame.")
                break

            # Send frame to streamer and recorder
            self.streamer.send_frame(frame)
            self.recorder.write_frame(frame)

            # Display the frame (optional)
            cv2.imshow("Webcam", frame)

            # Break the loop if 'q' is pressed
            if cv2.waitKey(1) & 0xFF == ord('q'):
                self.running = False

        self.stop()

    def stop(self):
        self.running = False
        self.cap.release()
        self.streamer.stop_stream()
        self.recorder.stop_recording()
        cv2.destroyAllWindows()


if __name__ == "__main__":
    rtmp_url = "rtmp://localhost/live/device1?key=F17461C168C25A5B63F97ADB677B9A9D3797C357BEB46E417E1E8B788B"
    record_filename = "recorded_video.mp4"
    video_capture = VideoCapture(rtmp_url, record_filename)
    video_capture.start()
