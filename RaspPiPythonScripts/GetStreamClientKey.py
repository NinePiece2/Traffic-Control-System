import requests
import time

class API:
    def __init__(self):
        self.token = None
        self.token_expiration = 0 
        self.token_url = "https://api-trafficcontrolsystem.romitsagu.com/Token/GetToken?userID=xrUXvjdVt0Kz3c7drkt9oSLR9Rcm5nPQXe8rU1riDT2qKLzPlH9VsetNzv68YHqG%2Bn%2BTWeP4ZSqZnBpNEc6UKpZVrleN6Yr5rYwhg7qhXQvQ5eohikcuyxFVbDATEK4aqEoRheBEoDh4VpDzOTi%2BS9zVE7p9f%2F%2B6l6ZPPtIhu2U%3D"

    def _get_new_token(self):
        """Fetch a new token from the token URL and update expiration time."""
        response_token = requests.get(self.token_url)
        if response_token.status_code == 200:
            self.token = response_token.text
            self.token_expiration = time.time() + 3600  
            print(f"Token received: {self.token}")
        else:
            print(f"Failed to get token. Status code: {response_token.status_code}")
            print(f"Response Content: {response_token.text}")
            self.token = None

    def _ensure_valid_token(self):
        """Ensure the token is valid and fetch a new one if expired or close to expiry."""
        if not self.token or time.time() >= self.token_expiration - 300:
            self._get_new_token()

    def get_stream_client_key(self, device_stream_id):
        """Get the Stream Client Key for a given DeviceStreamID."""
        self._ensure_valid_token()

        if not self.token:
            print("No token available to proceed.")
            return

        stream_url = f"https://api-trafficcontrolsystem.romitsagu.com/Traffic/GetStreamClientKey?DeviceStreamID={device_stream_id}"
        headers = {
            'Authorization': f'Bearer {self.token}'
        }

        response_stream = requests.get(stream_url, headers=headers)
        
        if response_stream.status_code == 200:
            print(f"Stream Client Key Response: {response_stream.text}")
        else:
            print(f"Failed to fetch stream client key. Status code: {response_stream.status_code}")
            print(f"Response Content: {response_stream.text}")

if __name__ == "__main__":
    api = API()
    api.get_stream_client_key("device1")
