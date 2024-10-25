import lgpio
import time

# Define the GPIO pins for each traffic light (Red, Yellow, Green)
traffic_lights = {
    "light1": {"red": 17, "yellow": 27, "green": 22},
    "light2": {"red": 23, "yellow": 24, "green": 25}
}

# Initialize the GPIO chip and set pins as outputs
chip = lgpio.gpiochip_open(0)

# Set each light's pins as output with an initial low state (off)
for light, pins in traffic_lights.items():
    lgpio.gpio_claim_output(chip, pins["red"], 0)
    lgpio.gpio_claim_output(chip, pins["yellow"], 0)
    lgpio.gpio_claim_output(chip, pins["green"], 0)

# Function to switch traffic light to green
def green_light(light):
    lgpio.gpio_write(chip, light["red"], 0)
    lgpio.gpio_write(chip, light["yellow"], 0)
    lgpio.gpio_write(chip, light["green"], 1)

# Function to switch traffic light to yellow
def yellow_light(light):
    lgpio.gpio_write(chip, light["green"], 0)
    lgpio.gpio_write(chip, light["yellow"], 1)
    lgpio.gpio_write(chip, light["red"], 0)

# Function to switch traffic light to red
def red_light(light):
    lgpio.gpio_write(chip, light["yellow"], 0)
    lgpio.gpio_write(chip, light["red"], 1)
    lgpio.gpio_write(chip, light["green"], 0)

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
    all_lights = set(traffic_lights.keys())
    all_lights.remove(given_light)
    return all_lights.pop()  # return the other light

# Main loop
try:
    while True:
        traffic_light_cycle("light1")
        traffic_light_cycle("light2")

except KeyboardInterrupt:
    # Clean up the GPIO pins on exit
    for light, pins in traffic_lights.items():
        lgpio.gpio_write(chip, pins["red"], 0)
        lgpio.gpio_write(chip, pins["yellow"], 0)
        lgpio.gpio_write(chip, pins["green"], 0)
    lgpio.gpiochip_close(chip)
