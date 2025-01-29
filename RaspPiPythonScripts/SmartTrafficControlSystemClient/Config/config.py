# Load saved configuration from config.json file
import json

class Config:
    def __init__(self):
        self.config = {}
        self.load_config()

    def load_config(self):
        try:
            with open('config.json', 'r') as file:
                self.config = json.load(file)
        except FileNotFoundError:
            print("Config file not found. Using default configuration.")
            self.config = {
                "rtmp_url": "rtmp://localhost/live",
                "width": 640,
                "height": 480,
                "fps": 30,
                "buffer_size": 20
            }

    def get(self, key):
        return self.config.get(key)
