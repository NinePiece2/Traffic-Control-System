import DeviceControl.deviceControl as deviceControl
import threading
import API.api as api
import Config.config as config
import Video.video as video
import json

def get_config_data():
    cfg = config.Config()
    return cfg.get_all()

def set_config_data(key, value):
    cfg = config.Config()
    cfg.set(key, value)

def start_video_capture():
    # Initialize video stream in a separate thread
    video.VideoCapture(f"{get_config_data()['Stream_URL']}{get_config_data()['Device_ID']}?key={get_config_data()['Stream_Key']}").start()

if __name__ == "__main__":

    # Initialize API
    api = api.API()
    streamKey = api.get_stream_client_key(get_config_data()['Device_ID'])
    set_config_data('Stream_Key', streamKey)

    # Start video capture in a new thread
    video_thread = threading.Thread(target=start_video_capture)
    video_thread.start()

    # Initialize traffic light control and needed config settings
    # Need to use 2 threads for lights and camera