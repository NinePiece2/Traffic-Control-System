FROM python:3.9.13-bullseye

# Set working directory
WORKDIR /app

# Copy application files
COPY . /app

# Install dependencies
RUN pip install -r requirements.txt

# Run the application
CMD ["python", "SmartTrafficControlSystemClient/core.py"]
