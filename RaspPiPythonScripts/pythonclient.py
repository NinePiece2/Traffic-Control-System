from signalrcore.hub_connection_builder import HubConnectionBuilder
import time

class SignalRClient:
    def __init__(self, hub_url):
        self.hub_url = hub_url
        self.hub_connection = HubConnectionBuilder().with_url(hub_url, options={"verify_ssl": False}).build()
        self.is_connected = False
        self.client_connection_id = None
        self.messages = []
        
        # Register event handlers
        self.hub_connection.on("ReceiveMessage", self.on_receive_message)
        self.hub_connection.on("ReceiveConnectionId", self.on_receive_connection_id)
        self.hub_connection.on_open(self.on_connected)
        self.hub_connection.on_close(self.on_disconnected)
        
        # Start the connection
        self.start_connection()
    
    def on_receive_message(self, args):
        user, message = args
        msg = f"{user}: {message}"
        print(msg)
        self.messages.append(msg)
    
    def on_receive_connection_id(self, connection_id):
        #print(f"My Connection ID: {connection_id}")
        self.client_connection_id = connection_id
    
    def on_connected(self):
        self.is_connected = True
        print("Connection established successfully.")
    
    def on_disconnected(self):
        self.is_connected = False
        print("Connection lost.")
    
    def send_message_to_client_by_deviceId(self, deviceID):
        if self.is_connected:
            print(f"Sending message to client withs deviceID: {deviceID}")
            self.hub_connection.send("SendMessageToClientByDeviceID", [deviceID])
        else:
            print("Not connected to the hub.")
    
    def start_connection(self):
        try:
            self.hub_connection.start()
            #print("Attempting to start the connection...")
            # while not self.is_connected:
            #     time.sleep(1)
            #print("Ready to send messages.")
        except Exception as e:
            print(f"Error starting connection: {e}")
            return False
        return True
    
    def stop_connection(self):
        self.hub_connection.stop()
        print("Connection stopped.")

if __name__ == "__main__":
    deviceID = "device1"  # Unique identifier
    hub_url = f"http://localhost:5062/controlhub?clientType=Python&deviceId={deviceID}"
    client = SignalRClient(hub_url)
    
    try:
        while True:
            user_input = input("Press Enter to send message")
            if user_input.lower() == "exit":
                break
            else:
                client.send_message_to_client_by_deviceId(deviceID)
    except KeyboardInterrupt:
        print("Stopping connection...")
    finally:
        client.stop_connection()
        print("Connection stopped.")