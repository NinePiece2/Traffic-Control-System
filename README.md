# **Traffic Control System** 🚦  

<div align="left">
  <img src="https://github.com/NinePiece2/Traffic-Control-System/actions/workflows/docker.yml/badge.svg" alt="CI/CD Pipeline" />
  <img src="https://github.com/NinePiece2/Traffic-Control-System/actions/workflows/python-docker.yml/badge.svg" alt="Client CI Pipeline" />
</div>

## Table of Contents
- [**Traffic Control System** 🚦](#traffic-control-system-)
  - [Table of Contents](#table-of-contents)
  - [Introduction](#introduction)
  - [Client Install Instructions:](#client-install-instructions)
    - [Client Debugging](#client-debugging)

## Introduction

A **smart traffic control system** designed to efficiently manage and optimize traffic flow.

---

## Client Install Instructions:

```sh
curl -O https://raw.githubusercontent.com/NinePiece2/Traffic-Control-System/refs/heads/master/RaspPiPythonScripts/installer.sh && sudo bash installer.sh
```

### Client Debugging

If you enconuter:

```sh
OSError: [Errno 12] Cannot allocate memory
```

Go to the file `sudo nano /boot/firmware/config.txt`

Locate the line `dtoverlay=vc4-fkms-v3d` and change it to `dtoverlay=vc4-fkms-v3d,cma-256`