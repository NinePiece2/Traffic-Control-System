FROM python:3.11

# 1) Add the Raspberry Pi OS repository so that we can install python3-picamera2
RUN echo "deb http://archive.raspberrypi.org/debian bullseye main" >> /etc/apt/sources.list.d/rpi.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends \
       # 2) Install the packages required by picamera2
       python3-picamera2 \
       python3-libcamera \
       python3-kms++ \
       libcamera-apps \
       libatlas-base-dev \
       ffmpeg \
    && rm -rf /var/lib/apt/lists/*

RUN pip install gpiozero rpi-lgpio

RUN wget https://github.com/Gadgetoid/PY_LGPIO/releases/download/0.2.2.0/lgpio-0.2.2.0.tar.gz \
    && pip install lgpio-0.2.2.0.tar.gz \
    && rm lgpio-0.2.2.0.tar.gz

WORKDIR /app

COPY . /app

RUN pip install --upgrade pip setuptools
RUN pip install -r requirements.txt

CMD ["python", "SmartTrafficControlSystemClient/main.py"]
