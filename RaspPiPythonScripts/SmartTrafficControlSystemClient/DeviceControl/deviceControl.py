import os
import platform

# Only set the mock pin factory if the OS is not Linux
if platform.system() != "Linux":
    os.environ["GPIOZERO_PIN_FACTORY"] = "mock"
    print("DEV DETECTED: Using mock pin factory")
else:
    print("PROD: Using RPi pin factory")

from gpiozero import LED
import time

# Define the GPIO pins for each traffic light (Red, Yellow, Green)
traffic_lights = {
    "light1": {"red": LED(17), "yellow": LED(27), "green": LED(22)},
    "light2": {"red": LED(23), "yellow": LED(24), "green": LED(25)}
}

# Function to switch traffic light to green
def green_light(light):
    light["red"].off()
    light["yellow"].off()
    light["green"].on()

# Function to switch traffic light to yellow
def yellow_light(light):
    light["green"].off()
    light["yellow"].on()
    light["red"].off()

# Function to switch traffic light to red
def red_light(light):
    light["yellow"].off()
    light["red"].on()
    light["green"].off()

# Function to handle one traffic light cycle
def traffic_light_cycle(current_light_name):
    current_light = traffic_lights[current_light_name]
    other_light_name = find_other_light(current_light_name)
    other_light = traffic_lights[other_light_name]

    # Red light for the other traffic light
    red_light(other_light)

    # Green light for 30 seconds
    green_light(current_light)
    time.sleep(30)

    # Yellow light for 4 seconds
    yellow_light(current_light)
    time.sleep(4)

    # Red light until next cycle
    red_light(current_light)
    time.sleep(4)

# Function to find the other light
def find_other_light(given_light):
    return "light2" if given_light == "light1" else "light1"

if __name__ == "__main__":
    # Main loop
    try:
        while True:
            traffic_light_cycle("light1")
            traffic_light_cycle("light2")

    except KeyboardInterrupt:
        # Turn off all LEDs on exit
        for light in traffic_lights.values():
            light["red"].off()
            light["yellow"].off()
            light["green"].off()

def start_traffic_light_control():
    while True:
        traffic_light_cycle("light1")
        traffic_light_cycle("light2")