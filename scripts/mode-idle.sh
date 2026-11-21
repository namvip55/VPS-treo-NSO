#!/bin/bash
ACC="${1:-acc1}"
PID_FILE="/opt/jar-manager/profiles/$ACC/$ACC.pid"

if [ -f "$PID_FILE" ]; then
    PID=$(cat "$PID_FILE")
    if ps -p "$PID" > /dev/null 2>&1; then
        # Hạ độ ưu tiên CPU xuống chế độ Treo Tiết Kiệm khi không có ai xem
        renice -n 15 -p "$PID" > /dev/null 2>&1
        echo "MODE:IDLE|$ACC|$PID"
        exit 0
    fi
fi
echo "MODE:IDLE_NO_PID"
