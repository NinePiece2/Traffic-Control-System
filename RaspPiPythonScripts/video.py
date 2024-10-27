import cv2
import asyncio
import websockets
import base64
import ssl


async def stream_camera():
    # Connect to the WebSocket server
    uri = "wss://localhost:44328/ws"

    ssl_context = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
    ssl_context.check_hostname = False
    ssl_context.verify_mode = ssl.CERT_NONE  # Disable certificate verification


    async with websockets.connect(uri, ssl=ssl_context) as websocket:
        # Open the camera
        cap = cv2.VideoCapture(0)
        try:
            while cap.isOpened():
                ret, frame = cap.read()
                if not ret:
                    break

                # Encode frame as JPEG
                _, buffer = cv2.imencode('.jpg', frame)
                # Convert to base64
                frame_data = base64.b64encode(buffer).decode('utf-8')
                
                # Send the frame data to the server
                await websocket.send(frame_data)

                # Optional: Add a short delay
                await asyncio.sleep(0.1)
        finally:
            cap.release()

# Run the asyncio event loop
asyncio.run(stream_camera())
