#!/bin/bash
for p in /opt/jar-manager/profiles/*; do
    if [ -d "$p" ]; then
        ACC=$(basename "$p")
        PID_FILE="$p/$ACC.pid"
        if [ -f "$PID_FILE" ]; then
            PID=$(cat "$PID_FILE")
            if ps -p "$PID" > /dev/null 2>&1; then
                echo "ACC:$ACC|RUNNING|$PID"
                continue
            fi
        fi
        echo "ACC:$ACC|STOPPED|"
    fi
done
