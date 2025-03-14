import DeviceControl.deviceControl as deviceControl
import threading
import API.api as api
import Config.config as config
import Video.video as video
import SignalR.signalRClient as signalRClient
import json
import time
import os

# Global variable to store the video capture instance
video_capture_instance = None
api_instance = None
signalR = None
device_control_instance = None

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
        print(f"Video clip saved as {filename}")
        api_instance.add_traffic_violation(get_config_data()['Device_ID'], "Plate", filename)
    else:
        print("Error: VideoCapture instance is not running!")

def start_device_control():
    global device_control_instance
    device_control_instance = deviceControl.TrafficLightController(signalR)
    device_control_instance.traffic_cycle()

def update_config():
    if get_config_data()['IsDebug'] == 1:
        os.environ['CURL_CA_BUNDLE'] = ''
        os.environ['REQUESTS_CA_BUNDLE'] = ''
    
    streamKey = api_instance.get_stream_client_key(get_config_data()['Device_ID'])
    set_config_data('Stream_Key', streamKey)

    dir_and_time = api_instance.getDirectionAndTime(get_config_data()['Device_ID'])
    set_config_data('Direction_1_Green_Time', dir_and_time['direction1Time'])
    set_config_data('Direction_2_Green_Time', dir_and_time['direction2Time'])
    set_config_data('Pedestrian_Walk_Time', dir_and_time['pedestrianWalkTime'])
    set_config_data('BuzzerVolume', dir_and_time['buzzerVolume'])

def signalR_recieved_callback(msg):
    global device_control_instance
    print(f"Received message: {msg}")
    if msg == "Manual Override":
        device_control_instance.manual_override()
    # elif msg == "Status Update":
    #     signalR.send_message_to_client_by_deviceId(get_config_data()['Device_ID'], "Status: Direction: || Time: ")
    elif msg == "Config Update":
        update_config()
        device_control_instance.config_update()
    else:
        print(f"Unknown message received ({msg}).")


if __name__ == "__main__":

    # Initialize API
    api_instance = api.API()

    # Get current Config from the API
    update_config()

    # Connect to the SignalR hub
    signalR = signalRClient.SignalRClient(signalR_recieved_callback)

    # Start video capture in a new thread
    video_thread = threading.Thread(target=start_video_capture)
    video_thread.start()
    
    # Initialize traffic light control and needed config settings
    # Need to use 2 threads for lights and camera
    device_control_thread = threading.Thread(target=start_device_control)
    device_control_thread.start()