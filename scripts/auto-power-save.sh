#!/bin/bash
# Tự động điều tiết tài nguyên: Nếu có người kết nối VNC -> Active Mode; Không có ai -> Idle Mode
if ss -H -t -n sport = :5901 2>/dev/null | grep -q 'ESTAB'; then
    /opt/jar-manager/scripts/mode-active.sh acc1
else
    /opt/jar-manager/scripts/mode-idle.sh acc1
fi
