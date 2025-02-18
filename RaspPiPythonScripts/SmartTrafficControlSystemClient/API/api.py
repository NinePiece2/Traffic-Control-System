import requests
import urllib.parse
import jwt
from jwt.exceptions import ExpiredSignatureError, DecodeError
from datetime import datetime, timezone
import json
import Config.config as config

class API:
    def __init__(self):
        self.token = None
     
    def _get_new_token(self):
        """Fetch a new token from the token URL and update expiration time."""
        token_url = config.Config().get("API_URL") + "Token/GetToken?userID="
        key = config.Config().get("API_KEY")
        response_token = requests.get(token_url + urllib.parse.quote(key, safe=''))
        if response_token.status_code == 200:
            self.token = response_token.text
            #print(f"Token received: {self.token}")
        else:
            print(f"Failed to get token. Status code: {response_token.status_code}")
            print(f"Response Content: {response_token.text}")
            self.token = None

    def _ensure_valid_token(self):
        if not self.token:
            #print("Fetching a new token...")
            self._get_new_token()

        try:
            decoded_token = jwt.decode(self.token, options={"verify_signature": False})
            #print(f"Decoded Token: {decoded_token}")

            exp = decoded_token.get("exp")
            #print(f"Expiration Claim (exp): {exp}")
            
            expiration_time = datetime.fromtimestamp(exp, tz=timezone.utc)
            #print(f"Expiration Time: {expiration_time}")

            current_time = datetime.now(tz=timezone.utc)
            #print(f"Current Time: {current_time}")

            if current_time >= expiration_time:
                #print("Token has expired.")
                self._get_new_token()

            elif (expiration_time - current_time).total_seconds() <= 300:
                #print("Token will expire in less than 5 minutes. Fetching a new one.")
                self._get_new_token()

        except DecodeError as e:
            print(f"Invalid token format: {e}")
            return False
        except Exception as e:
            print(f"Error while decoding token in _ensure_valid_token: {e}")
            return False


    def get_stream_client_key(self, device_stream_id):
        self._ensure_valid_token()

        if not self.token:
            #print("No token available to proceed.")
            return
        
        stream_url = f"{config.Config().get('API_URL')}Traffic/GetStreamClientKey?DeviceStreamID={device_stream_id}"

        headers = {
            'Authorization': f'Bearer {self.token}'
        }

        response_stream = requests.get(stream_url, headers=headers)
        
        if response_stream.status_code == 200:
            #print(f"Stream Client Key Response: {response_stream.text}")
            data = json.loads(response_stream.text)
            return data["deviceStreamKEY"]
        else:
            print(f"Failed to fetch stream client key. Status code: {response_stream.status_code}")
            print(f"Response Content: {response_stream.text}")
            
    def getDirectionAndTime(self,id):
        self._ensure_valid_token()
        
        if not self.token:
            #print("No token available to proceed.")
            return
        
        stream_url = f"{config.Config().get('API_URL')}Traffic/GetDirectionAndTime?ID={id}"

        headers = {
            'Authorization': f'Bearer {self.token}'
        }

        response_stream = requests.get(stream_url, headers=headers)
        
        if response_stream.status_code == 200:
            print(f"Get Direction and Time Response: {response_stream.text}")
            data = json.loads(response_stream.text)
            return data
        else:
            print(f"Failed to fetch Direction and Time. Status code: {response_stream.status_code}")
            print(f"Response Content: {response_stream.text}")
        
            
    def add_traffic_violation(self, ActiveSignalID, LicensePlate, VideoURL):
        self._ensure_valid_token()
        
        if not self.token:
            #print("No token available to proceed.")
            return
        
        stream_url = f"{config.Config().get('API_URL')}Traffic/AddTrafficViolation"
        
        headers = {
            'Authorization': f'Bearer {self.token}',
            'Content-Type': 'application/json'
        }
        
        payload = {
            "ActiveSignalID": ActiveSignalID,
            "LicensePlate": LicensePlate,
            "VideoURL": VideoURL
        }
        
        try:
            response_violation = requests.post(stream_url, json=payload, headers = headers)
            
            if response_violation.status_code == 200:
                print(f"Traffic Violation Added: {response_violation.json()}")
            
            else:
                print(f"Failed to add traffic violation. Status Code: {response_violation.status_code}")
                print(f"Response Content: {response_violation.text}")
                
        except requests.exceptions.RequestException as e:
            print(f"Error occurred during POST request: {e}")
        
    def get_guid(self):
        self._ensure_valid_token()
        
        if not self.token:
            #print("No token available to proceed.")
            return
        
        stream_url = f"{config.Config().get('API_URL')}Traffic/GetGUID"

        headers = {
            'Authorization': f'Bearer {self.token}'
        }

        response_stream = requests.get(stream_url, headers=headers)
        
        if response_stream.status_code == 200:
            data = json.loads(response_stream.text)
            return data["guid"]
        else:
            print(f"Failed to fetch Direction and Time. Status code: {response_stream.status_code}")
            print(f"Response Content: {response_stream.text}")



if __name__ == "__main__":
    api = API()
    print(api.get_stream_client_key("device1"))
    #print(api.add_traffic_violation(5,"test5","http://test5.com/test5"))
    print(api.getDirectionAndTime("8"))