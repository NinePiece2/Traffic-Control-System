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


class VideoCapture:
    def __init__(self, rtmp_url):
        self.rtmp_url = rtmp_url

        self.cap = cv2.VideoCapture(0)
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
