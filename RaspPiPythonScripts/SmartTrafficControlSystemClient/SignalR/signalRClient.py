from signalrcore.hub_connection_builder import HubConnectionBuilder
import time
import Config.config as config

class SignalRClient:
    def __init__(self, receive_message_callback):
        self.hub_url = f"{config.Config().get('MVC_URL')}controlhub?clientType=Python&deviceId={config.Config().get('Device_ID')}"
        self.hub_connection = HubConnectionBuilder()\
            .with_url(self.hub_url)\
            .build()
        self.is_connected = False
        self.client_connection_id = None

        # Callback that external code can set to handle received messages
        self.receive_message_callback = receive_message_callback
        
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
        #print(msg)
        # If a callback is registered, call it with the message.
        if self.receive_message_callback:
            self.receive_message_callback(message)
    
    def on_receive_connection_id(self, connection_id):
        self.client_connection_id = connection_id
    
    def on_connected(self):
        self.is_connected = True
        print("SignalR - Connection established successfully.")
    
    def on_disconnected(self):
        self.is_connected = False
        print("SignalR - Connection lost.")
    
    def send_message_to_client_by_deviceId(self, deviceID, msg):
        if self.is_connected:
            #print(f"Sending message to client with deviceID: {deviceID}")
            self.hub_connection.send("SendMessageToClientByDeviceID", [deviceID, msg])
        else:
            print("SignalR - Not connected to the hub.")
    
    def start_connection(self):
        try:
            self.hub_connection.start()
        except Exception as e:
            print(f"SignalR - Error starting connection: {e}")
            return False
        return True
    
    def stop_connection(self):
        self.hub_connection.stop()
        print("SignalR - Connection stopped.")

if __name__ == "__main__":
    def my_message_handler(msg):
        print("Callback received message:", msg)

    client = SignalRClient(my_message_handler)

    try:
        while True:
            user_input = input("Press Enter to send message (or type 'exit' to quit): ")
            if user_input.lower() == "exit":
                break
            else:
                client.send_message_to_client_by_deviceId(config.Config().get('Device_ID'), "Test")
    except KeyboardInterrupt:
        print("Stopping connection...")
    finally:
        client.stop_connection()
        print("Connection stopped.")
