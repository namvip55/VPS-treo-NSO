#!/bin/bash
ACC="${1:-acc1}"
PID_FILE="/opt/jar-manager/profiles/$ACC/$ACC.pid"

if [ -f "$PID_FILE" ]; then
    PID=$(cat "$PID_FILE")
    if ps -p "$PID" > /dev/null 2>&1; then
        # Nâng độ ưu tiên CPU lên bình thường khi khách thao tác (0 lag)
        renice -n 0 -p "$PID" > /dev/null 2>&1
        export DISPLAY=:1
        /opt/jar-manager/scripts/focus-acc.sh "$ACC" > /dev/null 2>&1
        echo "MODE:ACTIVE|$ACC|$PID"
        exit 0
    fi
fi
echo "MODE:ACTIVE_NO_PID"
