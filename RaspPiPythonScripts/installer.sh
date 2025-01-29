#!/bin/bash

# Update the package list
sudo apt-get update

# Install prerequisite packages
sudo apt-get install -y apt-transport-https ca-certificates curl software-properties-common

# Add Docker's official GPG key
curl -fsSL https://download.docker.com/linux/debian/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

# Set up the Docker stable repository for ARM architecture
echo "deb [arch=armhf signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Update the package list again
sudo apt-get update

# Install Docker packages
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Install Docker Compose (ARM-compatible version)
DOCKER_COMPOSE_VERSION="1.29.2"
sudo curl -L "https://github.com/docker/compose/releases/download/${DOCKER_COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose

# Apply executable permissions to the Docker Compose binary
sudo chmod +x /usr/local/bin/docker-compose

# Enable and start Docker service
sudo systemctl enable docker
sudo systemctl start docker

# Verify installation
sudo docker --version
sudo docker-compose --version

echo "Docker and Docker Compose have been successfully installed on your Raspberry Pi."

# Pull the docker image
#docker pull username/repo:tag

# Download the docker-compose.yml file
#sudo curl -LJO https://raw.githubusercontent.com/username/repo/main/docker-compose.yml

# Output a message for users to update the configuration file

# Output a message for users to start the container using docker-compose up -d

