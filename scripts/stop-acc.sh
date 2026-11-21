#!/bin/bash
ACC="${1:-acc1}"
PROFILE_DIR="/opt/jar-manager/profiles/$ACC"
PID_FILE="$PROFILE_DIR/$ACC.pid"

if [ -f "$PID_FILE" ]; then
    PID=$(cat "$PID_FILE")
    if ps -p "$PID" > /dev/null 2>&1; then
        kill "$PID" 2>/dev/null
        sleep 1
        if ps -p "$PID" > /dev/null 2>&1; then
            kill -9 "$PID" 2>/dev/null
        fi
        rm -f "$PID_FILE"
        echo "STOPPED:$PID"
        exit 0
    fi
    rm -f "$PID_FILE"
fi
echo "NOT_RUNNING"
