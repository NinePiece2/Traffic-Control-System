FROM python:3.11

RUN apt-get update && apt-get install -y --no-install-recommends \
    wget \
    gnupg \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

RUN wget -O /usr/share/keyrings/raspi.gpg.key \
      https://archive.raspberrypi.org/debian/raspberrypi.gpg.key

RUN echo "deb [signed-by=/usr/share/keyrings/raspi.gpg.key] http://archive.raspberrypi.org/debian bookworm main" \
    > /etc/apt/sources.list.d/raspi.list

RUN apt-get update && apt-get install -y --no-install-recommends \
    python3-picamera2 \
    python3-libcamera \
    python3-kms++ \
    libcamera-apps \
    libatlas-base-dev \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

RUN pip install --no-cache-dir gpiozero rpi-lgpio

RUN wget https://github.com/Gadgetoid/PY_LGPIO/releases/download/0.2.2.0/lgpio-0.2.2.0.tar.gz \
    && pip install lgpio-0.2.2.0.tar.gz \
    && rm lgpio-0.2.2.0.tar.gz

WORKDIR /app

COPY . /app

RUN pip install --no-cache-dir --upgrade pip setuptools \
    && pip install --no-cache-dir -r requirements.txt

CMD ["python", "SmartTrafficControlSystemClient/main.py"]