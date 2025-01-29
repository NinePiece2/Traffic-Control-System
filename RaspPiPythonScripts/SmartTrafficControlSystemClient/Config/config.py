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
                "Stream_URL": "rtmp://localhost/live",
                "API_URL": "https://localhost:5000/",
                "API_KEY": "API_KEY",
                "Device_ID": "Device0",
            }

    def get(self, key):
        return self.config.get(key)
