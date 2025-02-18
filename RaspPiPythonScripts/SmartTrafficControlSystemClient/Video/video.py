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
import numpy as np
import pytesseract  # For OCR

# Optional: if Tesseract is not in your PATH, set the tesseract_cmd
# pytesseract.pytesseract.tesseract_cmd = r"/usr/bin/tesseract"  # Adjust as needed

# Attempt to import Picamera2 and IMX500-related modules on Linux
try:
    if platform.system() == 'Linux':
        from picamera2 import Picamera2, MappedArray
        try:
            from picamera2.devices import IMX500
            from picamera2.devices.imx500 import NetworkIntrinsics, postprocess_nanodet_detection
        except ImportError:
            IMX500 = None
except ImportError:
    pass

# Global variable for detections
last_detections = []

# ----- IMX500 Object Detection Helpers -----
class Detection:
    def __init__(self, coords, category, conf, metadata, picam2, imx500_obj):
        """Stores the detection bounding box, category and confidence.
           Uses the imx500 helper to convert raw coordinates into pixel coordinates.
        """
        self.category = category
        self.conf = conf
        # Convert inference coordinates into pixel coordinates on the output image
        self.box = imx500_obj.convert_inference_coords(coords, metadata, picam2)

def parse_detections(metadata, picam2, imx500_obj, threshold, iou, max_detections):
    """Parses the raw outputs from the IMX500 detector into Detection objects."""
    global last_detections
    intrinsics = imx500_obj.network_intrinsics
    bbox_normalization = getattr(intrinsics, 'bbox_normalization', False)
    bbox_order = getattr(intrinsics, 'bbox_order', 'yx')  # default order
    np_outputs = imx500_obj.get_outputs(metadata, add_batch=True)
    input_w, input_h = imx500_obj.get_input_size()
    if np_outputs is None:
        return last_detections
    # Use nanodet postprocessing if specified
    if getattr(intrinsics, 'postprocess', None) == "nanodet":
        boxes, scores, classes = postprocess_nanodet_detection(
            outputs=np_outputs[0],
            conf=threshold,
            iou_thres=iou,
            max_out_dets=max_detections
        )[0]
        from picamera2.devices.imx500.postprocess import scale_boxes
        boxes = scale_boxes(boxes, 1, 1, input_h, input_w, False, False)
    else:
        boxes, scores, classes = np_outputs[0][0], np_outputs[1][0], np_outputs[2][0]
        if bbox_normalization:
            boxes = boxes / input_h
        if bbox_order == "xy":
            boxes = boxes[:, [1, 0, 3, 2]]
        boxes = np.array_split(boxes, 4, axis=1)
        boxes = list(zip(*boxes))
    detections = [
        Detection(box, int(category), float(score), metadata, picam2, imx500_obj)
        for box, score, category in zip(boxes, scores, classes)
        if score > threshold
    ]
    last_detections = detections
    return detections

def get_labels(intrinsics):
    """Returns a list of labels from intrinsics. Falls back to a default file if needed."""
    labels = intrinsics.labels
    if labels is None:
        try:
            with open("assets/coco_labels.txt", "r") as f:
                labels = f.read().splitlines()
        except Exception as e:
            print("Error loading default labels:", e)
            labels = []
    return labels

# ----- License Plate Recognition Helper -----
def recognize_license_plate(car_roi):
    """
    Given a region-of-interest (ROI) containing a car,
    attempt to locate and OCR the license plate.
    This function uses grayscale conversion, bilateral filtering,
    edge detection, contour approximation, and then Tesseract OCR.
    """
    # Convert to grayscale and filter noise while preserving edges
    gray = cv2.cvtColor(car_roi, cv2.COLOR_BGR2GRAY)
    gray = cv2.bilateralFilter(gray, 11, 17, 17)
    edged = cv2.Canny(gray, 30, 200)
    
    # Find contours and sort by area
    cnts, _ = cv2.findContours(edged.copy(), cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)
    cnts = sorted(cnts, key=cv2.contourArea, reverse=True)[:10]
    
    plate_contour = None
    for c in cnts:
        peri = cv2.arcLength(c, True)
        approx = cv2.approxPolyDP(c, 0.018 * peri, True)
        if len(approx) == 4:
            plate_contour = approx
            break

    if plate_contour is None:
        return None, None

    # Create a mask for the plate region and extract it
    mask = np.zeros(gray.shape, np.uint8)
    cv2.drawContours(mask, [plate_contour], 0, 255, -1)
    plate = cv2.bitwise_and(gray, gray, mask=mask)
    x, y, w, h = cv2.boundingRect(plate_contour)
    plate = plate[y:y+h, x:x+w]
    # Optional thresholding for better OCR performance
    plate = cv2.threshold(plate, 150, 255, cv2.THRESH_BINARY)[1]

    # Use Tesseract OCR; using PSM 7 (treat as a single line)
    config_tesseract = "--psm 7 -c tessedit_char_whitelist=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
    text = pytesseract.image_to_string(plate, config=config_tesseract).strip()
    return text, (x, y, w, h)

