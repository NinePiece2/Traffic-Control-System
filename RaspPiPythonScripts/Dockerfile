FROM python:3.11

RUN pip install gpiozero rpi-lgpio

RUN wget https://github.com/Gadgetoid/PY_LGPIO/releases/download/0.2.2.0/lgpio-0.2.2.0.tar.gz
RUN pip install lgpio-0.2.2.0.tar.gz

# Install FFMPEG
RUN apt-get update && apt-get install -y ffmpeg v4l-utils libv4l-dev && apt-get clean

# Set working directory
WORKDIR /app

# Copy application files
COPY . /app

# Install dependencies
RUN pip install -r requirements.txt

# Run the application
CMD ["python", "SmartTrafficControlSystemClient/main.py"]