import os
import platform
import time
import threading
from gpiozero import LED, Button
import Config.config as config

# Only set the mock pin factory if the OS is not Linux
if platform.system() != "Linux":
    os.environ["GPIOZERO_PIN_FACTORY"] = "mock"
    print("DEV DETECTED: Using mock pin factory")
else:
    print("PROD: Using RPi pin factory")


class TrafficLightController:
    def __init__(self):
        """
        Initializes the traffic light controller with configuration.
        """
        self.interrupt_flag = threading.Event()

        # Define traffic lights
        self.traffic_lights = {
            "light1": {"red": LED(17), "yellow": LED(27), "green": LED(22)},
            "light2": {"red": LED(23), "yellow": LED(24), "green": LED(25)}
        }

        # Define pedestrian buttons (One per direction)
        self.pedestrian_buttons = {
            "light1": Button(4),  # GPIO 4 for direction 1
            "light2": Button(5)   # GPIO 5 for direction 2
        }

        # Attach event listeners for pedestrian buttons
        self.pedestrian_buttons["light1"].when_pressed = lambda: self.pedestrian_pressed("light1")
        self.pedestrian_buttons["light2"].when_pressed = lambda: self.pedestrian_pressed("light2")

        # Track active light
        self.active_light = "light1"

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

    def traffic_cycle(self):
        """Main traffic light cycle that alternates between directions."""
        while True:
            for light_name in ["light1", "light2"]:
                self.active_light = light_name
                self.run_light_cycle(light_name)

                # Check if an interrupt occurred
                if self.interrupt_flag.is_set():
                    print("Cycle interrupted. Restarting with new settings...")
                    self.interrupt_flag.clear()

    def run_light_cycle(self, light_name):
        """Runs a single cycle for the given traffic light."""
        other_light_name = "light2" if light_name == "light1" else "light1"
        
        # Get config values
        green_time = config.Config().get('Direction_1_Green_Time') if light_name == "light1" else config.Config().get('Direction_2_Green_Time')
        pedestrian_time = config.Config().get('Pedestrian_Walk_Time')

        # Set red for the other light
        self.switch_light(other_light_name, "red")

        # Green light phase
        print(f"{light_name} is GREEN for {green_time} seconds.")
        start_time = time.time()
        while time.time() - start_time < green_time:
            if self.interrupt_flag.is_set():
                return

        # If pedestrian button is pressed, extend green time
        if self.pedestrian_buttons[light_name].is_pressed:
            remaining_time = max(green_time - (time.time() - start_time), pedestrian_time)
            print(f"Pedestrian crossing activated for {light_name}! Extending time to {remaining_time} seconds.")
            time.sleep(remaining_time)

        # Yellow light phase
        self.switch_light(light_name, "yellow")
        print(f"{light_name} is YELLOW for 4 seconds.")
        time.sleep(4)
        if self.interrupt_flag.is_set():
            return

        # Red light phase
        self.switch_light(light_name, "red")
        print(f"{light_name} is RED.")

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

# Initialize and Start the Traffic Light System
if __name__ == "__main__":
    traffic_controller = TrafficLightController()
    traffic_controller.start()

    try:
        while True:
            time.sleep(1)  # Keep the main thread alive
    except KeyboardInterrupt:
        print("Traffic Light System Shutting Down...")
        for light in traffic_controller.traffic_lights.values():
            light["red"].off()
            light["yellow"].off()
            light["green"].off()