# ----- Upload and Streaming Classes (unchanged) -----
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
    def __init__(self, rtmp_url, width, height):
        self.rtmp_url = rtmp_url
        self.width = width
        self.height = height
        self.process = None

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
            '-pixel_format', 'bgr24',
            '-video_size', f"{self.width}x{self.height}",
            '-framerate','12',
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
        self.buffer = deque(maxlen=abs(fps) * 20)
        self.recording = False
        self.writer = None
        self.filename = None

    def add_frame_to_buffer(self, frame):
        self.buffer.append(frame)

    def start_recording(self, filename):
        self.filename = filename
        self.recording = True
        fourcc = cv2.VideoWriter_fourcc(*'mp4v')
        self.writer = cv2.VideoWriter(self.filename, fourcc, self.fps, (self.width, self.height))
        for frame in self.buffer:
            self.writer.write(frame)
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
            upload = UploadClip(self.filename)
            upload.upload_clip(config.Config().get("Device_ID"))

# ----- Video Capture Class (Modified for IMX500) -----
class VideoCapture:
    def __init__(self, rtmp_url):
        self.rtmp_url = rtmp_url
        self.running = True

        if platform.system() == 'Linux':
            self.use_picamera2 = True
            use_imx500 = True
            if use_imx500 and IMX500 is not None:
                model_path = "/usr/share/imx500-models/imx500_network_ssd_mobilenetv2_fpnlite_320x320_pp.rpk"
                imx500_obj = IMX500(model_path)
                intrinsics = imx500_obj.network_intrinsics
                if not intrinsics:
                    from picamera2.devices.imx500 import NetworkIntrinsics
                    intrinsics = NetworkIntrinsics()
                    intrinsics.task = "object detection"
                labels_file = "assets/coco_labels.txt"
                try:
                    with open(labels_file, "r") as f:
                        intrinsics.labels = f.read().splitlines()
                except Exception as e:
                    print("Error loading labels:", e)
                self.width = 640
                self.height = 480
                self.fps = intrinsics.inference_rate if hasattr(intrinsics, "inference_rate") else 30
                self.picam2 = Picamera2(imx500_obj.camera_num)
                config_params = self.picam2.create_preview_configuration(
                    main={"format": 'XRGB8888'},
                    controls={"FrameRate": self.fps},
                    buffer_count=12
                )
                self.picam2.configure(config_params)
                self.picam2.start()
                self.use_imx500 = True
                self.imx500_obj = imx500_obj
            else:
                self.use_imx500 = False
                self.picam2 = Picamera2()
                self.width = 640
                self.height = 480
                self.fps = 30
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

        self.streamer = Streamer(self.rtmp_url, self.width, self.height)
        self.recorder = Recorder(self, self.width, self.height, self.fps)

    def read(self):
        """
        Returns (ret, frame).
        If using Picamera2 with 'XRGB8888' format, converts BGRA → BGR.
        If using IMX500 detection, retrieves metadata and overlays bounding boxes.
        Additionally, for detected 'car' objects, attempts license plate recognition.
        """
        if self.use_picamera2:
            frame = self.picam2.capture_array()
            frame = cv2.cvtColor(frame, cv2.COLOR_BGRA2BGR)
            # If IMX500 is enabled, run detection and overlay boxes
            if hasattr(self, "use_imx500") and self.use_imx500:
                metadata = self.picam2.capture_metadata()
                threshold = 0.55
                iou = 0.65
                max_detections = 10
                detections = parse_detections(metadata, self.picam2, self.imx500_obj, threshold, iou, max_detections)
                labels = get_labels(self.imx500_obj.network_intrinsics)
                for detection in detections:
                    x, y, w, h = detection.box
                    # Prepare label text
                    if detection.category < len(labels):
                        label_text = f"{labels[detection.category]} ({detection.conf:.2f})"
                    else:
                        label_text = f"{detection.category} ({detection.conf:.2f})"
                    cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 255, 0), 2)
                    cv2.putText(frame, label_text, (x + 5, y + 15),
                                cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 0, 255), 1)
                    # If the detected object is a car, attempt to read its license plate.
                    if labels[detection.category].lower() == "car":
                        car_roi = frame[y:y+h, x:x+w]
                        lp_text, plate_box = recognize_license_plate(car_roi)
                        if lp_text and plate_box is not None:
                            # Compute absolute coordinates of the plate region.
                            plate_x = x + plate_box[0]
                            plate_y = y + plate_box[1]
                            plate_w, plate_h = plate_box[2], plate_box[3]
                            cv2.rectangle(frame, (plate_x, plate_y), (plate_x + plate_w, plate_y + plate_h), (255, 0, 0), 2)
                            cv2.putText(frame, lp_text, (plate_x, plate_y - 10),
                                        cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 0, 0), 2)
                            print("Detected License Plate:", lp_text)
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
            self.streamer.send_frame(frame)
            self.recorder.add_frame_to_buffer(frame)
            # Optional: local preview (uncomment if needed)
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