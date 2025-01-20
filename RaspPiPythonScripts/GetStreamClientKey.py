import requests
import urllib.parse
import jwt
from jwt.exceptions import ExpiredSignatureError, DecodeError
from datetime import datetime, timezone
import json

class API:
    def __init__(self):
        self.token = None
        self.token_url = "https://localhost:44363/Token/GetToken?userID="
        self.key = 'ORmEqGUYyJaC2GVgGOKb5yIQR9uGe1Ca7AKbWhg772/u/Rqjme+CONBd6zUXSlmMpC9+SrLIFXyrf9XhMEKjFapDNgcwjjHr3ABaou451traWFwevwPsU9JKwrhUyMd9OQrK+XPYRpUW+jNdOrALXidF7jkGISfxLPunQld/5Nk='

    def _get_new_token(self):
        """Fetch a new token from the token URL and update expiration time."""
        response_token = requests.get(self.token_url + urllib.parse.quote(self.key, safe=''), verify=False)
        if response_token.status_code == 200:
            self.token = response_token.text
            print(f"Token received: {self.token}")
        else:
            print(f"Failed to get token. Status code: {response_token.status_code}")
            print(f"Response Content: {response_token.text}")
            self.token = None

    def _ensure_valid_token(self):
        if not self.token:
            print("Fetching a new token...")
            self._get_new_token()

        try:
            decoded_token = jwt.decode(self.token, options={"verify_signature": False})
            print(f"Decoded Token: {decoded_token}")

            exp = decoded_token.get("exp")
            print(f"Expiration Claim (exp): {exp}")
            
            expiration_time = datetime.fromtimestamp(exp, tz=timezone.utc)
            print(f"Expiration Time: {expiration_time}")

            current_time = datetime.now(tz=timezone.utc)
            print(f"Current Time: {current_time}")

            if current_time >= expiration_time:
                print("Token has expired.")
                self._get_new_token()

            elif (expiration_time - current_time).total_seconds() <= 300:
                print("Token will expire in less than 5 minutes. Fetching a new one.")
                self._get_new_token()

            else:
                print("Token is still valid.")


        except DecodeError as e:
            print(f"Invalid token format: {e}")
            return False
        except Exception as e:
            print(f"Error while decoding token in _ensure_valid_token: {e}")
            return False


    def get_stream_client_key(self, device_stream_id):
        self._ensure_valid_token()

        if not self.token:
            print("No token available to proceed.")
            return

        stream_url = f"https://localhost:44363/Traffic/GetStreamClientKey?DeviceStreamID={device_stream_id}"
        headers = {
            'Authorization': f'Bearer {self.token}'
        }

        response_stream = requests.get(stream_url, headers=headers, verify=False)
        
        if response_stream.status_code == 200:
            print(f"Stream Client Key Response: {response_stream.text}")
        else:
            print(f"Failed to fetch stream client key. Status code: {response_stream.status_code}")
            print(f"Response Content: {response_stream.text}")
            
    def post_AddTrafficViolation(self, ActiveSignalID, LicensePlate, VideoURL):
        self._ensure_valid_token()
        
        if not self.token:
            print("No token available to proceed.")
            return
        
        stream_url = f"https://localhost:44363/Traffic/AddTrafficViolation"
        
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
            response_violation = requests.post(stream_url, json=payload, headers = headers, verify=False)
            
            if response_violation.status_code == 200:
                print(f"Traffic Violation Added: {response_violation.json()}")
            
            else:
                print(f"Failed to add traffic violation. Status Code: {response_violation.status_code}")
                print(f"Response Content: {response_violation.text}")
                
        except requests.exceptions.RequestException as e:
            print(f"Error occurred during POST request: {e}")
        
        

if __name__ == "__main__":
    api = API()
    api.get_stream_client_key("device1")
    api.post_AddTrafficViolation(1,"randomtest","http://sagu.com/sagu.sagu4")