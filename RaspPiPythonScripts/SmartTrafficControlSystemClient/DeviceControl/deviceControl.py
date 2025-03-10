import os
import platform
import time
import threading
from gpiozero import LED, Button, PWMOutputDevice
import Config.config as config

# Only set the mock pin factory if the OS is not Linux
if platform.system() != "Linux":
    os.environ["GPIOZERO_PIN_FACTORY"] = "mock"
    print("DEV DETECTED: Using mock pin factory")
else:
    print("PROD: Using RPi pin factory")


class TrafficLightController:
    def __init__(self, signalR_client_callback):
        """
        Initializes the traffic light controller with configuration.
        """
        self.interrupt_flag = threading.Event()
        self.signalR_client_callback = signalR_client_callback

        # Define traffic lights for two directions
        self.traffic_lights = {
            "light_dir_1": {"red": LED(17), "yellow": LED(27), "green": LED(22)},
            "light_dir_2": {"red": LED(23), "yellow": LED(24), "green": LED(25)}
        }

        # Define pedestrian buttons (One per direction)
        self.pedestrian_buttons = {
            "light_dir_1": Button(18),  # GPIO 18 for direction 1
            "light_dir_2": Button(19)   # GPIO 19 for direction 2
        }

        # Attach event listeners for pedestrian buttons
        self.pedestrian_buttons["light_dir_1"].when_pressed = lambda: self.pedestrian_pressed("light_dir_1")
        self.pedestrian_buttons["light_dir_2"].when_pressed = lambda: self.pedestrian_pressed("light_dir_2")

        # Define passive buzzers on pins 12, 13, 14, and 15 using PWMOutputDevice
        self.buzzers = {
            "buzzer1": PWMOutputDevice(12),
            "buzzer2": PWMOutputDevice(13),
            "buzzer3": PWMOutputDevice(14),
            "buzzer4": PWMOutputDevice(15)
        }

        # Track active light (default direction)
        self.active_light = "light_dir_1"

    def pedestrian_pressed(self, light_name):
        """Handles pedestrian button press event for the given direction."""
        print(f"Pedestrian button for {light_name} pressed! Adjusting cycle...")
        self.interrupt_flag.set()

    def update_config(self):
        """Updates the config dynamically and interrupts the current cycle."""
        self.interrupt_flag.set()

    def manual_override(self):
        """Triggers a manual override, stopping the current cycle immediately."""
        self.interrupt_flag.set()

    def switch_light(self, light_name, color):
        """Controls the lights (green, yellow, red) based on the given color."""
        light = self.traffic_lights[light_name]
        if color == "green":
            light["red"].off()
            light["yellow"].off()
            light["green"].on()
        elif color == "yellow":
            light["green"].off()
            light["yellow"].on()
            light["red"].off()
        elif color == "red":
            light["yellow"].off()
            light["red"].on()
            light["green"].off()

    def play_buzzer_tone(self, frequency, duration):
        """
        Plays a tone on all buzzers.
        frequency: The tone frequency in Hz.
        duration: How long to play the tone (seconds).
        """
        print(f"Playing tone at {frequency}Hz for {duration} seconds on buzzers.")
        for buzzer in self.buzzers.values():
            buzzer.frequency = frequency
            buzzer.value = 0.5  # 50% duty cycle (adjust for loudness)
        time.sleep(duration)
        for buzzer in self.buzzers.values():
            buzzer.off()

    def traffic_cycle(self):
        """Main traffic light cycle that alternates between directions."""
        while True:
            for light_name in ["light_dir_1", "light_dir_2"]:
                self.active_light = light_name
                self.run_light_cycle(light_name)

                # Check if an interrupt occurred
                if self.interrupt_flag.is_set():
                    print("Cycle interrupted. Restarting with new settings...")
                    self.interrupt_flag.clear()

    def run_light_cycle(self, light_name):
        """Runs a single cycle for the given traffic light."""
        other_light_name = "light_dir_2" if light_name == "light_dir_1" else "light_dir_1"
        
        # Get config values
        green_time = config.Config().get('Direction_1_Green_Time') if light_name == "light_dir_1" else config.Config().get('Direction_2_Green_Time')
        pedestrian_time = config.Config().get('Pedestrian_Walk_Time')

        # Set red for the other light
        self.switch_light(other_light_name, "red")

        # Green light phase
        self.switch_light(light_name, "green")
        start_time = time.time()
        while time.time() - start_time < green_time:
            time.sleep(0.5)
            remaining = green_time - (time.time() - start_time)
            self.signalR_client_callback.send_message_to_client_by_deviceId(
                config.Config().get('Device_ID'),
                f"Status: {light_name} || Colour: Green || Time: {remaining}"
            )
            if self.interrupt_flag.is_set():
                print(f"Interrupt detected during {light_name} green phase.")
                break  # Interrupt green phase, but go to yellow

        # Example: play a short tone on buzzers at the end of green phase
        self.play_buzzer_tone(frequency=440, duration=0.5)

        # If pedestrian button is pressed, extend green time
        if self.pedestrian_buttons[light_name].is_pressed:
            remaining_time = max(green_time - (time.time() - start_time), pedestrian_time)
            print(f"Pedestrian crossing activated for {light_name}! Decreasing time to {remaining_time} seconds.")
            time.sleep(remaining_time)

        # Yellow light phase
        self.switch_light(light_name, "yellow")
        start_time = time.time()
        while time.time() - start_time <= 4:
            time.sleep(0.5)
            self.signalR_client_callback.send_message_to_client_by_deviceId(
                config.Config().get('Device_ID'),
                f"Status: {light_name} || Colour: Yellow || Time: {4 - (time.time() - start_time)}"
            )

        # Red light phase
        self.switch_light(light_name, "red")
        start_time = time.time()
        while time.time() - start_time <= 2:
            time.sleep(0.5)
            self.signalR_client_callback.send_message_to_client_by_deviceId(
                config.Config().get('Device_ID'),
                f"Status: {light_name} || Colour: Red || Time: {2 - (time.time() - start_time)}"
            )
        # Optionally, you can play a tone on buzzers during red phase too
        self.play_buzzer_tone(frequency=330, duration=0.5)

    def start(self):
        """Starts the traffic light system in a separate thread."""
        print("Starting traffic light control system...")
        traffic_thread = threading.Thread(target=self.traffic_cycle, daemon=True)
        traffic_thread.start()
    
    def stop(self):
        """Stops the traffic light system."""
        for light in self.traffic_lights.values():
            light["red"].off()
            light["yellow"].off()
            light["green"].off()
        for buzzer in self.buzzers.values():
            buzzer.off()


# Example usage:
if __name__ == "__main__":
    # Replace with your actual SignalR client callback object that has send_message_to_client_by_deviceId() method.
    class DummyCallback:
        def send_message_to_client_by_deviceId(self, device_id, message):
            print(f"[Device {device_id}]: {message}")

    dummy_callback = DummyCallback()
    traffic_controller = TrafficLightController(dummy_callback)
    traffic_controller.start()

    try:
        while True:
            time.sleep(1)  # Keep the main thread alive
    except KeyboardInterrupt:
        print("Traffic Light System Shutting Down...")
        traffic_controller.stop()
