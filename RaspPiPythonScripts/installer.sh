#!/bin/bash

# Check for -y flag (bypass confirmation)
AUTO_CONFIRM=false
while getopts "y" opt; do
    case $opt in
        y) AUTO_CONFIRM=true ;;
        *) echo "Usage: $0 [-y]"; exit 1 ;;
    esac
done

# Confirmation prompt (bypassable with -y)
if ! $AUTO_CONFIRM; then
    echo "This script will install Docker and pull the required container for the Traffic Control System."
    read -p "Do you want to continue? (y/N): " confirm
    if [[ "$confirm" != "y" && "$confirm" != "Y" ]]; then
        rm -- "$0"
        echo "Installation aborted."
        exit 0
    fi
fi

echo "Updating package list..."
sudo apt-get update

echo "Installing prerequisite packages..."
sudo apt-get install -y apt-transport-https ca-certificates curl software-properties-common

echo "Adding Docker's official GPG key..."
curl -fsSL https://download.docker.com/linux/debian/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

echo "Setting up Docker repository..."
echo "deb [arch=arm64 signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

echo "Updating package list again..."
sudo apt-get update

echo "Installing Docker..."
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

echo "Installing Docker Compose..."
DOCKER_COMPOSE_VERSION="v2.32.4"
sudo curl -L "https://github.com/docker/compose/releases/download/${DOCKER_COMPOSE_VERSION}/docker-compose-linux-aarch64" -o /usr/local/bin/docker-compose

echo "Applying executable permissions to Docker Compose..."
sudo chmod +x /usr/local/bin/docker-compose

echo "Enabling and starting Docker service..."
sudo systemctl enable docker
sudo systemctl start docker

echo "Verifying Docker installation..."
sudo docker --version
sudo docker-compose --version

echo "Pulling the required Docker image..."
docker pull ninepiece2/traffic-control-system:client

echo "Setting up the Traffic Control System directory..."
mkdir -p TrafficControlSystemClient/config
cd TrafficControlSystemClient

echo "Downloading docker-compose.yml..."
sudo curl -LJO https://raw.githubusercontent.com/NinePiece2/Traffic-Control-System/refs/heads/master/RaspPiPythonScripts/docker-compose.yml

echo "Downloading configuration file..."
sudo curl -LJ https://raw.githubusercontent.com/NinePiece2/Traffic-Control-System/refs/heads/master/RaspPiPythonScripts/DefaultConfig/config.json -o config/config.json

echo "Setting folder permissions..."
sudo chmod -R 777 .

echo "Cleaning up installer script..."
cd ..
rm -- "$0"

YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Installation complete!${NC}"
echo -e "${YELLOW}Docker and Docker Compose have been successfully installed on your Raspberry Pi.${NC}"
echo -e "${YELLOW}Please update the configuration file (config.json) with your own values.${NC}"
echo -e "${YELLOW}To start the container, run: docker-compose up -d${NC}"

