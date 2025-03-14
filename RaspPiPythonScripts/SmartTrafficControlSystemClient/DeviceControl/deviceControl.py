import os
import platform
import time
import threading
from gpiozero import LED, Button, PWMOutputDevice
import Config.config as config
import threading

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

        self.pedestrian_button_pressed = {
            "light_dir_1": False,
            "light_dir_2": False
        }

        # Define passive buzzers on pins 12, 13, 14, and 15 using PWMOutputDevice (Buzzers need to be attached to 3.3v)
        self.buzzers = {
            "buzzer1": PWMOutputDevice(12, active_high=False),
            # "buzzer2": PWMOutputDevice(13, active_high=False),
            # "buzzer3": PWMOutputDevice(14, active_high=False),
            # "buzzer4": PWMOutputDevice(15, active_high=False)
        }

        self.pressure_sensors = {
            "light_dir_1_sensor1": Button(5),
            "light_dir_1_sensor2": Button(6),
            "light_dir_2_sensor1": Button(13),
            "light_dir_2_sensor2": Button(19)
        }

        self.last_pressure_detected = {
            "light_dir_1_sensor1": time.mktime((1970, 1, 1, 0, 0, 0, 0, 1, -1)),
            "light_dir_1_sensor2": time.mktime((1970, 1, 1, 0, 0, 0, 0, 1, -1)),
            "light_dir_2_sensor1": time.mktime((1970, 1, 1, 0, 0, 0, 0, 1, -1)),
            "light_dir_2_sensor2": time.mktime((1970, 1, 1, 0, 0, 0, 0, 1, -1))
        }

        self.last_pressure_release_detected = {
            "light_dir_1_sensor1": time.mktime((1970, 1, 1, 0, 0, 0, 0, 1, -1)),
            "light_dir_1_sensor2": time.mktime((1970, 1, 1, 0, 0, 0, 0, 1, -1)),
            "light_dir_2_sensor1": time.mktime((1970, 1, 1, 0, 0, 0, 0, 1, -1)),
            "light_dir_2_sensor2": time.mktime((1970, 1, 1, 0, 0, 0, 0, 1, -1))
        }

        self.pressure_sensors["light_dir_1_sensor1"].when_pressed = lambda: self.pressure_sensor_pressed("light_dir_1_sensor1")
        self.pressure_sensors["light_dir_1_sensor2"].when_pressed = lambda: self.pressure_sensor_pressed("light_dir_1_sensor2")
        self.pressure_sensors["light_dir_2_sensor1"].when_pressed = lambda: self.pressure_sensor_pressed("light_dir_2_sensor1")
        self.pressure_sensors["light_dir_2_sensor2"].when_pressed = lambda: self.pressure_sensor_pressed("light_dir_2_sensor2")

        self.pressure_sensors["light_dir_1_sensor1"].when_released = lambda: self.pressure_sensor_released("light_dir_1_sensor1")
        self.pressure_sensors["light_dir_1_sensor2"].when_released = lambda: self.pressure_sensor_released("light_dir_1_sensor2")
        self.pressure_sensors["light_dir_2_sensor1"].when_released = lambda: self.pressure_sensor_released("light_dir_2_sensor1")
        self.pressure_sensors["light_dir_2_sensor2"].when_released = lambda: self.pressure_sensor_released("light_dir_2_sensor2")

        # Track active light (default direction)
        self.active_light = "light_dir_1"

    def pedestrian_pressed(self, light_name):
        """Handles pedestrian button press event for the given direction."""
        self.pedestrian_button_pressed[light_name] = True
    
    def pressure_sensor_pressed(self, sensor_name):
        """Handles pressure sensor press event for the given sensor."""
        self.last_pressure_detected[sensor_name] = time.time()
        self.last_pressure_release_detected[sensor_name] = time.mktime((1970, 1, 1, 0, 0, 0, 0, 1, -1))
        print(f"Pressure sensor {sensor_name} pressed.")

    def pressure_sensor_released(self, sensor_name):
        """Handles pressure sensor release event for the given sensor."""
        self.last_pressure_release_detected[sensor_name] = time.time()
        self.last_pressure_detected[sensor_name] = time.mktime((1970, 1, 1, 0, 0, 0, 0, 1, -1))
        print(f"Pressure sensor {sensor_name} released.")

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
            buzzer.value = 0.2  # 20% duty cycle (adjust for loudness)
        time.sleep(duration)
        for buzzer in self.buzzers.values():
            buzzer.off()
    
    def play_jingle(self, tones, durations):
        """
        Plays a sequence of tones as a jingle.
        tones: List of frequencies in Hz.
        durations: List of durations for each tone (seconds).
        """
        for tone, duration in zip(tones, durations):
            for buzzer in self.buzzers.values():
                buzzer.frequency = tone
                buzzer.value = 0.2
            time.sleep(duration)
            for buzzer in self.buzzers.values():
                buzzer.off()
            time.sleep(0.1)  # Short pause between tones

    def play_buzzer_tone_in_thread(self, frequency, duration):
        """
        Runs the play_buzzer_tone method in a separate thread.
        """
        # Create and start a new thread to run the play_buzzer_tone method
        tone_thread = threading.Thread(target=self.play_buzzer_tone, args=(frequency, duration))
        tone_thread.start()
    
    def incident_detected(self):
        for buzzer in self.buzzers.values():
            buzzer.off()
        
        self.play_jingle_in_thread(
            [1000, 700, 1000, 700, 1000, 700],  # Repeated 3 times
            [0.3, 0.3, 0.3, 0.3, 0.3, 0.3]  # Even timing
        )

    def play_jingle_in_thread(self, tones, durations):
        """
        Runs the play_jingle method in a separate thread.
        """
        jingle_thread = threading.Thread(target=self.play_jingle, args=(tones, durations))
        jingle_thread.start()

    def start_walking_jingle(self):
        """ Jingle for when it's safe to walk """
        self.play_jingle_in_thread([600, 900, 1200], [0.2, 0.2, 0.2])

    def prepare_to_stop_jingle(self):
        """ Jingle for when the light is about to change """
        self.play_jingle_in_thread([1200, 900, 1200], [0.3, 0.3, 0.3])

    def do_not_walk_jingle(self):
        """ Jingle for when pedestrians should stop immediately """
        self.play_jingle_in_thread([1200, 800, 400, 1200, 800, 400], [0.2, 0.2, 0.2, 0.2, 0.2, 0.2])

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
        endFlag = False
        self.start_walking_jingle()
        while time.time() - start_time < green_time:
            time.sleep(0.5)
            remaining = green_time - (time.time() - start_time)
            self.signalR_client_callback.send_message_to_client_by_deviceId(
                config.Config().get('Device_ID'),
                f"Status: {light_name} || Colour: Green || Time: {remaining}"
            )
            if self.pedestrian_button_pressed[light_name]:
                if (remaining > pedestrian_time):
                    green_time = pedestrian_time
                    start_time = time.time()
            self.pedestrian_button_pressed[other_light_name] = False
            self.pedestrian_button_pressed[light_name] = False
            if self.interrupt_flag.is_set():
                print(f"Interrupt detected during {light_name} green phase.")
                break  # Interrupt green phase, but go to yellow
            if (remaining <= 10 and endFlag == False):
                self.prepare_to_stop_jingle()
                endFlag = True

        self.do_not_walk_jingle()

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