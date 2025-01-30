FROM python:3.11

RUN pip install gpiozero rpi-lgpio

RUN wget https://github.com/Gadgetoid/PY_LGPIO/releases/download/0.2.2.0/lgpio-0.2.2.0.tar.gz
RUN pip install lgpio-0.2.2.0.tar.gz

# Set working directory
WORKDIR /app

# Copy application files
COPY . /app

# Install dependencies
RUN pip install -r requirements.txt

# Run the application
#CMD ["python", "SmartTrafficControlSystemClient/core.py"]
CMD ["python", "SmartTrafficControlSystemClient/main.py"]