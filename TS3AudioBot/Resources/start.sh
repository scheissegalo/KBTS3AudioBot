#!/bin/bash

# Detect system architecture (64-bit or 32-bit)
if [ "$(uname -m)" == "x86_64" ]; then
    LIB_PATH="lib/x64"
else
    LIB_PATH="lib/x86"
fi

# Set the library path
export LD_LIBRARY_PATH=$(dirname "$0")/$LIB_PATH:$LD_LIBRARY_PATH

# Start the application
./TS3AudioBot
