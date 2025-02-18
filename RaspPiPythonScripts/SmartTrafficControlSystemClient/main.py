#import DeviceControl.deviceControl as deviceControl
import threading
import API.api as api
import Config.config as config
import Video.video as video
import json
import time

# Global variable to store the video capture instance
video_capture_instance = None
api_instance = None

def get_config_data():
    cfg = config.Config()
    return cfg.get_all()

def set_config_data(key, value):
    cfg = config.Config()
    cfg.set(key, value)

def start_video_capture():
    global video_capture_instance
    # Initialize video stream in a separate thread
    video_capture_instance = video.VideoCapture(
        f"{get_config_data()['Stream_URL']}{get_config_data()['Device_ID']}?key={get_config_data()['Stream_Key']}"
    )
    video_capture_instance.start()

def incident_detected():
    global video_capture_instance
    global api_instance
    video_GUID = api_instance.get_guid()
    currentDateTime = time.strftime("%Y-%m-%d %H:%M:%S")
    filename = f"incident-{video_GUID}-{currentDateTime}.mp4"
    if video_capture_instance:
        video_capture_instance.record_clip(filename)  # Use the stored instance
    else:
        print("Error: VideoCapture instance is not running!")

if __name__ == "__main__":

    # Initialize API
    api_instance = api.API()
    streamKey = api_instance.get_stream_client_key(get_config_data()['Device_ID'])
    set_config_data('Stream_Key', streamKey)

    # Start video capture in a new thread
    video_thread = threading.Thread(target=start_video_capture)
    video_thread.start()

    # Initialize traffic light control and needed config settings
    # Need to use 2 threads for lights and camera
    #traffic_lights_thread = threading.Thread(target=deviceControl.start_traffic_light_control)
    #traffic_lights_thread.start()