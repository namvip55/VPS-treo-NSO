#!/bin/bash
# ==============================================================================
# Tự động điều tiết tài nguyên cho toàn bộ tài khoản:
# Có người kết nối VNC -> Chế độ Active (renice 0, mượt mà);
# Không có ai kết nối VNC -> Chế độ Idle (renice 15, tiết kiệm CPU tối đa).
# ==============================================================================

IS_ACTIVE=0
if ss -H -t -n sport = :5901 2>/dev/null | grep -q 'ESTAB'; then
    IS_ACTIVE=1
fi

TARGET_ACCS="$@"
if [ -z "$TARGET_ACCS" ]; then
    if [ -d "/opt/jar-manager/profiles" ]; then
        TARGET_ACCS=$(find /opt/jar-manager/profiles -mindepth 1 -maxdepth 1 -type d -exec basename {} \;)
    fi
    if [ -z "$TARGET_ACCS" ]; then
        TARGET_ACCS="acc1"
    fi
fi

for ACC in $TARGET_ACCS; do
    if [ "$IS_ACTIVE" -eq 1 ]; then
        /opt/jar-manager/scripts/mode-active.sh "$ACC"
    else
        /opt/jar-manager/scripts/mode-idle.sh "$ACC"
    fi
done
