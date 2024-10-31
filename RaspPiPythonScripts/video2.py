import cv2
import subprocess

# RTMP URL
rtmp_url = "rtmp://localhost:1935/live/stream?password=123456"

# Open the webcam
cap = cv2.VideoCapture(0)

# Check if the webcam is opened correctly
if not cap.isOpened():
    print("Error: Could not open webcam.")
    exit()

# Get the width and height of the webcam frame
width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))

# FFmpeg command to stream the video
command = [
    'ffmpeg',
    '-y',  # Overwrite output files
    '-f', 'rawvideo',  # Input format
    '-pixel_format', 'bgr24',  # Pixel format
    '-video_size', f"{width}x{height}",  # Video size
    '-i', '-',  # Input comes from stdin
    '-c:v', 'libx264',  # Video codec
    '-preset', 'veryfast',  # Preset for x264
    '-f', 'flv',  # Output format
    rtmp_url  # RTMP URL
]

# Start the FFmpeg process
process = subprocess.Popen(command, stdin=subprocess.PIPE)

while True:
    # Read a frame from the webcam
    ret, frame = cap.read()
    if not ret:
        print("Error: Failed to capture frame.")
        break

    # Write the frame to FFmpeg's stdin
    process.stdin.write(frame.tobytes())

    # Display the frame (optional)
    cv2.imshow("Webcam", frame)

    # Break the loop if 'q' is pressed
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# Release resources
cap.release()
process.stdin.close()
process.wait()
cv2.destroyAllWindows()
