FROM python:3.9.13-bullseye

# Update and install required packages
RUN apt-get update && apt-get install -y \
    lgpio \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy application files
COPY . /app

# Install dependencies
RUN pip install -r requirements.txt

# Run the application
#CMD ["python", "SmartTrafficControlSystemClient/core.py"]
CMD ["python", "SmartTrafficControlSystemClient/main.py"]