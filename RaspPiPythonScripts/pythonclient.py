from signalrcore.hub_connection_builder import HubConnectionBuilder
import time

# Define the SignalR hub URL
hub_url = "http://localhost:5062/controlhub"

# Create the hub connection
hub_connection = HubConnectionBuilder() \
    .with_url(hub_url, options={"verify_ssl": False}) \
    .build()

messages=[]

def on_receive_message(args):
    user, message = args  # Unpack the received list
    msg = f"{user}: {message}"  
    print(msg)  # Print the message
    messages.append(msg)

def on_receive_connection_id(connection_id):
    print(f"My Connection ID: {connection_id}")
    global client_connection_id
    client_connection_id = connection_id

# Register the function for the 'ReceiveMessage' event
hub_connection.on("ReceiveMessage", on_receive_message)

hub_connection.on("ReceiveConnectionId", on_receive_connection_id)

# Define a variable to track the connection state
is_connected = False

# Define a function to handle connection start
def on_connected():
    global is_connected
    is_connected = True
    print("Connection established successfully.")

def on_disconnected():
    global is_connected
    is_connected = False
    print("Connection lost.")

# Register connection event handlers
hub_connection.on_open(on_connected)
hub_connection.on_close(on_disconnected)

# Start the connection
def start_connection():
    try:
        hub_connection.start()
        print("Attempting to start the connection...")
    except Exception as e:
        print(f"Error starting connection: {e}")
        return False
    return True

# Wait until the connection is established before receiving messages
if start_connection():
    # Wait for the connection to establish
    while not is_connected:
        time.sleep(1)
    
    print("Waiting for messages from the server...")

    # Keep the connection open and receive messages from the server
    input("")
    hub_connection.stop()
else:
    print("Failed to connect.") 
