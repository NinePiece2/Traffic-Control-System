import json
import os

class Config:
    def __init__(self):
        self.config = {}
        self.config_folder = 'ConfigFile'
        self.config_file_path = os.path.join(self.config_folder, 'config.json')
        self.load_config()

    def load_config(self):
        # Ensure the config folder exists
        if not os.path.exists(self.config_folder):
            os.makedirs(self.config_folder)

        try:
            with open(self.config_file_path, 'r') as file:
                self.config = json.load(file)
        except FileNotFoundError:
            print("Config file not found. Using default configuration.")
            self.config = {
                "Stream_URL": "rtmp://localhost/live",
                "API_URL": "https://localhost:5000/",
                "Video_URL": "http://localhost:5000/",
                "API_KEY": "API_KEY",
                "Device_ID": "device0",
                "Stream_Key": "STREAM_KEY",
                "WebcamID": "0"
            }
            # Create config file within ConfigFile folder
            with open(self.config_file_path, 'w') as file:
                json.dump(self.config, file, indent=4)

    def get(self, key):
        self.load_config()
        return self.config.get(key)

    def set(self, key, value):
        self.config[key] = value
        with open(self.config_file_path, 'w') as file:
            json.dump(self.config, file, indent=4)
        
    def get_all(self):
        self.load_config()
        return self.config
