#!/bin/bash
# Set the library path
export LD_LIBRARY_PATH=$(dirname "$0")/lib/x64:$LD_LIBRARY_PATH

# Start the application
./TS3AudioBot
