import RPi.GPIO as GPIO
import time

# Set GPIO mode
GPIO.setmode(GPIO.BCM)

# Define the GPIO pins for each traffic light (Red, Yellow, Green)
# Example: Traffic light 1 is connected to pins 17, 27, and 22
traffic_lights = {
    "light1": {"red": 17, "yellow": 27, "green": 22},
    "light2": {"red": 23, "yellow": 24, "green": 25},
    "light3": {"red": 5, "yellow": 6, "green": 13},
    "light4": {"red": 19, "yellow": 26, "green": 16}
}

# Set up the GPIO pins as output
for light, pins in traffic_lights.items():
    GPIO.setup(pins["red"], GPIO.OUT)
    GPIO.setup(pins["yellow"], GPIO.OUT)
    GPIO.setup(pins["green"], GPIO.OUT)

# Function to switch traffic light to green
def green_light(light):
    GPIO.output(light["red"], GPIO.LOW)
    GPIO.output(light["yellow"], GPIO.LOW)
    GPIO.output(light["green"], GPIO.HIGH)

# Function to switch traffic light to yellow
def yellow_light(light):
    GPIO.output(light["green"], GPIO.LOW)
    GPIO.output(light["yellow"], GPIO.HIGH)

# Function to switch traffic light to red
def red_light(light):
    GPIO.output(light["yellow"], GPIO.LOW)
    GPIO.output(light["red"], GPIO.HIGH)

# Function to handle one traffic light cycle
def traffic_light_cycle(current_light):
    # Green light for 30 seconds
    green_light(current_light)
    time.sleep(30)

    # Yellow light for 4 seconds
    yellow_light(current_light)
    time.sleep(4)

    # Red light until next cycle
    red_light(current_light)

# Main loop
try:
    while True:
        # Iterate through each traffic light
        for light in traffic_lights:
            current_light = traffic_lights[light]

            # Turn the current light green, others red
            for l in traffic_lights:
                if l != light:
                    red_light(traffic_lights[l])

            traffic_light_cycle(current_light)

except KeyboardInterrupt:
    # Clean up the GPIO pins on exit
    GPIO.cleanup()
