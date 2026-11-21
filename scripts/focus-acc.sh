#!/bin/bash
ACC="${1:-acc1}"
PROFILE_DIR="/opt/jar-manager/profiles/$ACC"
PID_FILE="$PROFILE_DIR/$ACC.pid"

if [ -f "$PID_FILE" ]; then
    PID=$(cat "$PID_FILE")
    export DISPLAY=:1
    WIDS=$(xdotool search --pid "$PID" 2>/dev/null)
    for W in $WIDS; do
        xdotool windowactivate "$W" 2>/dev/null
        wmctrl -ir "$W" -b add,maximized_vert,maximized_horz 2>/dev/null
    done
    echo "FOCUSED:$ACC"
else
    echo "NO_PID_FOUND"
fi
