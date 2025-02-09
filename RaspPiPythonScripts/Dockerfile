FROM debian:bookworm

# Install system python3.11 and tools
RUN apt-get update && apt-get install -y --no-install-recommends \
    python3.11 python3.11-dev python3-pip python3-distutils \
    wget gnupg ca-certificates \
    && rm -rf /var/lib/apt/lists/*

RUN wget -O /usr/share/keyrings/raspi.gpg.key \
    https://archive.raspberrypi.org/debian/raspberrypi.gpg.key && \
    echo "deb [signed-by=/usr/share/keyrings/raspi.gpg.key] \
    http://archive.raspberrypi.org/debian bookworm main" \
    > /etc/apt/sources.list.d/raspi.list

RUN apt update && apt -y upgrade

RUN apt-get update && apt-get install -y --no-install-recommends \
  python3-picamera2 \
  ffmpeg \
  && apt-get clean \
  && apt-get autoremove \
  && rm -rf /var/cache/apt/archives/* \
  && rm -rf /var/lib/apt/lists/*

ENV PIP_BREAK_SYSTEM_PACKAGES=1

RUN pip install --upgrade pip setuptools

RUN pip install gpiozero rpi-lgpio

WORKDIR /app
COPY . /app
RUN pip install -r requirements.txt

CMD ["python3", "SmartTrafficControlSystemClient/main.py"]
