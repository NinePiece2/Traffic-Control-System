#!/bin/bash

# Update the package list
sudo apt-get update

# Install prerequisite packages
sudo apt-get install -y apt-transport-https ca-certificates curl software-properties-common

# Add Docker's official GPG key
curl -fsSL https://download.docker.com/linux/debian/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

# Set up the Docker stable repository for ARM64 architecture
echo "deb [arch=arm64 signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Update the package list again
sudo apt-get update

# Install Docker packages
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Install Docker Compose (ARM64-compatible version)
DOCKER_COMPOSE_VERSION="v2.32.4"
sudo curl -L "https://github.com/docker/compose/releases/download/${DOCKER_COMPOSE_VERSION}/docker-compose-linux-aarch64" -o /usr/local/bin/docker-compose

# Apply executable permissions to the Docker Compose binary
sudo chmod +x /usr/local/bin/docker-compose

# Enable and start Docker service
sudo systemctl enable docker
sudo systemctl start docker

# Verify installation
sudo docker --version
sudo docker-compose --version

# Pull the docker image
docker pull ninepiece2/traffic-control-system:client

mkdir TrafficControlSystemClient && cd TrafficControlSystemClient

# Download the docker-compose.yml file
sudo curl -LJO https://raw.githubusercontent.com/NinePiece2/Traffic-Control-System/refs/heads/master/RaspPiPythonScripts/docker-compose.yml

# Curl the configuration file
sudo curl -LJO https://raw.githubusercontent.com/NinePiece2/Traffic-Control-System/refs/heads/master/RaspPiPythonScripts/config.json

echo "Docker and Docker Compose have been successfully installed on your Raspberry Pi (ARM64)."
echo "Please update the configuration file (config.json) with your own values."
echo "To start the container, run the following command:"
echo "docker-compose up -d"